#!/bin/bash
./install-deps.sh && \
    cmake nativesrc --preset linux-debug && \
    cmake --build nativesrc/out/build/linux-debug && \
    cmake nativesrc --preset linux-release && \
    cmake --build nativesrc/out/build/linux-release
