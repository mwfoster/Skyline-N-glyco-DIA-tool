using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Text;

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

        public PrecursorMissingnessResult ApplyPrecursorMissingnessFilter(PrecursorMissingnessOptions options)
        {
            if (options == null || !options.Enabled) throw new ArgumentException("Enabled precursor missingness options are required.", "options");
            var measuredResults = _xml.Descendants().FirstOrDefault(e => e.Name.LocalName == "measured_results");
            var replicates = measuredResults == null ? new List<XElement>() : measuredResults.Elements().Where(e => e.Name.LocalName == "replicate").ToList();
            if (replicates.Count == 0) throw new InvalidDataException("The Skyline document has no results replicates for precursor missingness filtering.");
            var replicateNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var replicate in replicates)
            {
                var name = (string)replicate.Attribute("name");
                if (string.IsNullOrWhiteSpace(name) || !replicateNames.Add(name)) throw new InvalidDataException("Skyline replicate names must be nonblank and unique for precursor missingness filtering.");
            }

            var groups = new List<KeyValuePair<string, HashSet<string>>>();
            var annotated = 0; var unannotated = 0; var excluded = 0;
            if (options.Scope == PrecursorMissingnessScope.AllReplicates)
            {
                groups.Add(new KeyValuePair<string, HashSet<string>>("All replicates", replicateNames));
            }
            else
            {
                var map = PrecursorGroupMap.Load(options);
                var groupLookup = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                var groupNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var replicate in replicateNames)
                {
                    string group;
                    if (map.TryGetGroup(replicate, out group)) annotated++; else { group = "Unannotated"; unannotated++; }
                    if (options.ExcludeUnannotated && string.Equals(group, "Unannotated", StringComparison.OrdinalIgnoreCase)) { excluded++; continue; }
                    HashSet<string> members;
                    if (!groupLookup.TryGetValue(group, out members)) { members = new HashSet<string>(StringComparer.OrdinalIgnoreCase); groupLookup.Add(group, members); groupNames.Add(group, group); }
                    members.Add(replicate);
                }
                if (options.Scope == PrecursorMissingnessScope.SelectedGroup)
                {
                    HashSet<string> selected;
                    if (!groupLookup.TryGetValue(options.SelectedGroup, out selected) || selected.Count == 0) throw new InvalidDataException("The selected metadata group has no Skyline replicates: " + options.SelectedGroup);
                    groups.Add(new KeyValuePair<string, HashSet<string>>(groupNames[options.SelectedGroup], selected));
                }
                else foreach (var pair in groupLookup) if (pair.Value.Count > 0) groups.Add(new KeyValuePair<string, HashSet<string>>(groupNames[pair.Key], pair.Value));
                if (groups.Count == 0) throw new InvalidDataException("No metadata groups remain for precursor missingness filtering.");
            }

            var precursors = _xml.Descendants().Where(e => e.Name.LocalName == "precursor").ToList();
            var removed = 0;
            foreach (var precursor in precursors)
            {
                var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var peak in precursor.Descendants().Where(e => e.Name.LocalName == "precursor_peak"))
                {
                    var replicate = (string)peak.Attribute("replicate");
                    double area;
                    if (replicateNames.Contains(replicate ?? string.Empty) && double.TryParse((string)peak.Attribute("area"), NumberStyles.Float, CultureInfo.InvariantCulture, out area) && !double.IsNaN(area) && !double.IsInfinity(area) && area > 0) present.Add(replicate);
                }
                var passes = groups.Any(group => (group.Value.Count - group.Value.Count(present.Contains)) * 100 <= options.MaximumMissingPercent * group.Value.Count);
                if (!passes) { precursor.Remove(); removed++; }
            }
            return new PrecursorMissingnessResult { Evaluated = precursors.Count, Retained = precursors.Count - removed, Removed = removed, MaximumMissingPercent = options.MaximumMissingPercent,
                Scope = options.Scope == PrecursorMissingnessScope.AllReplicates ? "All replicates" : options.Scope == PrecursorMissingnessScope.SelectedGroup ? "Selected group" : "Any group",
                SelectedGroup = options.Scope == PrecursorMissingnessScope.SelectedGroup ? groups[0].Key : null, EvaluatedGroupCount = groups.Count,
                AnnotatedReplicates = annotated, UnannotatedReplicates = unannotated, ExcludedReplicates = excluded };
        }

        public void RemoveEmptyContainers()
        {
            foreach (var peptide in _xml.Descendants().Where(e => e.Name.LocalName == "peptide" && !e.Elements().Any(c => c.Name.LocalName == "precursor")).ToList()) peptide.Remove();
            foreach (var container in _xml.Descendants().Where(e => (e.Name.LocalName == "peptide_list" || (e.Name.LocalName == "protein" && (e.Parent == null || e.Parent.Name.LocalName != "protein_group"))) && !e.Elements().Any(c => c.Name.LocalName == "peptide")).ToList()) container.Remove();
            foreach (var group in _xml.Descendants().Where(e => e.Name.LocalName == "protein_group" && !e.Elements().Any(c => c.Name.LocalName == "peptide")).ToList()) group.Remove();
        }

        public ReplicateOrderResult ApplyReplicateOrdering(ReplicateManifest manifest)
        {
            if (manifest == null) throw new ArgumentNullException("manifest");
            var measuredResults = _xml.Descendants().FirstOrDefault(e => e.Name.LocalName == "measured_results");
            if (measuredResults == null) throw new InvalidDataException("The Skyline document has no measured results to reorder.");
            var replicates = measuredResults.Elements().Where(e => e.Name.LocalName == "replicate").ToList();
            if (replicates.Count == 0) throw new InvalidDataException("The Skyline document has no results replicates to reorder.");

            var byName = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var replicate in replicates)
            {
                var name = (string)replicate.Attribute("name");
                if (string.IsNullOrWhiteSpace(name) || byName.ContainsKey(name)) throw new InvalidDataException("Skyline replicate names must be nonblank and unique before ordering.");
                byName.Add(name, replicate);
            }

            var matchedElements = new List<XElement>();
            var entryByElement = new Dictionary<XElement, ReplicateManifestEntry>();
            var absentManifest = 0;
            foreach (var entry in manifest.Entries)
            {
                XElement replicate;
                if (!byName.TryGetValue(entry.Key, out replicate)) { absentManifest++; continue; }
                matchedElements.Add(replicate);
                entryByElement.Add(replicate, entry);
            }
            if (matchedElements.Count == 0) throw new InvalidDataException("No Skyline replicate names matched the selected metadata file.");

            var finalNames = new Dictionary<XElement, string>();
            var originalNames = replicates.ToDictionary(e => e, e => (string)e.Attribute("name"));
            var uniqueFinalNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var replicate in replicates)
            {
                var original = (string)replicate.Attribute("name");
                ReplicateManifestEntry entry;
                var finalName = entryByElement.TryGetValue(replicate, out entry) && !string.IsNullOrWhiteSpace(entry.ProposedName) ? entry.ProposedName : original;
                if (string.IsNullOrWhiteSpace(finalName) || !uniqueFinalNames.Add(finalName)) throw new InvalidDataException("Replicate renaming would create a duplicate or blank final name: " + finalName);
                finalNames.Add(replicate, finalName);
            }

            var renameMap = replicates.ToDictionary(e => (string)e.Attribute("name"), e => finalNames[e], StringComparer.OrdinalIgnoreCase);
            foreach (var replicate in replicates) replicate.SetAttributeValue("name", finalNames[replicate]);
            foreach (var attribute in _xml.Descendants().Attributes().Where(a => a.Name.LocalName == "replicate").ToList())
            {
                string replacement;
                if (renameMap.TryGetValue(attribute.Value, out replacement)) attribute.Value = replacement;
            }

            var matchedSet = new HashSet<XElement>(matchedElements);
            var reordered = matchedElements.Concat(replicates.Where(e => !matchedSet.Contains(e))).ToList();
            foreach (var replicate in replicates) replicate.Remove();
            measuredResults.Add(reordered);

            var renamed = entryByElement.Keys.Count(e => !string.Equals(originalNames[e], finalNames[e], StringComparison.Ordinal));
            return new ReplicateOrderResult
            {
                Matched = matchedElements.Count,
                IgnoredManifest = manifest.IgnoredRowCount + absentManifest,
                UnmatchedSkyline = replicates.Count - matchedElements.Count,
                DuplicateManifest = manifest.DuplicateCount,
                Renamed = renamed,
                Unchanged = matchedElements.Count - renamed
            };
        }

        public ReplicateAnnotationResult PrepareReplicateAnnotations(ReplicateAnnotationMetadata metadata)
        {
            if (metadata == null) throw new ArgumentNullException("metadata");
            var replicates = _xml.Descendants().Where(e => e.Name.LocalName == "replicate" && e.Parent != null && e.Parent.Name.LocalName == "measured_results").ToList();
            if (replicates.Count == 0) throw new InvalidDataException("The Skyline document has no results replicates for annotation import.");

            var definitions = _xml.Descendants().Where(e => e.Name.LocalName == "annotation" && e.Attribute("name") != null)
                .GroupBy(e => (string)e.Attribute("name"), StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var missing = new List<string>(); var existing = new List<string>(); var annotationNames = new List<string>();
            foreach (var name in metadata.AnnotationNames)
            {
                XElement definition;
                if (!definitions.TryGetValue(name, out definition)) { missing.Add(name); annotationNames.Add(name); continue; }
                var targets = ((string)definition.Attribute("targets") ?? string.Empty).Split(',').Select(value => value.Trim());
                if (!targets.Any(value => string.Equals(value, "replicate", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidDataException("The existing Skyline annotation '" + name + "' does not target replicates.");
                var canonicalName = (string)definition.Attribute("name"); existing.Add(canonicalName); annotationNames.Add(canonicalName);
            }
            if (missing.Count > 0)
            {
                var dataSettings = _xml.Descendants().FirstOrDefault(e => e.Name.LocalName == "data_settings");
                if (dataSettings == null)
                {
                    dataSettings = new XElement("data_settings");
                    var measuredResults = _xml.Descendants().FirstOrDefault(e => e.Name.LocalName == "measured_results");
                    if (measuredResults != null) measuredResults.AddBeforeSelf(dataSettings);
                    else
                    {
                        var settingsSummary = _xml.Descendants().FirstOrDefault(e => e.Name.LocalName == "settings_summary");
                        (settingsSummary ?? _xml.Root).Add(dataSettings);
                    }
                }
                foreach (var name in missing) dataSettings.Add(new XElement("annotation", new XAttribute("name", name), new XAttribute("targets", "replicate"), new XAttribute("type", "text")));
            }

            var matchedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var importRows = new List<string[]>();
            var expectedValues = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var replicate in replicates)
            {
                var aliases = new List<string> { (string)replicate.Attribute("name") };
                foreach (var sample in replicate.Descendants().Where(e => e.Name.LocalName == "sample_file"))
                {
                    aliases.Add((string)sample.Attribute("file_path"));
                    aliases.Add((string)sample.Attribute("sample_name"));
                }
                var matches = new List<string[]>();
                foreach (var alias in aliases.Select(ReplicateManifest.NormalizeKey).Where(value => !string.IsNullOrEmpty(value)).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    string[] values;
                    if (metadata.TryGetValues(alias, out values)) { matches.Add(values); matchedKeys.Add(alias); }
                }
                if (matches.Count == 0) continue;
                var selected = matches[0];
                if (matches.Skip(1).Any(values => !values.SequenceEqual(selected, StringComparer.Ordinal)))
                    throw new InvalidDataException("Raw files in Skyline replicate '" + (string)replicate.Attribute("name") + "' have conflicting metadata values.");
                var replicateName = (string)replicate.Attribute("name");
                importRows.Add(new[] { "Replicate:/" + replicateName }.Concat(selected).ToArray()); expectedValues.Add(replicateName, selected);
            }

            var importPath = _workingPath + ".replicate-annotations.csv";
            using (var writer = new StreamWriter(importPath, false, new UTF8Encoding(false)))
            {
                WriteCsvRow(writer, new[] { "ElementLocator" }.Concat(annotationNames.Select(name => "annotation_" + name)));
                foreach (var row in importRows) WriteCsvRow(writer, row);
            }
            return new ReplicateAnnotationResult
            {
                AnnotatedReplicates = importRows.Count, UnannotatedReplicates = replicates.Count - importRows.Count,
                UnmatchedMetadataRows = metadata.Keys.Count() - matchedKeys.Count, DuplicateMetadataKeys = metadata.DuplicateKeyCount,
                IgnoredMetadataRows = metadata.IgnoredRowCount, MissingDefinitions = missing, ExistingDefinitions = existing, AnnotationNames = annotationNames,
                ExpectedValuesByReplicate = expectedValues, ImportPath = importPath
            };
        }

        private static void WriteCsvRow(TextWriter writer, IEnumerable<string> values)
        {
            writer.WriteLine(string.Join(",", values.Select(value =>
            {
                var text = value ?? string.Empty;
                return text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0 ? text : "\"" + text.Replace("\"", "\"\"") + "\"";
            })));
        }

        internal void SaveXmlForTest() { _xml.Save(_workingPath); }

        public void NormalizeWithSkylineCmd(int maxVariableMods, ProteinAssociationOptions options, ReplicateAnnotationResult annotations)
        {
            _normalizedPath = _workingPath + ".normalized.sky";
            try
            {
                _xml.Save(_workingPath);
                var args = "--in=\"" + _workingPath + "\" --out=\"" + _normalizedPath + "\" --pep-max-variable-mods=" + maxVariableMods + " --refine-min-transitions=1 --refine-min-peptides=1";
                if (annotations != null) args += " --import-annotations=" + QuoteArgument(annotations.ImportPath);
                if (options != null && options.Enabled)
                {
                    using (var fasta = _fastaExporter.Export(options.FilePath))
                    {
                        args += " --background-proteome-file=\"" + options.FilePath + "\" --background-proteome-name=\"" + options.Name + "\" --associate-proteins-fasta=\"" + fasta.Path + "\" --associate-proteins-group-proteins --associate-proteins-shared-peptides=AssignedToBestProtein";
                        RunNormalization(args);
                    }
                }
                else RunNormalization(args);
                var normalizedXml = LoadSecure(_normalizedPath);
                if (annotations != null) VerifyReplicateAnnotations(normalizedXml, annotations);
                _workingPath = _normalizedPath;
                _xml = normalizedXml;
            }
            finally { if (annotations != null && File.Exists(annotations.ImportPath)) File.Delete(annotations.ImportPath); }
        }

        private static void VerifyReplicateAnnotations(XDocument document, ReplicateAnnotationResult annotations)
        {
            foreach (var name in annotations.AnnotationNames)
            {
                var definition = document.Descendants().FirstOrDefault(e => e.Name.LocalName == "annotation" && e.Parent != null && e.Parent.Name.LocalName == "data_settings" && string.Equals((string)e.Attribute("name"), name, StringComparison.OrdinalIgnoreCase));
                var targets = definition == null ? new string[0] : ((string)definition.Attribute("targets") ?? string.Empty).Split(',');
                if (definition == null || !targets.Any(value => string.Equals(value.Trim(), "replicate", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidDataException("SkylineCmd did not preserve replicate annotation definition: " + name);
            }
            var replicates = document.Descendants().Where(e => e.Name.LocalName == "replicate" && e.Parent != null && e.Parent.Name.LocalName == "measured_results")
                .ToDictionary(e => (string)e.Attribute("name"), e => e, StringComparer.OrdinalIgnoreCase);
            foreach (var expected in annotations.ExpectedValuesByReplicate)
            {
                XElement replicate;
                if (!replicates.TryGetValue(expected.Key, out replicate)) throw new InvalidDataException("SkylineCmd removed an annotated replicate: " + expected.Key);
                for (var index = 0; index < annotations.AnnotationNames.Count; index++)
                {
                    var name = annotations.AnnotationNames[index];
                    var value = replicate.Descendants().FirstOrDefault(e => e.Name.LocalName == "annotation" && string.Equals((string)e.Attribute("name"), name, StringComparison.OrdinalIgnoreCase));
                    var actual = value == null ? string.Empty : value.Value;
                    if (!string.Equals(actual, expected.Value[index] ?? string.Empty, StringComparison.Ordinal))
                        throw new InvalidDataException("SkylineCmd did not import annotation '" + name + "' for replicate '" + expected.Key + "'.");
                }
            }
        }

        private static string QuoteArgument(string value) { return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\""; }

        private void RunNormalization(string arguments)
        {
            var result = _runner(_skylineCommand, arguments);
            if (result.ExitCode != 0) throw new InvalidOperationException("SkylineCmd failed: " + result.Error + Environment.NewLine + result.Output);
            if (!File.Exists(_normalizedPath)) throw new InvalidOperationException("SkylineCmd did not create the normalized document.");
        }

        public void Verify(string match, int maxVariableMods)
        {
            if (ReadPeptides().Any(p => p.ModifiedSequence.IndexOf(match, StringComparison.Ordinal) < 0)) throw new InvalidDataException("The normalized document contains a nonmatching peptide.");
            VerifySettings(maxVariableMods);
        }

        public void VerifySettings(int maxVariableMods)
        {
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
