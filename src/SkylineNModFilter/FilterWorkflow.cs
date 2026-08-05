using System;
using System.IO;

namespace SkylineNModFilter
{
    internal sealed class FilterResult
    {
        public bool Cancelled { get; set; }
        public int RetainedCount { get; set; }
        public int RemovedCount { get; set; }
        public string OutputPath { get; set; }
    }

    internal sealed class FilterWorkflow
    {
        private readonly ISkylineDocument _document;
        private readonly Func<string, bool> _fileExists;
        private readonly ProteinAssociationOptions _associationOptions;

        public FilterWorkflow(ISkylineDocument document, Func<string, bool> fileExists, ProteinAssociationOptions associationOptions = null)
        {
            _document = document;
            _fileExists = fileExists;
            _associationOptions = associationOptions ?? ProteinAssociationOptions.Disabled;
        }

        public FilterResult Run(string sourcePath, bool replaceExisting)
        {
            var destination = OutputPath.Derive(sourcePath);
            if (_fileExists(destination) && !replaceExisting)
                return new FilterResult { Cancelled = true, OutputPath = destination };

            var workingPath = destination + ".tmp-" + Guid.NewGuid().ToString("N") + ".sky";
            var published = false;
            try
            {
                _document.CreateWorkingCopy(Path.GetFullPath(sourcePath), workingPath);
                var plan = FilterPlan.Create(_document.ReadPeptides());
                _document.DeletePeptides(plan.DeleteElements);
                _document.RemoveEmptyContainers();
                _document.NormalizeWithSkylineCmd(1, _associationOptions);
                _document.Verify("N[", 1);
                _document.PublishWorkingCopy(destination);
                published = true;
                return new FilterResult { OutputPath = destination, RetainedCount = plan.RetainedCount, RemovedCount = plan.RemovedCount };
            }
            finally
            {
                if (!published)
                    _document.DiscardWorkingCopy();
            }
        }
    }
}
