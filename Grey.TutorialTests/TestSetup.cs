namespace Grey.TutorialTests
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    class Program
    {
        [STAThread]
        static async Task Main() // Explicitly use System.Threading.Tasks.Task
        {
            string baseDirectory = @"/home/grey/dev/fhir-net-mappinglanguage/Grey.TutorialTests/maptutorial";
            var httpClient = new HttpClient(); // HTTP client for Matchbox API requests

            // Iterate through step1 to step13 directories
            for (int step = 1; step <= 13; step++)
            {
                string stepDirectory = Path.Combine(baseDirectory, $"step{step}", "map");

                if (Directory.Exists(stepDirectory))
                {
                    string[] mapFiles = Directory.GetFiles(stepDirectory, "*.map");

                    foreach (string mapFile in mapFiles)
                    {
                        try
                        {
                            // Read the .map content (FHIR Mapping Language)
                            string mapContent = File.ReadAllText(mapFile);

                            // *** Remote Conversion (using Matchbox API) ***
                            await ConvertWithMatchbox(httpClient, mapContent, mapFile, ".java.xml", "application/fhir+xml");
                            await ConvertWithMatchbox(httpClient, mapContent, mapFile, ".java.json", "application/fhir+json");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing {mapFile}: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Directory {stepDirectory} does not exist.");
                }
            }
        }

        // Function to handle remote conversion with Matchbox API
        private static async Task ConvertWithMatchbox(HttpClient httpClient, string mapContent, string mapFilePath, string outputExtension, string acceptHeader)
        {
            try
            {
                // Log details of the request
                Console.WriteLine($"Sending request to Matchbox for {mapFilePath}");
                Console.WriteLine($"Content-Type: text/fhir-mapping");
                Console.WriteLine($"Accept: {acceptHeader}");
                Console.WriteLine($"Map Content: {mapContent}");

                // Create the HTTP request
                var request = new HttpRequestMessage(HttpMethod.Post, "https://test.ahdis.ch/matchbox/fhir/StructureMap/$convert");
                request.Content = new StringContent(mapContent, Encoding.UTF8, "text/fhir-mapping");
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(acceptHeader));

                // Send the request
                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                // Read the response content
                string convertedContent = await response.Content.ReadAsStringAsync();
                string outputFilePath = Path.ChangeExtension(mapFilePath, outputExtension);
                File.WriteAllText(outputFilePath, convertedContent);

                Console.WriteLine($"Converted remotely via Matchbox: {mapFilePath} -> {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting {mapFilePath} with Matchbox: {ex.Message}");
            }
        }
    }
}
