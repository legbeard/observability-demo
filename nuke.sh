#!/bin/bash
docker compose down --remove-orphans
docker volume rm tech-demo-observability_loki
docker volume rm tech-demo-observability_prometheus
docker volume rm tech-demo-observability_tempo