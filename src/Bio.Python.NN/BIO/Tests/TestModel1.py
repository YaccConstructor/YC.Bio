"""
Test model:
- get true pos, true neg, false pos, false neg
- plot weights distribution
"""

import numpy as np
from keras.models import Sequential
from keras.layers import Activation, Dropout, Dense
from keras import optimizers
from keras import backend as be
import csv
import matplotlib.pylab as pl

data_dir = "Tests/Data/TestData1.csv"
names = []
arr_length = 15625
fp, fn, tp, tn = 0, 0, 0, 0


def generate_arrays(path):
    f = open(path)
    r = csv.reader(f)
    batch_x = []
    batch_y = []
    for ln in r:
        if len(ln) > 0:
            names.append(ln[0])
            x = np.array(ln[1:(len(ln) - 1)], dtype=np.uint32)
            y = np.array([int(ln[len(ln) - 1] == "p")])
            batch_x.append(np.array(x))
            batch_y.append(y)
    return np.array(batch_x), np.array(batch_y)


model = Sequential()

model.add(Dropout(0.5, input_shape=(arr_length,)))

model.add(Dense(4096))
model.add(Activation('sigmoid'))

model.add(Dropout(0.75))

model.add(Dense(1024))
model.add(Activation('sigmoid'))

model.add(Dropout(0.75))

model.add(Dense(1024))
model.add(Activation('sigmoid'))

model.add(Dropout(0.75))

model.add(Dense(512))
model.add(Activation('sigmoid'))

model.add(Dropout(0.75))

model.add(Dense(512))
model.add(Activation('sigmoid'))

model.add(Dropout(0.75))

model.add(Dense(256))
model.add(Activation('sigmoid'))

model.add(Dropout(0.5))

model.add(Dense(16))
model.add(Activation('sigmoid'))

model.add(Dropout(0.5))

model.add(Dense(1))
model.add(Activation('sigmoid'))

model.compile(loss='binary_crossentropy',
              optimizer=optimizers.Adagrad(lr=0.0058),
              metrics=['accuracy'])

model.load_weights("Models/Model1/weights.h5")

data = generate_arrays(data_dir)
res = model.predict_classes(data[0], verbose=0)
print("Total: ", len(res))

# true pos, true neg, false pos, false neg
for i in range(len(res)):
    if res[i] == 1 and data[1][i] == 1:
        tp += 1
    elif res[i] == 1 and data[1][i] == 0:
        fp += 1
    elif res[i] == 0 and data[1][i] == 1:
        fn += 1
    else:
        tn += 1
print("True Positive: ", tp, "\r\nFalse Positive: ", fp, "\r\nTrue Negative: ", tn, "\r\nFalse Negative: ", fn)

# weights distribution
inp = model.input
output = model.layers[len(model.layers) - 1].output
func = be.function([inp] + [be.learning_phase()], [output])
out = func([data[0], 0.])

as_pos = dict([(n, 0) for n in range(21)])
as_neg = dict([(n, 0) for n in range(21)])
for i in range(len(out[0])):
    if res[i] == [[1]]:
        as_pos[round(out[0][i][0] * 20)] += 1
    else:
        as_neg[round(out[0][i][0] * 20)] += 1
pl.plot(list(map(lambda x: x / 20, list(as_pos.keys()))), list(as_pos.values()))
pl.plot(list(map(lambda x: x / 20, list(as_neg.keys()))), list(as_neg.values()))
pl.show()
