#!/bin/bash
RELEASE=autobuild-2023-12-31-12-55
VERSION=ffmpeg-n6.1.1-linux64-lgpl-shared-6.1
LOCALNAME=linux64
ARCHIVE=$VERSION.tar.xz
DEST=./libs/ffmpeg

wget -O $ARCHIVE https://github.com/BtbN/FFmpeg-Builds/releases/download/$RELEASE/$ARCHIVE && \
  tar xf $ARCHIVE -C $DEST && \
  rm $ARCHIVE && \
  rm -rf $DEST/$LOCALNAME && \
  mv $DEST/$VERSION $DEST/$LOCALNAME