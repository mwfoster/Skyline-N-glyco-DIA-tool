using System.Collections.Generic;

namespace SkylineNModFilter
{
    internal interface ISkylineDocument
    {
        void CreateWorkingCopy(string sourcePath, string workingPath);
        IList<PeptideRecord> ReadPeptides();
        void DeletePeptides(IList<PeptideRecord> peptides);
        void RemoveEmptyContainers();
        ReplicateOrderResult ApplyReplicateOrdering(ReplicateManifest manifest);
        void NormalizeWithSkylineCmd(int maxVariableMods, ProteinAssociationOptions options);
        void Verify(string match, int maxVariableMods);
        void PublishWorkingCopy(string destinationPath);
        void DiscardWorkingCopy();
    }
}
