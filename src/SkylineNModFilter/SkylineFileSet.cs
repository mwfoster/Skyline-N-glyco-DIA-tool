using System.Collections.Generic;
using System.IO;

namespace SkylineNModFilter
{
    internal sealed class SkylineFile
    {
        public SkylineFile(string path, string suffix) { Path = path; Suffix = suffix; }
        public string Path { get; private set; }
        public string Suffix { get; private set; }
        public string DestinationFor(string finalSkyPath) { return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(finalSkyPath), System.IO.Path.GetFileNameWithoutExtension(finalSkyPath) + Suffix); }
    }

    internal sealed class SkylineFileSet
    {
        internal static readonly string[] PublicationSuffixes = { ".skyd", ".skyl", ".blib", ".redundant.blib", ".sky" };
        private SkylineFileSet(IList<SkylineFile> files) { FilesInPublicationOrder = files; }
        public IList<SkylineFile> FilesInPublicationOrder { get; private set; }

        public static SkylineFileSet Discover(string skyPath)
        {
            if (!File.Exists(skyPath)) throw new FileNotFoundException("Normalized Skyline document is missing.", skyPath);
            var stem = skyPath.Substring(0, skyPath.Length - System.IO.Path.GetExtension(skyPath).Length);
            var files = new List<SkylineFile>();
            foreach (var suffix in PublicationSuffixes)
            {
                var candidate = stem + suffix;
                if (File.Exists(candidate)) files.Add(new SkylineFile(candidate, suffix));
            }
            return new SkylineFileSet(files.AsReadOnly());
        }

        public static IEnumerable<string> ExistingRecognizedFiles(string skyPath)
        {
            var stem = skyPath.Substring(0, skyPath.Length - System.IO.Path.GetExtension(skyPath).Length);
            foreach (var suffix in PublicationSuffixes) { var candidate = stem + suffix; if (File.Exists(candidate)) yield return candidate; }
        }
    }
}
