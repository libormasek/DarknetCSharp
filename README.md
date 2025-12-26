# DarknetCSharp

## Overview

DarknetCSharp is a .NET C# wrapper for the native Darknet/YOLO framework, which is a free and open source neural network framework written in C, C++, and CUDA.

https://codeberg.org/CCodeRun/darknet

## Requirements

- .NET 10 SDK
- Pre-built and installed Darknet libraries (see detailed instructions at https://codeberg.org/CCodeRun/darknet)
- YOLO model files:
  - Configuration file (`.cfg`)
  - Weights file (`.weights`)
  - Class names file (`.names`)

## Usage

See the `DarknetCSharpExample` project for example C# code that reads in an image file, runs Darknet/YOLO object detection, and then outputs the results.