using System;

namespace SkylineNModFilter
{
    internal sealed class ReplicateOrderingOptions
    {
        private ReplicateOrderingOptions(bool enabled, string manifestPath, bool hasHeader, bool rename, int nameColumn)
        { Enabled = enabled; ManifestPath = manifestPath; HasHeader = hasHeader; Rename = rename; NameColumn = nameColumn; }
        public bool Enabled { get; private set; }
        public string ManifestPath { get; private set; }
        public bool HasHeader { get; private set; }
        public bool Rename { get; private set; }
        public int NameColumn { get; private set; }
        public static ReplicateOrderingOptions Disabled { get { return new ReplicateOrderingOptions(false, null, false, false, 0); } }
        public static ReplicateOrderingOptions EnabledFor(string path, bool hasHeader, bool rename, int nameColumn)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A replicate manifest path is required.");
            if (rename && nameColumn < 2) throw new ArgumentException("The replicate name column must be 2 or greater.");
            return new ReplicateOrderingOptions(true, path, hasHeader, rename, rename ? nameColumn : 0);
        }
    }
}
