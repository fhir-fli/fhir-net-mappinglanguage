using Hl7.Fhir.ElementModel;
using Hl7.Fhir.MappingLanguage;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Source;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Serialization;

namespace Test.FhirMappingLanguage
{
    [TestClass]
    public class TutorialTests
    {
        private FhirXmlSerializer _xmlSerializer = new FhirXmlSerializer(new SerializerSettings() { Pretty = true });
        private FhirXmlParser _xmlParser = new FhirXmlParser();
        private FhirJsonParser _jsonParser = new FhirJsonParser();
        const string mappingtutorial_folder = @"C:\git\hl7\fhir-mapping-tutorial";
        // const string mappingtutorial_folder = @"E:\git\OpenSource\fhir-mapping-tutorial-master";

        internal static StructureMapUtilitiesAnalyze.IWorkerContext CreateWorker()
        {
            var source = new CachedResolver(new MultiResolver(
                new DirectorySource(@"c:\temp\analyzetests"),
                ZipSource.CreateValidationSource()
                ));
            source.Load += Source_Load;
            var worker = new TestWorker(source);
            return worker;
        }

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

        [TestMethod]
        public void Transform_qr2patgender()
        {
            var expression = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}\qrtopat\map\qr2patgender.map");
            var qr = _jsonParser.Parse<QuestionnaireResponse>(System.IO.File.ReadAllText(@$"{mappingtutorial_folder}\qrtopat\qr.json"));

            var parser = new StructureMapUtilitiesParse();
            var sm = parser.parse(expression, null);
            var worker = CreateWorker();
            var provider = new PocoStructureDefinitionSummaryProvider();
            var engine = new StructureMapUtilitiesExecute(worker, null, provider);
            var output = new Patient();
            var target = ElementNode.FromElement(output.ToTypedElement());

            try
            {
                engine.transform(null, qr.ToTypedElement(), sm, target);
                target.ToPoco().CopyTo(output);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
            var xml2 = _xmlSerializer.SerializeToString(output);
            System.Diagnostics.Trace.WriteLine(xml2);
        }

        [TestMethod]
        public void Transform_medicationrequest()
        {
            var worker = CreateWorker();
            var expression = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}\medicationrequest\extension.map");
            var qr = _jsonParser.Parse<MedicationRequest>(System.IO.File.ReadAllText(@$"{mappingtutorial_folder}\medicationrequest\source.json"));

            var parser = new StructureMapUtilitiesParse();
            var sm = parser.parse(expression, null);
            var provider = new PocoStructureDefinitionSummaryProvider();
            var engine = new StructureMapUtilitiesExecute(worker, null, provider);
            var output = new MedicationRequest();
            var target = ElementNode.FromElement(output.ToTypedElement());
            try
            {
                engine.transform(null, qr.ToTypedElement(), sm, target);
                target.ToPoco().CopyTo(output);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
            var xml2 = _xmlSerializer.SerializeToString(output);
            System.Diagnostics.Trace.WriteLine(xml2);
        }

        [TestMethod]
        public void Tutorial_Step1()
        {
            var parser = new StructureMapUtilitiesParse();
            var mapStep1 = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}\maptutorial\step1\map\step1.map");
            var sm1 = parser.parse(mapStep1, "Step1");
            System.IO.File.WriteAllText(
                @$"{mappingtutorial_folder}\maptutorial\step1\map\step1.xml.new",
                _xmlSerializer.SerializeToString(sm1));

            var mapStep1b = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}\maptutorial\step1\map\step1b.map");
            var sm1b = parser.parse(mapStep1, "Step1");
            System.IO.File.WriteAllText(
                @$"{mappingtutorial_folder}\maptutorial\step1\map\step1b.xml.new",
                _xmlSerializer.SerializeToString(sm1b));

