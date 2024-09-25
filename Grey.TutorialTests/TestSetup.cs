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
                            await ConvertWithMatchbox(httpClient, mapContent, mapFile, ".java.xml", "application/fhir+xml");
                            await ConvertWithMatchbox(httpClient, mapContent, mapFile, ".java.json", "application/fhir+json");

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
                    await ConvertXmlToJsonInDirectory(logicalDirectory);
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
                    // Process XML structure maps
                    string[] xmlStructureMapFiles = Directory.GetFiles(mapDirectory, "*.xml");
                    foreach (string file in xmlStructureMapFiles)
                    {
                        string[] xmlSourceFiles = Directory.GetFiles(sourceDirectory, "*.xml");
                        foreach (string xmlFile in xmlSourceFiles)
                        {
                            var sourceFile = System.IO.File.ReadAllText(xmlFile);
                            var sourceNode = FhirXmlNode.Parse(sourceFile);

                            // Initialize a dictionary to store type names and their canonical URLs
                            Dictionary<string, string> typeToCanonicalMap = new Dictionary<string, string>();

                            // Iterate over all XML files in the logicalDirectory to load the StructureDefinitions
                            string[] xmlLogicalFiles = Directory.GetFiles(logicalDirectory, "*.xml");
                            foreach (string logicalFile in xmlLogicalFiles)
                            {
                                try
                                {
                                    string content = File.ReadAllText(logicalFile);
                                    var structureDefinition = _xmlParser.Parse<StructureDefinition>(content);  // Parse XML into StructureDefinition

                                    if (structureDefinition != null && !string.IsNullOrEmpty(structureDefinition.Name) && !string.IsNullOrEmpty(structureDefinition.Url))
                                    {
                                        // Store the StructureDefinition type name and canonical URL in the dictionary
                                        typeToCanonicalMap[structureDefinition.Name] = structureDefinition.Url;
                                        Console.WriteLine($"Loaded StructureDefinition: {structureDefinition.Name} -> {structureDefinition.Url}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error loading StructureDefinition from {logicalFile}: {ex.Message}");
                                }
                            }

                            var source = new CachedResolver(new MultiResolver(
                                new DirectorySource(logicalDirectory),
                                ZipSource.CreateValidationSource()
                            ));
                            source.Load += Source_Load;
                            var worker = new TestWorker(source);

                            // Create the dynamic StructureDefinitionSummaryProvider
                            IStructureDefinitionSummaryProvider provider = new StructureDefinitionSummaryProvider(
                                source,
                                (string name, out string canonical) =>
                                {
                                    // Check if the name matches any of the loaded StructureDefinitions
                                    if (typeToCanonicalMap.TryGetValue(name, out canonical))
                                    {
                                        return true;  // Return true if a match is found
                                    }

                                    // Fallback to the default name mapper if no match is found
                                    return StructureDefinitionSummaryProvider.DefaultTypeNameMapper(name, out canonical);
                                });

                            // Load the map content directly from the .xml file
                            var mapContent = System.IO.File.ReadAllText(file);
                            var structureMap = _xmlParser.Parse<StructureMap>(mapContent); // Parsing the StructureMap from the map file

                            // Initialize the StructureMapUtilitiesExecute to process the map
                            var engine = new StructureMapUtilitiesExecute(worker, null, provider);

                            var target = ElementNode.Root(provider, "TRight");

                            try
                            {
                                // Transform the source using the map
                                engine.transform(null, sourceNode.ToTypedElement(provider), structureMap, target);
                            }
                            catch (System.Exception ex)
                            {
                                System.Diagnostics.Trace.WriteLine(ex.Message);
                            }

                            // Serialize the result to XML
                            var resultXml = target.ToXml(new FhirXmlSerializationSettings() { Pretty = true });

                            // Combine the names of the map file and the source file to create the output file name
                            string mapName = Path.GetFileNameWithoutExtension(file);
                            string sourceName = Path.GetFileNameWithoutExtension(xmlFile);
                            string resultFileName = $"{mapName}.{sourceName}.xml";

                            // Write the result XML to the result directory
                            string resultFilePath = Path.Combine(resultDirectory, resultFileName);
                            File.WriteAllText(resultFilePath, resultXml);

                            Console.WriteLine($"Saved result to: {resultFilePath}");
                        }
                    }

                    // Process JSON structure maps
                    string[] jsonStructureMapFiles = Directory.GetFiles(mapDirectory, "*.json");
                    foreach (string file in jsonStructureMapFiles)
                    {
                        string[] jsonSourceFiles = Directory.GetFiles(sourceDirectory, "*.json");
                        foreach (string jsonFile in jsonSourceFiles)
                        {
                            var sourceFile = System.IO.File.ReadAllText(jsonFile);
                            var sourceNode = FhirJsonNode.Parse(sourceFile);

                            // Initialize a dictionary to store type names and their canonical URLs
                            Dictionary<string, string> typeToCanonicalMap = new Dictionary<string, string>();

                            // Iterate over all JSON files in the logicalDirectory to load the StructureDefinitions
                            string[] jsonLogicalFiles = Directory.GetFiles(logicalDirectory, "*.json");
                            foreach (string logicalFile in jsonLogicalFiles)
                            {
                                try
                                {
                                    string content = File.ReadAllText(logicalFile);
                                    var structureDefinition = _jsonParser.Parse<StructureDefinition>(content);  // Parse JSON into StructureDefinition

                                    if (structureDefinition != null && !string.IsNullOrEmpty(structureDefinition.Name) && !string.IsNullOrEmpty(structureDefinition.Url))
                                    {
                                        // Store the StructureDefinition type name and canonical URL in the dictionary
                                        typeToCanonicalMap[structureDefinition.Name] = structureDefinition.Url;
                                        Console.WriteLine($"Loaded StructureDefinition: {structureDefinition.Name} -> {structureDefinition.Url}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error loading StructureDefinition from {logicalFile}: {ex.Message}");
                                }
                            }

                            // Load the map content directly from the .json file
                            var mapContent = System.IO.File.ReadAllText(file);
                            var structureMap = _jsonParser.Parse<StructureMap>(mapContent); // Parsing the StructureMap from the map file

                            var source = new CachedResolver(new MultiResolver(
                                new DirectorySource(logicalDirectory),
                                ZipSource.CreateValidationSource()
                            ));
                            source.Load += Source_Load;
                            var worker = new TestWorker(source);

                            // Create the dynamic StructureDefinitionSummaryProvider
                            IStructureDefinitionSummaryProvider provider = new StructureDefinitionSummaryProvider(
                                source,
                                (string name, out string canonical) =>
                                {
                                    // Check if the name matches any of the loaded StructureDefinitions
                                    if (typeToCanonicalMap.TryGetValue(name, out canonical))
                                    {
                                        return true;  // Return true if a match is found
                                    }

                                    // Fallback to the default name mapper if no match is found
                                    return StructureDefinitionSummaryProvider.DefaultTypeNameMapper(name, out canonical);
                                });

                            // Initialize the StructureMapUtilitiesExecute to process the map
                            var engine = new StructureMapUtilitiesExecute(worker, null, provider);

                            var target = ElementNode.Root(provider, "TRight");

                            try
                            {
                                // Transform the source using the map
                                engine.transform(null, sourceNode.ToTypedElement(provider), structureMap, target);
                            }
                            catch (System.Exception ex)
                            {
                                System.Diagnostics.Trace.WriteLine(ex.Message);
                            }

                            // Serialize the result to JSON
                            var resultJson = target.ToJson(new FhirJsonSerializationSettings() { Pretty = true });

                            // Combine the names of the map file and the source file to create the output file name
                            string mapName = Path.GetFileNameWithoutExtension(file);
                            string sourceName = Path.GetFileNameWithoutExtension(jsonFile);
                            string resultFileName = $"{mapName}.{sourceName}.json";

                            // Write the result JSON to the result directory
                            string resultFilePath = Path.Combine(resultDirectory, resultFileName);
                            File.WriteAllText(resultFilePath, resultJson);

                            Console.WriteLine($"Saved result to: {resultFilePath}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Directory {mapDirectory} does not exist.");
                }
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
