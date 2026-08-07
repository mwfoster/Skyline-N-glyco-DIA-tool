using System.Collections.Generic;

namespace SkylineNModFilter
{
    internal sealed class FilterPlan
    {
        private FilterPlan(List<PeptideRecord> deleteElements, int retainedCount)
        {
            DeleteElements = deleteElements.AsReadOnly();
            RetainedCount = retainedCount;
        }

        public IList<PeptideRecord> DeleteElements { get; private set; }
        public int RetainedCount { get; private set; }
        public int RemovedCount { get { return DeleteElements.Count; } }

        public static FilterPlan Create(IEnumerable<PeptideRecord> peptides)
        {
            var deleteElements = new List<PeptideRecord>();
            var retainedCount = 0;
            foreach (var peptide in peptides)
            {
                if (FilterRule.IsMatch(peptide.ModifiedSequence))
                    retainedCount++;
                else
                    deleteElements.Add(peptide);
            }

            return new FilterPlan(deleteElements, retainedCount);
        }
    }
}
