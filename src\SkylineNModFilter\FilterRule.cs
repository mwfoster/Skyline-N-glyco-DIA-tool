using System;

namespace SkylineNModFilter
{
    internal static class FilterRule
    {
        public static bool IsMatch(string modifiedSequence)
        {
            return modifiedSequence != null &&
                   modifiedSequence.IndexOf("N[", StringComparison.Ordinal) >= 0;
        }
    }
}
