# Hướng dẫn chạy dự án Frontend Vue.js (NovaTech ERP & Store)

Thư mục này chứa mã nguồn của giao diện **Frontend Single Page Application (SPA)** được chuyển đổi hoàn toàn từ các View MVC sang **Vue.js 3** và **Vite**.

## Yêu cầu chuẩn bị
Bạn cần cài đặt **Node.js** phiên bản mới nhất (khuyên dùng v18+ hoặc v20+) trên máy tính. Nếu chưa cài đặt, vui lòng tải và cài đặt tại: [https://nodejs.org/](https://nodejs.org/)

---

## Các bước khởi chạy dự án

### Bước 1: Khởi động Backend (.NET Core MVC)
Chạy ứng dụng backend của bạn từ Visual Studio hoặc sử dụng dòng lệnh:
```bash
dotnet run
```
Backend sẽ khởi chạy trên cổng mặc định (HTTP: `http://localhost:5018` và HTTPS: `https://localhost:7047`).

### Bước 2: Cài đặt và Chạy Frontend Vue.js
1. Mở thư mục `frontend` này bằng **Visual Studio Code**.
2. Mở một Terminal mới trong VS Code (`Ctrl + ~`) và chạy lệnh cài đặt thư viện:
   ```bash
   npm install
   ```
3. Khởi động máy chủ phát triển (Vite Dev Server):
   ```bash
   npm run dev
   ```
4. Mở trình duyệt và truy cập: `http://localhost:5173/`

---

## Cơ chế kết nối API & Bảo mật Session

Dự án Vue.js sử dụng **Vite Proxy** được cấu hình trong `vite.config.js`. Toàn bộ các yêu cầu gọi tới cổng `http://localhost:5173/api/*` sẽ được tự động chuyển tiếp về Backend `http://localhost:5018/api/*`.

- **Ưu điểm**:
  - Không gặp lỗi CORS (Cross-Origin Resource Sharing).
  - Tự động chia sẻ **Cookie Session** (`ASP.NET_SessionId`) giữa Frontend và Backend. Hệ thống xác thực đăng nhập hiện tại trên Backend được giữ nguyên và hoạt động mượt mà.
  - Phân quyền (RBAC) cho nhân sự được kiểm tra đồng bộ thông qua hàm `authService.hasPermission()` trong tệp `src/services/auth.js`.

---

## Cấu trúc thư mục chính của Frontend
- `src/assets/admin_custom.css`: Tệp chứa toàn bộ phong cách CSS thiết kế cao cấp (Glassmorphism, Neon Gradient, Animations) được sao chép trực tiếp từ tệp CSS gốc của bạn.
- `src/services/auth.js`: Quản lý trạng thái đăng nhập, phân quyền người dùng và kết nối phiên với API backend.
- `src/services/cart.js`: Quản lý giỏ hàng trực tuyến của khách hàng sử dụng LocalStorage.
- `src/router/index.js`: Định tuyến và kiểm tra quyền truy cập cho tất cả các trang.
- `src/components/`:
  - `AdminLayout.vue`: Bố cục trang quản trị ERP (Sidebar + Header + Footer).
  - `OnlineLayout.vue`: Bố cục trang mua sắm Online Storefront (Mega Menu + Footer).
  - `POSLayout.vue`: Bố cục POS bán hàng tại quầy.
- `src/views/`:
  - `Login.vue`, `ForgotPassword.vue`, `Profile.vue`, `ChangePassword.vue`: Hệ thống đăng nhập và tài khoản.
  - `PortalSelection.vue`: Cổng chọn phân hệ (ERP, POS, Online Shop) dựa trên quyền của người dùng.
  - `POS.vue`: Màn hình bán hàng nhanh tại quầy.
  - `erp/`: Các màn hình quản trị ERP (Dashboard, Sản phẩm, Tồn kho, Khách hàng, Khuyến mãi, TikTok Shop, AI Assistant Chat, Nhân sự, Báo cáo, Cài đặt).
  - `store/`: Các màn hình của cửa hàng Online công cộng (Trang chủ, Danh sách lọc sản phẩm, Chi tiết sản phẩm, Giỏ hàng thanh toán).
