using System;

namespace SkylineNModFilter
{
    internal sealed class ReplicateAnnotationOptions
    {
        private ReplicateAnnotationOptions(bool enabled, string metadataPath)
        { Enabled = enabled; MetadataPath = metadataPath; }

        public bool Enabled { get; private set; }
        public string MetadataPath { get; private set; }

        public static ReplicateAnnotationOptions Disabled { get { return new ReplicateAnnotationOptions(false, null); } }

        public static ReplicateAnnotationOptions EnabledFor(string metadataPath)
        {
            if (string.IsNullOrWhiteSpace(metadataPath)) throw new ArgumentException("A metadata file path is required for replicate annotations.");
            return new ReplicateAnnotationOptions(true, metadataPath);
        }
    }
}
