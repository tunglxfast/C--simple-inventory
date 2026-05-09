# Publish Guide (Windows EXE)

## Prerequisites
- .NET SDK 8.0.x (global.json pins `8.0.420`)
- Runtime identifier target: `win-x64`

## Recommended Release Flow
1. Run restore/build/test
```bash
dotnet restore InventoryApp.sln
dotnet build InventoryApp.sln -c Release
dotnet test InventoryApp.sln -c Release --no-build
```
2. Publish UI project
```bash
dotnet publish src/InventoryApp.UI/InventoryApp.UI.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=false \
  -o publish/win-x64
```

## One-command publish script
```bash
./scripts/publish-win-x64.sh
```

## Output
- Main executable is generated under `publish/win-x64`.
- App runs offline with local SQLite file in `data/inventory.db` (created at runtime).

## Release Checklist
- `dotnet test` is green.
- Smoke test login/dashboard/product/stock-in/stock-out/import/report.
- Confirm DB auto-initialization (`Scripts/001_init.sql`) works on clean machine.
- Backup/restore procedure verified.
