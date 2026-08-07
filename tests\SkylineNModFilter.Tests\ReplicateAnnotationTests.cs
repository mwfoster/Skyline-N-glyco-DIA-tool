using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SkylineNModFilter.Tests
{
    internal static class ReplicateAnnotationTests
    {
        public static void Run()
        {
            MatchesRawFilesAfterRenameAndWritesCsv();
            PreservesCompatibleDefinitionsAndRejectsIncompatibleOnes();
            HandlesMultiFileReplicatesDeterministically();
            AddsDefinitionsAndImportsInOneSkylineCommand();
            RejectsSilentlyIgnoredAnnotationImport();
            CleansImportCsvWhenXmlSaveFails();
        }

        private static void RejectsSilentlyIgnoredAnnotationImport()
        {
            string root, working;
            var document = CreateDocument(out root, out working, null, new XElement("replicate", new XAttribute("name", "A")));
            var result = document.PrepareReplicateAnnotations(LoadMetadata(root, "File,Condition\r\nA.raw,Control\r\n"));
            var calls = 0;
            var verifying = new SkylineDocument("SkylineCmd.exe", delegate(string exe, string args)
            {
                calls++; if (calls == 1) File.Copy(working, Path.Combine(root, "verify.sky"), true); else File.Copy(Path.Combine(root, "verify.sky"), Path.Combine(root, "verify.sky.normalized.sky"), true);
                return new CommandResult(0, "", "");
            });
            verifying.CreateWorkingCopy(working, Path.Combine(root, "verify.sky"));
            result = verifying.PrepareReplicateAnnotations(LoadMetadata(root, "File,Condition\r\nA.raw,Control\r\n"));
            TestAssert.Throws<InvalidDataException>(delegate { verifying.NormalizeWithSkylineCmd(1, ProteinAssociationOptions.Disabled, result); }, "A successful SkylineCmd exit without imported values must fail before publication.");
            verifying.DiscardWorkingCopy(); document.DiscardWorkingCopy(); Directory.Delete(root, true);
        }

        private static void CleansImportCsvWhenXmlSaveFails()
        {
            string root, working;
            var document = CreateDocument(out root, out working, null, new XElement("replicate", new XAttribute("name", "A")));
            var result = document.PrepareReplicateAnnotations(LoadMetadata(root, "File,Condition\r\nA.raw,Control\r\n"));
            File.SetAttributes(working, FileAttributes.ReadOnly);
            try { TestAssert.Throws<UnauthorizedAccessException>(delegate { document.NormalizeWithSkylineCmd(1, ProteinAssociationOptions.Disabled, result); }, "XML save failure must escape."); }
            finally { File.SetAttributes(working, FileAttributes.Normal); }
            TestAssert.True(!File.Exists(result.ImportPath), "Temporary annotation CSV must be removed when XML saving fails.");
            document.DiscardWorkingCopy(); Directory.Delete(root, true);
        }

        private static void AddsDefinitionsAndImportsInOneSkylineCommand()
        {
            var root = Path.Combine(Path.GetTempPath(), "SkylineAnnotationCommand", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            var source = Path.Combine(root, "source.sky"); var working = Path.Combine(root, "working.sky");
            new XDocument(new XElement("srm_settings", new XElement("measured_results", new XElement("replicate", new XAttribute("name", "A"))))).Save(source);
            string captured = null; var calls = 0;
            var document = new SkylineDocument("SkylineCmd.exe", delegate(string exe, string args)
            {
                calls++; if (calls == 1) File.Copy(source, working, true); else
                {
                    captured = args; var normalized = XDocument.Load(working);
                    var replicate = normalized.Descendants("replicate").Single();
                    replicate.Add(new XElement("annotations", new XElement("annotation", new XAttribute("name", "Condition"), "Control"), new XElement("annotation", new XAttribute("name", "Batch"), "1")));
                    normalized.Save(working + ".normalized.sky");
                }
                return new CommandResult(0, "", "");
            });
            document.CreateWorkingCopy(source, working);
            var annotations = document.PrepareReplicateAnnotations(LoadMetadata(root, "File,Condition,Batch\r\nA.raw,Control,1\r\n"));
            var importPath = annotations.ImportPath;
            document.SaveXmlForTest();
            var created = XDocument.Load(working).Descendants("annotation").Select(e => (string)e.Attribute("name") + ":" + (string)e.Attribute("targets") + ":" + (string)e.Attribute("type"));
            TestAssert.Equal("Condition:replicate:text,Batch:replicate:text", string.Join(",", created), "All missing definitions must be added to the temporary document before SkylineCmd import.");
            document.NormalizeWithSkylineCmd(1, ProteinAssociationOptions.Disabled, annotations);
            TestAssert.True(!captured.Contains("--annotation-name"), "Multiple definitions must not use SkylineCmd's single-valued annotation-name option.");
            TestAssert.True(captured.Contains("--import-annotations=\"" + importPath + "\""), "The generated annotation CSV must be imported in the normalization command.");
            TestAssert.True(!File.Exists(importPath), "Temporary annotation CSV must be removed after normalization.");
            document.DiscardWorkingCopy(); Directory.Delete(root, true);
        }

        private static void MatchesRawFilesAfterRenameAndWritesCsv()
        {
            string root, working;
            var document = CreateDocument(out root, out working, null,
                new XElement("replicate", new XAttribute("name", "Renamed, A"), new XElement("sample_file", new XAttribute("file_path", "C:\\raw\\A.raw"))),
                new XElement("replicate", new XAttribute("name", "B"), new XElement("sample_file", new XAttribute("file_path", "B.raw"))),
                new XElement("replicate", new XAttribute("name", "Extra"), new XElement("sample_file", new XAttribute("file_path", "Extra.raw"))));
            var metadata = LoadMetadata(root, "File,Condition,Batch\r\nA.raw,Control,1\r\nB.raw,Treated,2\r\nAbsent.raw,X,9\r\n");
            var result = document.PrepareReplicateAnnotations(metadata);
            var lines = File.ReadAllLines(result.ImportPath);
            TestAssert.Equal("ElementLocator,annotation_Condition,annotation_Batch", lines[0], "Skyline annotation import header must include every metadata column.");
            TestAssert.Equal("\"Replicate:/Renamed, A\",Control,1", lines[1], "CSV must quote locator names containing commas and use final replicate names.");
            TestAssert.Equal("Replicate:/B,Treated,2", lines[2], "Current replicate-name matching must remain supported.");
            TestAssert.Equal(2, result.AnnotatedReplicates, "Matched replicates must be reported.");
            TestAssert.Equal(1, result.UnannotatedReplicates, "Unmatched Skyline replicates must be reported.");
            TestAssert.Equal(1, result.UnmatchedMetadataRows, "Metadata keys absent from Skyline must be reported.");
            TestAssert.Equal("Condition,Batch", string.Join(",", result.MissingDefinitions), "Missing definitions must be returned in metadata order.");
            document.DiscardWorkingCopy(); Directory.Delete(root, true);
        }

        private static void PreservesCompatibleDefinitionsAndRejectsIncompatibleOnes()
        {
            string root, working;
            var dataSettings = new XElement("data_settings",
                new XElement("annotation", new XAttribute("name", "Condition"), new XAttribute("targets", "replicate"), new XAttribute("type", "value_list")),
                new XElement("annotation", new XAttribute("name", "Batch"), new XAttribute("targets", "peptide"), new XAttribute("type", "text")));
            var document = CreateDocument(out root, out working, dataSettings, new XElement("replicate", new XAttribute("name", "A")));
            var metadata = LoadMetadata(root, "File,Condition\r\nA.raw,Control\r\n");
            var result = document.PrepareReplicateAnnotations(metadata);
            TestAssert.Equal(0, result.MissingDefinitions.Count, "Compatible existing definitions must be preserved instead of recreated.");
            document.DiscardWorkingCopy();

            document = CreateDocument(out root, out working, dataSettings, new XElement("replicate", new XAttribute("name", "A")));
            metadata = LoadMetadata(root, "File,Batch\r\nA.raw,1\r\n");
            TestAssert.Throws<InvalidDataException>(delegate { document.PrepareReplicateAnnotations(metadata); }, "An existing annotation without a replicate target must fail safely.");
            document.DiscardWorkingCopy(); Directory.Delete(root, true);
        }

        private static void HandlesMultiFileReplicatesDeterministically()
        {
            string root, working;
            var replicate = new XElement("replicate", new XAttribute("name", "Combined"),
                new XElement("sample_file", new XAttribute("file_path", "A.raw")), new XElement("sample_file", new XAttribute("file_path", "B.raw")));
            var document = CreateDocument(out root, out working, null, replicate);
            var metadata = LoadMetadata(root, "File,Condition\r\nA.raw,Control\r\nB.raw,Control\r\n");
            TestAssert.Equal(1, document.PrepareReplicateAnnotations(metadata).AnnotatedReplicates, "Identical multi-file metadata must annotate the combined replicate once.");
            document.DiscardWorkingCopy();

            document = CreateDocument(out root, out working, null, replicate);
            metadata = LoadMetadata(root, "File,Condition\r\nA.raw,Control\r\nB.raw,Treated\r\n");
            TestAssert.Throws<InvalidDataException>(delegate { document.PrepareReplicateAnnotations(metadata); }, "Conflicting multi-file metadata must fail safely.");
            document.DiscardWorkingCopy(); Directory.Delete(root, true);
        }

        private static ReplicateAnnotationMetadata LoadMetadata(string root, string text)
        {
            var path = Path.Combine(root, Guid.NewGuid().ToString("N") + ".csv"); File.WriteAllText(path, text);
            return ReplicateAnnotationMetadata.Load(ReplicateAnnotationOptions.EnabledFor(path));
        }

        private static SkylineDocument CreateDocument(out string root, out string working, XElement dataSettings, params XElement[] replicates)
        {
            root = Path.Combine(Path.GetTempPath(), "SkylineAnnotationTests", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            var source = Path.Combine(root, "source.sky"); working = Path.Combine(root, "working.sky");
            new XDocument(new XElement("srm_settings", dataSettings, new XElement("measured_results", replicates))).Save(source);
            var destination = working;
            var document = new SkylineDocument("SkylineCmd.exe", delegate { File.Copy(source, destination, true); return new CommandResult(0, "", ""); });
            document.CreateWorkingCopy(source, working); return document;
        }
    }
}
