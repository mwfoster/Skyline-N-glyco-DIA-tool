using System.Text;

namespace SkylineNModFilter
{
    internal static class CompletionMessage
    {
        public static string Build(FilterResult result)
        {
            var text = new StringBuilder("Filtered document created.\n\nRetained: ").Append(result.RetainedCount).Append("\nRemoved: ").Append(result.RemovedCount);
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
            return text.Append("\n\n").Append(result.OutputPath).ToString();
        }
    }
}
