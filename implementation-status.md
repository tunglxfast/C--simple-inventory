# Implementation Status - v0.1

## 1) Đã hoàn thành mới nhất
- Tạo `global.json` khóa SDK `8.0.420` để build/test ổn định trên .NET 8.
- Dựng `LoginView` + `DashboardView` + `ProductView` + `StockOutView` + `StockInView` + `OpeningStockView` + `ImportView` + `ReportView` theo Avalonia MVVM.
- Điều hướng cơ bản:
  - Login thành công -> Dashboard
  - Dashboard -> Product / StockOut / StockIn / OpeningStock / Import / Report
  - Các màn hình nghiệp vụ -> Dashboard
- Thêm `Product` repository/service mẫu (Dapper + CRUD + đổi mã sản phẩm).
- Thêm cơ chế generate số phiếu `P-DDMMYYYY-0001` (`DocNoGenerator`).
- Hoàn thiện `StockDocumentService` bản v0.1:
  - tạo phiếu mới mặc định `DRAFT`
  - thêm dòng hàng chỉ khi phiếu còn `DRAFT`
  - confirm chỉ từ `DRAFT -> CONFIRMED`
  - kiểm tra không âm tồn tại thời điểm confirm
  - HOLD lifecycle: lấy HOLD mở, chuyển `HOLD -> SOLD`, `HOLD -> RETURNED`
  - CANCEL workflow: hủy phiếu `CONFIRMED` và tạo phiếu đảo (`ADJUSTMENT`) trong cùng transaction.
- Bổ sung `OpeningStockRepository/Service` để ghi và đọc tồn đầu kỳ.
- Triển khai `ImportService` (ClosedXML) cho 3 luồng: products/opening_stock/transactions.
- Triển khai report + export:
  - report tồn theo date range
  - report HOLD
  - report theo sale
  - report theo requester
  - preset nhanh: tháng/quý/năm/30 ngày gần nhất
  - export Excel cho 4 loại report trên.
- Hoàn thiện STEP 10 nền tảng + thực thi publish:
  - cấu hình publish Release trong `InventoryApp.UI.csproj`
  - script `scripts/publish-win-x64.sh`
  - tài liệu `docs/PUBLISH.md`, `docs/OPERATIONS.md`
  - chạy publish thực tế thành công ra `publish/win-x64`.
- Build solution và test thành công.

## 2) Trạng thái kỹ thuật hiện tại
- `dotnet --version`: `8.0.420` (đã pin bằng `global.json`).
- `dotnet build InventoryApp.sln`: PASS (0 warning, 0 error).
- `dotnet test InventoryApp.sln`: PASS (1/1).
- `dotnet publish` (win-x64): PASS.

## 3) Mức độ bám planning.md (chi tiết)
- STEP 1 - Architecture + Folder: **Done (100%)**
- STEP 2 - SQLite Schema: **Done (100%)**
- STEP 3 - Model Classes: **In Progress (85%)**
- STEP 4 - Repository + Services: **In Progress (94%)**
- STEP 5 - Avalonia UI: **In Progress (86%)**
- STEP 6 - Dashboard: **In Progress (68%)**
- STEP 7 - Import/Export Excel: **In Progress (92%)**
- STEP 8 - HOLD/SOLD/RETURNED: **In Progress (92%)**
- STEP 9 - Reporting Engine: **In Progress (88%)**
- STEP 10 - Publish .exe: **In Progress (78%)**
  - Đã xong: cấu hình publish, script publish, tài liệu release/backup-restore, publish artifact thực tế.
  - Còn lại: smoke test thủ công trên Windows theo checklist `docs/SMOKE-TEST.md`.

## 4) Kế hoạch làm tiếp ngay
1. Chạy smoke test thủ công trên máy Windows theo `docs/SMOKE-TEST.md`.
2. Hoàn thiện STEP 6: KPI dashboard realtime từ dữ liệu thật.
3. Bổ sung test unit/integration cho `ImportService`, `StockDocumentService`, `ReportService`, `ExportService`.
