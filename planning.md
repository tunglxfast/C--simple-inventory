# Kế Hoạch Thực Thi Dự Án Inventory Desktop

## 1. Tóm tắt mục tiêu
- Xây dựng ứng dụng desktop quản lý xuất nhập tồn quần áo thể thao chạy offline hoàn toàn trên Windows.
- Công nghệ bắt buộc: .NET 8, C#, Avalonia UI, SQLite, Dapper, ClosedXML.
- Thiết kế theo kiến trúc sạch, dễ mở rộng, tối ưu cho thao tác kho thực tế.
- UI mặc định tiếng Việt có dấu, hỗ trợ chuyển đổi sang tiếng Anh.

## 2. Kế hoạch triển khai theo STEP 1-10 (bám sát requirement)

### STEP 1 - Architecture + Folder Structure
- Tạo solution và 5 project:
  - `InventoryApp.UI`
  - `InventoryApp.Core`
  - `InventoryApp.Infrastructure`
  - `InventoryApp.Database`
  - `InventoryApp.Shared`
- Áp dụng: Repository Pattern, Service Layer, DTO, Dependency Injection, async/await.
- Chuẩn hóa cấu trúc thư mục theo module nghiệp vụ (Products, Documents, Reports, ImportExport, Auth, Settings).

### STEP 2 - SQLite Schema đầy đủ + FK + Index
- Thiết kế schema chi tiết:
  - `products`
    - `id` INTEGER PK
    - `code` TEXT NOT NULL UNIQUE
    - `name` TEXT NOT NULL
    - `size` TEXT NULL
    - `category` TEXT NULL
    - `unit` TEXT NOT NULL
    - `barcode` TEXT NULL
    - `color` TEXT NULL
    - `note` TEXT NULL
    - `created_at` TEXT NOT NULL
    - `updated_at` TEXT NOT NULL
  - `opening_stock`
    - `id` INTEGER PK
    - `product_id` INTEGER NOT NULL FK -> `products(id)`
    - `qty` REAL NOT NULL
    - `created_at` TEXT NOT NULL
    - `note` TEXT NULL
  - `stock_documents`
    - `id` INTEGER PK
    - `doc_no` TEXT NOT NULL UNIQUE
    - `doc_type` TEXT NOT NULL (`IMPORT|EXPORT|RETURN|ADJUSTMENT`)
    - `reference_doc_no` TEXT NULL
    - `reversed_document_id` INTEGER NULL FK -> `stock_documents(id)`
    - `customer_name` TEXT NULL
    - `sale_employee_name` TEXT NULL
    - `request_employee_name` TEXT NULL
    - `area` TEXT NULL
    - `address` TEXT NULL
    - `phone` TEXT NULL
    - `payment_method` TEXT NULL (`CASH|BANK_TRANSFER`)
    - `document_status` TEXT NOT NULL (`DRAFT|CONFIRMED|CANCELLED`)
    - `note` TEXT NULL
    - `reporting_period_id` INTEGER NULL FK -> `reporting_periods(id)`
    - `doc_date` TEXT NOT NULL
    - `created_at` TEXT NOT NULL
    - `updated_at` TEXT NOT NULL
  - `stock_document_items`
    - `id` INTEGER PK
    - `document_id` INTEGER NOT NULL FK -> `stock_documents(id)`
    - `product_id` INTEGER NOT NULL FK -> `products(id)`
    - `qty` REAL NOT NULL
    - `stock_effect_type` TEXT NOT NULL (`IMPORT|EXPORT|HOLD|RETURN|DAMAGE`)
    - `item_status` TEXT NULL (`HOLD|SOLD|RETURNED`)
    - `note` TEXT NULL
  - `reporting_periods`
    - `id` INTEGER PK
    - `name` TEXT NOT NULL
    - `start_date` TEXT NOT NULL
    - `end_date` TEXT NOT NULL
    - `is_closed` INTEGER NOT NULL DEFAULT 0
    - `created_at` TEXT NOT NULL
  - `users`, `roles`, `user_roles` cho phân quyền `admin|employee|viewer`.
- Index bắt buộc:
  - `idx_products_code`
  - `idx_documents_doc_no`, `idx_documents_doc_date`, `idx_documents_doc_type`
  - `idx_items_document_id`, `idx_items_product_id`, `idx_items_stock_effect_type`, `idx_items_status`
- Ràng buộc nghiệp vụ:
  - Không cho xóa sản phẩm nếu đã có transaction.
  - Cho phép thay đổi mã sản phẩm qua chức năng riêng, không sửa trực tiếp bằng tay trong DB.
  - Khi đổi mã sản phẩm phải đồng bộ toàn bộ dữ liệu liên quan (chứng từ, dòng chứng từ, lịch sử, báo cáo, import/export mapping) và ghi log audit.
  - Không lưu tồn kho cố định, không update stock trực tiếp.
  - `doc_no` sinh tự động, không cho sửa tay, dùng format chung `P-DDMMYYYY-0001` (tăng tuần tự theo từng ngày).

