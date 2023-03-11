#!/bin/zsh
docker build -t becdetatcom .
docker compose up -d
sleep 10
open http://localhost:4050
echo "Complete!"
