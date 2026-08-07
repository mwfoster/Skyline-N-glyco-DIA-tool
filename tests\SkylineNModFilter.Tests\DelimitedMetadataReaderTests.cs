using System;
using System.IO;

namespace SkylineNModFilter.Tests
{
    internal static class DelimitedMetadataReaderTests
    {
        public static void Run()
        {
            ParsesCsvQuotesAndHeader();
            ParsesFragPipeEmptyFields();
        }

        private static void ParsesCsvQuotesAndHeader()
        {
            var path = Path.Combine(Path.GetTempPath(), "metadata-" + Guid.NewGuid().ToString("N") + ".csv");
            File.WriteAllText(path, "Path,Group,Name\r\n\"C:\\data\\sample,1.raw\",Group A,\"Patient \"\"One\"\"\"\r\n");
            var table = DelimitedMetadataReader.Read(path, true);
            TestAssert.Equal("Path", table.Header[0], "Header should parse.");
            TestAssert.Equal(1, table.Rows.Count, "Header must not be returned as data.");
            TestAssert.Equal("C:\\data\\sample,1.raw", table.Rows[0][0], "Quoted comma must stay in the field.");
            TestAssert.Equal("Patient \"One\"", table.Rows[0][2], "Escaped quotes must parse.");
            File.Delete(path);
        }

        private static void ParsesFragPipeEmptyFields()
        {
            var path = Path.Combine(Path.GetTempPath(), "metadata-" + Guid.NewGuid().ToString("N") + ".fp-manifest");
            File.WriteAllText(path, "I:\\data\\sample.raw\t\t\tDIA-Quant\r\n");
            var table = DelimitedMetadataReader.Read(path, false);
            TestAssert.Equal(4, table.Rows[0].Length, "Consecutive tabs must preserve empty fields.");
            TestAssert.Equal(string.Empty, table.Rows[0][1], "Empty metadata field must be preserved.");
            File.Delete(path);
        }
    }
}
