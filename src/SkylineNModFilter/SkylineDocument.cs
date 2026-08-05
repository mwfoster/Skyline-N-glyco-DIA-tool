using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace SkylineNModFilter
{
    internal sealed class CommandResult
    {
        public CommandResult(int exitCode, string output, string error) { ExitCode = exitCode; Output = output; Error = error; }
        public int ExitCode { get; private set; }
        public string Output { get; private set; }
        public string Error { get; private set; }
    }

    internal delegate CommandResult CommandRunner(string executable, string arguments);

    internal sealed class SkylineDocument : ISkylineDocument
    {
        private readonly string _skylineCommand;
        private readonly CommandRunner _runner;
        private readonly IBackgroundProteomeFastaExporter _fastaExporter;
        private string _workingPath;
        private string _normalizedPath;
        private XDocument _xml;

        public SkylineDocument(string skylineCommand, CommandRunner runner) : this(skylineCommand, runner, new BackgroundProteomeFastaExporter()) { }

        internal SkylineDocument(string skylineCommand, CommandRunner runner, IBackgroundProteomeFastaExporter fastaExporter)
        {
            _skylineCommand = skylineCommand;
            _runner = runner;
            _fastaExporter = fastaExporter;
        }

        public void CreateWorkingCopy(string sourcePath, string workingPath)
        {
            if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(workingPath), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The working path cannot be the source document.");
            var result = _runner(_skylineCommand, "--in=\"" + sourcePath + "\" --out=\"" + workingPath + "\"");
            if (result.ExitCode != 0) throw new InvalidOperationException("SkylineCmd could not create the working copy: " + result.Error + Environment.NewLine + result.Output);
            if (!File.Exists(workingPath)) throw new InvalidOperationException("SkylineCmd did not create the working copy.");
            _workingPath = workingPath;
            _xml = LoadSecure(workingPath);
        }

        public IList<PeptideRecord> ReadPeptides()
        {
            return _xml.Descendants().Where(e => e.Name.LocalName == "peptide").Select(PeptideRecord.FromElement).ToList();
        }

        public void DeletePeptides(IList<PeptideRecord> peptides) { foreach (var peptide in peptides) peptide.Element.Remove(); }

        public void RemoveEmptyContainers()
        {
            foreach (var peptide in _xml.Descendants().Where(e => e.Name.LocalName == "peptide" && !e.Elements().Any(c => c.Name.LocalName == "precursor")).ToList()) peptide.Remove();
            foreach (var container in _xml.Descendants().Where(e => (e.Name.LocalName == "peptide_list" || (e.Name.LocalName == "protein" && (e.Parent == null || e.Parent.Name.LocalName != "protein_group"))) && !e.Elements().Any(c => c.Name.LocalName == "peptide")).ToList()) container.Remove();
            foreach (var group in _xml.Descendants().Where(e => e.Name.LocalName == "protein_group" && !e.Elements().Any(c => c.Name.LocalName == "peptide")).ToList()) group.Remove();
        }

        internal void SaveXmlForTest() { _xml.Save(_workingPath); }

        public void NormalizeWithSkylineCmd(int maxVariableMods, ProteinAssociationOptions options)
        {
            _xml.Save(_workingPath);
            _normalizedPath = _workingPath + ".normalized.sky";
            var args = "--in=\"" + _workingPath + "\" --out=\"" + _normalizedPath + "\" --pep-max-variable-mods=" + maxVariableMods + " --refine-min-transitions=1 --refine-min-peptides=1";
            if (options != null && options.Enabled)
            {
                using (var fasta = _fastaExporter.Export(options.FilePath))
                {
                    args += " --background-proteome-file=\"" + options.FilePath + "\" --background-proteome-name=\"" + options.Name + "\" --associate-proteins-fasta=\"" + fasta.Path + "\" --associate-proteins-group-proteins --associate-proteins-shared-peptides=AssignedToBestProtein";
                    RunNormalization(args);
                }
            }
            else RunNormalization(args);
            _workingPath = _normalizedPath;
            _xml = LoadSecure(_workingPath);
        }

        private void RunNormalization(string arguments)
        {
            var result = _runner(_skylineCommand, arguments);
            if (result.ExitCode != 0) throw new InvalidOperationException("SkylineCmd failed: " + result.Error + Environment.NewLine + result.Output);
            if (!File.Exists(_normalizedPath)) throw new InvalidOperationException("SkylineCmd did not create the normalized document.");
        }

        public void Verify(string match, int maxVariableMods)
        {
            if (ReadPeptides().Any(p => p.ModifiedSequence.IndexOf(match, StringComparison.Ordinal) < 0)) throw new InvalidDataException("The normalized document contains a nonmatching peptide.");
            var setting = _xml.Descendants().Attributes("max_variable_mods").FirstOrDefault();
            if (setting == null || setting.Value != maxVariableMods.ToString()) throw new InvalidDataException("Maximum variable modifications was not applied.");
        }

        public void PublishWorkingCopy(string destinationPath)
        {
            var set = SkylineFileSet.Discover(_workingPath);
            var backups = new List<KeyValuePair<string, string>>();
            var published = new List<string>();
            var token = Guid.NewGuid().ToString("N");
            try
            {
                foreach (var file in set.FilesInPublicationOrder)
                {
                    var destination = file.DestinationFor(destinationPath);
                    if (File.Exists(destination))
                    {
                        var backup = destination + ".backup-" + token;
                        File.Move(destination, backup);
                        backups.Add(new KeyValuePair<string, string>(destination, backup));
                    }
                }
                foreach (var file in set.FilesInPublicationOrder)
                {
                    var destination = file.DestinationFor(destinationPath);
                    File.Move(file.Path, destination);
                    published.Add(destination);
                }
                foreach (var backup in backups) if (File.Exists(backup.Value)) File.Delete(backup.Value);
                _workingPath = null;
                _normalizedPath = null;
            }
            catch (Exception exception)
            {
                foreach (var path in published) if (File.Exists(path)) File.Delete(path);
                foreach (var backup in backups) if (File.Exists(backup.Value)) File.Move(backup.Value, backup.Key);
                throw new IOException("Failed to publish the Skyline document file set.", exception);
            }
        }

        public void DiscardWorkingCopy()
        {
            foreach (var skyPath in new[] { _workingPath, _normalizedPath }.Where(p => !string.IsNullOrEmpty(p)).Distinct(StringComparer.OrdinalIgnoreCase))
                foreach (var path in SkylineFileSet.ExistingRecognizedFiles(skyPath).ToList()) if (File.Exists(path)) File.Delete(path);
        }

        private static XDocument LoadSecure(string path)
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using (var reader = XmlReader.Create(path, settings)) return XDocument.Load(reader, LoadOptions.PreserveWhitespace);
        }

        public static CommandResult RunProcess(string executable, string arguments)
        {
            var start = new ProcessStartInfo(executable, arguments) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            using (var process = Process.Start(start)) { var output = process.StandardOutput.ReadToEnd(); var error = process.StandardError.ReadToEnd(); process.WaitForExit(); return new CommandResult(process.ExitCode, output, error); }
        }
    }
}
