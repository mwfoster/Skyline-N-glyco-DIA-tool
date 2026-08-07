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
            var missing = ToolArguments.Parse(new[] { "--document-path", "x.sky", "--filter-precursor-missingness", "--max-missing-percent", "50" });
            TestAssert.True(missing.PrecursorMissingnessOptions.Enabled, "Missingness flag must enable filtering.");
            TestAssert.Equal(50, missing.PrecursorMissingnessOptions.MaximumMissingPercent, "Missingness threshold must parse.");
            TestAssert.Throws<ArgumentException>(delegate { ToolArguments.Parse(new[] { "--document-path", "x.sky", "--max-missing-percent", "50" }); }, "Threshold without filter flag must fail.");
            TestAssert.Throws<ArgumentException>(delegate { ToolArguments.Parse(new[] { "--document-path", "x.sky", "--filter-precursor-missingness", "--max-missing-percent", "101" }); }, "Out-of-range thresholds must fail.");
            TestAssert.Throws<ArgumentException>(delegate { ToolArguments.Parse(new[] { "--document-path", "x.sky", "--filter-precursor-missingness", "--max-missing-percent", "abc" }); }, "Nonnumeric thresholds must fail.");
            var grouped = ToolArguments.Parse(new[] { "--document-path", "x.sky", "--missingness-only", "--filter-precursor-missingness", "--max-missing-percent", "40", "--missingness-scope", "selected", "--replicate-manifest", "groups.csv", "--manifest-has-header", "--group-column", "3", "--selected-group", "Case", "--exclude-unannotated" });
            TestAssert.True(grouped.MissingnessOnly, "Missingness-only mode must parse.");
            TestAssert.Equal(PrecursorMissingnessScope.SelectedGroup, grouped.PrecursorMissingnessOptions.Scope, "Selected-group scope must parse.");
            TestAssert.Equal("Case", grouped.PrecursorMissingnessOptions.SelectedGroup, "Selected group must parse.");
            TestAssert.True(grouped.PrecursorMissingnessOptions.ExcludeUnannotated, "Unannotated exclusion must parse.");
            TestAssert.Throws<ArgumentException>(delegate { ToolArguments.Parse(new[] { "--document-path", "x.sky", "--missingness-only" }); }, "Missingness-only mode requires filtering.");
            TestAssert.Throws<ArgumentException>(delegate { ToolArguments.Parse(new[] { "--document-path", "x.sky", "--filter-precursor-missingness", "--missingness-scope", "any" }); }, "Grouped scope requires metadata and a group column.");
            TestAssert.Throws<ArgumentException>(delegate { ToolArguments.Parse(new[] { "--document-path", "x.sky", "--filter-precursor-missingness", "--missingness-scope", "all", "--group-column", "2" }); }, "Group options are invalid for all-replicate scope.");
            var annotations = ToolArguments.Parse(new[] { "--document-path", "x.sky", "--import-replicate-annotations", "--replicate-manifest", "samples.csv", "--manifest-has-header" });
            TestAssert.True(annotations.ReplicateAnnotationOptions.Enabled, "Annotation import flag must enable metadata annotations.");
            TestAssert.Equal("samples.csv", annotations.ReplicateAnnotationOptions.MetadataPath, "Annotation metadata path must parse.");
            TestAssert.Throws<ArgumentException>(delegate { ToolArguments.Parse(new[] { "--document-path", "x.sky", "--import-replicate-annotations", "--replicate-manifest", "samples.csv" }); }, "Annotation import requires a header.");
            TestAssert.Throws<ArgumentException>(delegate { ToolArguments.Parse(new[] { "--document-path", "x.sky", "--import-replicate-annotations", "--manifest-has-header" }); }, "Annotation import requires a metadata path.");
        }
    }
}
