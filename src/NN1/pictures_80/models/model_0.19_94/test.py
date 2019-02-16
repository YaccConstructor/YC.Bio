from keras.models import Model,Sequential
from keras.layers import Conv2D, MaxPooling2D
from keras.layers import Activation, Dropout, Flatten, Dense
from keras import backend as K
from keras.preprocessing import image
from keras.layers import BatchNormalization
from keras import optimizers
from PIL import Image
from keras.layers.advanced_activations import PReLU
import tensorflow as tf
import os, os.path
import numpy as np


data_dir = 'C:/Users/User/Desktop/folder/tRna_img/data/80/prepared/test'
weights = 'C:/Users/User/Desktop/folder/tRna_img/models/model_0.19_94/model.h5'
pos, neg = 0, 0
img_size = 80

model = Sequential()

if K.image_data_format() == 'channels_first':
 input_shape = (3, img_size, img_size)
else:
 input_shape = (img_size, img_size, 3)

model.add(Conv2D(2, (3, 3), input_shape=input_shape))
model.add(Activation('relu'))

model.add(Flatten())

model.add(Dropout(0.5))

model.add(Dense(1024))
model.add(BatchNormalization())
model.add(Activation('relu'))

model.add(Dropout(0.9))

model.add(Dense(512))
model.add(BatchNormalization())
model.add(Activation('relu'))

model.add(Dropout(0.9))

model.add(Dense(128))
model.add(BatchNormalization())
model.add(Activation('relu'))

model.add(Dropout(0.75))

model.add(Dense(64))
model.add(Activation('sigmoid'))

model.add(Dense(4))
model.add(Activation('sigmoid'))

model.compile(loss='categorical_crossentropy',
              optimizer=
              optimizers.Adagrad(lr=0.05),
              metrics=['accuracy'])
model.load_weights(weights)

def match(filename):
    cls = filename.split('_')[2]
    if cls == 'a': return [0]
    if cls == 'b': return [1]
    if cls == 'f': return [2]
    if cls == 'p': return [3]

for dirpath, dirnames, filenames in os.walk(data_dir):
    for filename in [f for f in filenames if f.endswith(".bmp")]:
        img = image.load_img(os.path.join(dirpath, filename), target_size=(img_size,img_size))
        img = image.img_to_array(img)
        img = np.reshape(img,[1,img_size,img_size,3])
        res = model.predict_classes(img, verbose=0)
        if match(filename) == res: pos += 1
        else: neg += 1
print(pos,neg)