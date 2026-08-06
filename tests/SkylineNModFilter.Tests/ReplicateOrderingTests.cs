using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SkylineNModFilter.Tests
{
    internal static class ReplicateOrderingTests
    {
        public static void Run()
        {
            ReordersAndRenamesAllReferencesAtomically();
            RejectsNameCollisionWithoutMutation();
        }

        private static void ReordersAndRenamesAllReferencesAtomically()
        {
            string root, working;
            var document = CreateDocument(out root, out working);
            var manifest = CreateManifest(root, "Path,Name\r\nA.raw,B\r\nB.raw,A\r\nDDA.raw,D\r\n");
            var result = document.ApplyReplicateOrdering(manifest);
            document.SaveXmlForTest();
            var xml = XDocument.Load(working);
            var names = xml.Descendants("measured_results").Elements("replicate").Select(e => (string)e.Attribute("name")).ToArray();
            TestAssert.Equal("B,A,Extra", string.Join(",", names), "Matched replicates must follow manifest order and unmatched must remain last.");
            var references = xml.Descendants().Attributes("replicate").Select(a => a.Value).ToArray();
            TestAssert.Equal("B,A", string.Join(",", references), "All result references must use the swapped names exactly once.");
            TestAssert.Equal("raw-A", (string)xml.Descendants("sample_file").First().Attribute("sample_name"), "Sample metadata must not be renamed.");
            TestAssert.Equal(2, result.Matched, "Matched count should report Skyline matches.");
            TestAssert.Equal(1, result.IgnoredManifest, "Absent manifest entries should be ignored.");
            TestAssert.Equal(1, result.UnmatchedSkyline, "Unmatched Skyline replicate should be reported.");
            TestAssert.Equal(2, result.Renamed, "Both matched replicates should be renamed.");
            Directory.Delete(root, true);
        }

        private static void RejectsNameCollisionWithoutMutation()
        {
            string root, working;
            var document = CreateDocument(out root, out working);
            var before = File.ReadAllText(working);
            var manifest = CreateManifest(root, "Path,Name\r\nA.raw,Extra\r\n");
            TestAssert.Throws<InvalidDataException>(delegate { document.ApplyReplicateOrdering(manifest); }, "Rename collision with unmatched replicate must fail.");
            document.SaveXmlForTest();
            TestAssert.Equal(before, File.ReadAllText(working), "A failed rename must not mutate the temporary XML.");
            Directory.Delete(root, true);
        }

        private static ReplicateManifest CreateManifest(string root, string text)
        {
            var path = Path.Combine(root, Guid.NewGuid().ToString("N") + ".csv");
            File.WriteAllText(path, text);
            return ReplicateManifest.Load(ReplicateOrderingOptions.EnabledFor(path, true, true, 2));
        }

        private static SkylineDocument CreateDocument(out string root, out string working)
        {
            root = Path.Combine(Path.GetTempPath(), "SkylineReplicateTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var source = Path.Combine(root, "source.sky");
            working = Path.Combine(root, "working.sky");
            new XDocument(new XElement("srm_settings",
                new XElement("measured_results",
                    new XElement("replicate", new XAttribute("name", "B"), new XElement("sample_file", new XAttribute("sample_name", "raw-B"), new XAttribute("file_path", "B.raw"))),
                    new XElement("replicate", new XAttribute("name", "A"), new XElement("sample_file", new XAttribute("sample_name", "raw-A"), new XAttribute("file_path", "A.raw"))),
                    new XElement("replicate", new XAttribute("name", "Extra"), new XElement("sample_file", new XAttribute("sample_name", "raw-Extra"), new XAttribute("file_path", "Extra.raw")))),
                new XElement("peptide",
                    new XElement("precursor_peak", new XAttribute("replicate", "A"), new XAttribute("area", "10")),
                    new XElement("transition_peak", new XAttribute("replicate", "B"), new XAttribute("area", "20"))))).Save(source);
            var destination = working;
            var document = new SkylineDocument("SkylineCmd.exe", delegate { File.Copy(source, destination, true); return new CommandResult(0, "", ""); });
            document.CreateWorkingCopy(source, working);
            return document;
        }
    }
}
