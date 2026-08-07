using System.Text;

namespace SkylineNModFilter
{
    internal static class CompletionMessage
    {
        public static string Build(FilterResult result)
        {
            var text = new StringBuilder("Filtered document created.");
            if (result.SequenceFilterApplied) text.Append("\n\nPeptides retained: ").Append(result.RetainedCount).Append("\nPeptides removed: ").Append(result.RemovedCount);
            var order = result.ReplicateOrderResult;
            if (order != null)
            {
                text.Append("\n\nMatched replicates: ").Append(order.Matched)
                    .Append("\nIgnored manifest entries: ").Append(order.IgnoredManifest)
                    .Append("\nUnmatched Skyline replicates: ").Append(order.UnmatchedSkyline)
                    .Append("\nDuplicate manifest keys: ").Append(order.DuplicateManifest)
                    .Append("\nRenamed replicates: ").Append(order.Renamed)
                    .Append("\nUnchanged matched replicates: ").Append(order.Unchanged);
            }
            var missingness = result.PrecursorMissingnessResult;
            if (missingness != null)
            {
                text.Append("\n\nPrecursors evaluated: ").Append(missingness.Evaluated)
                    .Append("\nPrecursors retained: ").Append(missingness.Retained)
                    .Append("\nPrecursors removed: ").Append(missingness.Removed)
                    .Append("\nMaximum missing data: ").Append(missingness.MaximumMissingPercent).Append('%')
                    .Append("\nMissingness scope: ").Append(missingness.Scope)
                    .Append("\nGroups evaluated: ").Append(missingness.EvaluatedGroupCount);
                if (missingness.Scope != "All replicates") text.Append("\nAnnotated replicates: ").Append(missingness.AnnotatedReplicates)
                    .Append("\nUnannotated replicates: ").Append(missingness.UnannotatedReplicates)
                    .Append("\nExcluded replicates: ").Append(missingness.ExcludedReplicates);
                if (!string.IsNullOrWhiteSpace(missingness.SelectedGroup)) text.Append("\nSelected group: ").Append(missingness.SelectedGroup);
            }
            var annotations = result.ReplicateAnnotationResult;
            if (annotations != null)
            {
                text.Append("\n\nReplicates annotated: ").Append(annotations.AnnotatedReplicates)
                    .Append("\nReplicates left unannotated: ").Append(annotations.UnannotatedReplicates)
                    .Append("\nUnmatched metadata rows: ").Append(annotations.UnmatchedMetadataRows)
                    .Append("\nDuplicate metadata keys: ").Append(annotations.DuplicateMetadataKeys)
                    .Append("\nIgnored metadata rows: ").Append(annotations.IgnoredMetadataRows);
                if (annotations.MissingDefinitions != null && annotations.MissingDefinitions.Count > 0) text.Append("\nAnnotation definitions created: ").Append(string.Join(", ", annotations.MissingDefinitions));
                if (annotations.ExistingDefinitions != null && annotations.ExistingDefinitions.Count > 0) text.Append("\nExisting annotation definitions used: ").Append(string.Join(", ", annotations.ExistingDefinitions));
            }
            return text.Append("\n\n").Append(result.OutputPath).ToString();
        }
    }
}
