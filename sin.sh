#!/bin/bash
docker compose -f docker-compose.yml -f docker-compose.chaos.yml -f docker-compose.sin.yml up --build -d