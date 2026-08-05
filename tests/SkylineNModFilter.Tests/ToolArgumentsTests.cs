using System;

namespace SkylineNModFilter.Tests
{
    internal static class ToolArgumentsTests
    {
        public static void Run()
        {
            var parsed = ToolArguments.Parse(new[] { "--document-path", "C:\\data files\\sample.sky", "--skyline-command", "C:\\Skyline\\SkylineCmd.exe" });
            TestAssert.Equal("C:\\data files\\sample.sky", parsed.DocumentPath, "Document path should parse.");
            TestAssert.Throws<ArgumentException>(delegate { ToolArguments.Parse(new string[0]); }, "Missing document path should fail.");
            var associated = ToolArguments.Parse(new[] { "--document-path", "x.sky", "--associate-proteins", "--background-proteome-file", "C:\\db\\human.protdb", "--background-proteome-name", "Human" });
            TestAssert.True(associated.AssociationOptions.Enabled, "Association should be enabled.");
            TestAssert.Equal("Human", associated.AssociationOptions.Name, "Proteome name should parse.");
        }
    }
}
