import sys
sys.path.append('/data_preprocessing')

import equalize_length
import filter_data
import get_selection


def preprocessing_for_parsing(inp_path, length): 
    #filtered = filter_data.filter(inp_path)
    #selection = get_selection.for_parsing(filtered, 3, 2, 1)
    #equalized = equalize_length.align(selection, length, 'D')
    equalized = equalize_length.align(inp_path, length, 'D')


def preprocesing_for_neural_network_1(inp_path): 
    train, valid, test = get_selection.for_neural_network_1(inp_path)

def preprocesing_for_neural_network_2(inp_path, length):
    filtered = filter_data.filter(inp_path)
    equalized = equalize_length.align(filtered, length, 'D')
    train, valid, test = get_selection.for_neural_network_2(equalized, 3, 2, 1)
    

if __name__ == "__main__":
    inp_path = sys.argv[1]
    length = int(sys.argv[2])
    preprocessing_for_parsing(inp_path, length)
    #preprocesing_for_neural_network_1(inp_path)
    #preprocesing_for_neural_network_2(inp_path, length)