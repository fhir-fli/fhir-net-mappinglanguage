
# Base directory where the JSON files are located
base_directory="maptutorial"

# Loop through all JSON files in the directory and its subdirectories
find "$base_directory" -type f -name "*.json" | while read -r json_file; do
    echo "Processing $json_file"

    # Remove the "text" field from the JSON using jq
    jq 'del(.text)' "$json_file" > "$json_file.tmp" && mv "$json_file.tmp" "$json_file"

    echo "Removed 'text' field from $json_file"
done

echo "All JSON files processed."

# Loop through each step directory
for step_dir in "$base_directory"/step*; do
    # Look for the result directory in each step
    result_dir="$step_dir/result"
    
    if [ -d "$result_dir" ]; then
        # Loop through all JSON files in the result directory
        find "$result_dir" -type f -name "*.json" | while read -r json_file; do
            echo "Processing $json_file"

            # Ensure "resourceType": "TRight" is the first entry in the JSON file using jq
            jq '. as $json | {resourceType: "TRight"} + $json' "$json_file" > "$json_file.tmp" && mv "$json_file.tmp" "$json_file"

            echo "Updated 'resourceType' as the first entry in $json_file"
        done
    else
        echo "No result directory found in $step_dir"
    fi
done

echo "All JSON files in the result directories processed."


# Loop through all XML files in the directory and its subdirectories
find "$base_directory" -type f -name "*.xml" | while read -r xml_file; do
    echo "Processing $xml_file"

    # Remove the <text> field from the XML using xmlstarlet
    xmlstarlet ed -N f="http://hl7.org/fhir" \
        -d '//f:text' "$xml_file" > "$xml_file.tmp" && mv "$xml_file.tmp" "$xml_file"

    echo "Removed 'text' field from $xml_file"
done

echo "All XML files processed."

# Find and format all XML files
find "$base_directory" -type f -name "*.xml" | while read -r file; do
    echo "Formatting $file"
    xmllint --format "$file" --output "$file"
done

echo "All XML files have been formatted."
