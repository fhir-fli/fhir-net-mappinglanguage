namespace Grey.TutorialTests
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Hl7.Fhir.ElementModel;
    using Hl7.Fhir.MappingLanguage;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Serialization;

    class Program
    {
        private static FhirXmlSerializer _xmlSerializer = new FhirXmlSerializer(new SerializerSettings() { Pretty = true });
        private static FhirJsonSerializer _jsonSerializer = new FhirJsonSerializer(new SerializerSettings() { Pretty = true });

        [STAThread]
        static async System.Threading.Tasks.Task Main()
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

                            // *** Local Conversion (using .NET StructureMapParser) ***
                            // ConvertWithDotNet(mapContent, mapFile);
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

        // Function to handle remote conversion with Matchbox API (Java-based conversion)
        private static async System.Threading.Tasks.Task ConvertWithMatchbox(HttpClient httpClient, string mapContent, string mapFilePath, string outputExtension, string acceptHeader)
        {
            try
            {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting {mapFilePath} with Matchbox: {ex.Message}");
            }
        }

        // Function to handle local conversion with .NET FHIR libraries (StructureMapUtilitiesParse)
        private static void ConvertWithDotNet(string mapContent, string mapFilePath)
        {
            try
            {
                // Parse the .map file content to a StructureMap using the .NET FHIR library
                var parser = new StructureMapUtilitiesParse();
                var structureMap = parser.parse(mapContent, Path.GetFileNameWithoutExtension(mapFilePath)); // Map name from file

                // Serialize to XML and JSON using .NET FHIR serializers
                SerializeToXml(structureMap, mapFilePath);
                SerializeToJson(structureMap, mapFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting {mapFilePath} locally using .NET: {ex.Message}");
            }
        }

        // Function to serialize StructureMap to XML
        private static void SerializeToXml(StructureMap structureMap, string mapFilePath)
        {
            try
            {
                string xmlOutput = _xmlSerializer.SerializeToString(structureMap); // Convert to XML
                string outputFilePath = Path.ChangeExtension(mapFilePath, ".dotnet.xml"); // Change file extension to .dotnet.xml
                File.WriteAllText(outputFilePath, xmlOutput); // Write the XML output to file
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing {mapFilePath} to XML: {ex.Message}");
            }
        }

        // Function to serialize StructureMap to JSON
        private static void SerializeToJson(StructureMap structureMap, string mapFilePath)
        {
            try
            {
                string jsonOutput = _jsonSerializer.SerializeToString(structureMap); // Convert to JSON
                string outputFilePath = Path.ChangeExtension(mapFilePath, ".dotnet.json"); // Change file extension to .dotnet.json
                File.WriteAllText(outputFilePath, jsonOutput); // Write the JSON output to file
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing {mapFilePath} to JSON: {ex.Message}");
            }
        }
    }
}
