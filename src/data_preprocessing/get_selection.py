"""This module prepares train, validation and test selections"""


import numpy
import csv


def make_selection_trna_4(data):
    a,b,f,p = [],[],[],[]
    for el in data:
        cl = el.split('\n')[0].split('%')[1]
        if cl == 'a': a.append(el)
        if cl == 'b': b.append(el)
        if cl == 'f': f.append(el)
        if cl == 'p': p.append(el)
    numpy.random.shuffle(a) 
    numpy.random.shuffle(b) 
    numpy.random.shuffle(f) 
    numpy.random.shuffle(p) 
    selected_data = []
    for i in range(2000):
        selected_data.append('>[TRAIN]' + a[i])
        selected_data.append('>[TRAIN]' + b[i])
        selected_data.append('>[TRAIN]' + f[i])
        selected_data.append('>[TRAIN]' + p[i])
    for i in range(2000, 2750):
        selected_data.append('>[VALID]' + a[i])
        selected_data.append('>[VALID]' + b[i])
        selected_data.append('>[VALID]' + f[i])
        selected_data.append('>[VALID]' + p[i])
    for i in range(2750, 3000):
        selected_data.append('>[TEST]' + a[i])
        selected_data.append('>[TEST]' + b[i])
        selected_data.append('>[TEST]' + f[i])
        selected_data.append('>[TEST]' + p[i])
    numpy.random.shuffle(selected_data)
    return selected_data

def make_selection_1(data, train_num, valid_num, test_num): # TODO different selections with proportions depending on classes
    numpy.random.shuffle(data)                              # now just random depending on split numbers 
    selected_data = []
    for i in range(train_num):
        selected_data.append('>[TRAIN]' + data[i])
    for i in range(train_num, train_num + valid_num):
        selected_data.append('>[VALID]' + data[i])
    for i in range(train_num + valid_num, train_num + valid_num + test_num):
        selected_data.append('>[TEST]' + data[i])
    return selected_data


def get_class_from_meta(meta, classes): #TODO fix for a specific task
    for el in classes:
        if el in meta:
            return el


def csv_writer(data, path):
    with open(path, "w", newline='') as csv_file:
        writer = csv.writer(csv_file, delimiter=',', quotechar = "'")
        for line in data:
            writer.writerow(line)


def line_to_csv(line, classes):
    csv_line = []
    csv_line.append('"' + line.split('\n')[0] +'"')
    for x in list(line.split('\n')[1]):
        csv_line.append(x)
    cls = get_class_from_meta(line.split('\n')[0], classes)
    csv_line.append('"' + cls + '"')
    return csv_line 


def for_parsing(inp_path, train_num, valid_num, test_num): 
    """Select samples for parsing 
    input -- file with classes in the first line and samples meta and seq splitted by '\n'
    output -- file in the same format containing only samples for train, valid and test; each meta starts with '>[TRAIN]', '>[VALID]' or '>[TEST]' 
    """
    out_path = ''.join(inp_path.split('.')[:-1]) + '_selected.fasta'
    with open(inp_path, 'r') as inp, open(out_path, 'w') as out:
        file_data = inp.read().split('\n')
        classes, data = file_data[0], '\n'.join(file_data[1:]).split('>')[1:]
        data = list(map(lambda x: x.strip('\n'), data))
        selected_data = make_selection_trna_4(data)
        out.write(classes + '\n' + '\n'.join(selected_data))
    return out_path
        

def for_neural_network_1(inp_path):
    """Prepare parsed data for NN1 processing
    input -- csv file with vector data; each line looks like ”>[TRAIN]meta1”,1,0,..,“class1”; first line containes all classes
    output -- train.csv, valid.csv, test.csv; first lines contain all classes
    """
    out_path_train = ''.join(inp_path.split('.')[:-1]) + '_train.csv'
    out_path_valid = ''.join(inp_path.split('.')[:-1]) + '_valid.csv'
    out_path_test = ''.join(inp_path.split('.')[:-1]) + '_test.csv'
    with open(inp_path, 'r') as inp:
        data = csv.reader(inp)
        train, valid, test = [], [], []
        for line in data:
            if 'TRAIN' in line[0]:
                line[0] = '"' + line[0].replace('[TRAIN]', '') + '"'
                line[-1] = '"' + line[-1] + '"'
                train.append(line)
            elif 'VALID' in line[0]:
                line[0] = '"' + line[0].replace('[VALID]', '') + '"'
                line[-1] = '"' + line[-1] + '"'
                valid.append(line)
            elif 'TEST' in line[0]:
                line[0] = '"' + line[0].replace('[TEST]', '') + '"'
                line[-1] = '"' + line[-1] + '"'
                test.append(line)
            else:
                train.append(line)
                valid.append(line)
                test.append(line)
        numpy.random.shuffle(train)
        numpy.random.shuffle(valid)
        numpy.random.shuffle(test)
        csv_writer(train, out_path_train)
        csv_writer(valid, out_path_valid)
        csv_writer(test, out_path_test)
        return out_path_train, out_path_valid, out_path_test


def for_neural_network_2(inp_path, train_num, valid_num, test_num): 
    """Prepare seq data for NN2 processing
    input -- file with classes in the first line and samples meta and seq splitted by '\n'
    output -- train.csv, valid.csv, test.csv; first lines contain all classes; each sample line looks like ”>meta1”,A,D,..,“class1” 
    """
    out_path_train = ''.join(inp_path.split('.')[:-1]) + '_train.csv'
    out_path_valid = ''.join(inp_path.split('.')[:-1]) + '_valid.csv'
    out_path_test = ''.join(inp_path.split('.')[:-1]) + '_test.csv'
    with open(inp_path, 'r') as inp:
        file_data = inp.read().split('\n')
        classes, data = file_data[0], '\n'.join(file_data[1:]).split('>')[1:]
        data = list(map(lambda x: x.strip('\n'), data))
        train, valid, test = [classes.split(',')], [classes.split(',')], [classes.split(',')]
        selected_data = make_selection_1(data, train_num, valid_num, test_num)
        for line in selected_data:
            if 'TRAIN' in line:
                line = line.replace('[TRAIN]','')
                train.append(line_to_csv(line, classes))
            elif 'VALID' in line:
                line = line.replace('[VALID]','')
                valid.append(line_to_csv(line, classes))
            else:
                line = line.replace('[TEST]','')
                test.append(line_to_csv(line, classes))
        numpy.random.shuffle(train)
        numpy.random.shuffle(valid)
        numpy.random.shuffle(test)
        csv_writer(train, out_path_train)
        csv_writer(valid, out_path_valid)
        csv_writer(test, out_path_test)
    return out_path_train, out_path_valid, out_path_test