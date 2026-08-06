using System;

namespace SkylineNModFilter
{
    internal sealed class ToolArguments
    {
        public string DocumentPath { get; private set; }
        public string SkylineCommand { get; private set; }
        public ProteinAssociationOptions AssociationOptions { get; private set; }
        public ReplicateOrderingOptions ReplicateOrderingOptions { get; private set; }
        public PrecursorMissingnessOptions PrecursorMissingnessOptions { get; private set; }
        public bool MissingnessOnly { get; private set; }

        public static ToolArguments Parse(string[] args)
        {
            var result = new ToolArguments();
            var associate = false; string proteomeFile = null; string proteomeName = null;
            var reorder = false; var hasHeader = false; var rename = false; string manifestPath = null; int nameColumn = 0; var nameColumnSeen = false;
            var filterMissingness = false; var maximumMissingPercent = 50; var maximumMissingSeen = false; var maximumMissingValid = true;
            var scope = PrecursorMissingnessScope.AllReplicates; var scopeSeen = false; var groupColumn = 0; var groupColumnSeen = false; string selectedGroup = null; var excludeUnannotated = false;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "--document-path" && i + 1 < args.Length) result.DocumentPath = args[++i];
                else if (args[i] == "--skyline-command" && i + 1 < args.Length) result.SkylineCommand = args[++i];
                else if (args[i] == "--associate-proteins") associate = true;
                else if (args[i] == "--background-proteome-file" && i + 1 < args.Length) proteomeFile = args[++i];
                else if (args[i] == "--background-proteome-name" && i + 1 < args.Length) proteomeName = args[++i];
                else if (args[i] == "--reorder-replicates") reorder = true;
                else if (args[i] == "--replicate-manifest" && i + 1 < args.Length) manifestPath = args[++i];
                else if (args[i] == "--manifest-has-header") hasHeader = true;
                else if (args[i] == "--rename-replicates") rename = true;
                else if (args[i] == "--replicate-name-column" && i + 1 < args.Length) { nameColumnSeen = int.TryParse(args[++i], out nameColumn); }
                else if (args[i] == "--filter-precursor-missingness") filterMissingness = true;
                else if (args[i] == "--missingness-only") result.MissingnessOnly = true;
                else if (args[i] == "--max-missing-percent" && i + 1 < args.Length) { maximumMissingSeen = true; maximumMissingValid = int.TryParse(args[++i], out maximumMissingPercent); }
                else if (args[i] == "--missingness-scope" && i + 1 < args.Length) { scopeSeen = true; var value = args[++i]; if (value == "all") scope = PrecursorMissingnessScope.AllReplicates; else if (value == "selected") scope = PrecursorMissingnessScope.SelectedGroup; else if (value == "any") scope = PrecursorMissingnessScope.AnyGroup; else throw new ArgumentException("Missingness scope must be all, selected, or any."); }
                else if (args[i] == "--group-column" && i + 1 < args.Length) { groupColumnSeen = int.TryParse(args[++i], out groupColumn); }
                else if (args[i] == "--selected-group" && i + 1 < args.Length) selectedGroup = args[++i];
                else if (args[i] == "--exclude-unannotated") excludeUnannotated = true;
            }
            if (string.IsNullOrWhiteSpace(result.DocumentPath)) throw new ArgumentException("A saved Skyline document path is required.");
            result.AssociationOptions = associate ? ProteinAssociationOptions.EnabledFor(proteomeFile, proteomeName) : ProteinAssociationOptions.Disabled;
            var groupedMissingness = filterMissingness && scope != PrecursorMissingnessScope.AllReplicates;
            if (!reorder && !groupedMissingness && (hasHeader || !string.IsNullOrWhiteSpace(manifestPath))) throw new ArgumentException("Metadata file options require replicate ordering or grouped missingness filtering.");
            if (!reorder && (rename || nameColumnSeen)) throw new ArgumentException("Replicate rename options require --reorder-replicates.");
            if (rename && (!nameColumnSeen || nameColumn < 2)) throw new ArgumentException("Renaming requires a replicate name column of 2 or greater.");
            result.ReplicateOrderingOptions = reorder ? ReplicateOrderingOptions.EnabledFor(manifestPath, hasHeader, rename, nameColumn) : ReplicateOrderingOptions.Disabled;
            if (!filterMissingness && maximumMissingSeen) throw new ArgumentException("Maximum missing data requires --filter-precursor-missingness.");
            if (!filterMissingness && (result.MissingnessOnly || scopeSeen || groupColumnSeen || selectedGroup != null || excludeUnannotated)) throw new ArgumentException("Missingness mode and group options require --filter-precursor-missingness.");
            if (result.MissingnessOnly && !filterMissingness) throw new ArgumentException("Missingness-only mode requires --filter-precursor-missingness.");
            if (maximumMissingSeen && !maximumMissingValid) throw new ArgumentException("Maximum missing data must be a whole number between 0 and 100 percent.");
            if (filterMissingness && maximumMissingSeen && (maximumMissingPercent < 0 || maximumMissingPercent > 100)) throw new ArgumentException("Maximum missing data must be between 0 and 100 percent.");
            if (filterMissingness && !maximumMissingSeen) maximumMissingPercent = 50;
            if (groupedMissingness && (!groupColumnSeen || groupColumn < 2 || string.IsNullOrWhiteSpace(manifestPath))) throw new ArgumentException("Grouped missingness requires a metadata file and group column of 2 or greater.");
            if (filterMissingness && !groupedMissingness && (groupColumnSeen || selectedGroup != null || excludeUnannotated)) throw new ArgumentException("Group options require selected-group or any-group missingness scope.");
            if (scope == PrecursorMissingnessScope.SelectedGroup && string.IsNullOrWhiteSpace(selectedGroup)) throw new ArgumentException("Selected-group missingness requires --selected-group.");
            result.PrecursorMissingnessOptions = !filterMissingness ? PrecursorMissingnessOptions.Disabled : groupedMissingness ? PrecursorMissingnessOptions.EnabledForGroups(maximumMissingPercent, scope, manifestPath, hasHeader, groupColumn, selectedGroup, excludeUnannotated) : PrecursorMissingnessOptions.EnabledFor(maximumMissingPercent);
            return result;
        }
    }
}
