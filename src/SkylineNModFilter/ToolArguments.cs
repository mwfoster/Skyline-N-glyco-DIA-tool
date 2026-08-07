using System;

namespace SkylineNModFilter
{
    internal sealed class ToolArguments
    {
        public string DocumentPath { get; private set; }
        public string SkylineCommand { get; private set; }
        public ProteinAssociationOptions AssociationOptions { get; private set; }
        public ReplicateOrderingOptions ReplicateOrderingOptions { get; private set; }

        public static ToolArguments Parse(string[] args)
        {
            var result = new ToolArguments();
            var associate = false; string proteomeFile = null; string proteomeName = null;
            var reorder = false; var hasHeader = false; var rename = false; string manifestPath = null; int nameColumn = 0; var nameColumnSeen = false;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "--document-path" && i + 1 < args.Length) result.DocumentPath = args[++i];
                else if (args[i] == "--skyline-command" && i + 1 < args.Length) result.SkylineCommand = args[++i];
                else if (args[i] == "--associate-proteins") associate = true;
                else if (args[i] == "--background-proteome-file" && i + 1 < args.Length) proteomeFile = args[++i];
                else if (args[i] == "--background-proteome-name" && i + 1 < args.Length) proteomeName = args[++i];
                else if (args[i] == "--reorder-replicates") reorder = true;
                else if (args[i] == "--replicate-manifest" && i + 1 < args.Length) manifestPath = args[++i];
                else if (args[i] == "--manifest-has-header") hasHeader = true;
                else if (args[i] == "--rename-replicates") rename = true;
                else if (args[i] == "--replicate-name-column" && i + 1 < args.Length) { nameColumnSeen = int.TryParse(args[++i], out nameColumn); }
            }
            if (string.IsNullOrWhiteSpace(result.DocumentPath)) throw new ArgumentException("A saved Skyline document path is required.");
            result.AssociationOptions = associate ? ProteinAssociationOptions.EnabledFor(proteomeFile, proteomeName) : ProteinAssociationOptions.Disabled;
            if (!reorder && (hasHeader || rename || nameColumnSeen || !string.IsNullOrWhiteSpace(manifestPath))) throw new ArgumentException("Replicate manifest options require --reorder-replicates.");
            if (rename && (!nameColumnSeen || nameColumn < 2)) throw new ArgumentException("Renaming requires a replicate name column of 2 or greater.");
            result.ReplicateOrderingOptions = reorder ? ReplicateOrderingOptions.EnabledFor(manifestPath, hasHeader, rename, nameColumn) : ReplicateOrderingOptions.Disabled;
            return result;
        }
    }
}
