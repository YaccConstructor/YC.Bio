#!/bin/bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

cp -rf human_readable converted
cd converted/refseq/bacteria
for d in $(find . -mindepth 3 -type d) 
do
	gzip -d -f $d/*.gz
	gen_file=$(find $d -name '*.gbff' -print -quit)
	python3 $DIR/convert_to_positive.py "$gen_file"
	rm -r $d 
done
