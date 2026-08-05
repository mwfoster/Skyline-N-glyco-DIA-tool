using System;

namespace SkylineNModFilter
{
    internal sealed class ToolArguments
    {
        public string DocumentPath { get; private set; }
        public string SkylineCommand { get; private set; }
        public ProteinAssociationOptions AssociationOptions { get; private set; }

        public static ToolArguments Parse(string[] args)
        {
            var result = new ToolArguments();
            var associate = false; string proteomeFile = null; string proteomeName = null;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "--document-path" && i + 1 < args.Length) result.DocumentPath = args[++i];
                else if (args[i] == "--skyline-command" && i + 1 < args.Length) result.SkylineCommand = args[++i];
                else if (args[i] == "--associate-proteins") associate = true;
                else if (args[i] == "--background-proteome-file" && i + 1 < args.Length) proteomeFile = args[++i];
                else if (args[i] == "--background-proteome-name" && i + 1 < args.Length) proteomeName = args[++i];
            }
            if (string.IsNullOrWhiteSpace(result.DocumentPath)) throw new ArgumentException("A saved Skyline document path is required.");
            result.AssociationOptions = associate ? ProteinAssociationOptions.EnabledFor(proteomeFile, proteomeName) : ProteinAssociationOptions.Disabled;
            return result;
        }
    }
}
