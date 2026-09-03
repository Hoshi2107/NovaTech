# 🚀 TÀI LIỆU KỊCH BẢN BẢO VỆ ĐỒ ÁN NOVATECH
> **Ngày bảo vệ:** 04/09/2026  
> **Dự án:** Hệ thống Quản trị & Bán hàng Đa kênh NovaTech (ERP & E-Commerce)  
> **Các Module đảm nhiệm:** Kế toán (Chi tiết trọng tâm), Trợ lý AI Agent, Quy trình & Tích hợp TikTok Shop, Dashboard.

---

## 📌 MỤC LỤC
1. [Tổng Quan Kiến Trúc & Công Nghệ Hệ Thống](#1-tổng-quan-kiến-trúc--công-nghệ-hệ-thống)
2. [Module 1: Kế Toán & Quản Lý Dòng Tiền (TRỌNG TÂM CHI TIẾT)](#2-module-1-kế-toán--quản-lý-dòng-tiền-trọng-tâm-chi-tiết)
3. [Module 2: Trợ Lý AI Autonomous Agent (Gemini API)](#3-module-2-trợ-lý-ai-autonomous-agent-gemini-api)
4. [Module 3 & 4: Tích Hợp & Quy Trình Xử Lý TikTok Shop](#4-module-3--4-tích-hợp--quy-trình-xử-lý-tiktok-shop)
5. [Module 5: Dashboard (Tổng Quan Hệ Thống & HR KPIs)](#5-module-5-dashboard-tổng-quan-hệ-thống--hr-kpis)
6. [Bộ Câu Hỏi Phản Biện Từ Hội Đồng & Kịch Bản Trả Lời "Bao Đỗ"](#6-bộ-câu-hỏi-phản-biện-từ-hội-đồng--kịch-bản-trả-lời-bao-đỗ)

---

## 1. TỔNG QUAN KIẾN TRÚC & CÔNG NGHỆ HỆ THỐNG

### 🛠️ Tech Stack & Mô Hình
- **Backend:** C# ASP.NET Core MVC (.NET 8.0)
- **Database ORM:** Entity Framework Core (EF Core) với SQL Server.
- **AI Integration:** Google Gemini API (`GeminiService`) với kiến trúc Autonomous Agent (Agentic JSON Response).
- **Communication & Email:** MailKit / SmtpClient (`EmailService`) xử lý email HTML định dạng cao cấp.
- **Frontend & UI:** Razor Views (`.cshtml`), HTML5/CSS3, JavaScript (Fetch AJAX), Chart.js cho biểu đồ trực quan.

---

## 2. MODULE 1: KẾ TOÁN & QUẢN LÝ DÒNG TIỀN (TRỌNG TÂM CHI TIẾT)

### 2.1. Đặt Vấn Đề & Mục Tiêu Phân Hệ Kế Toán
Trong mô hình bán hàng đa kênh (Omnichannel ERP), việc ghi nhận doanh thu, chi phí và công nợ thủ công rất dễ gây thất thoát và sai lệch dòng tiền. Module Kế toán NovaTech được xây dựng với cơ chế **Tự động hóa đồng bộ gối đầu (Auto Synchronization)** giữa Bán hàng, Kho hàng và Sổ quỹ Kế toán.

---

### 2.2. Chi Tiết Các Chức Năng Cốt Lõi (Code-Level Logic)

#### A. Cơ chế Tự Động Đồng Bộ Dữ Liệu (`AutoSyncDataAsync`)
Mỗi khi truy cập Dashboard Kế toán (`AccountingController.Index`), hệ thống tự động kích hoạt hàm `AutoSyncDataAsync()` để cập nhật 2 luồng dữ liệu chính:

1. **Đồng bộ Doanh Thu Đơn Hàng Hoàn Thành vào Sổ Quỹ (`SoQuy`):**
   - Quét toàn bộ đơn hàng có trạng thái `"Hoàn thành"` chưa được ghi nhận trong Sổ Quỹ (`MaDonHang` chưa tồn tại trong `SoQuy`).
   - Tự động phát sinh **Phiếu Thu (`PT-xxxxx`)**.
   - Tự động nhận diện Kênh bán hàng dựa trên hàm `DetermineChannel(order)`:
     - **TikTok Shop:** Nếu `GhiChu` chứa `[TikTokShop#...]` hoặc `PhuongThucThanhToan` chứa `"TikTok"`.
     - **Bán lẻ tại quầy (POS):** Nếu `GhiChu` chứa `"POS"` hoặc `"tại quầy"`.
     - **Online / Web:** Nếu có địa chỉ giao hàng (`DiaChi`), phương thức thanh toán online (`VNPay`, `MoMo`, `COD`), hoặc ghi chú web.

2. **Đồng bộ Nhập Kho vào Công Nợ Nhà Cung Cấp (`CongNoNCC`):**
   - Quét các phiếu nhập kho đã phê duyệt (`InventoryTransaction.Type == "Nhập kho"` và `TrangThai == "Đã duyệt"`).
   - Tự động tạo bản ghi `PhieuNhap` và `ChiTietPhieuNhap` gối đầu nếu chưa có.
   - Khởi tạo bản ghi **Công nợ Nhà cung cấp (`CongNoNCC`)** với số tiền `TongTien = QuantityChange * GiaNhap`, hạn thanh toán mặc định 30 ngày (`HanThanhToan = NgayTao.AddDays(30)`), trạng thái ban đầu `"ChuaThanhToan"`.

```csharp
// Trích đoạn logic đồng bộ Công nợ NCC từ Giao dịch Kho đã duyệt
var congNo = new CongNoNCC
{
    MaNCC = product.MaNCC,
    MaPhieuNhap = pn.MaPhieuNhap,
    TongTien = tx.QuantityChange * product.GiaNhap,
    DaThanhToan = 0,
    NgayTao = tx.Date,
    HanThanhToan = tx.Date.AddDays(30),
    TrangThai = "ChuaThanhToan"
};
_context.CongNoNCCs.Add(congNo);
```

---

#### B. Sổ Quỹ Thu Chi & Điều Chỉnh Dòng Tiền (Cash Journal & Audit)
- **Tổng Thu (`TongThu`):** Tổng tiền các Phiếu Thu (`LoaiGiaoDich == "Thu"` và `TrangThai != "Đã hủy"`).
- **Tổng Chi (`TongChi`):** Tổng tiền các Phiếu Chi (`LoaiGiaoDich == "Chi"` và `TrangThai != "Đã hủy"`).
- **Số Dư Quỹ (`SoDuQuy`):** $\text{Số Dư} = \text{Tổng Thu} - \text{Tổng Chi}$.
- **Lập Phiếu Thu / Chi Thủ Công (`CreateTransaction`):** Tự động sinh mã `PT-xxxxx` hoặc `PC-xxxxx`, kiểm tra số tiền $> 0$, ghi nhận nhân viên lập phiếu qua Session.
- **Hủy Phiếu / Void Giao Dịch (`VoidTransaction`):** Không xóa cứng trong CSDL để đảm bảo tính **Audit Trail (Vết toán)**. Chuyển trạng thái sang `"Đã hủy"` và append lý do hủy vào `GhiChu`. Số dư quỹ tự động tính toán lại bỏ qua phiếu này.

---

#### C. Quản Lý Công Nợ Nhà Cung Cấp (`PaySupplierDebt` & `GetCongNoDetail`)
- **Báo cáo Tổng hợp Công nợ:** Nhóm theo Nhà cung cấp (`MaNCC`), tính Tổng nợ (`TongNo`), Đã thanh toán (`DaThanhToan`), Còn nợ (`ConNo`), và Số phiếu nợ active.
- **Thanh Toán Nợ Nhà Cung Cấp (`PaySupplierDebt`):**
  - Validation: Số tiền trả $0 < \text{paymentAmount} \le (\text{TongTien} - \text{DaThanhToan})$.
  - Cập nhật lũy kế `DaThanhToan += paymentAmount`. Chuyển trạng thái sang `"ThanhToanMotPhan"` hoặc `"DaHoanTat"`.
  - Tự động phát sinh **Phiếu Chi (`PC-xxxxx`)** tương ứng trong Sổ Quỹ với nhóm giao dịch `"Trả nợ NCC"`, gắn liên kết `CongNoId` để phục vụ tra cứu lịch sử thanh toán.

---

#### D. Báo Cáo Kết Quả Kinh Doanh P&L (Profit & Loss Statement)
Báo cáo tài chính P&L được tính toán linh hoạt theo khoảng thời gian lọc (`tuNgay` đến `denNgay`):

$$\begin{aligned}
\text{Doanh Thu Thuần} &= \sum \text{TongTien (Đơn hoàn thành trong kỳ)} \\
\text{Giá Vốn Hàng Bán (COGS)} &= \sum (\text{SoLuong} \times \text{GiaNhap}) \\
\text{Lợi Nhuận Gộp (Gross Profit)} &= \text{Doanh Thu Thuần} - \text{Giá Vốn Hàng Bán} \\
\text{Chi Phí Nhân Sự (HR/Payroll)} &= \sum (\text{TongGioLam} \times \text{LuongTheoGio}) \\
\text{Chi Phí Vận Hành Khác} &= \sum \text{Sổ Quỹ Chi (trừ nhóm "Trả lương")} \\
\text{Lợi Nhuận Ròng (Net Profit)} &= \text{Lợi Nhuận Gộp} - (\text{Chi Phí Nhân Sự} + \text{Chi Phí Vận Hành Khác})
\end{aligned}$$

---

#### E. Trực Quan Hóa Biểu Đồ Kế Toán
1. **Biểu đồ Dòng tiền 7 ngày:** So sánh Thu vs. Chi từng ngày trong tuần.
2. **Biểu đồ P&L 12 tháng:** So sánh Doanh thu, Giá vốn, và Lợi nhuận của 12 tháng gần nhất.
3. **Biểu đồ Doanh thu theo Kênh:** Phân tích tỷ lệ đóng góp của TikTok Shop, Bán lẻ tại quầy và Website Online.

---

## 3. MODULE 2: TRỢ LÝ AI AUTONOMOUS AGENT (GEMINI API)

### 3.1. Điểm Đột Phá Kiến Trúc (Agentic AI Pattern)
Trợ lý AI của NovaTech không chỉ dừng lại ở việc trả lời chatbot văn bản đơn thuần, mà đóng vai trò là một **Autonomous Agent** có khả năng phân tích dữ liệu kinh doanh real-time và **tự động đề xuất / thực thi tác vụ quản trị ERP**.

```
[User Chat Input] ➔ [AiController] ➔ [Fetch Real-Time DB Snapshot] ➔ [Gemini System Prompt Injection]
                                                                                │
                                                                                ▼
[Execute Action Endpoint] ◄── [Structured Agentic JSON Response (ActionType & Payload)]
```

---

### 3.2. Luồng Dữ Liệu & Cách Thức Hoạt Động

1. **Ràng buộc cách ly hội thoại (Per-User Isolation):**  
   Mỗi tin nhắn lưu trong `ChatMessages` được đánh dấu prefix `Sender = $"User:{userEmail}"` và `Sender = $"AI:{userEmail}"`.
2. **Bơm ngữ cảnh Real-time Database Snapshot:**  
   Trước khi gửi tới Gemini API, `AiController` thực thi SQL queries lấy toàn bộ thông số hiện tại của cửa hàng:
   - Tổng doanh thu, tổng số đơn, số đơn chờ / hoàn thành.
   - Danh sách Top 5 sản phẩm bán chạy nhất.
   - **Top 3 Sản phẩm Tồn kho cao nhưng Bán chậm nhất tháng này** (dùng để gợi ý xả hàng).
   - Cảnh báo sản phẩm tồn kho thấp ($\le 5$).
   - Danh sách Khách hàng VIP, Nhà cung cấp, Danh mục, Thương hiệu.

3. **Cấu trúc Trả về Agentic JSON:**
```json
{
  "message": "Nội dung phản hồi hỗ trợ Markdown...",
  "hasAction": true,
  "actionType": "CREATE_PRODUCT_AND_IMPORT | CREATE_PROMOTION_CAMPAIGN | SEND_VIP_REWARD | SEND_PROMO_EMAIL_DONG_PLUS",
  "actionPayload": { ... }
}
```

---

### 3.3. Các Action Types Tự Động Hóa Thực Tế (`ExecuteAction`)

| Action Type | Mô Tả Tự Động Hóa | CSDL & Hệ Thống Thay Đổi |
| :--- | :--- | :--- |
| `CREATE_PRODUCT_AND_IMPORT` | Gợi ý sản phẩm hot trend & tự động tạo phiếu nhập hàng | 1. Tạo `SanPham` mới<br/>2. Tạo `PhieuNhap` & `ChiTietPhieuNhap`<br/>3. Tạo `InventoryTransaction` ("Chờ duyệt")<br/>4. Phát thông báo `SystemNotification` |
| `CREATE_PROMOTION_CAMPAIGN` | Tự động khởi tạo chiến dịch Khuyến mãi | 1. Thêm `Voucher` mới vào CSDL<br/>2. Phát `SystemNotification` thông báo toàn hệ thống |
| `SEND_VIP_REWARD` | Lọc VIP & gửi email tri ân ưu đãi | 1. Lọc VIP ($\text{DiemTichLuy} \ge 500$ hoặc Target Email)<br/>2. Sinh Voucher VIP<br/>3. Render template HTML cao cấp và gửi SMTP thực tế qua MailKit |
| `SEND_PROMO_EMAIL_DONG_PLUS` | Xả hàng tồn bán chậm tự động | 1. Quét Top 3 SP tồn cao/bán chậm<br/>2. Sinh Voucher giảm giá<br/>3. Gửi Email HTML hiển thị Giá gốc vs Giá giảm cho khách hàng |

---

## 4. MODULE 3 & 4: TÍCH HỢP & QUY TRÌNH XỬ LÝ TIKTOK SHOP

### 4.1. Architecture & Phương Thức Tích Hợp
- **Mô hình kết nối:** RESTful HTTP Sync Client kết nối trực tiếp với Microservice Trình giả lập TikTok Shop (`http://localhost:6060/api/stream/orders`).
- **Cơ chế chống trùng đơn (Idempotency):** Mỗi đơn TikTok thu thập về được gán identifier chuẩn `[TikTokShop#{OrderId}]` lưu tại trường `DonHang.GhiChu`. Trước khi tạo đơn mới, hệ thống kiểm tra sự tồn tại của ID này.

---

### 4.2. Quy Trình Đồng Bộ & Xử Lý Đơn Hàng TikTok (`TriggerSync`)

```
[TikTok Simulator API] ➔ [Fetch Orders JSON] ➔ [Check Existing Identifier [TikTokShop#ID]]
                                                                │
                 ┌──────────────────────────────────────────────┴──────────────────────────────────────────────┐
                 ▼ (Nếu Đã Tồn Tại)                                                                             ▼ (Nếu Đơn Mới)
       [Cập Nhật Trạng Thái &                                                                      [Map / Tạo Khách Hàng mới]
     Phương Thức Thanh Toán]                                                                                   │
                                                                                                               ▼
                                                                                               [Kiểm Tra Sản Phẩm SKU Trong DB]
                                                                                                               │
                                                               ┌───────────────────────────────────────────────┴───────────────────────────────────────────────┐
                                                               ▼ (Tồn Tại Sản Phẩm)                                                                            ▼ (Không Tồn Tại)
                                                     [Tạo Đơn Hàng & Chi Tiết]                                                                      [Log Ghi Lỗi Thất Bại & Skip]
                                                               │                                                                                              (Tránh Sai Lệch Kho)
                                                               ▼
                                                     [Trừ Kho Trực Tiếp `SoLuongTon`]
                                                               │
                                                               ▼
                                                     [Tạo Thông Báo Hệ Thống]
```

---

### 4.3. Bảng Mapping Trạng Thái Đơn Hàng (Status Mapping Engine)

| TikTok Shop Status (API Source) | NovaTech ERP Status (Internal) | Ý Nghĩa Nghiệp Vụ |
| :--- | :--- | :--- |
| `Awaiting Shipment`, `Pending` | **Chờ duyệt** | Đơn mới tinh, chờ nhân viên xác nhận |
| `Paid` | **Đã thanh toán** | Đơn đã thanh toán qua TikTok Pay |
| `Ready To Ship` | **Đang đóng gói** | Kho đang đóng gói in phiếu giao hàng |
| `Shipped`, `In Transit` | **Đang giao** | Đã bàn giao cho đơn vị vận chuyển |
| `Delivered`, `Completed`, `Received` | **Hoàn thành** | Khách đã nhận hàng (Kế toán tự động ghi nhận thu) |
| `Cancelled`, `Canceled` | **Đã hủy** | Đơn bị hủy bởi người mua hoặc sàn |

---

## 5. MODULE 5: DASHBOARD (TỔNG QUAN HỆ THỐNG & HR KPIS)

### 5.1. Real-Time Business KPIs
- **Chỉ số kinh doanh tức thì:** Tổng Doanh thu, Doanh thu Hôm nay, Tổng số Đơn, Đơn chờ duyệt, Đơn hoàn thành, Tổng số Khách hàng, Tổng số Sản phẩm.
- **Tần suất mua hàng trung bình (Average Order Frequency):** $\text{AvgOrdersPerCustomer} = \frac{\text{TotalOrders}}{\text{TotalCustomers}}$.
- **Cảnh báo tồn kho tức thì (`LowStockProducts`):** Cảnh báo danh sách sản phẩm có số lượng tồn kho $\le 3$.

---

### 5.2. HR KPIs & Quản Lý Nhân Sự (Human Resource Analytics)
Hệ thống kết nối trực tiếp dữ liệu từ phân hệ Chấm công (`ChamCong`) và Nhân viên (`NhanVien`):
- **Tổng giờ làm tháng này (`TongGioLamThang`):** Tổng lũy kế giờ làm hoàn thành của toàn bộ nhân viên.
- **Tổng chi phí lương tháng (`TongChiPhiLuongThang`):**  
  $$\text{Tổng Lương} = \sum (\text{Tổng Giờ Làm Nhân Viên } i \times \text{Lương Theo Giờ } i)$$
- **Nhân viên xuất sắc nhất:** Tự động vinh danh nhân viên có tổng số giờ cống hiến cao nhất trong tháng.
- **Thống kê chuyên cần:** Tính số ngày vắng mặt để quản lý đưa ra nhắc nhở.

---

### 5.3. Top Selling Analytics & Biểu Đồ 7 Ngày
- **Top 5 Sản phẩm Bán chạy:** Thống kê theo tổng số lượng bán ra (`QuantitySold`) từ `ChiTietDonHang`.
- **Top 5 Khách hàng Thân thiết:** Nhóm theo `MaKhachHang`, sắp xếp theo số lượng đơn và tổng chi tiêu (`TotalSpent`).
- **Biểu đồ Doanh thu & Đơn hàng 7 ngày:** Sử dụng Chart.js hiển thị biến động doanh thu thực tế và số lượng đơn hàng theo các ngày trong tuần (T2 đến CN).

---

## 6. BỘ CÂU HỎI PHẢN BIỆN TỪ HỘI ĐỒNG & KỊCH BẢN TRẢ LỜI "BAO ĐỖ"

### ❓ Câu 1: "Tại sao trong Module Kế toán lại chọn cơ chế tự động đồng bộ gối đầu (`AutoSyncDataAsync`) thay vì cập nhật trực tiếp ngay khi bấm hoàn thành đơn hàng?"
> **💡 Kịch bản trả lời:**  
> *"Thưa thầy/cô, việc chọn kiến trúc **Event-Driven / Pull Synchronization (`AutoSyncDataAsync`)** mang lại 3 ưu điểm vượt trội:*  
> 1. **Tính độc lập & Giảm Coupling (Loose Coupling):** Phân hệ Bán hàng/Kho và Kế toán làm việc độc lập. Nếu phân hệ kế toán có bảo trì hay cập nhật logic, luồng đặt hàng online vẫn diễn ra mượt mà không bị ngắt quãng.  
> 2. **Tính toàn vẹn & Chống sót dữ liệu (Data Integrity):** Khi người dùng mở Dashboard Kế toán, hệ thống sẽ rà soát quét toàn bộ các đơn hàng và phiếu nhập phát sinh trước đó (bao gồm cả các đơn nhập từ TikTok hay hệ thống POS thứ 3) để đảm bảo không một giao dịch nào bị bỏ sót.  
> 3. **Hiệu năng:** Tránh việc phải chèn quá nhiều transaction DB cùng một lúc khi khách hàng bấm mua hàng ở Frontend."

---

### ❓ Câu 2: "Cơ chế tính Giá vốn hàng bán (COGS) và Lợi nhuận P&L trong hệ thống được xử lý như thế nào nếu giá nhập sản phẩm biến động?"
> **💡 Kịch bản trả lời:**  
> *"Thưa thầy/cô, trong phiên bản hiện tại, Giá vốn hàng bán được tính toán chính xác dựa trên công thức:*  
> $$\text{COGS} = \sum (\text{Số lượng bán} \times \text{Giá nhập tại thời điểm lưu của Sản phẩm})$$  
> *Đồng thời, hệ thống NovaTech có lưu vết `GiaNiemYetLucNhap` và `GiaNhap` trong bảng `ChiTietPhieuNhap`. Khi tính P&L, hệ thống bóc tách rõ Doanh thu thuần, Giá vốn, Chi phí lương nhân sự từ bảng Chấm công (`ChamCong.TongGioLam * NhanVien.LuongTheoGio`) và Chi phí vận hành từ Sổ Quỹ Chi để đưa ra **Lợi nhuận ròng (Net Profit)** thực tế nhất."*

---

### ❓ Câu 3: "Trợ lý AI của em đóng vai trò gì? Nếu Gemini API trả về sai định dạng JSON thì hệ thống xử lý ra sao để không bị crash?"
> **💡 Kịch bản trả lời:**  
> *"Thưa thầy/cô, Trợ lý AI của NovaTech là một **Autonomous Agent**. Em không chỉ dùng AI để chat text, mà bơm **Real-time DB Snapshot** (doanh thu, tồn kho, SP bán chậm) vào Prompt để AI đưa ra quyết định hành động (Action Type) như: Tự động nhập hàng hot trend, tạo voucher, hoặc gửi email tri ân VIP.*  
> *Về vấn đề xử lý lỗi: Em đã xây dựng cơ chế **Fallback & Exception Handling** 3 lớp trong `AiController.cs`:*  
> 1. Dùng Regex làm sạch chuỗi markdown code block ```json ... ``` từ API response.  
> 2. Đưa vào hàm `JsonSerializer.Deserialize` với `PropertyNameCaseInsensitive = true`.  
> 3. Nếu Deserialize thất bại (do API quá tải hoặc lỗi định dạng), hệ thống bắt block `catch` và tự động chuyển sang chế độ trả về tin nhắn văn bản thuần túy (`HasAction = false`), giúp giao diện UI luôn mượt mà và không bao giờ xảy ra lỗi Crash 500."*

---

### ❓ Câu 4: "Khi đồng bộ đơn hàng từ TikTok Shop về NovaTech, em xử lý trường hợp trùng đơn hàng hoặc sản phẩm trong đơn không tồn tại trên hệ thống như thế nào?"
> **💡 Kịch bản trả lời:**  
> *"Thưa thầy/cô, em đã giải quyết triệt để 2 rủi ro này bằng các thuật toán nghiệp vụ:*  
> 1. **Chống trùng đơn:** Mỗi đơn hàng TikTok có một mã OrderId duy nhất. Em gán identifier `[TikTokShop#{OrderId}]` vào trường `GhiChu`. Trước khi thêm mới, hệ thống truy vấn kiểm tra trùng lặp. Nếu đã có, hệ thống chỉ cập nhật trạng thái đơn hàng và phương thức thanh toán chứ không tạo mới.  
> 2. **Xử lý Sản phẩm không tồn tại:** Trước khi tạo đơn, hệ thống vòng lặp kiểm tra toàn bộ SKU/ProductId. Nếu phát hiện có sản phẩm chưa được khai báo trên NovaTech, hệ thống sẽ **bỏ qua (Skip)** đơn đó, đồng thời ghi lại bản ghi Log thất bại trong bảng `TikTokSyncLog` yêu cầu quản trị viên đồng bộ danh mục trước. Điều này ngăn chặn tuyệt đối việc sai lệch kho và rác dữ liệu CSDL."*

---

### ❓ Câu 5: "Quy trình void (hủy) một phiếu thu/chi trong Sổ Quỹ Kế toán hoạt động như thế nào?"
> **💡 Kịch bản trả lời:**  
> *"Thưa thầy/cô, trong kế toán tài chính, việc dùng lệnh `DELETE` xóa bỏ hoàn toàn một giao dịch là vi phạm nguyên tắc quản trị. Vì vậy trong hàm `VoidTransaction`, em áp dụng cơ chế **Soft Cancel & Audit Trail**:*  
> - Chuyển trạng thái phiếu thành `"Đã hủy"`.  
> - Ghi nối thêm thông tin kiểm toán: `[HỦY PHIẾU - {Thời gian}] {Lý do hủy}` vào ghi chú.  
> - Công thức tính Số dư quỹ tự động loại trừ các phiếu có `TrangThai == "Đã hủy"`. Điều này đảm bảo tính minh bạch và giúp quản lý dễ dàng truy vết lại các giao dịch bất thường."*
