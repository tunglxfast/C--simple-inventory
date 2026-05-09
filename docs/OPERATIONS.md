# Operations Guide

## Database Location
- Default path from `appsettings.json`:
  - `data/inventory.db`

## Backup
1. Close application.
2. Copy `data/inventory.db` to backup storage.
3. Recommended file naming:
   - `inventory-backup-YYYYMMDD-HHmm.db`

## Restore
1. Close application.
2. Replace `data/inventory.db` with backup file.
3. Reopen application and verify last transactions/reports.

## Safety Notes
- Never edit SQLite file while app is running.
- Keep at least 7 rolling backups.
- Keep one off-machine backup (cloud/shared drive/USB).
