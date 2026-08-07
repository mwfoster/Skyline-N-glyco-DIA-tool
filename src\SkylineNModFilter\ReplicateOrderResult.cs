namespace SkylineNModFilter
{
    internal sealed class ReplicateOrderResult
    {
        public int Matched { get; set; }
        public int IgnoredManifest { get; set; }
        public int UnmatchedSkyline { get; set; }
        public int DuplicateManifest { get; set; }
        public int Renamed { get; set; }
        public int Unchanged { get; set; }
    }
}
