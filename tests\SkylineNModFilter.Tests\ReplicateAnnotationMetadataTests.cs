using System;
using System.IO;

namespace SkylineNModFilter.Tests
{
    internal static class ReplicateAnnotationMetadataTests
    {
        public static void Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "SkylineAnnotations", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var path = Path.Combine(root, "samples.csv");
                File.WriteAllText(path, "File,Condition,Batch\r\nC:\\raw\\A.raw,Control,1\r\nB.RAW,Treated\r\nA.raw,Ignored,9\r\n");
                var metadata = ReplicateAnnotationMetadata.Load(ReplicateAnnotationOptions.EnabledFor(path));
                TestAssert.Equal("Condition,Batch", string.Join(",", metadata.AnnotationNames), "All columns after the key must become annotations in source order.");
                string[] values;
                TestAssert.True(metadata.TryGetValues("a", out values), "Keys must match case-insensitively after path and .raw normalization.");
                TestAssert.Equal("Control,1", string.Join(",", values), "The first duplicate metadata key must win.");
                TestAssert.True(metadata.TryGetValues("B", out values), "Uppercase .RAW must normalize.");
                TestAssert.Equal("Treated,", string.Join(",", values), "Short rows must be padded with blank annotation values.");
                TestAssert.Equal(1, metadata.DuplicateKeyCount, "Later duplicate keys must be counted.");

                File.WriteAllText(path, "File,Condition,condition\r\nA.raw,C,T\r\n");
                TestAssert.Throws<InvalidDataException>(delegate { ReplicateAnnotationMetadata.Load(ReplicateAnnotationOptions.EnabledFor(path)); }, "Duplicate annotation headers must fail case-insensitively.");
                File.WriteAllText(path, "File,Condition, \r\nA.raw,C,T\r\n");
                TestAssert.Throws<InvalidDataException>(delegate { ReplicateAnnotationMetadata.Load(ReplicateAnnotationOptions.EnabledFor(path)); }, "Blank annotation headers must fail.");
            }
            finally { Directory.Delete(root, true); }
        }
    }
}
