#!/bin/bash
docker compose -f docker-compose.yml -f docker-compose.chaos.yml up --build -d
./run.sh &