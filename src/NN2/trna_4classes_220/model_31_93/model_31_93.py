from keras.preprocessing.image import ImageDataGenerator
from keras.models import Model,Sequential
from keras.models import Sequential
from keras.layers import Conv2D, MaxPooling2D
from keras.layers import Activation, Dropout, Flatten, Dense
from keras import backend as K
from keras.preprocessing import image
from keras.layers import BatchNormalization
from keras.callbacks import CSVLogger
from keras.utils import plot_model
from keras.utils.vis_utils import model_to_dot

import pandas as pd
import csv

from keras import optimizers
from PIL import Image
import os
from keras.layers.advanced_activations import PReLU

import numpy as np
import tensorflow as tf
import random as rn
import struct

#Для одинакового результата при разных запусках.
os.environ['PYTHONHASHSEED'] = '0'
np.random.seed(42)
rn.seed(12345)
session_conf = tf.ConfigProto(intra_op_parallelism_threads=1, inter_op_parallelism_threads=1)
tf.set_random_seed(1234)
sess = tf.Session(graph=tf.get_default_graph(), config=session_conf)
K.set_session(sess)


arr_length = 3028
batch_size = 64
input_length = 219
train_size = 3360
valid_size = 1440
epochs = 500

data_train = 'YC.Bio/src/NN2/trna_4classes_220/data/train.csv'
data_valid = 'YC.Bio/src/NN2/trna_4classes_220/data/valid.csv'
weights = 'model_084_9728(base).h5'
weights2 = 'model_04_98(base2).h5'

model = Sequential()

model.add(Dropout(0.3, input_shape=(arr_length,)))

model.add(Dense(8194, trainable=False))
model.add(BatchNormalization())
model.add(Activation('relu'))

model.add(Dropout(0.9))

model.add(Dense(2048, trainable=False))
model.add(BatchNormalization())
model.add(Activation('relu'))

model.add(Dropout(0.9))

model.add(Dense(1024, trainable=False))
model.add(BatchNormalization())
model.add(Activation('relu'))

model.add(Dropout(0.9))

model.add(Dense(512, trainable=False))
model.add(BatchNormalization())
model.add(Activation('relu'))

model.add(Dropout(0.75))

model.add(Dense(64, trainable=False))
model.add(Activation('sigmoid'))

model.add(Dense(1, trainable=False))
model.add(Activation('sigmoid'))

model2 = Sequential()

model2.add(Dropout(0.05, input_shape=(input_length,)))

model2.add(Dense(512))
model2.add(BatchNormalization())
model2.add(Activation('relu'))

model2.add(Dense(1024))
model2.add(BatchNormalization())
model2.add(Activation('relu'))

model2.add(Dense(2048))
model2.add(BatchNormalization())
model2.add(Activation('relu'))


model2.add(Dense(3028))
model2.add(BatchNormalization())
model2.add(Activation('relu'))


model2.add(model)

model2.load_weights(weights2)

model2.layers.pop()
model2 = Model(model2.input, model2.layers[-1].output)

print(model2.summary())


model3 = Sequential()

model3.add(Dropout(0.3, input_shape=(arr_length,)))

model3.add(Dense(8194))
model3.add(BatchNormalization())
model3.add(Activation('relu'))

model3.add(Dropout(0.9))

model3.add(Dense(2048))
model3.add(BatchNormalization())
model3.add(Activation('relu'))

model3.add(Dropout(0.9))

model3.add(Dense(1024))
model3.add(BatchNormalization())
model3.add(Activation('relu'))

model3.add(Dropout(0.9))

model3.add(Dense(512))
model3.add(BatchNormalization())
model3.add(Activation('relu'))

model3.add(Dropout(0.75))

model3.add(Dense(64))
model3.add(Activation('sigmoid'))

model3.add(Dense(1))
model3.add(Activation('sigmoid'))

model3.load_weights(weights)

model3.layers.pop()
model3.layers.pop()
model3.layers.pop()
model3.layers.pop()

model3.add(Dense(128))
model3.add(Activation('relu'))

model3.add(Dense(32))
model3.add(Activation('sigmoid'))

model3.add(Dense(16))
model3.add(Activation('sigmoid'))

model3.add(Dense(4))
model3.add(Activation('sigmoid'))

print(model3.summary())

model4 = Sequential()
model4.add(model2)
model4.add(model3)

print(model4.summary())
print("-----------------------------")
print(model2.summary())

model4.compile(loss='categorical_crossentropy',
              optimizer=
              optimizers.Adagrad(lr=0.05),
              metrics=['categorical_accuracy'])


def get_flag(ln):
    cls = ln[len(ln)-1]
    flag = []
    if cls == "a": flag = [0,0,0,1]
    elif cls == "b": flag = [0,0,1,0]
    elif cls == "f": flag = [0,1,0,0]
    else: flag = [1,0,0,0]
    return np.array(flag)

def generate_arrays_from_dir(path, batchsz):
    while 1:
       with open(path) as f:
            r = csv.reader(f)
            batchCount = 0
            batchX = []
            batchy = []
            for ln in r:
                for i in range(1,len(ln)-1):
                    if ln[i] == "A": ln[i] = "2"
                    elif ln[i] == "C": ln[i] = "3"
                    elif ln[i] == "G": ln[i] = "5"
                    elif ln[i] == "T": ln[i] = "7"
                    else: ln[i] = "0"
                X = np.array(ln[1:220],dtype=np.uint32)
                y = get_flag(ln)
                batchX.append(np.array(X))
                batchy.append(y)
                batchCount = batchCount + 1
                if batchCount == batchsz:
                    yield (np.array(batchX), np.array(batchy))
                    batchCount = 0
                    batchX = []
                    batchy = []

csv_logger = CSVLogger('model_31_93.log')
model4.fit_generator(
    generate_arrays_from_dir(data_train,batch_size),
    steps_per_epoch=int(train_size/batch_size) - 1,
    validation_data=generate_arrays_from_dir(data_valid,batch_size),
    validation_steps=int(valid_size/batch_size) - 1,
    epochs=epochs,
    verbose=2,
    callbacks=[csv_logger])

model4.save_weights('model_31_93.h5')
