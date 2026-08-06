using System;
using System.Collections.Generic;
using System.IO;

namespace SkylineNModFilter
{
    internal sealed class PrecursorGroupMap
    {
        private readonly IDictionary<string, string> _groupsByReplicate;
        private PrecursorGroupMap(IDictionary<string, string> groupsByReplicate) { _groupsByReplicate = groupsByReplicate; }

        public bool TryGetGroup(string replicateName, out string group)
        {
            return _groupsByReplicate.TryGetValue(ReplicateManifest.NormalizeKey(replicateName), out group) && !string.IsNullOrWhiteSpace(group);
        }

        public static PrecursorGroupMap Load(PrecursorMissingnessOptions options)
        {
            if (options == null || options.Scope == PrecursorMissingnessScope.AllReplicates) throw new ArgumentException("Grouped missingness options are required.");
            var table = DelimitedMetadataReader.Read(options.MetadataPath, options.HasHeader);
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var canonicalGroups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in table.Rows)
            {
                var key = ReplicateManifest.NormalizeKey(row.Length == 0 ? null : row[0]);
                if (string.IsNullOrWhiteSpace(key) || map.ContainsKey(key)) continue;
                var group = row.Length >= options.GroupColumn ? (row[options.GroupColumn - 1] ?? string.Empty).Trim().Trim('"').Trim() : string.Empty;
                if (group.Length > 0)
                {
                    string canonical;
                    if (!canonicalGroups.TryGetValue(group, out canonical)) { canonical = group; canonicalGroups.Add(group, group); }
                    group = canonical;
                }
                map.Add(key, group);
            }
            if (map.Count == 0) throw new InvalidDataException("The metadata file contains no usable replicate keys: " + options.MetadataPath);
            return new PrecursorGroupMap(map);
        }
    }
}
