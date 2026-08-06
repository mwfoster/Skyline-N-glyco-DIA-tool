using System.Collections.Generic;

namespace SkylineNModFilter
{
    internal static class CollectorArguments
    {
        public static string[] Build(bool associate, string proteomeFile, string proteomeName, bool reorder, string manifestPath, bool hasHeader, bool rename, int nameColumn)
        {
            var args = new List<string>();
            if (associate)
            {
                args.Add("--associate-proteins"); args.Add("--background-proteome-file"); args.Add(proteomeFile); args.Add("--background-proteome-name"); args.Add(proteomeName);
            }
            if (reorder)
            {
                args.Add("--reorder-replicates"); args.Add("--replicate-manifest"); args.Add(manifestPath);
                if (hasHeader) args.Add("--manifest-has-header");
                if (rename) { args.Add("--rename-replicates"); args.Add("--replicate-name-column"); args.Add(nameColumn.ToString()); }
            }
            return args.ToArray();
        }
    }
}