### STEP 3 - Model Classes
- Tạo Entity model + DTO cho từng bảng.
- Enum hóa các giá trị chuẩn:
  - `DocumentType`: `IMPORT`, `EXPORT`, `RETURN`, `ADJUSTMENT`.
  - `StockEffectType`: `IMPORT`, `EXPORT`, `HOLD`, `RETURN`, `DAMAGE`.
  - `DocumentStatus`: `DRAFT`, `CONFIRMED`, `CANCELLED`.
  - `ItemStatus`: `HOLD`, `SOLD`, `RETURNED`.
  - `PaymentMethod`: `CASH`, `BANK_TRANSFER`.
- Tách model request/response cho import và báo cáo.

### STEP 4 - Repository + Services
- Repository cho CRUD và query báo cáo.
- Service xử lý nghiệp vụ:
  - Stock calculation service (transaction-based)
  - HOLD lifecycle service
  - Reporting service theo date range/kỳ
  - Import/Export service
- Ma trận hợp lệ `doc_type` - `stock_effect_type`:
  - `IMPORT` -> `IMPORT`
  - `EXPORT` -> `EXPORT`, `HOLD`, `DAMAGE`
  - `RETURN` -> `RETURN`
  - `ADJUSTMENT` -> `IMPORT` hoặc `EXPORT` (theo dấu điều chỉnh)

### STEP 5 - Avalonia UI (MVVM)
- Tạo màn hình:
  - `LoginView`
  - `DashboardView`
  - `ProductView`
  - `OpeningStockView`
  - `StockInView`
  - `StockOutView`
  - `ReportView`
  - `ReportingPeriodView`
  - `SettingsView`
- `StockOutView` hỗ trợ xuất thường + HOLD, và tra cứu bill HOLD để chuyển `SOLD/RETURNED`.
- Bổ sung chức năng `Đổi mã sản phẩm` trong `ProductView` với xác nhận 2 bước và hiển thị phạm vi dữ liệu sẽ được đồng bộ.

### STEP 6 - Dashboard bằng Avalonia
- Dashboard card hiển thị:
  - Tổng tồn kho
  - Hàng sắp hết
  - Bill HOLD chưa trả
  - Top sản phẩm xuất nhiều
  - Doanh số theo ngày
- UI style constraints:
  - Hiện đại, bo góc, datagrid đẹp
  - Dark/Light mode
  - Bảng màu chủ đạo xanh lá hoặc xanh dương
  - Cảnh báo hàng sắp hết có cấu hình bật/tắt trong `Settings` (mặc định tắt), ngưỡng mặc định là `3`.

### STEP 7 - Import/Export Excel
- Import bằng ClosedXML:
  - Danh mục sản phẩm
  - Số dư đầu kỳ
  - Lịch sử nhập xuất
- Chuẩn cột import nhật ký (song ngữ VN-EN):
  - `so_phieu (doc_no)`
  - `ngay_lap_phieu (doc_date)`
  - `loai_phieu (doc_type)`
  - `trang_thai_phieu (document_status)`
  - `nghiep_vu (stock_effect_type)`
  - `ma_hang (product_code)`
  - `so_luong (qty)`
  - metadata nhân sự/khách hàng/thanh toán/ghi chú
- Quy tắc import ngày chứng từ:
  - Chấp nhận `dd/MM/yyyy` hoặc `dd/MM` (năm mặc định là năm tại thời điểm import dữ liệu).
  - Nếu sai ngày hoặc sai format: dừng import file và thông báo rõ số dòng bị lỗi.
- Export:
  - Excel

### STEP 8 - HOLD/SOLD/RETURNED Workflow
- Quy tắc:
  - Phiếu có HOLD phải ở trạng thái `CONFIRMED` để hạch toán tồn kho.
  - HOLD trừ tồn ngay khi ghi dòng HOLD.
  - HOLD -> SOLD không trừ tồn thêm.
  - HOLD -> RETURNED cộng tồn lại.
  - Một phiếu cho phép nhiều sản phẩm (quan hệ `stock_documents` 1-n `stock_document_items`).
- Hỗ trợ gửi nhiều size thử, đổi size, trả size không phù hợp.

### STEP 9 - Reporting Engine theo Date Range
- Báo cáo hỗ trợ:
  - Tháng, quý, năm, khoảng ngày bất kỳ.
- Logic:
  - Đầu kỳ = tổng transaction trước `start_date`
  - Nhập = `IMPORT` trong kỳ
  - Xuất = `EXPORT + HOLD` trong kỳ
  - Trả = `RETURN` trong kỳ
  - Tồn cuối = Đầu kỳ + Nhập - Xuất + Trả - Hư hỏng
- Hỗ trợ khóa sổ theo `reporting_periods`.

