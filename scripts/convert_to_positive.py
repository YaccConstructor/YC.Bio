import sys

from Bio import SeqIO
from Bio.Seq import Seq
from Bio.SeqRecord import SeqRecord
from Bio.SeqFeature import *


def extract_intervals(source, intervals):
    return [l.extract(source) for l in intervals]


def new_description(desc, intervals):
    new_desc = ''
    desc_parts = desc.split(',')
    if len(desc_parts) == 1:
    	new_desc = (desc.strip() + ', complete genome') 
    elif len(desc_parts) == 2:
    	new_desc = desc.strip()
    else:
    	new_desc = (desc_parts[0] + ', ' + desc_parts[1]) 
    new_desc += ', '
    new_desc += ' '.join(["{}:{}".format(x.start, x.end) for x in intervals])
    return new_desc

filename = sys.argv[1]
file = open(filename, 'r')
path_parts = filename.rsplit('/', 1)
output = open(path_parts[0] + '.fasta', 'w')

record = list(SeqIO.parse(file, 'genbank'))[0]
dna = record.seq

final_parts = []
intervals_16s = []
final_intervals_16s = []

cur_pos = 0
offset = 0
prev_location = FeatureLocation(0, 0)

for feature in record.features[1:]:
    if not (feature.type == 'repeat_region'
            or type(feature.location) == CompoundLocation):
        f_start = feature.location.start
        f_end = feature.location.end
        if feature.location == prev_location:
            if (feature.type == 'rRNA'
                    and 'product' in feature.qualifiers
                    and feature.qualifiers['product'][0] == '16S ribosomal RNA'):
                intervals_16s.append(feature.location)
                final_intervals_16s.append(FeatureLocation(f_start + offset, f_end + offset))
        else:
            if f_start < cur_pos:
                offset += (cur_pos - f_start)
            final_parts.append(str(dna[cur_pos:feature.location.start]))
            final_parts.append(str(feature.extract(dna)))

            cur_pos = f_end
            prev_location = feature.location

final_parts.append(str(dna[cur_pos:]))
final_dna = ''.join(final_parts)

source_16s = extract_intervals(dna, intervals_16s)
final_16s = extract_intervals(final_dna, final_intervals_16s)

assert \
    len(final_dna) - (len(dna) + offset) == 0, \
    'File: {0}: resulting genome has an incorrect length'.format(filename)
assert \
    (source_16s == final_16s), \
    'File {0}: 16s genes were extracted incorrectly'.format(filename)

final_record = SeqRecord(Seq(final_dna),
                         record.id + ',',
                         description=new_description(record.description, final_intervals_16s))

SeqIO.write([final_record], output, 'fasta')
file.close()
output.close()
