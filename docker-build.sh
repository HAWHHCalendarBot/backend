#!/bin/bash

docker build -t hawhhcalendarbotdownloader -f Dockerfile.Downloader .
docker build -t hawhhcalendarbotmensa -f Dockerfile.Mensa .
docker build -t hawhhcalendarbotparser -f Dockerfile.Parser .
