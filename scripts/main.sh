#!/bin/bash

#requirements: python3, biopython >= 1.70, ncbi-genome-download

echo 'start downloading...'
sh ./download.sh species
echo 'complete'
echo 'converting...'
sh ./organize.sh
echo 'complete'
