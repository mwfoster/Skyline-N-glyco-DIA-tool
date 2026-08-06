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
            var ordered = ToolArguments.Parse(new[] { "--document-path", "x.sky", "--reorder-replicates", "--replicate-manifest", "samples.csv", "--manifest-has-header", "--rename-replicates", "--replicate-name-column", "5" });
            TestAssert.True(ordered.ReplicateOrderingOptions.Enabled, "Replicate ordering should parse.");
            TestAssert.True(ordered.ReplicateOrderingOptions.HasHeader, "Header flag should parse.");
            TestAssert.True(ordered.ReplicateOrderingOptions.Rename, "Rename flag should parse.");
            TestAssert.Equal(5, ordered.ReplicateOrderingOptions.NameColumn, "Rename column should parse.");
            TestAssert.Throws<ArgumentException>(delegate { ToolArguments.Parse(new[] { "--document-path", "x.sky", "--reorder-replicates" }); }, "Ordering without a manifest must fail.");
            TestAssert.Throws<ArgumentException>(delegate { ToolArguments.Parse(new[] { "--document-path", "x.sky", "--manifest-has-header" }); }, "Header without ordering must fail.");
            TestAssert.Throws<ArgumentException>(delegate { ToolArguments.Parse(new[] { "--document-path", "x.sky", "--rename-replicates", "--replicate-name-column", "2" }); }, "Rename without ordering must fail.");
            TestAssert.Throws<ArgumentException>(delegate { ToolArguments.Parse(new[] { "--document-path", "x.sky", "--reorder-replicates", "--replicate-manifest", "x.csv", "--rename-replicates", "--replicate-name-column", "1" }); }, "Rename column 1 must fail.");
        }
    }
}
