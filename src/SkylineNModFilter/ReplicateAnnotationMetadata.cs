using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkylineNModFilter
{
    internal sealed class ReplicateAnnotationMetadata
    {
        private readonly IDictionary<string, string[]> _valuesByKey;

        private ReplicateAnnotationMetadata(IList<string> annotationNames, IDictionary<string, string[]> valuesByKey, int duplicateKeyCount, int ignoredRowCount)
        { AnnotationNames = annotationNames; _valuesByKey = valuesByKey; DuplicateKeyCount = duplicateKeyCount; IgnoredRowCount = ignoredRowCount; }

        public IList<string> AnnotationNames { get; private set; }
        public IEnumerable<string> Keys { get { return _valuesByKey.Keys; } }
        public int DuplicateKeyCount { get; private set; }
        public int IgnoredRowCount { get; private set; }

        public bool TryGetValues(string key, out string[] values)
        { return _valuesByKey.TryGetValue(ReplicateManifest.NormalizeKey(key), out values); }

        public static ReplicateAnnotationMetadata Load(ReplicateAnnotationOptions options)
        {
            if (options == null || !options.Enabled) throw new ArgumentException("Replicate annotation import is not enabled.");
            var table = DelimitedMetadataReader.Read(options.MetadataPath, true);
            if (table.Header.Length < 2) throw new InvalidDataException("Annotation metadata must contain at least one column after the filename key.");
            var names = table.Header.Skip(1).Select(value => (value ?? string.Empty).Trim()).ToList();
            if (names.Any(string.IsNullOrWhiteSpace)) throw new InvalidDataException("Annotation metadata contains a blank annotation header.");
            var duplicateHeader = names.GroupBy(value => value, StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
            if (duplicateHeader != null) throw new InvalidDataException("Annotation metadata contains a duplicate header: " + duplicateHeader.Key);

            var valuesByKey = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            var duplicates = 0; var ignored = 0;
            foreach (var row in table.Rows)
            {
                var key = ReplicateManifest.NormalizeKey(row.Length == 0 ? null : row[0]);
                if (string.IsNullOrEmpty(key)) { ignored++; continue; }
                if (valuesByKey.ContainsKey(key)) { duplicates++; continue; }
                var values = new string[names.Count];
                for (var index = 0; index < values.Length; index++) values[index] = row.Length > index + 1 ? row[index + 1] ?? string.Empty : string.Empty;
                valuesByKey.Add(key, values);
            }
            if (valuesByKey.Count == 0) throw new InvalidDataException("The annotation metadata contains no usable filename keys: " + options.MetadataPath);
            return new ReplicateAnnotationMetadata(names, valuesByKey, duplicates, ignored);
        }
    }
}
