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
        public ReplicateOrderResult ReplicateOrderResult { get; set; }
        public PrecursorMissingnessResult PrecursorMissingnessResult { get; set; }
        public ReplicateAnnotationResult ReplicateAnnotationResult { get; set; }
        public bool SequenceFilterApplied { get; set; }
    }

    internal sealed class FilterWorkflow
    {
        private readonly ISkylineDocument _document;
        private readonly Func<string, bool> _fileExists;
        private readonly ProteinAssociationOptions _associationOptions;
        private readonly ReplicateOrderingOptions _replicateOrderingOptions;
        private readonly PrecursorMissingnessOptions _precursorMissingnessOptions;
        private readonly bool _missingnessOnly;
        private readonly ReplicateAnnotationOptions _replicateAnnotationOptions;

        public FilterWorkflow(ISkylineDocument document, Func<string, bool> fileExists, ProteinAssociationOptions associationOptions = null, ReplicateOrderingOptions replicateOrderingOptions = null, PrecursorMissingnessOptions precursorMissingnessOptions = null, bool missingnessOnly = false, ReplicateAnnotationOptions replicateAnnotationOptions = null)
        {
            _document = document;
            _fileExists = fileExists;
            _associationOptions = associationOptions ?? ProteinAssociationOptions.Disabled;
            _replicateOrderingOptions = replicateOrderingOptions ?? ReplicateOrderingOptions.Disabled;
            _precursorMissingnessOptions = precursorMissingnessOptions ?? PrecursorMissingnessOptions.Disabled;
            _missingnessOnly = missingnessOnly;
            _replicateAnnotationOptions = replicateAnnotationOptions ?? ReplicateAnnotationOptions.Disabled;
            if (_missingnessOnly && !_precursorMissingnessOptions.Enabled) throw new ArgumentException("Missingness-only mode requires precursor missingness filtering.");
        }

        public FilterResult Run(string sourcePath, bool replaceExisting)
        {
            var destination = OutputPath.Derive(sourcePath, _missingnessOnly);
            if (_fileExists(destination) && !replaceExisting)
                return new FilterResult { Cancelled = true, OutputPath = destination };

            var workingPath = destination + ".tmp-" + Guid.NewGuid().ToString("N") + ".sky";
            var published = false;
            try
            {
                _document.CreateWorkingCopy(Path.GetFullPath(sourcePath), workingPath);
                FilterPlan plan = null;
                if (!_missingnessOnly) { plan = FilterPlan.Create(_document.ReadPeptides()); _document.DeletePeptides(plan.DeleteElements); }
                PrecursorMissingnessResult missingnessResult = null;
                if (_precursorMissingnessOptions.Enabled) missingnessResult = _document.ApplyPrecursorMissingnessFilter(_precursorMissingnessOptions);
                _document.RemoveEmptyContainers();
                ReplicateOrderResult replicateResult = null;
                if (_replicateOrderingOptions.Enabled) replicateResult = _document.ApplyReplicateOrdering(ReplicateManifest.Load(_replicateOrderingOptions));
                ReplicateAnnotationResult annotationResult = null;
                if (_replicateAnnotationOptions.Enabled) annotationResult = _document.PrepareReplicateAnnotations(ReplicateAnnotationMetadata.Load(_replicateAnnotationOptions));
                _document.NormalizeWithSkylineCmd(1, _associationOptions, annotationResult);
                if (_missingnessOnly) _document.VerifySettings(1); else _document.Verify("N[", 1);
                _document.PublishWorkingCopy(destination);
                published = true;
                return new FilterResult { OutputPath = destination, RetainedCount = plan == null ? 0 : plan.RetainedCount, RemovedCount = plan == null ? 0 : plan.RemovedCount, SequenceFilterApplied = !_missingnessOnly, ReplicateOrderResult = replicateResult, PrecursorMissingnessResult = missingnessResult, ReplicateAnnotationResult = annotationResult };
            }
            finally
            {
                if (!published)
                    _document.DiscardWorkingCopy();
            }
        }
    }
}
