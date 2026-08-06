namespace SkylineNModFilter.Tests
{
    internal static class CompletionMessageTests
    {
        public static void Run()
        {
            var plain = CompletionMessage.Build(new FilterResult { SequenceFilterApplied = true, RetainedCount = 3, RemovedCount = 2, OutputPath = "x.sky" });
            TestAssert.True(!plain.Contains("Matched replicates"), "Disabled ordering should not add a replicate section.");
            TestAssert.True(!plain.Contains("Precursors evaluated"), "Disabled missingness should not add a precursor section.");
            var ordered = CompletionMessage.Build(new FilterResult
            {
                SequenceFilterApplied = true, RetainedCount = 3, RemovedCount = 2, OutputPath = "x.sky",
                ReplicateOrderResult = new ReplicateOrderResult { Matched = 4, IgnoredManifest = 5, UnmatchedSkyline = 6, DuplicateManifest = 7, Renamed = 8, Unchanged = 9 }
            });
            foreach (var expected in new[] { "Matched replicates: 4", "Ignored manifest entries: 5", "Unmatched Skyline replicates: 6", "Duplicate manifest keys: 7", "Renamed replicates: 8", "Unchanged matched replicates: 9" })
                TestAssert.True(ordered.Contains(expected), "Completion message missing: " + expected);
            var missing = CompletionMessage.Build(new FilterResult { RetainedCount = 3, RemovedCount = 2, OutputPath = "x.sky", PrecursorMissingnessResult = new PrecursorMissingnessResult { Evaluated = 10, Retained = 7, Removed = 3, MaximumMissingPercent = 50, Scope = "Any group", EvaluatedGroupCount = 3, AnnotatedReplicates = 8, UnannotatedReplicates = 2, ExcludedReplicates = 2 } });
            foreach (var expected in new[] { "Precursors evaluated: 10", "Precursors retained: 7", "Precursors removed: 3", "Maximum missing data: 50%" }) TestAssert.True(missing.Contains(expected), "Completion message missing: " + expected);
            foreach (var expected in new[] { "Missingness scope: Any group", "Groups evaluated: 3", "Annotated replicates: 8", "Unannotated replicates: 2", "Excluded replicates: 2" }) TestAssert.True(missing.Contains(expected), "Grouped completion message missing: " + expected);
            TestAssert.True(!missing.Contains("Retained: 3"), "Missingness-only reporting must not show sequence-filter counts.");
        }
    }
}
