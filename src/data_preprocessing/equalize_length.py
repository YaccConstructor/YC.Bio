"""This module alignes input sequences"""
    

def align (inp_path, length, symbol):
    """Add special symbol to sequence till the defined length or cut the sequence 
    input -- file with classes in the first line and samples meta and seq splitted by '\n'
    output -- file in the same format containing aligned sequences
    """
    out_path = ''.join(inp_path.split('.')[:-1]) + '_' + str(length) + '.' + inp_path.split('.')[-1]
    with open(inp_path, 'r') as inp, open(out_path, 'w') as out:
        file_data = inp.read().split('\n')
        classes, data = file_data[0], '\n'.join(file_data[1:]).split('>')[1:]
        aligned_data = []
        for line in data:
            [meta, seq] = list(map(lambda x: x.strip('\n').strip(), line.split('\n')[0:2]))
            if len(seq) < length:
                seq = seq + symbol * (length - len(seq))
            if len(seq) > length:
                seq = seq[0:length]
            aligned_data.append('>' + meta + '\n' + seq)
        out.write(classes + '\n' + '\n'.join(aligned_data))
    return out_path
