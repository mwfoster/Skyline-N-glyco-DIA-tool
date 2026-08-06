using System;
using System.IO;

namespace SkylineNModFilter
{
    internal static class OutputPath
    {
        public static string Derive(string sourcePath)
        {
            return Derive(sourcePath, false);
        }

        public static string Derive(string sourcePath, bool missingnessOnly)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new ArgumentException("The Skyline document must be saved before filtering.", "sourcePath");
            if (!string.Equals(Path.GetExtension(sourcePath), ".sky", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The source document must have a .sky extension.", "sourcePath");

            var source = Path.GetFullPath(sourcePath);
            var destination = Path.Combine(
                Path.GetDirectoryName(source),
                Path.GetFileNameWithoutExtension(source) + (missingnessOnly ? "_missingness-filtered.sky" : "_N-mod-filtered.sky"));
            destination = Path.GetFullPath(destination);
            if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The output path resolves to the source document.", "sourcePath");
            return destination;
        }
    }
}
