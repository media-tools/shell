#!/bin/bash

g++ -o Shell.HolePunching/biconn/biconn -O3 Shell.HolePunching/biconn/biconn.cpp -Wall -pthread -std=c++11
i686-w64-mingw32-g++ -o Shell.HolePunching/biconn/biconn.exe -O3 Shell.HolePunching/biconn/biconn.cpp -Wall  -std=c++11 -lws2_32

