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
    using Hl7.Fhir.Specification;
    using Hl7.Fhir.Specification.Source;

    class Program
    {
        private static FhirXmlSerializer _xmlSerializer = new FhirXmlSerializer(new SerializerSettings() { Pretty = true });
        private static FhirJsonSerializer _jsonSerializer = new FhirJsonSerializer(new SerializerSettings() { Pretty = true });
        private static FhirXmlParser _xmlParser = new FhirXmlParser();
        private static FhirJsonParser _jsonParser = new FhirJsonParser();

        private static void Source_Load(object sender, CachedResolver.LoadResourceEventArgs e)
        {
            if (e.Resource is StructureDefinition sd)
            {
                sd.Abstract = false;
                if (sd.Snapshot == null)
                {
                    sd.Snapshot = new StructureDefinition.SnapshotComponent();
                    sd.Snapshot.Element.AddRange(sd.Differential.Element);
                }
            }
        }

        [STAThread]
        static async System.Threading.Tasks.Task Main()
        {
            // Base directory where all the files are located
            string baseDirectory = @"/home/grey/dev/fhir-net-mappinglanguage/Grey.TutorialTests/maptutorial";

            // HttpClient for interacting with the Matchbox API
            var httpClient = new HttpClient();

            // Step 1: File generation (sending the files to Matchbox API or using local conversion)
            await GenerateFilesForSteps(baseDirectory, httpClient);

            // Step 2: Perform transformations for each step (XML and JSON)
            await PerformTransformationsForSteps(baseDirectory);
        }

        private static async System.Threading.Tasks.Task GenerateFilesForSteps(string baseDirectory, HttpClient httpClient)
        {
            for (int step = 1; step <= 13; step++)
            {
                // Define directories for map and logical files
                string stepDirectory = Path.Combine(baseDirectory, $"step{step}", "map");
                string logicalDirectory = Path.Combine(baseDirectory, $"step{step}", "logical");

                // Process map files if the directory exists
                if (Directory.Exists(stepDirectory))
                {
                    string[] mapFiles = Directory.GetFiles(stepDirectory, "*.map");

                    // Process each .map file
                    foreach (var mapFile in mapFiles)
                    {
                        try
                        {
                            // Read the map content
                            string mapContent = File.ReadAllText(mapFile);

                            // Send the map content to Matchbox for remote conversion
                            // await ConvertWithMatchbox(httpClient, mapContent, mapFile, ".java.xml", "application/fhir+xml");
                            // await ConvertWithMatchbox(httpClient, mapContent, mapFile, ".java.json", "application/fhir+json");

                            // Local conversion (optional, currently commented out)
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

                // Convert XML files in logicalDirectory to JSON
                if (Directory.Exists(logicalDirectory))
                {
                    // await ConvertXmlToJsonInDirectory(logicalDirectory);
                }
                else
                {
                    Console.WriteLine($"Directory {logicalDirectory} does not exist.");
                }
            }
        }

        private static async System.Threading.Tasks.Task ConvertWithMatchbox(HttpClient httpClient, string mapContent, string mapFilePath, string outputExtension, string acceptHeader)
        {
            try
            {
                // Create the HTTP request for the Matchbox API
                var request = new HttpRequestMessage(HttpMethod.Post, "https://test.ahdis.ch/matchbox/fhir/StructureMap/$convert")
                {
                    Content = new StringContent(mapContent, Encoding.UTF8, "text/fhir-mapping")
                };
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(acceptHeader));

                // Send the request and ensure success
                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                // Write the response content to a file
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


        private static async System.Threading.Tasks.Task ConvertXmlToJsonInDirectory(string directoryPath)
        {
            string[] xmlFiles = Directory.GetFiles(directoryPath, "*.xml");

            foreach (var xmlFile in xmlFiles)
            {
                try
                {
                    // Read and parse the XML content as a FHIR Resource
                    string xmlContent = File.ReadAllText(xmlFile);
                    var resource = _xmlParser.Parse<Resource>(xmlContent);

                    // Serialize the resource to JSON
                    string jsonContent = _jsonSerializer.SerializeToString(resource);

                    // Write the JSON to a new file
                    string jsonFilePath = Path.ChangeExtension(xmlFile, ".json");
                    await File.WriteAllTextAsync(jsonFilePath, jsonContent);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error converting {xmlFile} to JSON: {ex.Message}");
                }
            }
        }


        private static async System.Threading.Tasks.Task PerformTransformationsForSteps(string baseDirectory)
        {
            for (int step = 1; step <= 13; step++)
            {
                // Define directories for logical, map, source, and result files
                string logicalDirectory = Path.Combine(baseDirectory, $"step{step}", "logical");
                string mapDirectory = Path.Combine(baseDirectory, $"step{step}", "map");
                string sourceDirectory = Path.Combine(baseDirectory, $"step{step}", "source");
                string resultDirectory = Path.Combine(baseDirectory, $"step{step}", "result");

                if (Directory.Exists(mapDirectory))
                {
                    // Process both XML and JSON structure maps
                    await ProcessStructureMapFiles(logicalDirectory, mapDirectory, sourceDirectory, resultDirectory, "*.xml", "xml");
                    await ProcessStructureMapFiles(logicalDirectory, mapDirectory, sourceDirectory, resultDirectory, "*.json", "json");
                }
                else
                {
                    Console.WriteLine($"Directory {mapDirectory} does not exist.");
                }
            }
        }

        // Helper method to process both XML and JSON files
        private static async System.Threading.Tasks.Task ProcessStructureMapFiles(string logicalDirectory, string mapDirectory, string sourceDirectory, string resultDirectory, string fileType, string format)
        {
            string[] structureMapFiles = Directory.GetFiles(mapDirectory, fileType);

            foreach (string file in structureMapFiles)
            {
                string[] sourceFiles = Directory.GetFiles(sourceDirectory, fileType);
                foreach (string sourceFile in sourceFiles)
                {
                    await ProcessSingleMapFile(logicalDirectory, sourceFile, file, resultDirectory, format);
                }
            }
        }

        // Helper to process a single map file
        private static async System.Threading.Tasks.Task ProcessSingleMapFile(string logicalDirectory, string sourceFile, string mapFile, string resultDirectory, string format)
        {
            // Read source file and load the map
            var sourceContent = File.ReadAllText(sourceFile);
            var sourceNode = format == "xml" ? FhirXmlNode.Parse(sourceContent) : FhirJsonNode.Parse(sourceContent);

            // Load StructureDefinitions
            Dictionary<string, string> typeToCanonicalMap = LoadStructureDefinitions(logicalDirectory, format);

            var source = new CachedResolver(new MultiResolver(new DirectorySource(logicalDirectory), ZipSource.CreateValidationSource()));
            source.Load += Source_Load;
            var worker = new TestWorker(source);

            var provider = CreateStructureDefinitionProvider(source, typeToCanonicalMap);

            // Load the map content
            var mapContent = File.ReadAllText(mapFile);
            var structureMap = format == "xml" ? _xmlParser.Parse<StructureMap>(mapContent) : _jsonParser.Parse<StructureMap>(mapContent);

            // Initialize the engine and transform
            var engine = new StructureMapUtilitiesExecute(worker, null, provider);
            var target = ElementNode.Root(provider, "TRight");

            try
            {
                engine.transform(null, sourceNode.ToTypedElement(provider), structureMap, target);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            // Serialize the result
            var resultContent = format == "xml" ? target.ToXml(new FhirXmlSerializationSettings() { Pretty = true }) : target.ToJson(new FhirJsonSerializationSettings() { Pretty = true });

            // Combine names for output file
            string mapName = Path.GetFileNameWithoutExtension(mapFile);
            string sourceName = Path.GetFileNameWithoutExtension(sourceFile);
            string resultFileName = $"{mapName}.{sourceName}.{format}";

            // Write the result to file
            string resultFilePath = Path.Combine(resultDirectory, resultFileName);
            await File.WriteAllTextAsync(resultFilePath, resultContent);

            Console.WriteLine($"Saved result to: {resultFilePath}");
        }

        // Load the StructureDefinitions (XML or JSON) into a dictionary
        private static Dictionary<string, string> LoadStructureDefinitions(string logicalDirectory, string format)
        {
            var typeToCanonicalMap = new Dictionary<string, string>();
            string fileType = format == "xml" ? "*.xml" : "*.json";
            string[] logicalFiles = Directory.GetFiles(logicalDirectory, fileType);

            foreach (var logicalFile in logicalFiles)
            {
                try
                {
                    string content = File.ReadAllText(logicalFile);
                    var structureDefinition = format == "xml" ? _xmlParser.Parse<StructureDefinition>(content) : _jsonParser.Parse<StructureDefinition>(content);

                    if (!string.IsNullOrEmpty(structureDefinition.Name) && !string.IsNullOrEmpty(structureDefinition.Url))
                    {
                        typeToCanonicalMap[structureDefinition.Name] = structureDefinition.Url;
                        Console.WriteLine($"Loaded StructureDefinition: {structureDefinition.Name} -> {structureDefinition.Url}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading StructureDefinition from {logicalFile}: {ex.Message}");
                }
            }

            return typeToCanonicalMap;
        }

        // Create a dynamic StructureDefinitionSummaryProvider
        private static IStructureDefinitionSummaryProvider CreateStructureDefinitionProvider(IResourceResolver source, Dictionary<string, string> typeToCanonicalMap)
        {
            return new StructureDefinitionSummaryProvider(
                source,
                (string name, out string canonical) =>
                {
                    if (typeToCanonicalMap.TryGetValue(name, out canonical))
                    {
                        return true;
                    }

                    return StructureDefinitionSummaryProvider.DefaultTypeNameMapper(name, out canonical);
                });
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
