using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System;
using System.IO;

class Program
{
    static void Main()
    {
        string baseDirectory = @"/home/grey/dev/fhir-net-mappinglanguage/Test.Hl7.Fhir.MappingLanguage/maptutorial";
        var xmlParser = new FhirXmlParser();
        var jsonSerializer = new FhirJsonSerializer();

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

                        // Serialize the resource to JSON
                        string jsonOutput = jsonSerializer.SerializeToString(resource);

                        // Save the JSON to a new file (in the same directory)
                        string jsonFilePath = Path.ChangeExtension(xmlFile, ".json");
                        File.WriteAllText(jsonFilePath, jsonOutput);

                        Console.WriteLine($"Converted: {xmlFile} -> {jsonFilePath}");
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
}
