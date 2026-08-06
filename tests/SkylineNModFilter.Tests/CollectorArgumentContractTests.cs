using System.Linq;

namespace SkylineNModFilter.Tests
{
    internal static class CollectorArgumentContractTests
    {
        public static void Run()
        {
            var args = CollectorArguments.Build(false, null, null, true, "samples.csv", true, true, 4);
            TestAssert.True(args.Contains("--reorder-replicates"), "Ordering-only dialog state must return ordering arguments.");
            TestAssert.True(args.Contains("--manifest-has-header"), "Header flag must be returned.");
            TestAssert.True(args.Contains("--rename-replicates"), "Rename flag must be returned.");
            TestAssert.Equal("4", args[args.ToList().IndexOf("--replicate-name-column") + 1], "Column number must be returned.");
            var all = CollectorArguments.Build(true, "human.protdb", "Human", true, "samples.tsv", false, false, 0);
            TestAssert.True(all.Contains("--associate-proteins") && all.Contains("--reorder-replicates"), "Association and ordering arguments must compose.");
        }
    }
}
