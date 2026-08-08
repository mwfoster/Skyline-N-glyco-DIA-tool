using System.Linq;

namespace SkylineNModFilter.Tests
{
    internal static class CollectorArgumentContractTests
    {
        public static void Run()
        {
            TestAssert.Equal("Number matched replicates for manual ordering", ReplicateNamingText.EnableLabel, "The dialog must not claim to reorder Skyline results.");
            TestAssert.Equal("Use selected column after number prefix", ReplicateNamingText.RenameLabel, "The rename option must explain the numbered prefix.");
            var args = CollectorArguments.Build(false, null, null, true, "samples.csv", true, true, 4, true, 50, true, "selected", 3, "Case", true, true);
            TestAssert.True(args.Contains("--reorder-replicates"), "Numbering dialog state must retain the legacy compatibility argument.");
            TestAssert.True(args.Contains("--manifest-has-header"), "Header flag must be returned.");
            TestAssert.True(args.Contains("--rename-replicates"), "Rename flag must be returned.");
            TestAssert.Equal("4", args[args.ToList().IndexOf("--replicate-name-column") + 1], "Column number must be returned.");
            TestAssert.True(args.Contains("--filter-precursor-missingness"), "Missingness flag must be returned.");
            TestAssert.Equal("50", args[args.ToList().IndexOf("--max-missing-percent") + 1], "Missingness threshold must be returned.");
            TestAssert.True(args.Contains("--missingness-only") && args.Contains("--group-column") && args.Contains("--selected-group") && args.Contains("--exclude-unannotated"), "Grouped missingness arguments must be returned.");
            TestAssert.True(args.Contains("--import-replicate-annotations"), "Annotation import checkbox must return its command-line flag.");
            var all = CollectorArguments.Build(true, "human.protdb", "Human", true, "samples.tsv", false, false, 0, false, 50, false, "all", 0, null, false, false);
            TestAssert.True(all.Contains("--associate-proteins") && all.Contains("--reorder-replicates") && !all.Contains("--filter-precursor-missingness"), "Disabled missingness must preserve existing arguments.");
        }
    }
}