            var source1 = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}\maptutorial\step1\source\source1.xml");
            var sourceNode = FhirXmlNode.Parse(source1);

            var source = new CachedResolver(new MultiResolver(
               new DirectorySource(@$"{mappingtutorial_folder}\maptutorial\step1\logical"),
               ZipSource.CreateValidationSource()
               ));
            source.Load += Source_Load;
            var worker = new TestWorker(source);

            IStructureDefinitionSummaryProvider provider = new StructureDefinitionSummaryProvider(
                source,
                (string name, out string canonical) =>
                {
                    switch (name)
                    {
                        case "TLeft":
                            canonical = "http://hl7.org/fhir/StructureDefinition/tutorial-left";
                            return true;
                        case "TRight":
                            canonical = "http://hl7.org/fhir/StructureDefinition/tutorial-right";
                            return true;
                    }
                    return StructureDefinitionSummaryProvider.DefaultTypeNameMapper(name, out canonical);
                });
            var engine = new StructureMapUtilitiesExecute(worker, null, provider);
            // var ti = provider.Provide("http://hl7.org/fhir/StructureDefinition/tutorial-left");

            var target = ElementNode.Root(provider, "TRight");
            try
            {
                engine.transform(null, sourceNode.ToTypedElement(provider), sm1, target);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
            var xml2 = target.ToXml(new FhirXmlSerializationSettings() { Pretty = true });
            // var xml2 = target.ToJson(new FhirJsonSerializationSettings() { Pretty = true });
            System.Diagnostics.Trace.WriteLine(xml2);


            // Now for Step 1b
            target = ElementNode.Root(provider, "TRight");
            try
            {
                engine.transform(null, sourceNode.ToTypedElement(provider), sm1b, target);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
            xml2 = target.ToXml(new FhirXmlSerializationSettings() { Pretty = true });
            // var xml2 = target.ToJson(new FhirJsonSerializationSettings() { Pretty = true });
            System.Diagnostics.Trace.WriteLine(xml2);

        }

        [TestMethod]
        public void Tutorial_Step2()
        {
            var parser = new StructureMapUtilitiesParse();
            var mapStep2 = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}\maptutorial\step2\map\step2.map");
            var sm2 = parser.parse(mapStep2, "Step2");
            System.IO.File.WriteAllText(
                @$"{mappingtutorial_folder}\maptutorial\step2\map\step2.xml.new",
                _xmlSerializer.SerializeToString(sm2));

            var source2 = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}\maptutorial\step2\source\source2.xml");
            var sourceNode = FhirXmlNode.Parse(source2);

            var source = new CachedResolver(new MultiResolver(
               new DirectorySource(@$"{mappingtutorial_folder}\maptutorial\step2\logical"),
               ZipSource.CreateValidationSource()
               ));
            source.Load += Source_Load;
            var worker = new TestWorker(source);

            IStructureDefinitionSummaryProvider provider = new StructureDefinitionSummaryProvider(
                source,
                (string name, out string canonical) =>
                {
                    switch (name)
                    {
                        case "TLeft":
                            canonical = "http://hl7.org/fhir/StructureDefinition/tutorial-left";
                            return true;
                        case "TRight":
                            canonical = "http://hl7.org/fhir/StructureDefinition/tutorial-right";
                            return true;
                    }
                    return StructureDefinitionSummaryProvider.DefaultTypeNameMapper(name, out canonical);
                });
            var engine = new StructureMapUtilitiesExecute(worker, null, provider);

            var target = ElementNode.Root(provider, "TRight");
            try
            {
                engine.transform(null, sourceNode.ToTypedElement(provider), sm2, target);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
            var xml2 = target.ToXml(new FhirXmlSerializationSettings() { Pretty = true });
            // var xml2 = target.ToJson(new FhirJsonSerializationSettings() { Pretty = true });
            System.Diagnostics.Trace.WriteLine(xml2);
        }

        [TestMethod]
        public void Tutorial_Step3a()
        {
            var parser = new StructureMapUtilitiesParse();
            var mapStep3a = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}\maptutorial\step3\map\step3a.map");
            var sm3a = parser.parse(mapStep3a, "Step3a");
            System.IO.File.WriteAllText(
                @$"{mappingtutorial_folder}\maptutorial\step3\map\step3a.xml.new",
                _xmlSerializer.SerializeToString(sm3a));

            var source3 = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}\maptutorial\step3\source\source3.xml");
            var sourceNode = FhirXmlNode.Parse(source3);

            var source = new CachedResolver(new MultiResolver(
               new DirectorySource(@$"{mappingtutorial_folder}\maptutorial\step3\logical"),
               ZipSource.CreateValidationSource()
               ));
            source.Load += Source_Load;
            var worker = new TestWorker(source);

            IStructureDefinitionSummaryProvider provider = new StructureDefinitionSummaryProvider(
                source,
                (string name, out string canonical) =>
                {
                    switch (name)
                    {
                        case "TLeft":
                            canonical = "http://hl7.org/fhir/StructureDefinition/tutorial-left";
                            return true;
                        case "TRight":
                            canonical = "http://hl7.org/fhir/StructureDefinition/tutorial-right";
                            return true;
                    }
                    return StructureDefinitionSummaryProvider.DefaultTypeNameMapper(name, out canonical);
                });
            var engine = new StructureMapUtilitiesExecute(worker, null, provider);



            var target = ElementNode.Root(provider, "TRight");
            try
            {
                engine.transform(null, sourceNode.ToTypedElement(provider), sm3a, target);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
            var xml2 = target.ToXml(new FhirXmlSerializationSettings() { Pretty = true });
            // var xml2 = target.ToJson(new FhirJsonSerializationSettings() { Pretty = true });
            System.Diagnostics.Trace.WriteLine(xml2);
        }

        [TestMethod]
        public void Tutorial_Step3b()
        {
            var parser = new StructureMapUtilitiesParse();
            var mapStep3b = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}\maptutorial\step3\map\step3b.map");
            var sm3b = parser.parse(mapStep3b, "Step3b");
            System.IO.File.WriteAllText(
                @$"{mappingtutorial_folder}\maptutorial\step3\map\step3b.xml.new",
                _xmlSerializer.SerializeToString(sm3b));

            var source3 = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}\maptutorial\step3\source\source3.xml");
            var sourceNode = FhirXmlNode.Parse(source3);

            var source = new CachedResolver(new MultiResolver(
               new DirectorySource(@$"{mappingtutorial_folder}\maptutorial\step3\logical"),
               ZipSource.CreateValidationSource()
               ));
            source.Load += Source_Load;
            var worker = new TestWorker(source);

            IStructureDefinitionSummaryProvider provider = new StructureDefinitionSummaryProvider(
                source,
                (string name, out string canonical) =>
                {
                    switch (name)
                    {
                        case "TLeft":
                            canonical = "http://hl7.org/fhir/StructureDefinition/tutorial-left";
                            return true;
                        case "TRight":
                            canonical = "http://hl7.org/fhir/StructureDefinition/tutorial-right";
                            return true;
                    }
                    return StructureDefinitionSummaryProvider.DefaultTypeNameMapper(name, out canonical);
                });
            var engine = new StructureMapUtilitiesExecute(worker, null, provider);

            var target = ElementNode.Root(provider, "TRight");
            try
            {
                engine.transform(null, sourceNode.ToTypedElement(provider), sm3b, target);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
            var xml2 = target.ToXml(new FhirXmlSerializationSettings() { Pretty = true });
            // var xml2 = target.ToJson(new FhirJsonSerializationSettings() { Pretty = true });
            System.Diagnostics.Trace.WriteLine(xml2);
        }

        [TestMethod]
        public void Tutorial_Step3c()
        {
            var parser = new StructureMapUtilitiesParse();
            var mapStep3c = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}\maptutorial\step3\map\step3c.map");
            var sm3c = parser.parse(mapStep3c, "Step3c");
            System.IO.File.WriteAllText(
                @$"{mappingtutorial_folder}\maptutorial\step3\map\step3c.xml.new",
                _xmlSerializer.SerializeToString(sm3c));

            var source3 = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}\maptutorial\step3\source\source3.xml");
            var sourceNode = FhirXmlNode.Parse(source3);

            var source = new CachedResolver(new MultiResolver(
               new DirectorySource(@$"{mappingtutorial_folder}\maptutorial\step3\logical"),
               ZipSource.CreateValidationSource()
               ));
            source.Load += Source_Load;
            var worker = new TestWorker(source);

            IStructureDefinitionSummaryProvider provider = new StructureDefinitionSummaryProvider(
                source,
                (string name, out string canonical) =>
                {
                    switch (name)
                    {
                        case "TLeft":
                            canonical = "http://hl7.org/fhir/StructureDefinition/tutorial-left";
                            return true;
                        case "TRight":
                            canonical = "http://hl7.org/fhir/StructureDefinition/tutorial-right";
                            return true;
                    }
                    return StructureDefinitionSummaryProvider.DefaultTypeNameMapper(name, out canonical);
                });
            var engine = new StructureMapUtilitiesExecute(worker, null, provider);



            var target = ElementNode.Root(provider, "TRight");
            try
            {
                engine.transform(null, sourceNode.ToTypedElement(provider), sm3c, target);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
            var xml2 = target.ToXml(new FhirXmlSerializationSettings() { Pretty = true });
            // var xml2 = target.ToJson(new FhirJsonSerializationSettings() { Pretty = true });
            System.Diagnostics.Trace.WriteLine(xml2);
        }

        [TestMethod]
        public void Tutorial_Step5a()
        {
            var parser = new StructureMapUtilitiesParse();
            var mapStep5 = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}\maptutorial\step5\map\step5.map");
            var sm5 = parser.parse(mapStep5, "Step5");
            System.IO.File.WriteAllText(
                @$"{mappingtutorial_folder}\maptutorial\step5\map\step5.xml.new",
                _xmlSerializer.SerializeToString(sm5));

            var source3 = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}\maptutorial\step5\source\source5.xml");
            var sourceNode = FhirXmlNode.Parse(source3);

            var source = new CachedResolver(new MultiResolver(
               new DirectorySource(@$"{mappingtutorial_folder}\maptutorial\step5\logical"),
               ZipSource.CreateValidationSource()
               ));
            source.Load += Source_Load;
            var worker = new TestWorker(source);

            IStructureDefinitionSummaryProvider provider = new StructureDefinitionSummaryProvider(
                source,
                (string name, out string canonical) =>
                {
                    switch (name)
                    {
                        case "TLeft":
                            canonical = "http://hl7.org/fhir/StructureDefinition/tutorial-left";
                            return true;
                        case "TRight":
                            canonical = "http://hl7.org/fhir/StructureDefinition/tutorial-right";
                            return true;
                    }
                    return StructureDefinitionSummaryProvider.DefaultTypeNameMapper(name, out canonical);
                });
            var engine = new StructureMapUtilitiesExecute(worker, null, provider);



            var target = ElementNode.Root(provider, "TRight");
            try
            {
                engine.transform(null, sourceNode.ToTypedElement(provider), sm5, target);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
            var xml2 = target.ToXml(new FhirXmlSerializationSettings() { Pretty = true });
            // var xml2 = target.ToJson(new FhirJsonSerializationSettings() { Pretty = true });
            System.Diagnostics.Trace.WriteLine(xml2);
        }

        [TestMethod]
        public void Tutorial_Step5b()
        {
            var parser = new StructureMapUtilitiesParse();
            var mapStep5 = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}\maptutorial\step5\map\step5.map");
            var sm5 = parser.parse(mapStep5, "Step5");
            System.IO.File.WriteAllText(
                @$"{mappingtutorial_folder}\maptutorial\step5\map\step5.xml.new",
                _xmlSerializer.SerializeToString(sm5));

            var source3 = System.IO.File.ReadAllText(@$"{mappingtutorial_folder}\maptutorial\step5\source\source5b.xml");
            var sourceNode = FhirXmlNode.Parse(source3);

            var source = new CachedResolver(new MultiResolver(
               new DirectorySource(@$"{mappingtutorial_folder}\maptutorial\step5\logical"),
               ZipSource.CreateValidationSource()
               ));
            source.Load += Source_Load;
            var worker = new TestWorker(source);

            IStructureDefinitionSummaryProvider provider = new StructureDefinitionSummaryProvider(
                source,
                (string name, out string canonical) =>
                {
                    switch (name)
                    {
                        case "TLeft":
                            canonical = "http://hl7.org/fhir/StructureDefinition/tutorial-left";
                            return true;
                        case "TRight":
                            canonical = "http://hl7.org/fhir/StructureDefinition/tutorial-right";
                            return true;
                    }
                    return StructureDefinitionSummaryProvider.DefaultTypeNameMapper(name, out canonical);
                });
            var engine = new StructureMapUtilitiesExecute(worker, null, provider);

            var target = ElementNode.Root(provider, "TRight");
            try
            {
                engine.transform(null, sourceNode.ToTypedElement(provider), sm5, target);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
            var xml2 = target.ToXml(new FhirXmlSerializationSettings() { Pretty = true });
            // var xml2 = target.ToJson(new FhirJsonSerializationSettings() { Pretty = true });
            System.Diagnostics.Trace.WriteLine(xml2);
        }


    }
}