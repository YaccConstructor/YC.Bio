"""This module removes sequences that don't satisfy user defined conditions"""


def filter_conditions_16s (meta, seq):
    if all(x in meta for x in ["k__Bacteria", "p__", "c__", "o__", "f__", "g__", "s__"]) and not 'andidat' in meta and not 'nclassified' in meta:
        return True
    else:
        return False

def filter_conditions_trna (meta, seq):
    if not 'andidat' in meta and not 'nclassified' in meta and not 'ncultured' in meta:
        return True
    else:
        return False


def filter(inp_path):
    """Filter unappropriate data 
    input -- file with classes in the first line and samples meta and seq splitted by '\n'
    output -- file in the same format containing only samples satisfying filter_conditions
    """
    out_path = ''.join(inp_path.split('.')[:-1]) + '_filtered' + '.' + inp_path.split('.')[-1]
    with open(inp_path, 'r') as inp, open(out_path, 'w') as out:
        file_data = inp.read().split('\n')
        classes, data = file_data[0], '\n'.join(file_data[1:]).split('>')[1:]
        filtered_data = []
        for line in data:
            [meta, seq] = list(map(lambda x: x.strip('\n').strip(), line.split('\n')[0:2]))
            if filter_conditions_trna(meta, seq):
                filtered_data.append('>' + meta + '\n' + seq)
        out.write(classes + '\n' + '\n'.join(filtered_data))
    return out_path