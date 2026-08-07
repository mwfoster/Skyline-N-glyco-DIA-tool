using System;

namespace SkylineNModFilter.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                FilterRuleTests.Run();
                FilterPlanTests.Run();
                OutputPathTests.Run();
                FilterWorkflowTests.Run();
                SkylineDocumentTests.Run();
                ToolArgumentsTests.Run();
                SkylineFileSetTests.Run();
                BackgroundProteomeFastaExporterTests.Run();
                DelimitedMetadataReaderTests.Run();
                ReplicateManifestTests.Run();
                ReplicateOrderingTests.Run();
                PrecursorMissingnessTests.Run();
                ReplicateAnnotationMetadataTests.Run();
                ReplicateAnnotationTests.Run();
                CompletionMessageTests.Run();
                CollectorArgumentContractTests.Run();
                Console.WriteLine("PASS: filtering core");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("FAIL: " + exception);
                return 1;
            }
        }
    }
}
