using System;
using System.IO;

namespace SkylineNModFilter.Tests
{
    internal static class OutputPathTests
    {
        public static void Run()
        {
            var source = Path.Combine(Path.GetTempPath(), "sample.sky");
            var expected = Path.Combine(Path.GetTempPath(), "sample_N-mod-filtered.sky");
            TestAssert.Equal(Path.GetFullPath(expected), OutputPath.Derive(source), "Output suffix should be deterministic.");
            TestAssert.Equal(Path.GetFullPath(Path.Combine(Path.GetTempPath(), "sample_missingness-filtered.sky")), OutputPath.Derive(source, true), "Missingness-only suffix should be deterministic.");
            TestAssert.Throws<ArgumentException>(delegate { OutputPath.Derive(null); }, "Null path should fail.");
            TestAssert.Throws<ArgumentException>(delegate { OutputPath.Derive(string.Empty); }, "Unsaved path should fail.");
            TestAssert.Throws<ArgumentException>(delegate { OutputPath.Derive("sample.txt"); }, "Non-Skyline path should fail.");
        }
    }
}
