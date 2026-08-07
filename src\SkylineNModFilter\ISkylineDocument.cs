using System.Collections.Generic;

namespace SkylineNModFilter
{
    internal interface ISkylineDocument
    {
        void CreateWorkingCopy(string sourcePath, string workingPath);
        IList<PeptideRecord> ReadPeptides();
        void DeletePeptides(IList<PeptideRecord> peptides);
        PrecursorMissingnessResult ApplyPrecursorMissingnessFilter(PrecursorMissingnessOptions options);
        void RemoveEmptyContainers();
        ReplicateOrderResult ApplyReplicateOrdering(ReplicateManifest manifest);
        ReplicateAnnotationResult PrepareReplicateAnnotations(ReplicateAnnotationMetadata metadata);
        void NormalizeWithSkylineCmd(int maxVariableMods, ProteinAssociationOptions options, ReplicateAnnotationResult annotations);
        void Verify(string match, int maxVariableMods);
        void VerifySettings(int maxVariableMods);
        void PublishWorkingCopy(string destinationPath);
        void DiscardWorkingCopy();
    }
}
