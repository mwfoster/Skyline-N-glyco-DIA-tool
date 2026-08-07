using System;

namespace SkylineNModFilter
{
    internal enum PrecursorMissingnessScope { AllReplicates, SelectedGroup, AnyGroup }

    internal sealed class PrecursorMissingnessOptions
    {
        private PrecursorMissingnessOptions(bool enabled, int maximumMissingPercent, PrecursorMissingnessScope scope, string metadataPath, bool hasHeader, int groupColumn, string selectedGroup, bool excludeUnannotated)
        {
            Enabled = enabled;
            MaximumMissingPercent = maximumMissingPercent;
            Scope = scope; MetadataPath = metadataPath; HasHeader = hasHeader; GroupColumn = groupColumn; SelectedGroup = selectedGroup; ExcludeUnannotated = excludeUnannotated;
        }

        public bool Enabled { get; private set; }
        public int MaximumMissingPercent { get; private set; }
        public PrecursorMissingnessScope Scope { get; private set; }
        public string MetadataPath { get; private set; }
        public bool HasHeader { get; private set; }
        public int GroupColumn { get; private set; }
        public string SelectedGroup { get; private set; }
        public bool ExcludeUnannotated { get; private set; }
        public static PrecursorMissingnessOptions Disabled { get { return new PrecursorMissingnessOptions(false, 50, PrecursorMissingnessScope.AllReplicates, null, false, 0, null, false); } }

        public static PrecursorMissingnessOptions EnabledFor(int maximumMissingPercent)
        {
            if (maximumMissingPercent < 0 || maximumMissingPercent > 100) throw new ArgumentOutOfRangeException("maximumMissingPercent", "Maximum missing data must be between 0 and 100 percent.");
            return new PrecursorMissingnessOptions(true, maximumMissingPercent, PrecursorMissingnessScope.AllReplicates, null, false, 0, null, false);
        }

        public static PrecursorMissingnessOptions EnabledForGroups(int maximumMissingPercent, PrecursorMissingnessScope scope, string metadataPath, bool hasHeader, int groupColumn, string selectedGroup, bool excludeUnannotated)
        {
            if (scope == PrecursorMissingnessScope.AllReplicates) return EnabledFor(maximumMissingPercent);
            if (maximumMissingPercent < 0 || maximumMissingPercent > 100) throw new ArgumentOutOfRangeException("maximumMissingPercent", "Maximum missing data must be between 0 and 100 percent.");
            if (string.IsNullOrWhiteSpace(metadataPath)) throw new ArgumentException("A metadata file is required for group missingness filtering.", "metadataPath");
            if (groupColumn < 2) throw new ArgumentOutOfRangeException("groupColumn", "The group column must be 2 or greater.");
            if (scope == PrecursorMissingnessScope.SelectedGroup && string.IsNullOrWhiteSpace(selectedGroup)) throw new ArgumentException("A selected metadata group is required.", "selectedGroup");
            if (scope == PrecursorMissingnessScope.SelectedGroup && excludeUnannotated && string.Equals(selectedGroup, "Unannotated", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("The Unannotated group cannot be selected while unannotated replicates are excluded.");
            return new PrecursorMissingnessOptions(true, maximumMissingPercent, scope, metadataPath, hasHeader, groupColumn, selectedGroup == null ? null : selectedGroup.Trim(), excludeUnannotated);
        }
    }
}
