namespace Grey.TutorialTests
{
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Serialization;
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks; // Keep this for async/await

    class Program
    {
        [STAThread]
        static async System.Threading.Tasks.Task Main() // Explicitly use System.Threading.Tasks.Task
        {
            string baseDirectory = @"/home/grey/dev/fhir-net-mappinglanguage/Grey.TutorialTests/maptutorial";
            var xmlParser = new FhirXmlParser();
            var jsonSerializer = new FhirJsonSerializer(new SerializerSettings() { Pretty = true }); // Enable pretty-printing
            var xmlSerializer = new FhirXmlSerializer(new SerializerSettings() { Pretty = true });
            var httpClient = new HttpClient(); // HTTP client for Matchbox API requests

            // Iterate through step1 to step13 directories
            for (int step = 1; step <= 13; step++)
            {
                string stepDirectory = Path.Combine(baseDirectory, $"step{step}", "logical");

                if (Directory.Exists(stepDirectory))
                {
                    string[] xmlFiles = Directory.GetFiles(stepDirectory, "*.xml");

                    foreach (string xmlFile in xmlFiles)
                    {
                        try
                        {
                            // Read the XML content
                            string xmlContent = File.ReadAllText(xmlFile);

                            // Parse the XML to a FHIR resource
                            Resource resource = xmlParser.Parse<Resource>(xmlContent);

                            // Serialize the resource to JSON with pretty-printing
                            string jsonOutput = jsonSerializer.SerializeToString(resource);
                            string jsonFilePath = Path.ChangeExtension(xmlFile, ".json");
                            File.WriteAllText(jsonFilePath, jsonOutput);

                            // Serialize to XML and save locally
                            string xmlOutput = xmlSerializer.SerializeToString(resource);
                            string xmlFilePath = Path.ChangeExtension(xmlFile, ".xml.new");
                            File.WriteAllText(xmlFilePath, xmlOutput);

                            Console.WriteLine($"Converted locally: {xmlFile} -> {jsonFilePath}, {xmlFilePath}");

                            // *** Remote Conversion (using Matchbox API) ***
                            await ConvertWithMatchbox(httpClient, xmlContent, xmlFile, ".java.xml", "application/fhir+xml;fhirVersion=4.0");
                            await ConvertWithMatchbox(httpClient, xmlContent, xmlFile, ".java.json", "application/fhir+json;fhirVersion=4.0");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing {xmlFile}: {ex.Message}");
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
        private static async System.Threading.Tasks.Task ConvertWithMatchbox(HttpClient httpClient, string mapContent, string mapFilePath, string outputExtension, string acceptHeader)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://test.ahdis.ch/matchbox/fhir/StructureMap/$convert");
                request.Content = new StringContent(mapContent, Encoding.UTF8, "text/fhir-mapping");
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(acceptHeader));

                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

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
