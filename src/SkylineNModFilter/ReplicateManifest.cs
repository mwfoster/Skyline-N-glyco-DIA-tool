using System;
using System.Collections.Generic;
using System.IO;

namespace SkylineNModFilter
{
    internal sealed class ReplicateManifestEntry
    {
        public ReplicateManifestEntry(string key, string proposedName, int order) { Key = key; ProposedName = proposedName; Order = order; }
        public string Key { get; private set; }
        public string ProposedName { get; private set; }
        public int Order { get; private set; }
    }

    internal sealed class ReplicateManifest
    {
        private ReplicateManifest(IList<ReplicateManifestEntry> entries, int duplicates, int ignored)
        { Entries = entries; DuplicateCount = duplicates; IgnoredRowCount = ignored; }
        public IList<ReplicateManifestEntry> Entries { get; private set; }
        public int DuplicateCount { get; private set; }
        public int IgnoredRowCount { get; private set; }

        public static ReplicateManifest Load(ReplicateOrderingOptions options)
        {
            if (options == null || !options.Enabled) throw new ArgumentException("Replicate ordering is not enabled.");
            var table = DelimitedMetadataReader.Read(options.ManifestPath, options.HasHeader);
            var entries = new List<ReplicateManifestEntry>();
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var duplicates = 0; var ignored = 0;
            foreach (var row in table.Rows)
            {
                var key = NormalizeKey(row.Length == 0 ? null : row[0]);
                if (string.IsNullOrEmpty(key)) { ignored++; continue; }
                if (!keys.Add(key)) { duplicates++; continue; }
                string proposed = null;
                if (options.Rename && row.Length >= options.NameColumn)
                {
                    proposed = Clean(row[options.NameColumn - 1]);
                    if (proposed.Length == 0) proposed = null;
                }
                entries.Add(new ReplicateManifestEntry(key, proposed, entries.Count));
            }
            if (entries.Count == 0) throw new InvalidDataException("The replicate manifest contains no usable filename keys: " + options.ManifestPath);
            return new ReplicateManifest(entries, duplicates, ignored);
        }

        internal static string NormalizeKey(string value)
        {
            var cleaned = Clean(value).Replace('\\', '/');
            var slash = cleaned.LastIndexOf('/');
            if (slash >= 0) cleaned = cleaned.Substring(slash + 1);
            if (cleaned.EndsWith(".raw", StringComparison.OrdinalIgnoreCase)) cleaned = cleaned.Substring(0, cleaned.Length - 4);
            return cleaned.Trim();
        }

        private static string Clean(string value) { return (value ?? string.Empty).Trim().Trim('"').Trim(); }
    }
}
