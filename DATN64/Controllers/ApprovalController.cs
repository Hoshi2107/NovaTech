using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DATN64.Models;
using DATN64.Helpers;
using System;
using System.Linq;

namespace DATN64.Controllers
{
    public class ApprovalController : Controller
    {
        private readonly AppDbContext _context;

        public ApprovalController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var session = HttpContext.Session;
            var email = session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login", "Account");

            // Check at least one relevant permission
            bool canApproveOrder    = AuthHelper.HasPermission(HttpContext, "Approve_Order");
            bool canApproveInv      = AuthHelper.HasPermission(HttpContext, "Import_Inventory");

            if (!canApproveOrder && !canApproveInv)
                return View("~/Views/Shared/AccessDenied.cshtml");

            // Pending orders — trạng thái "Đơn mới" hoặc "Chờ duyệt"
            var pendingOrders = _context.DonHangs
                .Include(o => o.KhachHang)
                .Where(o => o.TrangThai == "Đơn mới" || o.TrangThai == "Chờ duyệt")
                .OrderByDescending(o => o.NgayDat)
                .ToList();

            // Pending inventory transactions — trạng thái "Chờ duyệt" hoặc "Chờ duyệt PN"
            var pendingInventory = _context.InventoryTransactions
                .Where(t => t.TrangThai == "Chờ duyệt" || t.TrangThai == "Chờ duyệt PN")
                .OrderByDescending(t => t.Date)
                .ToList();

            // Pending purchase requests (YeuCauNhap) — trạng thái "Chờ duyệt YC"
            var pendingRequests = _context.InventoryTransactions
                .Where(t => t.PhanLoai == "YeuCauNhap" && t.TrangThai == "Chờ duyệt YC")
                .OrderByDescending(t => t.Date)
                .ToList();

            ViewBag.PendingOrders    = pendingOrders;
            ViewBag.PendingInventory = pendingInventory;
            ViewBag.PendingRequests  = pendingRequests;
            ViewBag.CanApproveOrder  = canApproveOrder;
            ViewBag.CanApproveInv    = canApproveInv;

            return View();
        }

