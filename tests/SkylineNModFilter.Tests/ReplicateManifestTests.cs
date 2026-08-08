using System;
using System.IO;
using System.Text;

namespace SkylineNModFilter.Tests
{
    internal static class ReplicateManifestTests
    {
        public static void Run()
        {
            NormalizesPathsAndKeepsFirstDuplicate();
            CreatesSortableNumberedNames();
            ExpandsPaddingForLargeManifests();
        }

        private static void NormalizesPathsAndKeepsFirstDuplicate()
        {
            var path = Path.Combine(Path.GetTempPath(), "replicates-" + Guid.NewGuid().ToString("N") + ".csv");
            File.WriteAllText(path, "Path,Group,New Name\r\n" +
                "I:\\11094\\glyco\\ID137723_gDIA_01.raw,A,Patient 1\r\n" +
                "/data/ID137724.RAW,B,\r\n" +
                "I:\\duplicate\\id137723_gdia_01.raw,C,Ignored\r\n" +
                ",D,Blank\r\n");
            var options = ReplicateOrderingOptions.EnabledFor(path, true, true, 3);
            var manifest = ReplicateManifest.Load(options);
            TestAssert.Equal(2, manifest.Entries.Count, "Only unique usable keys should remain.");
            TestAssert.Equal("ID137723_gDIA_01", manifest.Entries[0].Key, "Windows raw path should normalize.");
            TestAssert.Equal("Patient 1", manifest.Entries[0].ProposedName, "Rename value should parse.");
            TestAssert.Equal("ID137724", manifest.Entries[1].Key, "Unix path and uppercase extension should normalize.");
            TestAssert.Equal("sample", ReplicateManifest.NormalizeKey(@"C:\data\sample.mzML"), ".mzML should normalize case-insensitively.");
            TestAssert.Equal(null, manifest.Entries[1].ProposedName, "Blank rename should preserve original name.");
            TestAssert.Equal(1, manifest.DuplicateCount, "Later duplicate keys should be counted.");
            TestAssert.Equal(1, manifest.IgnoredRowCount, "Blank keys should be counted.");
            File.Delete(path);
        }

        private static void CreatesSortableNumberedNames()
        {
            var path = Path.Combine(Path.GetTempPath(), "replicate-names-" + Guid.NewGuid().ToString("N") + ".csv");
            File.WriteAllText(path, "Path,Name\r\nA.raw,Control\r\nB.mzML,\r\n");
            var manifest = ReplicateManifest.Load(ReplicateOrderingOptions.EnabledFor(path, true, true, 2));
            TestAssert.Equal("001_Control", manifest.CreateNumberedName(manifest.Entries[0], "A"), "First usable row should have a three-digit prefix.");
            TestAssert.Equal("002_Original", manifest.CreateNumberedName(manifest.Entries[1], "007_Original"), "Blank rename values should use the original name without its prior prefix.");
            File.Delete(path);
        }

        private static void ExpandsPaddingForLargeManifests()
        {
            var path = Path.Combine(Path.GetTempPath(), "replicate-width-" + Guid.NewGuid().ToString("N") + ".csv");
            var text = new StringBuilder("Path,Name\r\n");
            for (var i = 1; i <= 1000; i++) text.Append("S").Append(i).Append(".raw,Name").Append(i).Append("\r\n");
            File.WriteAllText(path, text.ToString());
            var manifest = ReplicateManifest.Load(ReplicateOrderingOptions.EnabledFor(path, true, true, 2));
            TestAssert.Equal("0001_Name1", manifest.CreateNumberedName(manifest.Entries[0], "S1"), "Large manifests should expand numeric padding.");
            TestAssert.Equal("1000_Name1000", manifest.CreateNumberedName(manifest.Entries[999], "S1000"), "Last large-manifest prefix should remain sortable.");
            File.Delete(path);
        }
    }
}