### STEP 10 - Publish .exe
- Build phát hành bằng `dotnet publish`.
- Cấu hình đóng gói chạy offline.
- Tài liệu backup/restore SQLite và checklist go-live.

## 3. Kế hoạch theo phase và tiến độ

### Phase 1 (MVP) - 6 đến 8 tuần
- Bao gồm STEP 1 -> STEP 5 + phần cốt lõi của STEP 7, STEP 8, STEP 9, STEP 10.
- Exit criteria:
  - Luồng end-to-end: import -> vận hành kho -> báo cáo -> export.
  - Build `.exe` chạy offline ổn định.

### Phase 2 (Nâng cao) - 3 đến 4 tuần
- Bao gồm STEP 6 đầy đủ + báo cáo nâng cao + tối ưu import/export.
- Exit criteria:
  - Dashboard và report nâng cao khớp số liệu nghiệp vụ thực tế.

### Phase 3 (Hardening) - 2 tuần
- Tối ưu hiệu năng, regression test, tài liệu vận hành production.
- Exit criteria:
  - Không còn lỗi nghiêm trọng trước go-live.

## 4. Yêu cầu tiếng Việt có dấu/không dấu
- Cột database dùng tiếng Anh 100%; dữ liệu tiếng Việt có dấu vẫn được lưu bình thường.
- Không thêm cột `search_name/search_code`; chuẩn hóa chuỗi tìm kiếm bằng code logic ở tầng service/query.
- Tìm kiếm realtime hỗ trợ cả input có dấu và không dấu.
- Chiến lược kỹ thuật tìm kiếm:
  - Chuẩn hóa Unicode về dạng NFD, loại bỏ dấu (NonSpacingMark), chuyển lowercase, trim khoảng trắng thừa cho cả từ khóa và dữ liệu so sánh.
  - Kết hợp lọc coarse bằng SQL (`code`, `name` theo prefix/contains có giới hạn) rồi chuẩn hóa và đối sánh ở application layer để đảm bảo tốc độ.
  - Áp dụng debounce trên ô tìm kiếm và giới hạn page size để giữ realtime khi dữ liệu lớn.
- Ví dụ: `Áo thể thao` và `ao the thao` trả cùng kết quả.

## 5. Kế hoạch kiểm thử và nghiệm thu
- Unit test:
  - Công thức tồn kho transaction-based.
  - HOLD lifecycle.
  - Hàm chuẩn hóa tiếng Việt và tìm kiếm có dấu/không dấu.
  - Validation import + mapping cột.
- Integration test:
  - Toàn vẹn transaction nhiều dòng.
  - Báo cáo theo date range và kỳ khóa sổ.
- UAT:
  - Xuất thường.
  - HOLD đổi size.
  - Import dữ liệu từ template chuẩn.
  - Phiếu nhiều sản phẩm với trạng thái hỗn hợp HOLD/SOLD/RETURNED.

## 6. Quy tắc phân quyền và chỉnh sửa chứng từ
- `viewer`: chỉ xem, không tạo/sửa/xóa/chuyển trạng thái.
- `employee`, `admin`: tạo phiếu và xử lý `HOLD -> SOLD/RETURNED`.
- Phiếu tạo mới mặc định `document_status = DRAFT`; chỉ chuyển `CONFIRMED` khi người dùng thực hiện thao tác xác nhận phiếu.
- Phiếu `CONFIRMED`: không sửa/xóa trực tiếp; muốn hủy phải chuyển `CANCELLED`, ghi log, và tạo phiếu đảo mới.
- Phiếu thuộc kỳ đã khóa: chỉ `admin` được mở khóa kỳ trước khi chỉnh sửa dữ liệu.
- Luồng hủy phiếu:
  - Phiếu gốc đổi sang `CANCELLED`.
  - Tạo phiếu reversal mới (`doc_type = ADJUSTMENT`) chứa các dòng đảo nghiệp vụ.
  - Phiếu reversal liên kết phiếu gốc qua `reversed_document_id` và/hoặc `reference_doc_no`.
  - Không cho hủy lặp lại cùng một phiếu đã có reversal.

## 7. Traceability: Requirement -> Deliverable
- Architecture + 5 project -> STEP 1
- SQLite schema/FK/index -> STEP 2
- Models -> STEP 3
- Repository/Service/DTO/DI/async -> STEP 4
- Avalonia views (MVVM) -> STEP 5
- Dashboard Avalonia -> STEP 6
- Import/Export Excel -> STEP 7
- HOLD/SOLD/RETURNED -> STEP 8
- Reporting date range + kỳ -> STEP 9
- Publish `.exe` -> STEP 10

## 8. Giả định chốt
- UI mặc định tiếng Việt có dấu; có cơ chế chuyển ngôn ngữ Việt/Anh.
- Cột DB và enum nội bộ dùng tiếng Anh; UI hiển thị tiếng Việt mặc định qua lớp mapping label.
- Go-live bằng dữ liệu import chuẩn hóa, không vận hành song song Excel-App dài hạn.
