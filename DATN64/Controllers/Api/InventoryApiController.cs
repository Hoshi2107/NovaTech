using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using DATN64.Models;

namespace DATN64.Controllers.Api
{
    [Route("api/inventory")]
    [ApiController]
    public class InventoryApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InventoryApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/inventory
        [HttpGet]
        public async Task<IActionResult> GetInventory(string? search, int? category, int? brand)
        {
            try
            {
                var query = _context.SanPhams
                    .Include(p => p.DanhMuc)
                    .Include(p => p.ThuongHieu)
                    .Include(p => p.NhaCungCap)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => p.TenSanPham.Contains(search) || p.MaSanPham.ToString().Contains(search));
                }
                if (category.HasValue && category.Value > 0)
                {
                    query = query.Where(p => p.MaDanhMuc == category.Value);
                }
                if (brand.HasValue && brand.Value > 0)
                {
                    query = query.Where(p => p.MaThuongHieu == brand.Value);
                }

                var products = await query.Select(p => new {
                    p.MaSanPham,
                    p.TenSanPham,
                    p.MaDanhMuc,
                    p.MaThuongHieu,
                    p.MaNCC,
                    p.GiaNhap,
                    p.GiaBan,
                    p.SoLuongTon,
                    p.MoTa,
                    p.HinhAnh,
                    p.TrangThai,
                    DanhMucTen = p.DanhMuc != null ? p.DanhMuc.TenDanhMuc : "",
                    ThuongHieuTen = p.ThuongHieu != null ? p.ThuongHieu.TenThuongHieu : "",
                    NhaCungCapTen = p.NhaCungCap != null ? p.NhaCungCap.TenNCC : "",
                    KhoMacDinh = "Kho chính" // Mặc định hiển thị Kho chính
                }).ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải danh sách tồn kho", error = ex.Message });
            }
        }

        // GET: api/inventory/transactions
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions()
        {
            try
            {
                var transactions = await _context.InventoryTransactions.ToListAsync();

                var result = transactions.OrderByDescending(t => t.Date).Select(t => new {
                    t.Id,
                    t.Code,
                    t.Type,
                    t.ProductSKU,
                    t.ProductName,
                    t.QuantityChange,
                    t.Creator,
                    Date = t.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                    t.Note,
                    t.SoLuongTruoc,
                    t.SoLuongSau,
                    t.TrangThai
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải lịch sử giao dịch kho", error = ex.Message });
            }
        }

        // POST: api/inventory/import
        [HttpPost("import")]
        public async Task<IActionResult> ImportInventory([FromBody] ImportRequest request)
        {
            if (request == null || request.ProductId <= 0 || request.Quantity <= 0)
            {
                return BadRequest(new { message = "Dữ liệu nhập kho không hợp lệ!" });
            }

            try
            {
                var product = await _context.SanPhams.FirstOrDefaultAsync(p => p.MaSanPham == request.ProductId);
                if (product == null)
                {
                    return NotFound(new { message = "Không tìm thấy sản phẩm!" });
                }

                int count = await _context.InventoryTransactions.CountAsync(t => t.Type == "Nhập kho") + 1;
                string code = "PN" + count.ToString("D6");

                var tx = new InventoryTransaction
                {
                    Code = code,
                    Type = "Nhập kho",
                    ProductSKU = product.MaSanPham.ToString(),
                    ProductName = product.TenSanPham,
                    QuantityChange = request.Quantity,
                    Creator = HttpContext.Session.GetString("UserName") ?? "Thủ kho",
                    Date = DateTime.Now,
                    Note = string.IsNullOrEmpty(request.Note) ? $"Nhập kho ({request.Source})" : $"{request.Note} (Nguồn: {request.Source})",
                    TrangThai = "Chờ duyệt",
                    SoLuongTruoc = null,
                    SoLuongSau = null
                };

                _context.InventoryTransactions.Add(tx);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = $"Đã tạo phiếu nhập kho chờ duyệt cho {request.Quantity} sản phẩm! Vui lòng đợi quản lý phê duyệt.", 
                    code = code 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi tạo phiếu nhập kho", error = ex.Message });
            }
        }

        // POST: api/inventory/export
        [HttpPost("export")]
        public async Task<IActionResult> ExportInventory([FromBody] ExportRequest request)
        {
            if (request == null || request.ProductId <= 0 || request.Quantity <= 0)
            {
                return BadRequest(new { message = "Dữ liệu xuất kho không hợp lệ!" });
            }

            try
            {
                var product = await _context.SanPhams.FirstOrDefaultAsync(p => p.MaSanPham == request.ProductId);
                if (product == null)
                {
                    return NotFound(new { message = "Không tìm thấy sản phẩm!" });
                }

                if (product.SoLuongTon < request.Quantity)
                {
                    return BadRequest(new { message = $"Số lượng tồn kho khả dụng ({product.SoLuongTon}) không đủ để xuất {request.Quantity}!" });
                }

                int count = await _context.InventoryTransactions.CountAsync(t => t.Type == "Xuất kho") + 1;
                string code = "PX" + count.ToString("D6");

                var tx = new InventoryTransaction
                {
                    Code = code,
                    Type = "Xuất kho",
                    ProductSKU = product.MaSanPham.ToString(),
                    ProductName = product.TenSanPham,
                    QuantityChange = -request.Quantity,
                    Creator = HttpContext.Session.GetString("UserName") ?? "Thủ kho",
                    Date = DateTime.Now,
                    Note = string.IsNullOrEmpty(request.Note) ? $"Xuất kho ({request.Source})" : $"{request.Note} (Lý do: {request.Source})",
                    TrangThai = "Chờ duyệt",
                    SoLuongTruoc = null,
                    SoLuongSau = null
                };

                _context.InventoryTransactions.Add(tx);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = $"Đã tạo phiếu xuất kho chờ duyệt cho {request.Quantity} sản phẩm! Vui lòng đợi quản lý phê duyệt.", 
                    code = code 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi tạo phiếu xuất kho", error = ex.Message });
            }
        }

        // POST: api/inventory/approve/{id}
        [HttpPost("approve/{id}")]
        public async Task<IActionResult> ApproveTransaction(int id)
        {
            var rolesString = HttpContext.Session.GetString("UserRoles") ?? "";
            var roles = rolesString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()).ToList();
            bool isManager = roles.Contains("Super Admin") || roles.Contains("Admin") || roles.Contains("Quản lý") || roles.Contains("Quản lý cửa hàng");
            if (!isManager)
            {
                return StatusCode(403, new { message = "Bạn không có quyền thực hiện thao tác này! Chỉ Quản lý trưởng/Admin mới có quyền duyệt đơn." });
            }

            try
            {
                var tx = await _context.InventoryTransactions.FirstOrDefaultAsync(t => t.Id == id);
                if (tx == null)
                {
                    return NotFound(new { message = "Không tìm thấy giao dịch!" });
                }

                if (tx.TrangThai != "Chờ duyệt")
                {
                    return BadRequest(new { message = "Giao dịch này đã được xử lý từ trước!" });
                }

                int productId = int.Parse(tx.ProductSKU);
                var product = await _context.SanPhams.FirstOrDefaultAsync(p => p.MaSanPham == productId);
                if (product == null)
                {
                    return NotFound(new { message = "Không tìm thấy sản phẩm tương ứng!" });
                }

                int beforeQty = product.SoLuongTon;

                if (tx.Type == "Nhập kho")
                {
                    product.SoLuongTon += tx.QuantityChange;
                }
                else if (tx.Type == "Xuất kho")
                {
                    if (product.SoLuongTon + tx.QuantityChange < 0)
                    {
                        return BadRequest(new { message = $"Số lượng tồn kho khả dụng ({product.SoLuongTon}) không đủ để thực hiện xuất {Math.Abs(tx.QuantityChange)}!" });
                    }
                    product.SoLuongTon += tx.QuantityChange;
                }
                else
                {
                    return BadRequest(new { message = "Loại giao dịch không hỗ trợ duyệt!" });
                }

                int afterQty = product.SoLuongTon;

                tx.TrangThai = "Đã duyệt";
                tx.SoLuongTruoc = beforeQty;
                tx.SoLuongSau = afterQty;
                tx.Date = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Đã phê duyệt phiếu và cập nhật số lượng tồn kho thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi duyệt phiếu", error = ex.Message });
            }
        }

        // POST: api/inventory/reject/{id}
        [HttpPost("reject/{id}")]
        public async Task<IActionResult> RejectTransaction(int id)
        {
            var rolesString = HttpContext.Session.GetString("UserRoles") ?? "";
            var roles = rolesString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()).ToList();
            bool isManager = roles.Contains("Super Admin") || roles.Contains("Admin") || roles.Contains("Quản lý") || roles.Contains("Quản lý cửa hàng");
            if (!isManager)
            {
                return StatusCode(403, new { message = "Bạn không có quyền thực hiện thao tác này! Chỉ Quản lý trưởng/Admin mới có quyền từ chối đơn." });
            }

            try
            {
                var tx = await _context.InventoryTransactions.FirstOrDefaultAsync(t => t.Id == id);
                if (tx == null)
                {
                    return NotFound(new { message = "Không tìm thấy giao dịch!" });
                }

                if (tx.TrangThai != "Chờ duyệt")
                {
                    return BadRequest(new { message = "Giao dịch này đã được xử lý từ trước!" });
                }

                tx.TrangThai = "Đã từ chối";
                tx.Date = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Đã từ chối phiếu giao dịch kho thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi từ chối phiếu", error = ex.Message });
            }
        }

        // POST: api/inventory/audit
        [HttpPost("audit")]
        public async Task<IActionResult> AuditInventory([FromBody] AuditRequest request)
        {
            if (request == null || request.ProductId <= 0 || request.ActualQuantity < 0)
            {
                return BadRequest(new { message = "Dữ liệu kiểm kê không hợp lệ!" });
            }

            try
            {
                var product = await _context.SanPhams.FirstOrDefaultAsync(p => p.MaSanPham == request.ProductId);
                if (product == null)
                {
                    return NotFound(new { message = "Không tìm thấy sản phẩm!" });
                }

                int beforeQty = product.SoLuongTon;
                int diff = request.ActualQuantity - beforeQty;
                product.SoLuongTon = request.ActualQuantity;

                int count = await _context.InventoryTransactions.CountAsync(t => t.Type == "Kiểm kê") + 1;
                string code = "PK" + count.ToString("D6");

                var tx = new InventoryTransaction
                {
                    Code = code,
                    Type = "Kiểm kê",
                    ProductSKU = product.MaSanPham.ToString(),
                    ProductName = product.TenSanPham,
                    QuantityChange = diff,
                    Creator = HttpContext.Session.GetString("UserName") ?? "Thủ kho",
                    Date = DateTime.Now,
                    Note = string.IsNullOrEmpty(request.Note) 
                        ? $"Kiểm kê kho. Thực tế: {request.ActualQuantity}, Lệch: {(diff >= 0 ? "+" : "")}{diff}"
                        : $"{request.Note} (Thực tế: {request.ActualQuantity}, Lệch: {(diff >= 0 ? "+" : "")}{diff})",
                    SoLuongTruoc = beforeQty,
                    SoLuongSau = request.ActualQuantity,
                    TrangThai = "Đã duyệt" // Audit is approved immediately
                };

                _context.InventoryTransactions.Add(tx);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Lưu kết quả kiểm kê kho thành công!", 
                    before = beforeQty, 
                    after = request.ActualQuantity, 
                    diff = diff,
                    code = code 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi kiểm kê kho", error = ex.Message });
            }
        }

        // POST: api/inventory/adjust
        [HttpPost("adjust")]
        public async Task<IActionResult> AdjustInventory([FromBody] AdjustRequest request)
        {
            if (request == null || request.ProductId <= 0 || request.QuantityChange == 0 || string.IsNullOrEmpty(request.Reason))
            {
                return BadRequest(new { message = "Dữ liệu điều chỉnh không hợp lệ. Lý do điều chỉnh là bắt buộc!" });
            }

            try
            {
                var product = await _context.SanPhams.FirstOrDefaultAsync(p => p.MaSanPham == request.ProductId);
                if (product == null)
                {
                    return NotFound(new { message = "Không tìm thấy sản phẩm!" });
                }

                if (product.SoLuongTon + request.QuantityChange < 0)
                {
                    return BadRequest(new { message = $"Số lượng điều chỉnh giảm ({request.QuantityChange}) làm tồn kho âm (Hiện tại: {product.SoLuongTon})!" });
                }

                int beforeQty = product.SoLuongTon;
                product.SoLuongTon += request.QuantityChange;
                int afterQty = product.SoLuongTon;

                int count = await _context.InventoryTransactions.CountAsync(t => t.Type == "Điều chỉnh") + 1;
                string code = "DC" + count.ToString("D6");

                var tx = new InventoryTransaction
                {
                    Code = code,
                    Type = "Điều chỉnh",
                    ProductSKU = product.MaSanPham.ToString(),
                    ProductName = product.TenSanPham,
                    QuantityChange = request.QuantityChange,
                    Creator = HttpContext.Session.GetString("UserName") ?? "Thủ kho",
                    Date = DateTime.Now,
                    Note = string.IsNullOrEmpty(request.Note) 
                        ? $"Điều chỉnh tồn kho. Lý do: {request.Reason}"
                        : $"Lý do: {request.Reason}. Ghi chú: {request.Note}",
                    SoLuongTruoc = beforeQty,
                    SoLuongSau = afterQty,
                    TrangThai = "Đã duyệt" // Adjust is approved immediately
                };

                _context.InventoryTransactions.Add(tx);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Điều chỉnh tồn kho thành công!", 
                    before = beforeQty, 
                    after = afterQty, 
                    code = code 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi điều chỉnh kho", error = ex.Message });
            }
        }
    }

    // Request DTOs
    public class ImportRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string Source { get; set; } = "Nhà cung cấp";
        public string? Note { get; set; }
    }

    public class ExportRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string Source { get; set; } = "Xuất POS";
        public string? Note { get; set; }
    }

    public class AuditRequest
    {
        public int ProductId { get; set; }
        public int ActualQuantity { get; set; }
        public string? Note { get; set; }
    }

    public class AdjustRequest
    {
        public int ProductId { get; set; }
        public int QuantityChange { get; set; }
        public string Reason { get; set; } = "";
        public string? Note { get; set; }
    }
}
