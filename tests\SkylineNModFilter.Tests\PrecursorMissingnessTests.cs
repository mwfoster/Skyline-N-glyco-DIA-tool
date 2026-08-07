using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SkylineNModFilter.Tests
{
    internal static class PrecursorMissingnessTests
    {
        public static void Run()
        {
            FiltersEachPrecursorAcrossAllReplicates();
            RejectsDocumentsWithoutReplicates();
            ValidatesOptions();
            FiltersWithinSelectedAndAnyMetadataGroups();
        }

        private static void FiltersWithinSelectedAndAnyMetadataGroups()
        {
            var root = Path.Combine(Path.GetTempPath(), "SkylineGroupedMissingness", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            var metadata = Path.Combine(root, "groups.csv");
            File.WriteAllText(metadata, "File,Condition\r\nA.raw,Control\r\nB.raw,control\r\nC.raw,Treatment\r\n");

            var selected = RunGrouped(root, "selected", PrecursorMissingnessOptions.EnabledForGroups(50, PrecursorMissingnessScope.SelectedGroup, metadata, true, 2, "CONTROL", false));
            TestAssert.Equal("control-only", selected.Remaining, "Selected group matching must be case-insensitive.");
            TestAssert.Equal(3, selected.Result.AnnotatedReplicates, "Three replicates are annotated.");
            TestAssert.Equal(1, selected.Result.UnannotatedReplicates, "The unmatched replicate must be Unannotated.");

            var anyIncluding = RunGrouped(root, "any-including", PrecursorMissingnessOptions.EnabledForGroups(50, PrecursorMissingnessScope.AnyGroup, metadata, true, 2, null, false));
            TestAssert.Equal("control-only,treatment-only,unannotated-only", anyIncluding.Remaining, "Passing any annotated or Unannotated group must retain a precursor.");
            TestAssert.Equal(3, anyIncluding.Result.EvaluatedGroupCount, "Control, Treatment, and Unannotated must be evaluated.");

            var anyExcluding = RunGrouped(root, "any-excluding", PrecursorMissingnessOptions.EnabledForGroups(50, PrecursorMissingnessScope.AnyGroup, metadata, true, 2, null, true));
            TestAssert.Equal("control-only,treatment-only", anyExcluding.Remaining, "Excluded Unannotated replicates must not rescue a precursor.");
            TestAssert.Equal(1, anyExcluding.Result.ExcludedReplicates, "One Unannotated replicate must be excluded.");
            Directory.Delete(root, true);
        }

        private static GroupRun RunGrouped(string root, string name, PrecursorMissingnessOptions options)
        {
            var source = Path.Combine(root, name + "-source.sky"); var working = Path.Combine(root, name + "-working.sky");
            new XDocument(new XElement("srm_settings",
                new XElement("measured_results", new XElement("replicate", new XAttribute("name", "A")), new XElement("replicate", new XAttribute("name", "B")), new XElement("replicate", new XAttribute("name", "C")), new XElement("replicate", new XAttribute("name", "D"))),
                new XElement("protein", new XElement("peptide", new XAttribute("modified_sequence", "AN[+1]BC"),
                    Precursor("control-only", Peak("A", "10"), Peak("B", "10")),
                    Precursor("treatment-only", Peak("C", "10")),
                    Precursor("unannotated-only", Peak("D", "10")),
                    Precursor("none"))))).Save(source);
            var document = new SkylineDocument("SkylineCmd.exe", delegate { File.Copy(source, working, true); return new CommandResult(0, "", ""); });
            document.CreateWorkingCopy(source, working); var result = document.ApplyPrecursorMissingnessFilter(options); document.SaveXmlForTest();
            return new GroupRun { Result = result, Remaining = string.Join(",", XDocument.Load(working).Descendants("precursor").Select(e => (string)e.Attribute("id"))) };
        }

        private sealed class GroupRun { public PrecursorMissingnessResult Result; public string Remaining; }

        private static void FiltersEachPrecursorAcrossAllReplicates()
        {
            var root = Path.Combine(Path.GetTempPath(), "SkylineMissingness", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var source = Path.Combine(root, "source.sky");
            var working = Path.Combine(root, "working.sky");
            new XDocument(new XElement("srm_settings",
                new XElement("measured_results",
                    new XElement("replicate", new XAttribute("name", "A")),
                    new XElement("replicate", new XAttribute("name", "B")),
                    new XElement("replicate", new XAttribute("name", "C")),
                    new XElement("replicate", new XAttribute("name", "D"))),
                new XElement("protein", new XElement("peptide", new XAttribute("modified_sequence", "AN[+1]BC"),
                    Precursor("retain-exactly-50", Peak("A", "10"), Peak("B", "2")),
                    Precursor("remove-75", Peak("A", "10")),
                    Precursor("remove-zero-invalid", Peak("A", "0"), Peak("B", "-1"), Peak("C", "bad"), Peak("D", "NaN")),
                    Precursor("retain-all", Peak("A", "1"), Peak("B", "2"), Peak("C", "3"), Peak("D", "4")))))).Save(source);

            var document = new SkylineDocument("SkylineCmd.exe", delegate { File.Copy(source, working, true); return new CommandResult(0, "", ""); });
            document.CreateWorkingCopy(source, working);
            var result = document.ApplyPrecursorMissingnessFilter(PrecursorMissingnessOptions.EnabledFor(50));
            document.SaveXmlForTest();
            var remaining = XDocument.Load(working).Descendants("precursor").Select(e => (string)e.Attribute("id")).ToArray();
            TestAssert.Equal("retain-exactly-50,retain-all", string.Join(",", remaining), "The exact threshold must be retained and values above it removed.");
            TestAssert.Equal(4, result.Evaluated, "Every precursor must be evaluated independently.");
            TestAssert.Equal(2, result.Retained, "Two precursors should remain.");
            TestAssert.Equal(2, result.Removed, "Two precursors should be removed.");
            TestAssert.Equal(50, result.MaximumMissingPercent, "The configured threshold must be reported.");
            Directory.Delete(root, true);
        }

        private static void RejectsDocumentsWithoutReplicates()
        {
            var root = Path.Combine(Path.GetTempPath(), "SkylineMissingness", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var source = Path.Combine(root, "source.sky"); var working = Path.Combine(root, "working.sky");
            new XDocument(new XElement("srm_settings", new XElement("protein", new XElement("peptide", new XAttribute("modified_sequence", "AN[+1]"), new XElement("precursor"))))).Save(source);
            var document = new SkylineDocument("SkylineCmd.exe", delegate { File.Copy(source, working, true); return new CommandResult(0, "", ""); });
            document.CreateWorkingCopy(source, working);
            TestAssert.Throws<InvalidDataException>(delegate { document.ApplyPrecursorMissingnessFilter(PrecursorMissingnessOptions.EnabledFor(50)); }, "Enabled filtering requires results replicates.");
            Directory.Delete(root, true);
        }

        private static void ValidatesOptions()
        {
            TestAssert.Throws<ArgumentOutOfRangeException>(delegate { PrecursorMissingnessOptions.EnabledFor(-1); }, "Negative thresholds must fail.");
            TestAssert.Throws<ArgumentOutOfRangeException>(delegate { PrecursorMissingnessOptions.EnabledFor(101); }, "Thresholds above 100 must fail.");
            TestAssert.True(!PrecursorMissingnessOptions.Disabled.Enabled, "The compatibility default must remain disabled.");
        }

        private static XElement Precursor(string id, params XElement[] peaks) { return new XElement("precursor", new XAttribute("id", id), new XElement("precursor_results", peaks)); }
        private static XElement Peak(string replicate, string area) { return new XElement("precursor_peak", new XAttribute("replicate", replicate), new XAttribute("area", area)); }
    }
}
