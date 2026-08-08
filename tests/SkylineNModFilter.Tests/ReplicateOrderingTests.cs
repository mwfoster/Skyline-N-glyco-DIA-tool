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
            NumbersNamesWithoutChangingReplicateOrder();
            ReplacesExistingNumberPrefixOnRerun();
            RejectsNameCollisionWithoutMutation();
        }

        private static void NumbersNamesWithoutChangingReplicateOrder()
        {
            string root, working;
            var document = CreateDocument(out root, out working);
            var manifest = CreateManifest(root, "Path,Name\r\nA.raw,Control A\r\nB.mzML,Control B\r\nDDA.raw,D\r\n");
            var result = document.ApplyReplicateOrdering(manifest);
            document.SaveXmlForTest();
            var xml = XDocument.Load(working);
            var names = xml.Descendants("measured_results").Elements("replicate").Select(e => (string)e.Attribute("name")).ToArray();
            TestAssert.Equal("002_Control B,001_Control A,001_Extra", string.Join(",", names), "Renaming must preserve Skyline replicate element order.");
            var paths = xml.Descendants("measured_results").Elements("replicate").Select(e => (string)e.Element("sample_file").Attribute("file_path")).ToArray();
            TestAssert.Equal("B.mzML,A.raw,Extra.raw", string.Join(",", paths), "Raw-file identities must remain attached to their original replicate positions.");
            var references = xml.Descendants().Attributes("replicate").Select(a => a.Value).ToArray();
            TestAssert.Equal("001_Control A,002_Control B", string.Join(",", references), "All result references must use numbered names exactly once.");
            TestAssert.Equal("raw-B", (string)xml.Descendants("sample_file").First().Attribute("sample_name"), "Sample metadata must remain attached to the first original replicate.");
            TestAssert.Equal(2, result.Matched, "Matched count should report Skyline matches.");
            TestAssert.Equal(1, result.IgnoredManifest, "Absent manifest entries should be ignored.");
            TestAssert.Equal(1, result.UnmatchedSkyline, "Unmatched Skyline replicate should be reported.");
            TestAssert.Equal(2, result.Renamed, "Both matched replicates should be renamed.");
            Directory.Delete(root, true);
        }

        private static void ReplacesExistingNumberPrefixOnRerun()
        {
            string root, working;
            var document = CreateDocument(out root, out working);
            var manifest = CreateManifest(root, "Path,Name\r\nA.raw,009_Control A\r\n");
            document.ApplyReplicateOrdering(manifest);
            document.ApplyReplicateOrdering(manifest);
            document.SaveXmlForTest();
            var name = XDocument.Load(working).Descendants("measured_results").Elements("replicate")
                .First(e => ((string)e.Element("sample_file").Attribute("file_path")) == "A.raw").Attribute("name").Value;
            TestAssert.Equal("001_Control A", name, "Rerunning should replace, not stack, numeric prefixes.");
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
                    new XElement("replicate", new XAttribute("name", "B"), new XElement("sample_file", new XAttribute("sample_name", "raw-B"), new XAttribute("file_path", "B.mzML"))),
                    new XElement("replicate", new XAttribute("name", "A"), new XElement("sample_file", new XAttribute("sample_name", "raw-A"), new XAttribute("file_path", "A.raw"))),
                    new XElement("replicate", new XAttribute("name", "001_Extra"), new XElement("sample_file", new XAttribute("sample_name", "raw-Extra"), new XAttribute("file_path", "Extra.raw")))),
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
