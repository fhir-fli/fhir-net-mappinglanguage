# Tutorial Testing

1. Started with xml StructureDefinitions from the Matchbox [fhir-mapping-tutorial](https://github.com/ahdis/fhir-mapping-tutorial/tree/master)
2. Used dotnet to convert these to json
3. Started with .map files from the Matchbox [fhir-mapping-tutorial](https://github.com/ahdis/fhir-mapping-tutorial/tree/master)
    - Used both Matchbox and dotnet to convert to StructureMaps in xml and json
    - There were some maps that didn't start with ```map``` and I couldn't get dotnet to parse them
4. Sources I again started with the xml files the Matchbox [fhir-mapping-tutorial](https://github.com/ahdis/fhir-mapping-tutorial/tree/master)
    - Converted them manually using [this FHIR in XML to JSON converter](https://fhir-formats.github.io/#) (I wasn't able to figure out how to get dotnet to do it for me)
    - If you wanted to double-check #12 for me, that would be probably be good