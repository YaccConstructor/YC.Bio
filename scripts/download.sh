#!/bin/bash

while IFS='' read -r line || [[ -n "$line" ]]; do
    echo "download: $line"
    ncbi-genome-download -l complete -F genbank -H -p 4 -g "$line" bacteria
done < "$1"
