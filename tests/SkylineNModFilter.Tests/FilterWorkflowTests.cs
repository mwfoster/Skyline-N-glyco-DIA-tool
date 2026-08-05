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
            public void RemoveEmptyContainers() { Calls.Add("empty"); }
            public void NormalizeWithSkylineCmd(int maxVariableMods, ProteinAssociationOptions options) { Calls.Add("normalize:" + maxVariableMods); if (FailAtNormalize) throw new InvalidOperationException("normalize"); }
            public void Verify(string match, int maxVariableMods) { Calls.Add("verify"); }
            public void PublishWorkingCopy(string destinationPath) { Calls.Add("publish"); }
            public void DiscardWorkingCopy() { Calls.Add("discard"); }
        }
    }
}
