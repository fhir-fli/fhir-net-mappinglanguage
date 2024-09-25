# Tutorial Testing

1. Started with the structures from the Matchbox [fhir-mapping-tutorial](https://github.com/ahdis/fhir-mapping-tutorial/tree/master)
    - StructureDefinitions (```XML```)
    - FHIR mapping (```.map```) files
    - Sources (```XML```)
2. Formatting for consistency & ease of testing
    - TLeft1 -> TLeft
    - TRight1 -> TRight
    - StructureDefinitions are all either 
        - http://hl7.org/fhir/StructureDefinition/tutorial-left
        - http://hl7.org/fhir/StructureDefinition/tutorial-leftinner (step10)
        - http://hl7.org/fhir/StructureDefinition/tutorial-right
        - http://hl7.org/fhir/StructureDefinition/tutorial-rightinner (step10)
3. Used dotnet to convert StructureDefinitions to ```JSON```
4. Used matchbox to convert mapping files to StructureMaps (```XML``` and ```JSON```)
    - There were some maps that didn't start with ```map``` and I couldn't get dotnet to parse them
5. Converted sources manually using [this FHIR in XML to JSON converter](https://fhir-formats.github.io/#)
    - I wasn't able to figure out how to get dotnet to do it for me
    - If you wanted to double-check #12 for me, that would be probably be good
6. Transformations
    - dotnet version works and writes to file
    - I'm not sure if there's an issue with matchbox or with my code, because all I'm getting back is {"resourceType":"TRight"}
