using System.Collections.Generic;

namespace SkylineNModFilter
{
    internal static class CollectorArguments
    {
        public static string[] Build(bool associate, string proteomeFile, string proteomeName, bool reorder, string manifestPath, bool hasHeader, bool rename, int nameColumn, bool filterMissingness, int maximumMissingPercent, bool missingnessOnly, string missingnessScope, int groupColumn, string selectedGroup, bool excludeUnannotated, bool importAnnotations)
        {
            var args = new List<string>();
            if (associate)
            {
                args.Add("--associate-proteins"); args.Add("--background-proteome-file"); args.Add(proteomeFile); args.Add("--background-proteome-name"); args.Add(proteomeName);
            }
            var grouped = filterMissingness && missingnessScope != "all";
            if (reorder || grouped || importAnnotations)
            {
                if (reorder) args.Add("--reorder-replicates"); args.Add("--replicate-manifest"); args.Add(manifestPath);
                if (hasHeader) args.Add("--manifest-has-header");
                if (reorder && rename) { args.Add("--rename-replicates"); args.Add("--replicate-name-column"); args.Add(nameColumn.ToString()); }
            }
            if (importAnnotations) args.Add("--import-replicate-annotations");
            if (filterMissingness)
            {
                args.Add("--filter-precursor-missingness"); args.Add("--max-missing-percent"); args.Add(maximumMissingPercent.ToString());
                if (missingnessOnly) args.Add("--missingness-only");
                args.Add("--missingness-scope"); args.Add(missingnessScope);
                if (grouped) { args.Add("--group-column"); args.Add(groupColumn.ToString()); if (missingnessScope == "selected") { args.Add("--selected-group"); args.Add(selectedGroup); } if (excludeUnannotated) args.Add("--exclude-unannotated"); }
            }
            return args.ToArray();
        }
    }
}
