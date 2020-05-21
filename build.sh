#!/bin/bash

csc log_parser.cs || {
  echo "Build failed! Exiting."
  exit 3
}

mono log_parser.exe logs/ csv/ || {
  echo "Run failure! Exiting."
  exit 4
}

R -f test.R -q