        // POST: Approve/Reject đơn hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveOrder(int id, string action, string? lyDo)
        {
            if (!AuthHelper.HasPermission(HttpContext, "Approve_Order"))
                return Forbid();

            var order = _context.DonHangs.FirstOrDefault(o => o.MaDonHang == id);
            if (order == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy đơn hàng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            if (action == "approve")
            {
                order.TrangThai = "Đã xác nhận";
                TempData["ToastMessage"] = $"Đã duyệt đơn hàng #{id} thành công (Trạng thái: Đã xác nhận).";
                TempData["ToastType"] = "success";
            }
            else if (action == "reject")
            {
                order.TrangThai = "Đã hủy";
                order.GhiChu = (order.GhiChu ?? "") + $"\n[Từ chối] {lyDo}";

                // ✅ Hoàn lại hàng vào kho khi hủy/từ chối đơn
                var orderDetails = _context.ChiTietDonHangs
                    .Where(ct => ct.MaDonHang == id)
                    .ToList();

                foreach (var detail in orderDetails)
                {
                    var product = _context.SanPhams.FirstOrDefault(p => p.MaSanPham == detail.MaSanPham);
                    if (product != null)
                    {
                        product.SoLuongTon += detail.SoLuong;
                    }
                }

                TempData["ToastMessage"] = $"Đã từ chối đơn hàng #{id}. Hàng đã được hoàn về kho.";
                TempData["ToastType"] = "warning";
            }

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // POST: Approve/Reject phiếu kho
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveInventory(int id, string action, string? lyDo)
        {
            if (!AuthHelper.HasPermission(HttpContext, "Import_Inventory"))
                return Forbid();

            var txn = _context.InventoryTransactions.FirstOrDefault(t => t.Id == id);
            if (txn == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy phiếu kho.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            var approver = HttpContext.Session.GetString("UserName") ?? "Hệ thống";

            // Nếu là phiếu nhập thuộc quy trình 2 bước, cần validate trạng thái đúng
            if ((txn.PhanLoai == "PhieuNhap") && txn.TrangThai != "Chờ duyệt PN")
            {
                TempData["ToastMessage"] = "Phiếu này không ở trạng thái chờ duyệt!";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }
            if ((txn.PhanLoai != "PhieuNhap") && txn.TrangThai != "Chờ duyệt")
            {
                TempData["ToastMessage"] = "Phiếu này không ở trạng thái chờ duyệt!";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            if (action == "approve")
            {
                txn.TrangThai  = "Đã duyệt";
                txn.NguoiDuyet = approver;
                txn.NgayDuyet  = DateTime.Now;

                // Tìm sản phẩm theo SKU hoặc Tên sản phẩm
                SanPham? product = null;
                if (int.TryParse(txn.ProductSKU, out int pId) && pId > 0)
                {
                    product = _context.SanPhams.FirstOrDefault(p => p.MaSanPham == pId);
                }
                if (product == null)
                {
                    product = _context.SanPhams.FirstOrDefault(p => p.TenSanPham == txn.ProductName);
                }

                if (product != null)
                {
                    int beforeQty = product.SoLuongTon;
                    product.SoLuongTon += txn.QuantityChange;
                    if (product.SoLuongTon < 0) product.SoLuongTon = 0;
                    
                    txn.SoLuongTruoc = beforeQty;
                    txn.SoLuongSau = product.SoLuongTon;

                    if (txn.Type == "Nhập kho" && txn.QuantityChange > 0)
                    {
                        // Trích xuất đơn giá nhập từ Note nếu có
                        decimal importCost = product.GiaNhap;
                        if (txn.Note != null && txn.Note.Contains("Đơn giá nhập:"))
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(txn.Note, @"Đơn giá nhập:\s*([\d\.,]+)");
                            if (match.Success)
                            {
                                var numStr = match.Groups[1].Value.Replace(".", "").Replace(",", "");
                                if (decimal.TryParse(numStr, out var parsedCost) && parsedCost > 0)
                                {
                                    importCost = parsedCost;
                                }
                            }
                        }

                        if (importCost > 0)
                        {
                            product.GiaNhap = importCost;
                        }

                        // Ghi nhận chính thức vào PhieuNhap & ChiTietPhieuNhap cho Báo Cáo Biến Động Giá
                        var phieuNhap = new PhieuNhap
                        {
                            MaNCC = product.MaNCC > 0 ? product.MaNCC : 1,
                            MaNhanVien = 1,
                            NgayNhap = DateTime.Now
                        };
                        _context.PhieuNhaps.Add(phieuNhap);
                        _context.SaveChanges();

                        var ctPhieuNhap = new ChiTietPhieuNhap
                        {
                            MaPhieuNhap = phieuNhap.MaPhieuNhap,
                            MaSanPham = product.MaSanPham,
                            SoLuong = txn.QuantityChange,
                            GiaNhap = importCost
                        };
                        _context.ChiTietPhieuNhaps.Add(ctPhieuNhap);
                    }
                }

                // Cập nhật trạng thái YeuCauNhap liên kết → "Hoàn thành" để ẩn khỏi danh sách
                if (txn.PhanLoai == "PhieuNhap" && !string.IsNullOrEmpty(txn.MaYeuCauNhap) && int.TryParse(txn.MaYeuCauNhap, out int yeuCauId))
                {
                    var yeuCau = _context.InventoryTransactions.FirstOrDefault(t => t.Id == yeuCauId && t.PhanLoai == "YeuCauNhap");
                    if (yeuCau != null)
                    {
                        yeuCau.TrangThai = "Hoàn thành";
                    }
                }

                TempData["ToastMessage"] = $"Đã duyệt phiếu #{txn.Code} thành công và đồng bộ vào Báo cáo biến động giá!";
                TempData["ToastType"] = "success";
            }
            else if (action == "reject")
            {
                txn.TrangThai  = "Từ chối";
                txn.NguoiDuyet = approver;
                txn.NgayDuyet  = DateTime.Now;
                txn.LyDoTuChoi = lyDo;

                // Nếu từ chối phiếu nhập thực tế, trả trạng thái Yêu Cầu Nhập liên kết về "Đã duyệt YC" để có thể tạo lại
                if (txn.PhanLoai == "PhieuNhap" && !string.IsNullOrEmpty(txn.MaYeuCauNhap) && int.TryParse(txn.MaYeuCauNhap, out int yeuCauId))
                {
                    var yeuCau = _context.InventoryTransactions.FirstOrDefault(t => t.Id == yeuCauId && t.PhanLoai == "YeuCauNhap");
                    if (yeuCau != null)
                    {
                        yeuCau.TrangThai = "Đã duyệt YC";
                    }
                }

                TempData["ToastMessage"] = $"Đã từ chối phiếu #{txn.Code}.";
                TempData["ToastType"] = "warning";
            }

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult GetOrderDetails(int id)
        {
            var order = _context.DonHangs
                .Include(o => o.KhachHang)
                .Include(o => o.ChiTietDonHangs)
                    .ThenInclude(ct => ct.SanPham)
                .FirstOrDefault(o => o.MaDonHang == id);

            if (order == null)
                return NotFound(new { message = "Không tìm thấy đơn hàng." });

            var details = new
            {
                MaDonHang = order.MaDonHang,
                NgayDat = order.NgayDat?.ToString("dd/MM/yyyy HH:mm"),
                TongTien = order.TongTien,
                TrangThai = order.TrangThai,
                PhuongThucThanhToan = order.PhuongThucThanhToan,
                GhiChu = order.GhiChu,
                KhachHang = new
                {
                    HoTen = order.KhachHang?.HoTen ?? "Khách vãng lai",
                    SoDienThoai = order.KhachHang?.SoDienThoai ?? "",
                    Email = order.KhachHang?.Email ?? "",
                    DiaChi = order.KhachHang?.DiaChi ?? ""
                },
                Items = (order.ChiTietDonHangs ?? new List<ChiTietDonHang>()).Select(ct => new
                {
                    MaSanPham = ct.MaSanPham,
                    TenSanPham = ct.SanPham?.TenSanPham ?? "Sản phẩm không rõ",
                    HinhAnh = ct.SanPham?.HinhAnh ?? "",
                    SoLuong = ct.SoLuong,
                    DonGia = ct.DonGia,
                    ThanhTien = ct.SoLuong * ct.DonGia
                }).ToList()
            };

            return Json(details);
        }
    }
}
