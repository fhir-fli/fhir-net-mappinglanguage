using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.MappingLanguage;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Snapshot;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Validation;
using FhirModel = Hl7.Fhir.Model;

namespace Grey.TutorialTests
{
    class Program
    {
        // Class-level fields
        private static FhirXmlParser _xmlParser = new FhirXmlParser();
        private static FhirJsonParser _jsonParser = new FhirJsonParser();
        private static FhirXmlSerializer _xmlSerializer = new FhirXmlSerializer(new SerializerSettings { Pretty = true });
        private static FhirJsonSerializer _jsonSerializer = new FhirJsonSerializer(new SerializerSettings { Pretty = true });
        private static IStructureDefinitionSummaryProvider _summaryProvider = default!;
        private static IResourceResolver _resolver = default!;

        [STAThread]
        static async System.Threading.Tasks.Task Main()
        {
            string baseDirectory = @"/home/grey/dev/fhir-net-mappinglanguage/Grey.TutorialTests/maptutorial";

            // Iterate through steps 1 to 13
            for (int step = 1; step <= 1; step++)
            {
                string stepDirectory = Path.Combine(baseDirectory, $"step{step}");
                string logicalDirectory = Path.Combine(stepDirectory, "logical");
                string mapDirectory = Path.Combine(stepDirectory, "map");
                string sourceDirectory = Path.Combine(stepDirectory, "source");
                string resultDirectory = Path.Combine(stepDirectory, "result");

                // Ensure the result directory exists
                Directory.CreateDirectory(resultDirectory);

                // Process files for each step
                await ProcessFiles(logicalDirectory, mapDirectory, sourceDirectory, resultDirectory);
            }
        }

        private static async System.Threading.Tasks.Task ProcessFiles(string logicalDirectory, string mapDirectory, string sourceDirectory, string resultDirectory)
        {
            // Load and adjust StructureDefinitions
            _resolver = CreateResolver(logicalDirectory);
            _summaryProvider = new StructureDefinitionSummaryProvider(_resolver);

            // Load all mapping files (.xml and .json)
            string[] xmlMapFiles = Directory.GetFiles(mapDirectory, "*.xml");
            string[] jsonMapFiles = Directory.GetFiles(mapDirectory, "*.json");
            string[] mapFiles = xmlMapFiles.Concat(jsonMapFiles).ToArray();

            foreach (string mapFile in mapFiles)
            {
                // Parse the StructureMap
                FhirModel.StructureMap structureMap = ParseStructureMap(mapFile);

                // Process source files (XML and JSON)
                await ProcessSourceFiles(structureMap, sourceDirectory, resultDirectory, true);  // XML
                await ProcessSourceFiles(structureMap, sourceDirectory, resultDirectory, false); // JSON
            }

            await System.Threading.Tasks.Task.CompletedTask;
        }


        private static async System.Threading.Tasks.Task ProcessSourceFiles(FhirModel.StructureMap structureMap, string sourceDirectory, string resultDirectory, bool useXml)
        {
            // Load all source files
            string[] sourceFiles = Directory.GetFiles(sourceDirectory, useXml ? "*.xml" : "*.json");
            foreach (string sourceFile in sourceFiles)
            {
                Console.WriteLine($"Processing source file: {sourceFile}");

                // Parse the source resource
                ITypedElement sourceElement;
                try
                {
                    sourceElement = ParseSourceResource(sourceFile, useXml, structureMap);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during source resource parsing: {ex.Message}");
                    continue;  // Skip to the next file if there's an error
                }

                // Prepare the target resource
                string targetResourceType = GetTargetResourceType(structureMap);
                Console.WriteLine($"Detected target resource type: {targetResourceType}");

                var targetElement = ElementNode.Root(_summaryProvider, targetResourceType);

                // Create a worker context
                var worker = new TestWorker(_resolver);
                var engine = new StructureMapUtilitiesExecute(worker);

                try
                {
                    engine.transform(null, sourceElement, structureMap, targetElement);

                    // Serialize the result to XML and JSON
                    string xmlResult = targetElement.ToXml(new FhirXmlSerializationSettings { Pretty = true });
                    string jsonResult = targetElement.ToJson(new FhirJsonSerializationSettings { Pretty = true });

                    // Write the results to the result directory
                    string sourceFileName = Path.GetFileNameWithoutExtension(sourceFile);
                    string mapFileName = Path.GetFileNameWithoutExtension(structureMap.Url ?? "unknown_map");
                    string resultBaseName = $"{sourceFileName}_using_{mapFileName}";

                    string xmlResultFile = Path.Combine(resultDirectory, $"{resultBaseName}.result.xml");
                    string jsonResultFile = Path.Combine(resultDirectory, $"{resultBaseName}.result.json");

                    File.WriteAllText(xmlResultFile, xmlResult);
                    File.WriteAllText(jsonResultFile, jsonResult);

                    Console.WriteLine($"Successfully transformed {sourceFileName} using {mapFileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error transforming {sourceFile} using {structureMap.Url}: {ex.Message}");
                }
            }

            await System.Threading.Tasks.Task.CompletedTask;
        }

