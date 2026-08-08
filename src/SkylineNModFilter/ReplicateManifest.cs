using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

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
        private static readonly Regex ExistingOrderPrefix = new Regex(@"^\d+_", RegexOptions.CultureInvariant);

        private ReplicateManifest(IList<ReplicateManifestEntry> entries, int duplicates, int ignored)
        { Entries = entries; DuplicateCount = duplicates; IgnoredRowCount = ignored; NumberWidth = Math.Max(3, entries.Count.ToString().Length); }
        public IList<ReplicateManifestEntry> Entries { get; private set; }
        public int DuplicateCount { get; private set; }
        public int IgnoredRowCount { get; private set; }
        public int NumberWidth { get; private set; }

        public string CreateNumberedName(ReplicateManifestEntry entry, string originalName)
        {
            if (entry == null) throw new ArgumentNullException("entry");
            var selected = string.IsNullOrWhiteSpace(entry.ProposedName) ? originalName ?? string.Empty : entry.ProposedName.Trim();
            selected = ExistingOrderPrefix.Replace(selected, string.Empty);
            return (entry.Order + 1).ToString("D" + NumberWidth) + "_" + selected;
        }

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
            foreach (var extension in new[] { ".raw", ".mzml" })
                if (cleaned.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    return cleaned.Substring(0, cleaned.Length - extension.Length).Trim();
            return cleaned.Trim();
        }

        private static string Clean(string value) { return (value ?? string.Empty).Trim().Trim('"').Trim(); }
    }
}
