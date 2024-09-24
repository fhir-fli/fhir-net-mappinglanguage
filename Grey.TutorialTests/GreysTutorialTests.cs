// using Hl7.Fhir.ElementModel;
// using Hl7.Fhir.MappingLanguage;
// using Hl7.Fhir.Model;
// using Hl7.Fhir.Rest;
// using Hl7.Fhir.Serialization;
// using Hl7.Fhir.Specification;
// using Hl7.Fhir.Specification.Source;
// using Microsoft.VisualStudio.TestTools.UnitTesting;
// using System.IO;

// namespace Test.FhirMappingLanguage
// {
//     [TestClass]
//     public class GreysTutorialTests
//     {
//         private FhirXmlSerializer _xmlSerializer = new FhirXmlSerializer(new SerializerSettings() { Pretty = true });
//         private FhirXmlParser _xmlParser = new FhirXmlParser();
//         private FhirJsonParser _jsonParser = new FhirJsonParser();
//         const string mappingtutorial_folder = @"/home/grey/dev/fhir-net-mappinglanguage/Test.Hl7.Fhir.MappingLanguage/maptutorial";

//         internal static StructureMapUtilitiesAnalyze.IWorkerContext CreateWorker()
//         {
//             var source = new CachedResolver(new MultiResolver(
//                 new DirectorySource(@"/home/grey/temp/analyzetests"),
//                 ZipSource.CreateValidationSource()
//                 ));
//             source.Load += Source_Load;
//             var worker = new TestWorker(source);
//             return worker;
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

//         [TestMethod]
//         public void Tutorial_Step1()
//         {
//             var parser = new StructureMapUtilitiesParse();

//             // Updated paths to your map files
//             var mapStep1 = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}/step1/map/step1.map");
//             var sm1 = parser.parse(mapStep1, "Step1");
//             System.IO.File.WriteAllText(
//                 @$"{mappingtutorial_folder}/step1/map/step1.xml.new",
//                 _xmlSerializer.SerializeToString(sm1));

//             var mapStep1b = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}/step1/map/step1b.map");
//             var sm1b = parser.parse(mapStep1b, "Step1b");
//             System.IO.File.WriteAllText(
//                 @$"{mappingtutorial_folder}/step1/map/step1b.xml.new",
//                 _xmlSerializer.SerializeToString(sm1b));

//             var source1 = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}/step1/source/source.json");
//             var sourceNode = FhirJsonNode.Parse(source1);

//             var source = new CachedResolver(new MultiResolver(
//                new DirectorySource(@$"{mappingtutorial_folder}/step1/logical"),
//                ZipSource.CreateValidationSource()
//                ));
//             source.Load += Source_Load;
//             var worker = new TestWorker(source);

//             IStructureDefinitionSummaryProvider provider = new StructureDefinitionSummaryProvider(
//                 source,
//                 (string name, out string canonical) =>
//                 {
//                     switch (name)
//                     {
//                         case "TLeft1":
//                             canonical = "http://hl7.org/fhir/StructureDefinition/tutorial-left1";
//                             return true;
//                         case "TRight1":
//                             canonical = "http://hl7.org/fhir/StructureDefinition/tutorial-right1";
//                             return true;
//                     }
//                     return StructureDefinitionSummaryProvider.DefaultTypeNameMapper(name, out canonical);
//                 });
//             var engine = new StructureMapUtilitiesExecute(worker, null, provider);

//             // First Step
//             var target = ElementNode.Root(provider, "TRight1");
//             try
//             {
//                 engine.transform(null, sourceNode.ToTypedElement(provider), sm1, target);
//             }
//             catch (System.Exception ex)
//             {
//                 // Output the error message to the trace
//                 System.Diagnostics.Trace.WriteLine($"Error during Step 1 transformation: {ex.Message}");
//             }

//             var xml2 = target.ToXml(new FhirXmlSerializationSettings() { Pretty = true });

//             // Output the result to the trace
//             System.Diagnostics.Trace.WriteLine("Step 1 Transformation Result:");
//             System.Diagnostics.Trace.WriteLine(xml2);

//             // Ensure the directory exists before saving the result
//             string resultDirectory = @$"{mappingtutorial_folder}/step1/result/";
//             if (!Directory.Exists(resultDirectory))
//             {
//                 Directory.CreateDirectory(resultDirectory);
//             }
//             // Save the result to a file
//             System.IO.File.WriteAllText(@$"{resultDirectory}/step1_result.xml", xml2);

