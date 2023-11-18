#!/bin/bash
VERSION=ffmpeg-n6.0-latest-linux64-lgpl-shared-6.0
LOCALNAME=linux64
ARCHIVE=$VERSION.tar.xz
DEST=./libs/ffmpeg

wget -O $ARCHIVE https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/$ARCHIVE && \
  tar xf $ARCHIVE -C $DEST && \
  rm $ARCHIVE && \
  mv $DEST/$VERSION $DEST/$LOCALNAME