        private static Dictionary<string, string> _typeToCanonicalMap = new Dictionary<string, string>();

        private static IResourceResolver CreateResolver(string logicalDirectory)
        {
            // Create a DirectorySource for the custom definitions
            var customSource = new DirectorySource(logicalDirectory, new DirectorySourceSettings
            {
                IncludeSubDirectories = true,
                Mask = "*.xml;*.json"
            });

            // Load the core FHIR definitions
            string coreDefinitionsPath = @"/path/to/core/definitions"; // Update this path
            var coreSource = new DirectorySource(coreDefinitionsPath, new DirectorySourceSettings
            {
                IncludeSubDirectories = true,
                Mask = "*.xml;*.json"
            });

            // Combine the custom and core sources
            var resolver = new CachedResolver(new MultiResolver(
                customSource,
                coreSource
            ));

            // Create a SnapshotGenerator
            var generator = new SnapshotGenerator(resolver);

            // Get all StructureDefinition files in the logical directory
            string[] sdFiles = Directory.GetFiles(logicalDirectory, "*.xml")
                .Concat(Directory.GetFiles(logicalDirectory, "*.json"))
                .ToArray();

            foreach (var sdFile in sdFiles)
            {
                FhirModel.StructureDefinition sd;

                string content = File.ReadAllText(sdFile);

                if (sdFile.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    sd = _xmlParser.Parse<FhirModel.StructureDefinition>(content);
                }
                else if (sdFile.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    sd = _jsonParser.Parse<FhirModel.StructureDefinition>(content);
                }
                else
                {
                    continue; // Skip unsupported file formats
                }

                // Log the URL of the StructureDefinition
                Console.WriteLine($"Loaded StructureDefinition for {sd.Url}");

                // Ensure the StructureDefinition has a snapshot
                if (!sd.HasSnapshot)
                {
                    Console.WriteLine($"Generating snapshot for {sd.Url}");
                    generator.UpdateAsync(sd).Wait(); // Wait until the snapshot is generated
                }
                else
                {
                    Console.WriteLine($"Snapshot already available for {sd.Url}");
                }

                // Extract the type (e.g., "TLeft") and URL (canonical)
                string typeName = sd.Name ?? sd.Type;
                if (!string.IsNullOrEmpty(typeName) && !string.IsNullOrEmpty(sd.Url))
                {
                    _typeToCanonicalMap[typeName] = sd.Url;  // Store in dictionary
                    Console.WriteLine($"Mapped {typeName} to {sd.Url}");
                }
            }

            // Create the StructureDefinitionSummaryProvider with dynamic mapping
            _summaryProvider = new StructureDefinitionSummaryProvider(resolver, (string name, out string canonical) =>
            {
                if (_typeToCanonicalMap.TryGetValue(name, out canonical))
                {
                    return true;
                }
                else
                {
                    return StructureDefinitionSummaryProvider.DefaultTypeNameMapper(name, out canonical);
                }
            });

            return resolver;
        }

        private static FhirModel.StructureMap ParseStructureMap(string mapFile)
        {
            string content = File.ReadAllText(mapFile);
            FhirModel.StructureMap structureMap;

            if (mapFile.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                structureMap = _xmlParser.Parse<FhirModel.StructureMap>(content);
            }
            else if (mapFile.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                structureMap = _jsonParser.Parse<FhirModel.StructureMap>(content);
            }
            else
            {
                throw new NotSupportedException("Unsupported mapping file format. Only .xml and .json files are supported.");
            }

            return structureMap;
        }

