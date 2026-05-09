# Smoke Test Checklist (Windows Release)

## Build Artifact
- [x] Publish script executed successfully.
- [x] Output folder exists: `publish/win-x64`.
- [x] Main executable exists: `InventoryApp.UI.exe`.

## Runtime Basic (Windows machine)
- [ ] Launch app from `publish/win-x64/InventoryApp.UI.exe`.
- [ ] First run creates local DB file under `data/inventory.db`.
- [ ] Login screen opens without crash.
- [ ] Navigate to Dashboard, Product, StockIn, StockOut, OpeningStock, Import, Report.

## Core Flow
- [ ] Create Product and update/delete/rename code.
- [ ] Create StockIn draft -> add item -> confirm.
- [ ] Create StockOut draft (EXPORT/HOLD) -> add item -> confirm.
- [ ] HOLD item status transition: HOLD -> SOLD and HOLD -> RETURNED.
- [ ] Cancel confirmed document -> reversal document created.

## Import/Export
- [ ] Import products from template.
- [ ] Import opening stock from template.
- [ ] Import transactions from template.
- [ ] Run report and export all Excel report variants.

## Backup/Restore
- [ ] Backup `data/inventory.db` when app closed.
- [ ] Restore from backup and verify recent data.

## Acceptance Notes
- [ ] No crash in above flows.
- [ ] No negative stock on confirm.
- [ ] Report formula results match expectation for test dataset.