//             // Now for Step 1b
//             target = ElementNode.Root(provider, "TRight1");
//             try
//             {
//                 engine.transform(null, sourceNode.ToTypedElement(provider), sm1b, target);
//             }
//             catch (System.Exception ex)
//             {
//                 // Output the error message to the trace
//                 System.Diagnostics.Trace.WriteLine($"Error during Step 1b transformation: {ex.Message}");
//             }

//             xml2 = target.ToXml(new FhirXmlSerializationSettings() { Pretty = true });

//             // Output the result to the trace
//             System.Diagnostics.Trace.WriteLine("Step 1b Transformation Result:");
//             System.Diagnostics.Trace.WriteLine(xml2);

//             // Save the result to a file
//             System.IO.File.WriteAllText(@$"{resultDirectory}/step1b_result.xml", xml2);
//         }

//         [TestMethod]
//         public void Tutorial_Step7()
//         {
//             var parser = new StructureMapUtilitiesParse();

//             // Updated paths to your map files
//             var mapStep7 = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}/step7/map/step7.map");
//             var sm7 = parser.parse(mapStep7, "Step7");
//             System.IO.File.WriteAllText(
//                 @$"{mappingtutorial_folder}/step7/map/step7.xml.new",
//                 _xmlSerializer.SerializeToString(sm7));

//             var mapStep7b = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}/step7/map/step7b.map");
//             var sm7b = parser.parse(mapStep7b, "Step7b");
//             System.IO.File.WriteAllText(
//                 @$"{mappingtutorial_folder}/step7/map/step7b.xml.new",
//                 _xmlSerializer.SerializeToString(sm7b));

//             var source7 = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}/step7/source/source.json");
//             var sourceNode = FhirJsonNode.Parse(source7);

//             var source = new CachedResolver(new MultiResolver(
//                new DirectorySource(@$"{mappingtutorial_folder}/step7/logical"),
//                ZipSource.CreateValidationSource()
//                ));
//             source.Load += Source_Load;
//             var worker = new TestWorker(source);

//             IStructureDefinitionSummaryProvider provider = new StructureDefinitionSummaryProvider(
//                 source,
//                 (string name, out string canonical) =>
//                 {
//                     switch (name)
//                     {
//                         case "TLeft":
//                             canonical = "http://hl7.org/fhir/StructureDefinition/tutorial-left-7";
//                             return true;
//                         case "TRight1":
//                             canonical = "http://hl7.org/fhir/StructureDefinition/tutorial-right-7";
//                             return true;
//                     }
//                     return StructureDefinitionSummaryProvider.DefaultTypeNameMapper(name, out canonical);
//                 });
//             var engine = new StructureMapUtilitiesExecute(worker, null, provider);

//             // First Step
//             var target = ElementNode.Root(provider, "TRight");
//             try
//             {
//                 engine.transform(null, sourceNode.ToTypedElement(provider), sm7, target);
//             }
//             catch (System.Exception ex)
//             {
//                 // Output the error message to the trace
//                 System.Diagnostics.Trace.WriteLine($"Error during Step 7 transformation: {ex.Message}");
//             }

//             var xml2 = target.ToXml(new FhirXmlSerializationSettings() { Pretty = true });

//             // Output the result to the trace
//             System.Diagnostics.Trace.WriteLine("Step 7 Transformation Result:");
//             System.Diagnostics.Trace.WriteLine(xml2);

//             // Ensure the directory exists before saving the result
//             string resultDirectory = @$"{mappingtutorial_folder}/step7/result/";
//             if (!Directory.Exists(resultDirectory))
//             {
//                 Directory.CreateDirectory(resultDirectory);
//             }
//             // Save the result to a file
//             System.IO.File.WriteAllText(@$"{resultDirectory}/step7_result.xml", xml2);

//             // Now for Step 7b
//             target = ElementNode.Root(provider, "TRight");
//             try
//             {
//                 engine.transform(null, sourceNode.ToTypedElement(provider), sm7b, target);
//             }
//             catch (System.Exception ex)
//             {
//                 // Output the error message to the trace
//                 System.Diagnostics.Trace.WriteLine($"Error during Step 7b transformation: {ex.Message}");
//             }

//             xml2 = target.ToXml(new FhirXmlSerializationSettings() { Pretty = true });

//             // Output the result to the trace
//             System.Diagnostics.Trace.WriteLine("Step 7b Transformation Result:");
//             System.Diagnostics.Trace.WriteLine(xml2);

//             // Save the result to a file
//             System.IO.File.WriteAllText(@$"{resultDirectory}/step7b_result.xml", xml2);
//         }


//     }
// }
