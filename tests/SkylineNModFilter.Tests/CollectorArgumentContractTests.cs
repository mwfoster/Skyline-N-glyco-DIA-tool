using System.Linq;

namespace SkylineNModFilter.Tests
{
    internal static class CollectorArgumentContractTests
    {
        public static void Run()
        {
            var args = CollectorArguments.Build(false, null, null, true, "samples.csv", true, true, 4, true, 50, true, "selected", 3, "Case", true);
            TestAssert.True(args.Contains("--reorder-replicates"), "Ordering-only dialog state must return ordering arguments.");
            TestAssert.True(args.Contains("--manifest-has-header"), "Header flag must be returned.");
            TestAssert.True(args.Contains("--rename-replicates"), "Rename flag must be returned.");
            TestAssert.Equal("4", args[args.ToList().IndexOf("--replicate-name-column") + 1], "Column number must be returned.");
            TestAssert.True(args.Contains("--filter-precursor-missingness"), "Missingness flag must be returned.");
            TestAssert.Equal("50", args[args.ToList().IndexOf("--max-missing-percent") + 1], "Missingness threshold must be returned.");
            TestAssert.True(args.Contains("--missingness-only") && args.Contains("--group-column") && args.Contains("--selected-group") && args.Contains("--exclude-unannotated"), "Grouped missingness arguments must be returned.");
            var all = CollectorArguments.Build(true, "human.protdb", "Human", true, "samples.tsv", false, false, 0, false, 50, false, "all", 0, null, false);
            TestAssert.True(all.Contains("--associate-proteins") && all.Contains("--reorder-replicates") && !all.Contains("--filter-precursor-missingness"), "Disabled missingness must preserve existing arguments.");
        }
    }
}
