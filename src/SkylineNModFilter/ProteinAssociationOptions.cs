using System;
using System.IO;

namespace SkylineNModFilter
{
    internal sealed class ProteinAssociationOptions
    {
        private ProteinAssociationOptions(bool enabled, string filePath, string name) { Enabled = enabled; FilePath = filePath; Name = name; }
        public bool Enabled { get; private set; }
        public string FilePath { get; private set; }
        public string Name { get; private set; }
        public static ProteinAssociationOptions Disabled { get { return new ProteinAssociationOptions(false, null, null); } }
        public static ProteinAssociationOptions EnabledFor(string filePath, string name)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !string.Equals(Path.GetExtension(filePath), ".protdb", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("A .protdb path is required.");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A background proteome name is required.");
            return new ProteinAssociationOptions(true, filePath, name);
        }
    }
}
