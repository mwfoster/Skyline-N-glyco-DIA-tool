using System.Collections.Generic;

namespace SkylineNModFilter
{
    internal sealed class ReplicateAnnotationResult
    {
        public int AnnotatedReplicates { get; set; }
        public int UnannotatedReplicates { get; set; }
        public int UnmatchedMetadataRows { get; set; }
        public int DuplicateMetadataKeys { get; set; }
        public int IgnoredMetadataRows { get; set; }
        public IList<string> MissingDefinitions { get; set; }
        public IList<string> ExistingDefinitions { get; set; }
        public IList<string> AnnotationNames { get; set; }
        public IDictionary<string, string[]> ExpectedValuesByReplicate { get; set; }
        public string ImportPath { get; set; }
    }
}
