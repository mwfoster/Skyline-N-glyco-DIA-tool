using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic.FileIO;

namespace SkylineNModFilter
{
    internal sealed class DelimitedMetadataTable
    {
        public DelimitedMetadataTable(string[] header, IList<string[]> rows) { Header = header; Rows = rows; }
        public string[] Header { get; private set; }
        public IList<string[]> Rows { get; private set; }
    }

    internal static class DelimitedMetadataReader
    {
        public static DelimitedMetadataTable Read(string path, bool hasHeader)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) throw new InvalidDataException("The metadata file cannot be read: " + path);
            var rows = new List<string[]>();
            string[] header = null;
            try
            {
                using (var parser = new TextFieldParser(path))
                {
                    parser.SetDelimiters(string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase) ? "," : "\t");
                    parser.HasFieldsEnclosedInQuotes = true;
                    parser.TrimWhiteSpace = false;
                    while (!parser.EndOfData)
                    {
                        var fields = parser.ReadFields();
                        if (fields == null || fields.All(string.IsNullOrWhiteSpace)) continue;
                        if (hasHeader && header == null) header = fields;
                        else rows.Add(fields);
                    }
                }
            }
            catch (MalformedLineException exception)
            {
                throw new InvalidDataException("Malformed metadata record at row " + exception.LineNumber + ": " + path, exception);
            }
            if (hasHeader && header == null) throw new InvalidDataException("The metadata file does not contain a nonblank header row: " + path);
            return new DelimitedMetadataTable(header, rows);
        }
    }
}
