using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace SkylineNModFilter.Tests
{
    internal static class FilterWorkflowTests
    {
        public static void Run()
        {
            RunsOperationsInSafeOrder();
            CancelsBeforeMutationWhenOutputExists();
            DiscardsWorkingCopyAfterFailure();
            ReordersBeforeNormalizationWhenEnabled();
            FiltersMissingPrecursorsBeforeEmptyCleanup();
            MissingnessOnlySkipsSequenceFiltering();
        }

        private static void MissingnessOnlySkipsSequenceFiltering()
        {
            var fake = new RecordingDocument();
            var workflow = new FilterWorkflow(fake, delegate { return false; }, ProteinAssociationOptions.Disabled, ReplicateOrderingOptions.Disabled, PrecursorMissingnessOptions.EnabledFor(50), true);
            var result = workflow.Run(Path.Combine(Path.GetTempPath(), "baseline.sky"), false);
            TestAssert.Equal("create,missingness,empty,normalize:1,verify-settings,publish", string.Join(",", fake.Calls), "Missingness-only mode must skip sequence reading, deletion, and N-sequence verification.");
            TestAssert.True(result.OutputPath.EndsWith("baseline_missingness-filtered.sky", StringComparison.OrdinalIgnoreCase), "Missingness-only output suffix is required.");
        }

        private static void FiltersMissingPrecursorsBeforeEmptyCleanup()
        {
            var fake = new RecordingDocument();
            fake.MissingnessResult = new PrecursorMissingnessResult { Evaluated = 5, Retained = 3, Removed = 2, MaximumMissingPercent = 50 };
            var workflow = new FilterWorkflow(fake, delegate { return false; }, ProteinAssociationOptions.Disabled, ReplicateOrderingOptions.Disabled, PrecursorMissingnessOptions.EnabledFor(50));
            var result = workflow.Run(Path.Combine(Path.GetTempPath(), "missing.sky"), false);
            TestAssert.Equal("create,read,delete,missingness,empty,normalize:1,verify,publish", string.Join(",", fake.Calls), "Missing precursors must be removed before empty peptide and protein cleanup.");
            TestAssert.Equal(2, result.PrecursorMissingnessResult.Removed, "Workflow must return missingness statistics.");
        }

        private static void ReordersBeforeNormalizationWhenEnabled()
        {
            var path = Path.Combine(Path.GetTempPath(), "workflow-" + Guid.NewGuid().ToString("N") + ".fp-manifest");
            File.WriteAllText(path, "A.raw\t\t\tDIA-Quant\r\n");
            var fake = new RecordingDocument();
            fake.OrderResult = new ReplicateOrderResult { Matched = 1, Renamed = 1 };
            var workflow = new FilterWorkflow(fake, delegate { return false; }, ProteinAssociationOptions.Disabled, ReplicateOrderingOptions.EnabledFor(path, false, false, 0));
            var result = workflow.Run(Path.Combine(Path.GetTempPath(), "ordered.sky"), false);
            TestAssert.Equal("create,read,delete,empty,reorder,normalize:1,verify,publish", string.Join(",", fake.Calls), "Ordering must occur before Skyline normalization.");
            TestAssert.Equal(1, result.ReplicateOrderResult.Matched, "Workflow must return ordering statistics.");
            File.Delete(path);
        }

        private static void RunsOperationsInSafeOrder()
        {
            var fake = new RecordingDocument();
            var workflow = new FilterWorkflow(fake, delegate(string path) { return false; });
            var source = Path.Combine(Path.GetTempPath(), "workflow.sky");
            var result = workflow.Run(source, false);
            TestAssert.Equal("create,read,delete,empty,normalize:1,verify,publish", string.Join(",", fake.Calls), "Workflow order is safety-critical.");
            TestAssert.Equal(1, result.RetainedCount, "One target should remain.");
            TestAssert.Equal(1, result.RemovedCount, "One target should be removed.");
        }

        private static void CancelsBeforeMutationWhenOutputExists()
        {
            var fake = new RecordingDocument();
            var workflow = new FilterWorkflow(fake, delegate(string path) { return true; });
            var result = workflow.Run(Path.Combine(Path.GetTempPath(), "collision.sky"), false);
            TestAssert.True(result.Cancelled, "Existing output without replacement permission should cancel.");
            TestAssert.Equal(0, fake.Calls.Count, "Cancellation must occur before document access.");
        }

        private static void DiscardsWorkingCopyAfterFailure()
        {
            var fake = new RecordingDocument();
            fake.FailAtNormalize = true;
            var workflow = new FilterWorkflow(fake, delegate(string path) { return false; });
            TestAssert.Throws<InvalidOperationException>(delegate { workflow.Run(Path.Combine(Path.GetTempPath(), "failure.sky"), false); }, "Normalization failure should propagate.");
            TestAssert.Equal("discard", fake.Calls[fake.Calls.Count - 1], "A failed workflow must discard its temporary copy.");
        }

        private sealed class RecordingDocument : ISkylineDocument
        {
            public readonly List<string> Calls = new List<string>();
            public bool FailAtNormalize;
            public ReplicateOrderResult OrderResult = new ReplicateOrderResult();
            public PrecursorMissingnessResult MissingnessResult = new PrecursorMissingnessResult();

            public void CreateWorkingCopy(string sourcePath, string destinationPath) { Calls.Add("create"); }
            public IList<PeptideRecord> ReadPeptides()
            {
                Calls.Add("read");
                return new[]
                {
                    PeptideRecord.FromElement(new XElement("peptide", new XAttribute("modified_sequence", "AN[+1]BC"))),
                    PeptideRecord.FromElement(new XElement("peptide", new XAttribute("modified_sequence", "ANBC")))
                };
            }
            public void DeletePeptides(IList<PeptideRecord> peptides) { Calls.Add("delete"); }
            public PrecursorMissingnessResult ApplyPrecursorMissingnessFilter(PrecursorMissingnessOptions options) { Calls.Add("missingness"); return MissingnessResult; }
            public void RemoveEmptyContainers() { Calls.Add("empty"); }
            public ReplicateOrderResult ApplyReplicateOrdering(ReplicateManifest manifest) { Calls.Add("reorder"); return OrderResult; }
            public void NormalizeWithSkylineCmd(int maxVariableMods, ProteinAssociationOptions options) { Calls.Add("normalize:" + maxVariableMods); if (FailAtNormalize) throw new InvalidOperationException("normalize"); }
            public void Verify(string match, int maxVariableMods) { Calls.Add("verify"); }
            public void VerifySettings(int maxVariableMods) { Calls.Add("verify-settings"); }
            public void PublishWorkingCopy(string destinationPath) { Calls.Add("publish"); }
            public void DiscardWorkingCopy() { Calls.Add("discard"); }
        }
    }
}
