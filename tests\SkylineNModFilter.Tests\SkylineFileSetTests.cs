using System;
using System.IO;
using System.Linq;

namespace SkylineNModFilter.Tests
{
    internal static class SkylineFileSetTests
    {
        public static void Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "SkylineFileSet", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var sky = Path.Combine(root, "work.sky");
            foreach (var suffix in new[] { ".sky", ".skyd", ".skyl", ".blib", ".redundant.blib", ".unknown" }) File.WriteAllText(Path.Combine(root, "work" + suffix), suffix);
            var set = SkylineFileSet.Discover(sky);
            TestAssert.Equal(5, set.FilesInPublicationOrder.Count, "Only recognized Skyline files should be discovered.");
            TestAssert.Equal(".sky", set.FilesInPublicationOrder.Last().Suffix, "The .sky file must publish last.");
            var redundant = set.FilesInPublicationOrder.Single(f => f.Suffix == ".redundant.blib");
            TestAssert.Equal(Path.Combine(root, "final.redundant.blib"), redundant.DestinationFor(Path.Combine(root, "final.sky")), "Compound suffix must be preserved.");
            Directory.Delete(root, true);
        }
    }
}
