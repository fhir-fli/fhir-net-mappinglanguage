// using System;
// using System.IO;
// using System.Linq;
// using System.Threading.Tasks;
// using Hl7.Fhir.ElementModel;
// using Hl7.Fhir.MappingLanguage;
// using Hl7.Fhir.Model;
// using Hl7.Fhir.Serialization;
// using Hl7.Fhir.Specification;
// using Hl7.Fhir.Specification.Source;

// namespace Grey.TutorialTests
// {
//     class Program
//     {
//         // Class-level fields
//         private static FhirXmlParser _xmlParser = new FhirXmlParser();
//         private static FhirJsonParser _jsonParser = new FhirJsonParser();
//         private static FhirXmlSerializer _xmlSerializer = new FhirXmlSerializer(new SerializerSettings { Pretty = true });
//         private static FhirJsonSerializer _jsonSerializer = new FhirJsonSerializer(new SerializerSettings { Pretty = true });

//         static async Task Main()
//         {
//             string baseDirectory = @"/home/grey/dev/fhir-net-mappinglanguage/Grey.TutorialTests/maptutorial";

//             // Iterate through steps 1 to 13
//             for (int step = 1; step <= 13; step++)
//             {
//                 string stepDirectory = Path.Combine(baseDirectory, $"step{step}");
//                 string logicalDirectory = Path.Combine(stepDirectory, "logical");
//                 string mapDirectory = Path.Combine(stepDirectory, "map");
//                 string sourceDirectory = Path.Combine(stepDirectory, "source");
//                 string resultDirectory = Path.Combine(stepDirectory, "result");

//                 // Ensure the result directory exists
//                 Directory.CreateDirectory(resultDirectory);

//                 // Process XML files first, then JSON files
//                 await ProcessFiles(logicalDirectory, mapDirectory, sourceDirectory, resultDirectory, true);  // XML files
//                 await ProcessFiles(logicalDirectory, mapDirectory, sourceDirectory, resultDirectory, false); // JSON files
//             }
//         }

//         private static async Task ProcessFiles(string logicalDirectory, string mapDirectory, string sourceDirectory, string resultDirectory, bool useXml)
//         {
//             // Load all StructureDefinitions from the logical directory
//             IResourceResolver resolver = CreateResolver(logicalDirectory, useXml);
//             IStructureDefinitionSummaryProvider provider = new StructureDefinitionSummaryProvider(resolver);

//             // Initialize the FHIR parsers and serializers
//             FhirXmlParser xmlParser = new FhirXmlParser();
//             FhirJsonParser jsonParser = new FhirJsonParser();
//             FhirXmlSerializer xmlSerializer = new FhirXmlSerializer(new SerializerSettings { Pretty = true });
//             FhirJsonSerializer jsonSerializer = new FhirJsonSerializer(new SerializerSettings { Pretty = true });

//             // Load all map files
//             string[] mapFiles = Directory.GetFiles(mapDirectory, useXml ? "*.xml" : "*.json");
//             foreach (string mapFile in mapFiles)
//             {
//                 // Parse the StructureMap
//                 StructureMap structureMap = ParseStructureMap(mapFile, useXml);

//                 // Load all source files
//                 string[] sourceFiles = Directory.GetFiles(sourceDirectory, useXml ? "*.xml" : "*.json");
//                 foreach (string sourceFile in sourceFiles)
//                 {
//                     // Parse the source resource
//                     ITypedElement sourceElement = ParseSourceResource(sourceFile, provider, useXml);

//                     // Prepare the target resource
//                     string targetResourceType = GetTargetResourceType(structureMap);
//                     var targetElement = ElementNode.Root(provider, targetResourceType);

//                     // Create a worker context
//                     var worker = new TestWorker(resolver);

//                     // Execute the transformation
//                     var engine = new StructureMapUtilitiesExecute(worker, null, provider);
//                     try
//                     {
//                         engine.transform(null, sourceElement, structureMap, targetElement);

//                         // Serialize the result to XML and JSON
//                         string xmlResult = targetElement.ToXml(new FhirXmlSerializationSettings { Pretty = true });
//                         string jsonResult = targetElement.ToJson(new FhirJsonSerializationSettings { Pretty = true });

