using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SkylineNModFilter.Tests
{
    internal static class SkylineDocumentTests
    {
        public static void Run()
        {
            FiltersOnlyWorkingCopyAndRemovesEmptyContainers();
            BuildsQuotedSkylineCommand();
            SuppliesAndCleansAssociationFasta();
            PublishesAndDiscardsCompleteFileSets();
        }

        private static void SuppliesAndCleansAssociationFasta()
        {
            var root = Path.Combine(Path.GetTempPath(), "SkylineAssociationTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var source = Path.Combine(root, "source.sky");
            new XDocument(new XElement("srm_settings")).Save(source);

            string captured = null;
            var successExporter = new RecordingFastaExporter();
            var successCalls = 0;
            var successDocument = new SkylineDocument("SkylineCmd.exe", delegate(string exe, string args)
            {
                successCalls++;
                if (successCalls == 1) File.Copy(source, Path.Combine(root, "success.sky"), true);
                else
                {
                    captured = args;
                    File.Copy(Path.Combine(root, "success.sky"), Path.Combine(root, "success.sky.normalized.sky"), true);
                }
                return new CommandResult(0, string.Empty, string.Empty);
            }, successExporter);
            successDocument.CreateWorkingCopy(source, Path.Combine(root, "success.sky"));
            successDocument.NormalizeWithSkylineCmd(1, ProteinAssociationOptions.EnabledFor(Path.Combine(root, "human.protdb"), "Human"));
            TestAssert.Equal(1, successExporter.CallCount, "Association must export exactly one FASTA.");
            TestAssert.True(captured.Contains("--associate-proteins-fasta=\"" + successExporter.LastPath + "\""), "Association command must quote the exported FASTA path.");
            TestAssert.True(!File.Exists(successExporter.LastPath), "Temporary FASTA must be removed after successful SkylineCmd execution.");

            var failureExporter = new RecordingFastaExporter();
            var failureCalls = 0;
            var failureDocument = new SkylineDocument("SkylineCmd.exe", delegate(string exe, string args)
            {
                failureCalls++;
                if (failureCalls == 1) { File.Copy(source, Path.Combine(root, "failure.sky"), true); return new CommandResult(0, string.Empty, string.Empty); }
                return new CommandResult(1, string.Empty, "association failed");
            }, failureExporter);
            failureDocument.CreateWorkingCopy(source, Path.Combine(root, "failure.sky"));
            TestAssert.Throws<InvalidOperationException>(delegate { failureDocument.NormalizeWithSkylineCmd(1, ProteinAssociationOptions.EnabledFor(Path.Combine(root, "human.protdb"), "Human")); }, "Association failure must escape.");
            TestAssert.True(!File.Exists(failureExporter.LastPath), "Temporary FASTA must be removed after failed SkylineCmd execution.");

            var disabledExporter = new RecordingFastaExporter();
            var disabledCalls = 0;
            var disabledDocument = new SkylineDocument("SkylineCmd.exe", delegate(string exe, string args)
            {
                disabledCalls++;
                if (disabledCalls == 1) File.Copy(source, Path.Combine(root, "disabled.sky"), true);
                else File.Copy(Path.Combine(root, "disabled.sky"), Path.Combine(root, "disabled.sky.normalized.sky"), true);
                return new CommandResult(0, string.Empty, string.Empty);
            }, disabledExporter);
            disabledDocument.CreateWorkingCopy(source, Path.Combine(root, "disabled.sky"));
            disabledDocument.NormalizeWithSkylineCmd(1, ProteinAssociationOptions.Disabled);
            TestAssert.Equal(0, disabledExporter.CallCount, "Disabled association must not export a FASTA.");
            Directory.Delete(root, true);
        }

        private static void PublishesAndDiscardsCompleteFileSets()
        {
            var root = Path.Combine(Path.GetTempPath(), "SkylinePublish", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var normalized = Path.Combine(root, "temp.normalized.sky");
            foreach (var suffix in new[] { ".sky", ".skyd", ".skyl", ".blib", ".redundant.blib", ".unknown" }) File.WriteAllText(Path.Combine(root, "temp.normalized" + suffix), suffix);
            var document = new SkylineDocument("unused", delegate { return new CommandResult(0, "", ""); });
            typeof(SkylineDocument).GetField("_workingPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(document, normalized);
            typeof(SkylineDocument).GetField("_normalizedPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(document, normalized);
            var finalSky = Path.Combine(root, "final.sky");
            document.PublishWorkingCopy(finalSky);
            foreach (var suffix in new[] { ".sky", ".skyd", ".skyl", ".blib", ".redundant.blib" }) TestAssert.True(File.Exists(Path.Combine(root, "final" + suffix)), "Final companion missing: " + suffix);
            TestAssert.True(File.Exists(Path.Combine(root, "temp.normalized.unknown")), "Unknown files must be preserved.");
            Directory.Delete(root, true);
        }

        private static void FiltersOnlyWorkingCopyAndRemovesEmptyContainers()
        {
            var root = Path.Combine(Path.GetTempPath(), "Skyline N Mod Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var source = Path.Combine(root, "source.sky");
            var working = Path.Combine(root, "working.sky");
            var xml = new XDocument(new XElement("srm_settings",
                new XElement("settings_summary", new XElement("peptide_settings", new XElement("peptide_modifications", new XAttribute("max_variable_mods", "3")))),
                new XElement("protein", new XAttribute("name", "keep"),
                    new XElement("peptide", new XAttribute("modified_sequence", "AN[+1]BC"), new XElement("precursor", new XElement("transition"))),
                    new XElement("peptide", new XAttribute("modified_sequence", "ANBC"), new XElement("precursor", new XElement("transition")))),
                new XElement("protein", new XAttribute("name", "remove"),
                    new XElement("peptide", new XAttribute("modified_sequence", "OTHER"), new XElement("precursor", new XElement("transition")))),
                new XElement("protein_group", new XAttribute("name", "empty-group"),
                    new XElement("protein", new XAttribute("name", "group-member"))),
                new XElement("protein_group", new XAttribute("name", "kept-group"),
                    new XElement("protein", new XAttribute("name", "required-member"), new XElement("sequence", "ANBC")),
                    new XElement("peptide", new XAttribute("modified_sequence", "AN[+1]BC"), new XElement("precursor", new XElement("transition"))))));
            xml.Save(source);

            var createCalls = 0;
            var document = new SkylineDocument("SkylineCmd.exe", delegate(string exe, string args) { createCalls++; File.Copy(source, working, true); return new CommandResult(0, string.Empty, string.Empty); });
            document.CreateWorkingCopy(source, working);
            TestAssert.Equal(1, createCalls, "SkylineCmd must create the working copy so companion files remain resolvable.");
            var plan = FilterPlan.Create(document.ReadPeptides());
            document.DeletePeptides(plan.DeleteElements);
            document.RemoveEmptyContainers();
            document.SaveXmlForTest();

            var result = XDocument.Load(working);
            TestAssert.Equal(2, result.Descendants("peptide").Count(), "Only matching peptides should remain.");
            TestAssert.Equal(2, result.Descendants("protein").Count(), "Only top-level proteins emptied by filtering should be removed.");
            TestAssert.Equal(1, result.Descendants("protein_group").Count(), "Only a protein group emptied by filtering should be removed.");
            TestAssert.Equal(1, result.Descendants("protein_group").Descendants("protein").Count(), "Protein-group member metadata must be retained.");
            TestAssert.Equal(4, XDocument.Load(source).Descendants("peptide").Count(), "Source XML must remain unchanged.");
            Directory.Delete(root, true);
        }

        private static void BuildsQuotedSkylineCommand()
        {
            string captured = null;
            var root = Path.Combine(Path.GetTempPath(), "path with spaces", Guid.NewGuid().ToString("N"));
            var source = Path.Combine(root, "source.sky");
            var commandCalls = 0;
            var document = new SkylineDocument("SkylineCmd.exe", delegate(string exe, string args) { commandCalls++; captured = args; if (commandCalls == 1) { File.Copy(source, Path.Combine(root, "work.sky"), true); return new CommandResult(0, string.Empty, string.Empty); } return new CommandResult(1, string.Empty, "stop"); }, new RecordingFastaExporter());
            Directory.CreateDirectory(root);
            new XDocument(new XElement("srm_settings", new XElement("protein", new XElement("peptide", new XAttribute("modified_sequence", "AN[+1]"), new XElement("precursor"))))).Save(source);
            document.CreateWorkingCopy(source, Path.Combine(root, "work.sky"));
            TestAssert.Throws<InvalidOperationException>(delegate { document.NormalizeWithSkylineCmd(1, ProteinAssociationOptions.EnabledFor("C:\\db\\human.protdb", "Human Proteome")); }, "Nonzero SkylineCmd exit must fail.");
            TestAssert.True(captured.Contains("--pep-max-variable-mods=1"), "Command must set maximum variable mods.");
            TestAssert.True(captured.Contains("--refine-min-peptides=1"), "Command must remove empty proteins.");
            TestAssert.True(captured.Contains("--background-proteome-file=\"C:\\db\\human.protdb\""), "Selected background proteome path must be quoted.");
            TestAssert.True(captured.Contains("--associate-proteins-group-proteins"), "Protein grouping must be enabled.");
            TestAssert.True(captured.Contains("--associate-proteins-shared-peptides=AssignedToBestProtein"), "Shared peptides must be assigned to the best protein.");
            TestAssert.True(captured.Contains("\"" + Path.Combine(root, "work.sky") + "\""), "Input path must be quoted.");
            Directory.Delete(root, true);

            var diagnosticCalls = 0;
            var diagnosticDocument = new SkylineDocument("SkylineCmd.exe", delegate(string exe, string args) { diagnosticCalls++; if (diagnosticCalls == 1) { File.Copy(source, Path.Combine(root, "diagnostic.sky"), true); return new CommandResult(0, string.Empty, string.Empty); } return new CommandResult(1, "useful stdout", string.Empty); });
            Directory.CreateDirectory(root);
            new XDocument(new XElement("srm_settings")).Save(source);
            diagnosticDocument.CreateWorkingCopy(source, Path.Combine(root, "diagnostic.sky"));
            var exception = TestAssert.Throws<InvalidOperationException>(delegate { diagnosticDocument.NormalizeWithSkylineCmd(1, ProteinAssociationOptions.Disabled); }, "Failure should report command output.");
            TestAssert.True(exception.Message.Contains("useful stdout"), "SkylineCmd stdout must be included in diagnostics.");
            Directory.Delete(root, true);
        }

        private sealed class RecordingFastaExporter : IBackgroundProteomeFastaExporter
        {
            public int CallCount { get; private set; }
            public string LastPath { get; private set; }

            public TemporaryFasta Export(string protdbPath)
            {
                CallCount++;
                LastPath = Path.Combine(Path.GetTempPath(), "Skyline association " + Guid.NewGuid().ToString("N") + ".fasta");
                File.WriteAllText(LastPath, ">test" + Environment.NewLine + "PEPTIDE" + Environment.NewLine);
                return new TemporaryFasta(LastPath);
            }
        }
    }
}
