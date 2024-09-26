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
        // Create serializers and parsers for both XML and JSON formats.
        private static FhirXmlSerializer _xmlSerializer = new FhirXmlSerializer(new SerializerSettings() { Pretty = true });
        private static FhirJsonSerializer _jsonSerializer = new FhirJsonSerializer(new SerializerSettings() { Pretty = true });
        private static FhirXmlParser _xmlParser = new FhirXmlParser();
        private static FhirJsonParser _jsonParser = new FhirJsonParser();

        // Event handler for loading resources.
        // When a StructureDefinition is loaded, ensure that snapshots are generated from the differential if not already present.
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
            // Base directory where all the files for different tutorial steps are located
            string baseDirectory = @"/home/grey/dev/fhir-net-mappinglanguage/Grey.TutorialTests/maptutorial";

            // Create an HttpClient for interacting with the Matchbox API
            var httpClient = new HttpClient();

            // Step 1: Process files for each step (sending maps to Matchbox API or doing local conversion)
            await GenerateFilesForSteps(baseDirectory, httpClient);

            // Step 2: Perform the actual transformations for each step (XML and JSON files)
            await PerformTransformationsForSteps(baseDirectory);
        }

        // Generate the necessary files for all tutorial steps, converting maps using either Matchbox or a local .NET implementation.
        private static async System.Threading.Tasks.Task GenerateFilesForSteps(string baseDirectory, HttpClient httpClient)
        {
            for (int step = 4; step <= 4; step++) // There are 13 steps
            {
                // Define directories for maps and logical files for this step
                string stepDirectory = Path.Combine(baseDirectory, $"step{step}", "map");
                string logicalDirectory = Path.Combine(baseDirectory, $"step{step}", "logical");

                // Process map files if the map directory exists
                if (Directory.Exists(stepDirectory))
                {
                    string[] mapFiles = Directory.GetFiles(stepDirectory, "*.map"); // Get all map files

                    // Process each .map file
                    foreach (var mapFile in mapFiles)
                    {
                        try
                        {
                            // Read the map file content
                            string mapContent = File.ReadAllText(mapFile);

                            // Optional: Send the map content to Matchbox for conversion or do local conversion
                            await ConvertWithMatchbox(httpClient, mapContent, mapFile, ".xml", "application/fhir+xml");
                            await ConvertWithMatchbox(httpClient, mapContent, mapFile, ".json", "application/fhir+json");

                            // Optional: Use local conversion
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

                // Optionally convert XML files to JSON in the logical directory
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

        // Example of how to convert maps using Matchbox (if needed). It sends the map to Matchbox and gets it converted.
        private static async System.Threading.Tasks.Task ConvertWithMatchbox(HttpClient httpClient, string mapContent, string mapFilePath, string outputExtension, string acceptHeader)
        {
            try
            {
                // Create the HTTP request for sending the map content to Matchbox
                var request = new HttpRequestMessage(HttpMethod.Post, "https://test.ahdis.ch/matchbox/fhir/StructureMap/$convert")
                {
                    Content = new StringContent(mapContent, Encoding.UTF8, "text/fhir-mapping") // Map content in FHIR mapping format
                };
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(acceptHeader)); // Set response type

                // Send the request and ensure success
                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                // Write the response content (converted file) to the appropriate output path
                string convertedContent = await response.Content.ReadAsStringAsync();
                string outputFilePath = Path.ChangeExtension(mapFilePath, outputExtension);
                File.WriteAllText(outputFilePath, convertedContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting {mapFilePath} with Matchbox: {ex.Message}");
            }
        }

        // Example of how to perform local map conversion using .NET libraries.
        private static void ConvertWithDotNet(string mapContent, string mapFilePath)
        {
            try
            {
                // Parse the map content into a StructureMap object
                var parser = new StructureMapUtilitiesParse();
                var structureMap = parser.parse(mapContent, Path.GetFileNameWithoutExtension(mapFilePath)); // Use the file name as the map name

                // Serialize to XML and JSON formats
                SerializeToXml(structureMap, mapFilePath);
                SerializeToJson(structureMap, mapFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting {mapFilePath} locally using .NET: {ex.Message}");
            }
        }

        // Function to convert XML files to JSON (optional, if needed).
        private static async System.Threading.Tasks.Task ConvertXmlToJsonInDirectory(string directoryPath)
        {
            string[] xmlFiles = Directory.GetFiles(directoryPath, "*.xml");

            foreach (var xmlFile in xmlFiles)
            {
                try
                {
                    // Parse XML content into a FHIR resource
                    string xmlContent = File.ReadAllText(xmlFile);
                    var resource = _xmlParser.Parse<Resource>(xmlContent);

                    // Serialize the resource to JSON
                    string jsonContent = _jsonSerializer.SerializeToString(resource);

                    // Write the JSON content to a new file
                    string jsonFilePath = Path.ChangeExtension(xmlFile, ".json");
                    await File.WriteAllTextAsync(jsonFilePath, jsonContent);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error converting {xmlFile} to JSON: {ex.Message}");
                }
            }
        }

        // Main function to perform the FHIR transformations for all steps (XML and JSON maps).
        private static async System.Threading.Tasks.Task PerformTransformationsForSteps(string baseDirectory)
        {
            for (int step = 4; step <= 4; step++) // Loop through 13 steps
            {
                // Define directories for logical, map, source, and result files for this step
                string logicalDirectory = Path.Combine(baseDirectory, $"step{step}", "logical");
                string mapDirectory = Path.Combine(baseDirectory, $"step{step}", "map");
                string sourceDirectory = Path.Combine(baseDirectory, $"step{step}", "source");
                string resultDirectory = Path.Combine(baseDirectory, $"step{step}", "result");

                if (Directory.Exists(mapDirectory))
                {
                    // Process XML structure maps
                    await ProcessStructureMapFiles(logicalDirectory, mapDirectory, sourceDirectory, resultDirectory, "*.xml", "xml");
                    // Process JSON structure maps
                    await ProcessStructureMapFiles(logicalDirectory, mapDirectory, sourceDirectory, resultDirectory, "*.json", "json");
                }
                else
                {
                    Console.WriteLine($"Directory {mapDirectory} does not exist.");
                }
            }
        }

        // Function to process structure maps and their corresponding source files.
        private static async System.Threading.Tasks.Task ProcessStructureMapFiles(string logicalDirectory, string mapDirectory, string sourceDirectory, string resultDirectory, string fileType, string format)
        {
            // Get all structure map files in the map directory (based on file type)
            string[] structureMapFiles = Directory.GetFiles(mapDirectory, fileType);

            // Process each structure map file
            foreach (string mapFile in structureMapFiles)
            {
                // Get all source files in the source directory
                string[] sourceFiles = Directory.GetFiles(sourceDirectory, fileType);
                foreach (string sourceFile in sourceFiles)
                {
                    // Process each combination of map and source file
                    await ProcessSingleMapFile(logicalDirectory, sourceFile, mapFile, resultDirectory, format);
                }
            }
        }

        // Process a single structure map and source file pair.
        private static async System.Threading.Tasks.Task ProcessSingleMapFile(
            string logicalDirectory,
            string sourceFile,
            string mapFile,
            string resultDirectory,
            string format)
        {
            // Step 1: Read source file content and parse it
            var sourceContent = File.ReadAllText(sourceFile);
            var sourceNode = format == "xml" ? FhirXmlNode.Parse(sourceContent) : FhirJsonNode.Parse(sourceContent);

            // Step 2: Load all StructureDefinitions
            Dictionary<string, string> typeToCanonicalMap = LoadStructureDefinitions(logicalDirectory, format);

            // Setup resource resolver and worker context
            var source = new CachedResolver(new MultiResolver(new DirectorySource(logicalDirectory), ZipSource.CreateValidationSource()));
            source.Load += Source_Load;
            var worker = new TestWorker(source);
            var provider = CreateStructureDefinitionProvider(source, typeToCanonicalMap);

            // Step 3: Upload StructureDefinitions to Matchbox before sending the map
            await UploadStructureDefinitionsToMatchbox(typeToCanonicalMap, logicalDirectory, format);

            // Step 4: Upload the StructureMap to Matchbox (after StructureDefinitions)
            await UploadStructureMapToMatchbox(mapFile, format);

            // Step 5: Perform transformation using Matchbox API
            await TransformSourceUsingMatchbox(mapFile, sourceFile, sourceContent, format, resultDirectory);

            // Local transformation using .NET (if needed)
            var engine = new StructureMapUtilitiesExecute(worker, null, provider);
            var target = ElementNode.Root(provider, "TRight");

            try
            {
                var mapContent = File.ReadAllText(mapFile);
                var structureMap = format == "xml" ? _xmlParser.Parse<StructureMap>(mapContent) : _jsonParser.Parse<StructureMap>(mapContent);
                engine.transform(null, sourceNode.ToTypedElement(provider), structureMap, target);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            // Step 6: Serialize and save the transformation result
            var resultContent = format == "xml" ? target.ToXml(new FhirXmlSerializationSettings() { Pretty = true }) : target.ToJson(new FhirJsonSerializationSettings() { Pretty = true });
            string mapName = Path.GetFileNameWithoutExtension(mapFile).Split('.')[0];
            string sourceName = Path.GetFileNameWithoutExtension(sourceFile).Split('.')[0];
            string resultFileName = $"{mapName}.{sourceName}.{format}";
            string resultFilePath = Path.Combine(resultDirectory, resultFileName);
            await File.WriteAllTextAsync(resultFilePath, resultContent);
            Console.WriteLine($"Saved result to: {resultFilePath}");
        }

        // Upload the StructureMap to Matchbox.
        private static async System.Threading.Tasks.Task UploadStructureMapToMatchbox(string mapFile, string format)
        {
            var httpClient = new HttpClient();

            // Read the StructureMap content
            string structureMapContent = File.ReadAllText(mapFile);
            string contentType = format == "xml" ? "application/fhir+xml" : "application/fhir+json";

            // Create the HTTP request for uploading the StructureMap
            var request = new HttpRequestMessage(HttpMethod.Post, "https://test.ahdis.ch/matchbox/fhir/StructureMap")
            {
                Content = new StringContent(structureMapContent, Encoding.UTF8, contentType)
            };
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(contentType));
            request.Headers.Add("fhirVersion", "4.0");

            // Send the request and handle the response
            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error uploading StructureMap: {mapFile}");
            }
            else
            {
                Console.WriteLine($"Uploaded StructureMap: {mapFile}");
            }
        }

        // Upload StructureDefinitions to Matchbox.
        private static async System.Threading.Tasks.Task UploadStructureDefinitionsToMatchbox(Dictionary<string, string> typeToCanonicalMap, string logicalDirectory, string format)
        {
            var httpClient = new HttpClient();

            foreach (var structureDefFile in Directory.GetFiles(logicalDirectory, format == "xml" ? "*.xml" : "*.json"))
            {
                string structureDefContent = File.ReadAllText(structureDefFile);
                string contentType = format == "xml" ? "application/fhir+xml" : "application/fhir+json";

                // Create the HTTP request for uploading the StructureDefinition
                var request = new HttpRequestMessage(HttpMethod.Post, "https://test.ahdis.ch/matchbox/fhir/StructureDefinition")
                {
                    Content = new StringContent(structureDefContent, Encoding.UTF8, contentType)
                };

                // Add necessary headers
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(contentType));
                request.Headers.Add("fhirVersion", "4.0");

                // Send the request and handle the response
                var response = await httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error uploading StructureDefinition: {structureDefFile}");
                }
                else
                {
                    Console.WriteLine($"Uploaded StructureDefinition: {structureDefFile}");
                }
            }
        }

        // Perform the transformation by sending the source content to Matchbox, along with the StructureMap URL.
        private static async System.Threading.Tasks.Task TransformSourceUsingMatchbox(
            string mapFile,
            string sourceFile,
            string sourceContent,
            string format,
            string resultDirectory)
        {
            var httpClient = new HttpClient();

            // Load the StructureMap to extract the URL
            var mapContent = File.ReadAllText(mapFile);
            StructureMap structureMap = format == "xml" ? _xmlParser.Parse<StructureMap>(mapContent) : _jsonParser.Parse<StructureMap>(mapContent);

            // Extract the URL from the StructureMap
            string structureMapUrl = structureMap.Url;
            if (string.IsNullOrEmpty(structureMapUrl))
            {
                Console.WriteLine($"Error: StructureMap URL is missing in {mapFile}");
                return;
            }

            // Prepare the transformation request
            string sourceContentType = format == "xml" ? "application/fhir+xml" : "application/fhir+json";
            string resultAcceptHeader = format == "xml" ? "application/fhir+xml" : "application/fhir+json";

            // Create the HTTP request to transform the source using the StructureMap
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://test.ahdis.ch/matchbox/fhir/StructureMap/$transform?source={structureMapUrl}")
            {
                Content = new StringContent(sourceContent, Encoding.UTF8, sourceContentType)
            };
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(resultAcceptHeader));

            // Send the request and handle the response
            var response = await httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                // Save the transformation result to a file
                string transformedContent = await response.Content.ReadAsStringAsync();
                string mapName = Path.GetFileNameWithoutExtension(mapFile).Split('.')[0];
                string sourceName = Path.GetFileNameWithoutExtension(sourceFile).Split('.')[0];
                string resultFileName = $"{mapName}.{sourceName}.{format}";
                string resultFilePath = Path.Combine(resultDirectory, resultFileName);
                // await File.WriteAllTextAsync(resultFilePath, transformedContent);

                Console.WriteLine($"Matchbox transformation saved to: {resultFilePath}");
            }
            else
            {
                Console.WriteLine($"Error in Matchbox transformation: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
        }

        // Load all StructureDefinitions from the logical directory into a dictionary
        private static Dictionary<string, string> LoadStructureDefinitions(string logicalDirectory, string format)
        {
            var typeToCanonicalMap = new Dictionary<string, string>();
            string fileType = format == "xml" ? "*.xml" : "*.json";
            string[] logicalFiles = Directory.GetFiles(logicalDirectory, fileType);

            foreach (var logicalFile in logicalFiles)
            {
                try
                {
                    // Parse each StructureDefinition from XML or JSON
                    string content = File.ReadAllText(logicalFile);
                    var structureDefinition = format == "xml" ? _xmlParser.Parse<StructureDefinition>(content) : _jsonParser.Parse<StructureDefinition>(content);

                    // Add the StructureDefinition to the map if it has a name and URL
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
                    // Try to get the canonical URL from the map based on the name
                    if (typeToCanonicalMap.TryGetValue(name, out canonical))
                    {
                        return true;
                    }

                    // Fallback to the default name mapper if not found
                    return StructureDefinitionSummaryProvider.DefaultTypeNameMapper(name, out canonical);
                });
        }

        // Function to serialize a StructureMap to XML format.
        private static void SerializeToXml(StructureMap structureMap, string mapFilePath)
        {
            try
            {
                string xmlOutput = _xmlSerializer.SerializeToString(structureMap); // Convert to XML
                string outputFilePath = Path.ChangeExtension(mapFilePath, ".xml"); // Save as .dotnet.xml
                File.WriteAllText(outputFilePath, xmlOutput); // Write XML output to a file
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing {mapFilePath} to XML: {ex.Message}");
            }
        }

        // Function to serialize a StructureMap to JSON format.
        private static void SerializeToJson(StructureMap structureMap, string mapFilePath)
        {
            try
            {
                string jsonOutput = _jsonSerializer.SerializeToString(structureMap); // Convert to JSON
                string outputFilePath = Path.ChangeExtension(mapFilePath, ".json"); // Save as .dotnet.json
                File.WriteAllText(outputFilePath, jsonOutput); // Write JSON output to a file
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing {mapFilePath} to JSON: {ex.Message}");
            }
        }
    }
}