//                         // Write the results to the result directory
//                         string sourceFileName = Path.GetFileNameWithoutExtension(sourceFile);
//                         string mapFileName = Path.GetFileNameWithoutExtension(mapFile);
//                         string resultBaseName = $"{sourceFileName}_using_{mapFileName}";

//                         string xmlResultFile = Path.Combine(resultDirectory, $"{resultBaseName}.result.xml");
//                         string jsonResultFile = Path.Combine(resultDirectory, $"{resultBaseName}.result.json");

//                         File.WriteAllText(xmlResultFile, xmlResult);
//                         File.WriteAllText(jsonResultFile, jsonResult);

//                         Console.WriteLine($"Successfully transformed {sourceFileName} using {mapFileName}");
//                     }
//                     catch (Exception ex)
//                     {
//                         Console.WriteLine($"Error transforming {sourceFile} using {mapFile}: {ex.Message}");
//                     }
//                 }
//             }
//         }

//         private static IResourceResolver CreateResolver(string logicalDirectory, bool useXml)
//         {
//             // Create a resolver that includes your custom definitions
//             var settings = new DirectorySourceSettings
//             {
//                 IncludeSubDirectories = true,
//                 Mask = useXml ? "*.xml" : "*.json"
//             };
//             var source = new DirectorySource(logicalDirectory, settings);
//             source.Load += Source_Load;

//             var resolver = new CachedResolver(new MultiResolver(
//                 source,
//                 ZipSource.CreateValidationSource()
//             ));

//             return resolver;
//         }

//         private static void Source_Load(object sender, CachedResolver.LoadResourceEventArgs e)
//         {
//             if (e.Resource is StructureDefinition sd)
//             {
//                 sd.Abstract = false;
//                 if (sd.Snapshot == null)
//                 {
//                     sd.Snapshot = new StructureDefinition.SnapshotComponent();
//                     sd.Snapshot.Element.AddRange(sd.Differential.Element);
//                 }
//             }
//         }

//         private static StructureMap ParseStructureMap(string mapFile, bool useXml)
//         {
//             string mapContent = File.ReadAllText(mapFile);
//             if (useXml)
//             {
//                 return _xmlParser.Parse<StructureMap>(mapContent);
//             }
//             else
//             {
//                 return _jsonParser.Parse<StructureMap>(mapContent);
//             }
//         }

//         private static ITypedElement ParseSourceResource(string sourceFile, IStructureDefinitionSummaryProvider provider, bool useXml)
//         {
//             string content = File.ReadAllText(sourceFile);
//             if (useXml)
//             {
//                 var resource = _xmlParser.Parse<Resource>(content);
//                 return resource.ToTypedElement(provider);
//             }
//             else
//             {
//                 var resource = _jsonParser.Parse<Resource>(content);
//                 return resource.ToTypedElement(provider);
//             }
//         }

//         private static string GetTargetResourceType(StructureMap structureMap)
//         {
//             // Assume that the target resource type is specified in the StructureMap's group rule
//             // This may need to be adjusted based on how your StructureMaps are defined
//             return structureMap.Group.FirstOrDefault()?.Input.FirstOrDefault(i => i.Mode == StructureMap.StructureMapGroupInputMode.Target)?.Type ?? "Bundle";
//         }

//         // Implement a simple IWorkerContext for the mapping engine
//         public class TestWorker : StructureMapUtilitiesExecute.IWorkerContext
//         {
//             private readonly IResourceResolver _resolver;

//             public TestWorker(IResourceResolver resolver)
//             {
//                 _resolver = resolver;
//             }

//             public CodeSystem FetchCodeSystem(string system)
//             {
//                 return _resolver.ResolveByUri(system) as CodeSystem;
//             }

//             public ValueSet FetchValueSet(string uri)
//             {
//                 return _resolver.ResolveByUri(uri) as ValueSet;
//             }

//             public StructureDefinition FetchTypeDefinition(string typeName)
//             {
//                 return _resolver.FindStructureDefinitionForCoreType(typeName);
//             }

//             public StructureDefinition FetchResource(string uri)
//             {
//                 return _resolver.ResolveByUri(uri) as StructureDefinition;
//             }

//             public IEnumerable<StructureDefinition> AllStructures()
//             {
//                 return Enumerable.Empty<StructureDefinition>();
//             }

//             public NamingSystem FetchNamingSystem(string uri)
//             {
//                 return _resolver.ResolveByUri(uri) as NamingSystem;
//             }
//         }
//     }
// }
