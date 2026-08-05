using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkylineNModFilter
{
    internal interface IBackgroundProteomeFastaExporter
    {
        TemporaryFasta Export(string protdbPath);
    }

    internal sealed class TemporaryFasta : IDisposable
    {
        public TemporaryFasta(string path) { Path = path; }
        public string Path { get; private set; }

        public void Dispose()
        {
            try
            {
                if (!string.IsNullOrEmpty(Path) && File.Exists(Path)) File.Delete(Path);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    internal sealed class BackgroundProteomeFastaExporter : IBackgroundProteomeFastaExporter
    {
        public TemporaryFasta Export(string protdbPath)
        {
            var fastaPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SkylineNModFilter-" + Guid.NewGuid().ToString("N") + ".fasta");
            try
            {
                if (string.IsNullOrWhiteSpace(protdbPath) || !File.Exists(protdbPath))
                    throw new InvalidDataException("The background proteome cannot be read: " + protdbPath);
                using (var connection = new SQLiteConnection("Data Source=" + protdbPath + ";Read Only=True;FailIfMissing=True;"))
                {
                    connection.Open();
                    RequireColumns(connection, protdbPath, "ProteomeDbProtein", "Id", "Sequence");
                    RequireColumns(connection, protdbPath, "ProteomeDbProteinName", "Id", "Protein", "IsPrimary", "Name");
                    var records = ReadRecords(connection);
                    if (records.Count == 0)
                        throw new InvalidDataException("The background proteome contains no valid protein sequences: " + protdbPath);
                    using (var writer = new StreamWriter(fastaPath))
                    {
                        foreach (var record in records)
                        {
                            writer.WriteLine(">" + SanitizeHeader(record.Name));
                            for (var offset = 0; offset < record.Sequence.Length; offset += 60)
                                writer.WriteLine(record.Sequence.Substring(offset, Math.Min(60, record.Sequence.Length - offset)));
                        }
                    }
                }
                return new TemporaryFasta(fastaPath);
            }
            catch (InvalidDataException)
            {
                if (File.Exists(fastaPath)) File.Delete(fastaPath);
                throw;
            }
            catch (Exception exception)
            {
                if (File.Exists(fastaPath)) File.Delete(fastaPath);
                throw new InvalidDataException("The background proteome could not be converted to FASTA: " + protdbPath, exception);
            }
        }

        private static void RequireColumns(SQLiteConnection connection, string path, string table, params string[] required)
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info([" + table.Replace("]", "]]" ) + "])";
                using (var reader = command.ExecuteReader()) while (reader.Read()) columns.Add(Convert.ToString(reader["name"]));
            }
            var missing = required.Where(column => !columns.Contains(column)).ToArray();
            if (missing.Length > 0)
                throw new InvalidDataException("The background proteome has an unsupported schema (" + table + "." + string.Join(", ", missing) + "): " + path);
        }

        private static List<ProteinRecord> ReadRecords(SQLiteConnection connection)
        {
            var records = new List<ProteinRecord>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT p.Id, p.Sequence, COALESCE((SELECT pn.Name FROM ProteomeDbProteinName pn WHERE pn.Protein = p.Id AND pn.Name IS NOT NULL AND TRIM(pn.Name) <> '' ORDER BY pn.IsPrimary DESC, pn.Id LIMIT 1), 'protein_' || p.Id) AS ProteinName FROM ProteomeDbProtein p ORDER BY p.Id";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var sequence = Convert.ToString(reader["Sequence"]).Trim().ToUpperInvariant();
                        if (sequence.Length == 0 || sequence.Any(character => character < 'A' || character > 'Z') || !seen.Add(sequence)) continue;
                        records.Add(new ProteinRecord(Convert.ToString(reader["ProteinName"]), sequence));
                    }
                }
            }
            return records;
        }

        private static string SanitizeHeader(string header)
        {
            var sanitized = (header ?? string.Empty).Trim().Replace('\r', '_').Replace('\n', '_').Replace('>', '_');
            return sanitized.Length == 0 ? "protein" : sanitized;
        }

        private sealed class ProteinRecord
        {
            public ProteinRecord(string name, string sequence) { Name = name; Sequence = sequence; }
            public string Name { get; private set; }
            public string Sequence { get; private set; }
        }
    }
}
