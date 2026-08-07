using System;
using System.Data.SQLite;
using System.IO;

namespace SkylineNModFilter.Tests
{
    internal static class BackgroundProteomeFastaExporterTests
    {
        public static void Run()
        {
            ExportsProteinAndDeletesTemporaryFasta();
            NormalizesDeduplicatesAndWrapsRecords();
            RejectsUnsupportedOrEmptyDatabases();
            CleanupFailureDoesNotThrow();
        }

        private static void CleanupFailureDoesNotThrow()
        {
            var path = Path.Combine(Path.GetTempPath(), "SkylineNModFilter-locked-" + Guid.NewGuid().ToString("N") + ".fasta");
            File.WriteAllText(path, ">test" + Environment.NewLine + "PEPTIDE");
            using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                new TemporaryFasta(path).Dispose();
            TestAssert.True(File.Exists(path), "A locked temporary file may remain for operating-system cleanup.");
            File.Delete(path);
        }

        private static void NormalizesDeduplicatesAndWrapsRecords()
        {
            var root = Path.Combine(Path.GetTempPath(), "SkylineProtdbTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var databasePath = Path.Combine(root, "edge.protdb");
            CreateDatabase(databasePath,
                "INSERT INTO ProteomeDbProtein VALUES (1, '  " + new string('a', 65) + "  ');" +
                "INSERT INTO ProteomeDbProtein VALUES (2, '" + new string('A', 65) + "');" +
                "INSERT INTO ProteomeDbProtein VALUES (3, '   ');" +
                "INSERT INTO ProteomeDbProteinName VALUES (1, 1, 1, 'first>name' || char(10) || 'line');" +
                "INSERT INTO ProteomeDbProteinName VALUES (2, 2, 1, 'duplicate');");

            using (var fasta = new BackgroundProteomeFastaExporter().Export(databasePath))
            {
                var expected = ">first_name_line" + Environment.NewLine + new string('A', 60) + Environment.NewLine + new string('A', 5) + Environment.NewLine;
                TestAssert.Equal(expected, File.ReadAllText(fasta.Path), "Exporter must normalize, wrap, and deduplicate protein records.");
            }
            Directory.Delete(root, true);
        }

        private static void RejectsUnsupportedOrEmptyDatabases()
        {
            var root = Path.Combine(Path.GetTempPath(), "SkylineProtdbTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var unsupportedPath = Path.Combine(root, "unsupported.protdb");
            SQLiteConnection.CreateFile(unsupportedPath);
            var unsupported = TestAssert.Throws<InvalidDataException>(delegate { new BackgroundProteomeFastaExporter().Export(unsupportedPath); }, "Unsupported schema must fail clearly.");
            TestAssert.True(unsupported.Message.Contains(unsupportedPath), "Schema error must name the selected proteome.");

            var emptyPath = Path.Combine(root, "empty.protdb");
            CreateDatabase(emptyPath, "INSERT INTO ProteomeDbProtein VALUES (1, ' ');");
            TestAssert.Throws<InvalidDataException>(delegate { new BackgroundProteomeFastaExporter().Export(emptyPath); }, "A proteome without sequences must fail.");
            Directory.Delete(root, true);
        }

        private static void CreateDatabase(string databasePath, string inserts)
        {
            SQLiteConnection.CreateFile(databasePath);
            using (var connection = new SQLiteConnection("Data Source=" + databasePath))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "CREATE TABLE ProteomeDbProtein (Id INTEGER PRIMARY KEY, Sequence TEXT NOT NULL);" +
                                          "CREATE TABLE ProteomeDbProteinName (Id INTEGER PRIMARY KEY, Protein INTEGER NOT NULL, IsPrimary INTEGER NOT NULL, Name TEXT);" + inserts;
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void ExportsProteinAndDeletesTemporaryFasta()
        {
            var root = Path.Combine(Path.GetTempPath(), "SkylineProtdbTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var databasePath = Path.Combine(root, "minimal.protdb");
            SQLiteConnection.CreateFile(databasePath);
            using (var connection = new SQLiteConnection("Data Source=" + databasePath))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "CREATE TABLE ProteomeDbProtein (Id INTEGER PRIMARY KEY, Sequence TEXT NOT NULL);" +
                                          "CREATE TABLE ProteomeDbProteinName (Id INTEGER PRIMARY KEY, Protein INTEGER NOT NULL, IsPrimary INTEGER NOT NULL, Name TEXT);" +
                                          "INSERT INTO ProteomeDbProtein VALUES (1, 'MPEPTIDESEQ');" +
                                          "INSERT INTO ProteomeDbProteinName VALUES (1, 1, 1, 'sp|P00001|TEST_PROTEIN');";
                    command.ExecuteNonQuery();
                }
            }

            string fastaPath;
            using (var fasta = new BackgroundProteomeFastaExporter().Export(databasePath))
            {
                fastaPath = fasta.Path;
                TestAssert.True(File.Exists(fastaPath), "Exporter must create a temporary FASTA.");
                TestAssert.Equal(">sp|P00001|TEST_PROTEIN" + Environment.NewLine + "MPEPTIDESEQ" + Environment.NewLine,
                    File.ReadAllText(fastaPath), "FASTA must contain the primary protein name and sequence.");
            }
            TestAssert.True(!File.Exists(fastaPath), "Disposing the temporary FASTA must delete it.");
            Directory.Delete(root, true);
        }
    }
}
