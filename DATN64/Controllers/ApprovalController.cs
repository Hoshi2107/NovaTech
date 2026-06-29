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

            // Pending inventory transactions — trạng thái "Chờ duyệt"
            var pendingInventory = _context.InventoryTransactions
                .Where(t => t.TrangThai == "Chờ duyệt")
                .OrderByDescending(t => t.Date)
                .ToList();

            ViewBag.PendingOrders    = pendingOrders;
            ViewBag.PendingInventory = pendingInventory;
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
                order.TrangThai = "Đã duyệt";
                TempData["ToastMessage"] = $"Đã duyệt đơn hàng #{id} thành công.";
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

            if (action == "approve")
            {
                txn.TrangThai  = "Đã duyệt";
                txn.NguoiDuyet = approver;
                txn.NgayDuyet  = DateTime.Now;

                // Update actual stock if not yet applied (match by product name)
                var product = _context.SanPhams.FirstOrDefault(p => p.TenSanPham == txn.ProductName);
                if (product != null)
                {
                    product.SoLuongTon += txn.QuantityChange;
                    if (product.SoLuongTon < 0) product.SoLuongTon = 0;
                }

                TempData["ToastMessage"] = $"Đã duyệt phiếu #{txn.Code} thành công.";
                TempData["ToastType"] = "success";
            }
            else if (action == "reject")
            {
                txn.TrangThai  = "Từ chối";
                txn.NguoiDuyet = approver;
                txn.NgayDuyet  = DateTime.Now;
                txn.LyDoTuChoi = lyDo;
                TempData["ToastMessage"] = $"Đã từ chối phiếu #{txn.Code}.";
                TempData["ToastType"] = "warning";
            }

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
    }
}
