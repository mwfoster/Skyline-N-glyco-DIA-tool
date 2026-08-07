using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SkylineNModFilter.Tests
{
    internal static class FilterPlanTests
    {
        public static void Run()
        {
            SelectsOnlyNonmatchingPeptidesForDeletion();
            RejectsMissingModifiedSequence();
        }

        private static void SelectsOnlyNonmatchingPeptidesForDeletion()
        {
            var keep = new XElement("peptide", new XAttribute("modified_sequence", "AN[+1]BC"));
            var remove = new XElement("peptide", new XAttribute("modified_sequence", "ANBC"));
            var plan = FilterPlan.Create(new[]
            {
                PeptideRecord.FromElement(keep),
                PeptideRecord.FromElement(remove)
            });

            TestAssert.Equal(1, plan.RetainedCount, "One peptide should be retained.");
            TestAssert.Equal(1, plan.RemovedCount, "One peptide should be removed.");
            TestAssert.True(object.ReferenceEquals(remove, plan.DeleteElements.Single().Element),
                "The nonmatching XML element should be selected.");
        }

        private static void RejectsMissingModifiedSequence()
        {
            TestAssert.Throws<InvalidDataException>(
                delegate { PeptideRecord.FromElement(new XElement("peptide")); },
                "A missing modified_sequence must fail before mutation.");
        }
    }
}
