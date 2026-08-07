namespace SkylineNModFilter.Tests
{
    internal static class CompletionMessageTests
    {
        public static void Run()
        {
            var plain = CompletionMessage.Build(new FilterResult { RetainedCount = 3, RemovedCount = 2, OutputPath = "x.sky" });
            TestAssert.True(!plain.Contains("Matched replicates"), "Disabled ordering should not add a replicate section.");
            var ordered = CompletionMessage.Build(new FilterResult
            {
                RetainedCount = 3, RemovedCount = 2, OutputPath = "x.sky",
                ReplicateOrderResult = new ReplicateOrderResult { Matched = 4, IgnoredManifest = 5, UnmatchedSkyline = 6, DuplicateManifest = 7, Renamed = 8, Unchanged = 9 }
            });
            foreach (var expected in new[] { "Matched replicates: 4", "Ignored manifest entries: 5", "Unmatched Skyline replicates: 6", "Duplicate manifest keys: 7", "Renamed replicates: 8", "Unchanged matched replicates: 9" })
                TestAssert.True(ordered.Contains(expected), "Completion message missing: " + expected);
        }
    }
}
