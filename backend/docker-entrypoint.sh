#!/bin/sh
set -e
if [ "$DOTNET_ROLE" = "worker" ]; then
  echo "Starting PathLens Worker (background processor)..."
  exec dotnet /app/worker/PathLens.Worker.dll
else
  echo "Starting PathLens API..."
  exec dotnet /app/api/PathLens.Api.dll
fi
