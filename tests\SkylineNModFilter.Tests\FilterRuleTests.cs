namespace SkylineNModFilter.Tests
{
    internal static class FilterRuleTests
    {
        public static void Run()
        {
            TestAssert.True(FilterRule.IsMatch("PEPTN[+0.984]IDE"), "Modified N should match.");
            TestAssert.True(FilterRule.IsMatch("AN[Label:13C]BC"), "Named N modification should match.");
            TestAssert.False(FilterRule.IsMatch("PEPTNIDE"), "Unmodified N should not match.");
            TestAssert.False(FilterRule.IsMatch("PEPTn[+0.984]IDE"), "Matching should be case-sensitive.");
            TestAssert.False(FilterRule.IsMatch(string.Empty), "Empty sequence should not match.");
            TestAssert.False(FilterRule.IsMatch(null), "Null sequence should not match.");
        }
    }
}
