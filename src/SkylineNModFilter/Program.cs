using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SkylineNModFilter
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                var options = ToolArguments.Parse(args);
                var destination = OutputPath.Derive(options.DocumentPath, options.MissingnessOnly);
                var replace = false;
                if (File.Exists(destination))
                {
                    replace = MessageBox.Show("Replace the existing filtered document?\n\n" + destination, "N-Mod Filter", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
                    if (!replace) return 0;
                }
                var command = string.IsNullOrWhiteSpace(options.SkylineCommand) ? FindSkylineCommand() : options.SkylineCommand;
                var workflow = new FilterWorkflow(new SkylineDocument(command, SkylineDocument.RunProcess), File.Exists, options.AssociationOptions, options.ReplicateOrderingOptions, options.PrecursorMissingnessOptions, options.MissingnessOnly);
                var result = workflow.Run(options.DocumentPath, replace);
                MessageBox.Show(CompletionMessage.Build(result), "N-Mod Filter", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 0;
            }
            catch (ArgumentException exception) { MessageBox.Show(exception.Message, "N-Mod Filter", MessageBoxButtons.OK, MessageBoxIcon.Warning); return 2; }
            catch (Exception exception) { MessageBox.Show(exception.Message, "N-Mod Filter", MessageBoxButtons.OK, MessageBoxIcon.Error); return 4; }
        }

        private static string FindSkylineCommand()
        {
            foreach (var process in Process.GetProcesses().Where(p => p.ProcessName.StartsWith("Skyline", StringComparison.OrdinalIgnoreCase)))
            {
                try { var candidate = Path.Combine(Path.GetDirectoryName(process.MainModule.FileName), "SkylineCmd.exe"); if (File.Exists(candidate)) return candidate; } catch { }
            }
            throw new InvalidOperationException("Could not locate SkylineCmd.exe beside the running Skyline process.");
        }
    }
}