        private static ITypedElement ParseSourceResource(string sourceFile, bool useXml, FhirModel.StructureMap structureMap)
        {
            Console.WriteLine($"Parsing source file: {sourceFile}, Use XML: {useXml}");
            string content = File.ReadAllText(sourceFile);
            FhirModel.Resource resource;

            if (useXml)
            {
                Console.WriteLine("Parsing XML...");
                resource = _xmlParser.Parse<FhirModel.Resource>(content);
            }
            else
            {
                Console.WriteLine("Parsing JSON...");
                resource = _jsonParser.Parse<FhirModel.Resource>(content);
            }

            Console.WriteLine($"Parsed resource type: {resource.TypeName}");

            // Serialize the resource to XML for uniform processing
            var serializedXml = _xmlSerializer.SerializeToString(resource);
            var sourceNode = FhirXmlNode.Parse(serializedXml);

            // Get the type from StructureMap
            string sourceType = structureMap.Group.FirstOrDefault()?
                .Input.FirstOrDefault(i => i.Mode == FhirModel.StructureMap.StructureMapInputMode.Source)?
                .Type;

            if (string.IsNullOrEmpty(sourceType))
            {
                throw new Exception("Unable to determine the source type from the StructureMap.");
            }

            Console.WriteLine($"Detected source type from StructureMap: {sourceType}");

            // Check if the source type has a matching canonical URL in the summary provider
            string canonical;
            if (!_typeToCanonicalMap.TryGetValue(sourceType, out canonical))
            {
                Console.WriteLine($"No canonical URL found for source type: {sourceType}");
            }
            else
            {
                Console.WriteLine($"Canonical URL for {sourceType}: {canonical}");
            }

            // Attempt to convert the source to ITypedElement using the type from StructureMap
            Console.WriteLine($"Attempting to convert sourceNode to ITypedElement with type: {sourceType}...");
            try
            {
                ITypedElement sourceElement = sourceNode.ToTypedElement(_summaryProvider, sourceType);
                Console.WriteLine($"Successfully converted sourceNode to ITypedElement.");
                return sourceElement;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to convert sourceNode to ITypedElement. Error: {ex.Message}");
                throw;
            }
        }

        private static string GetSourceResourceType(FhirModel.StructureMap structureMap)
        {
            return structureMap.Group.FirstOrDefault()?
                .Input.FirstOrDefault(i => i.Mode == FhirModel.StructureMap.StructureMapInputMode.Source)?
                .Type ?? "Bundle";
        }

        private static string GetTargetResourceType(FhirModel.StructureMap structureMap)
        {
            return structureMap.Group.FirstOrDefault()?
                .Input.FirstOrDefault(i => i.Mode == FhirModel.StructureMap.StructureMapInputMode.Target)?
                .Type ?? "Bundle";
        }

        // Implement the IWorkerContext interface
        public class TestWorker : StructureMapUtilitiesAnalyze.IWorkerContext
        {
            private readonly IResourceResolver _resolver;

            public TestWorker(IResourceResolver resolver)
            {
                _resolver = resolver;
            }

            public T fetchResource<T>(string uri) where T : FhirModel.Resource
            {
                return _resolver.ResolveByUri(uri) as T;
            }

            public T fetchResourceWithException<T>(string uri) where T : FhirModel.Resource
            {
                var resource = _resolver.ResolveByUri(uri) as T;
                if (resource == null)
                {
                    throw new Exception($"Resource not found: {uri}");
                }
                return resource;
            }

            public FhirModel.StructureDefinition fetchTypeDefinition(string typeName)
            {
                return _resolver.ResolveByCanonicalUri($"http://hl7.org/fhir/StructureDefinition/{typeName}") as FhirModel.StructureDefinition;
            }

            public FhirModel.ValueSet.ExpansionComponent expandVS(FhirModel.ValueSet vs, bool cacheOk, bool heiarchical)
            {
                throw new NotImplementedException();
            }

            public ValidationResult validateCode(TerminologyServiceOptions options, string system, string code, string display)
            {
                throw new NotImplementedException();
            }

            public string getOverrideVersionNs()
            {
                return null;
            }

            public IEnumerable<FhirModel.StructureMap> listTransforms(string url)
            {
                return Enumerable.Empty<FhirModel.StructureMap>();
            }

            public FhirModel.StructureMap getTransform(string url)
            {
                throw new NotImplementedException();
            }

            public string oid2Uri(string oid)
            {
                throw new NotImplementedException();
            }
        }
    }
}
