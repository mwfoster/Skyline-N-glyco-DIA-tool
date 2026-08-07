namespace SkylineNModFilter
{
    internal sealed class PrecursorMissingnessResult
    {
        public int Evaluated { get; set; }
        public int Retained { get; set; }
        public int Removed { get; set; }
        public int MaximumMissingPercent { get; set; }
        public string Scope { get; set; }
        public string SelectedGroup { get; set; }
        public int EvaluatedGroupCount { get; set; }
        public int AnnotatedReplicates { get; set; }
        public int UnannotatedReplicates { get; set; }
        public int ExcludedReplicates { get; set; }
    }
}
