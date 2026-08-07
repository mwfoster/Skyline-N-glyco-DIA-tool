using System;
using System.IO;

namespace SkylineNModFilter.Tests
{
    internal static class ReplicateManifestTests
    {
        public static void Run()
        {
            NormalizesPathsAndKeepsFirstDuplicate();
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
            TestAssert.Equal(null, manifest.Entries[1].ProposedName, "Blank rename should preserve original name.");
            TestAssert.Equal(1, manifest.DuplicateCount, "Later duplicate keys should be counted.");
            TestAssert.Equal(1, manifest.IgnoredRowCount, "Blank keys should be counted.");
            File.Delete(path);
        }
    }
}
