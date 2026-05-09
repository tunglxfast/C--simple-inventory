#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
OUT_DIR="$ROOT_DIR/publish/win-x64"

echo "[1/3] Restore"
dotnet restore "$ROOT_DIR/InventoryApp.sln"

echo "[2/3] Test"
dotnet test "$ROOT_DIR/InventoryApp.sln" --configuration Release --no-restore

echo "[3/3] Publish win-x64"
dotnet publish "$ROOT_DIR/src/InventoryApp.UI/InventoryApp.UI.csproj" \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=false \
  -o "$OUT_DIR"

echo "Publish completed: $OUT_DIR"
