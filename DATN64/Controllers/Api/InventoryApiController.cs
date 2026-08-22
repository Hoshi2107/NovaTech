using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using DATN64.Models;
using DATN64.Helpers;

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
                var transactions = await _context.InventoryTransactions
                    .Where(t => t.PhanLoai != "YeuCauNhap")
                    .ToListAsync();
                var productIds = transactions.Select(t => {
                    int.TryParse(t.ProductSKU, out var id);
                    return id;
                }).Where(id => id > 0).Distinct().ToList();

                var productsDict = await _context.SanPhams
                    .Where(p => productIds.Contains(p.MaSanPham))
                    .ToDictionaryAsync(p => p.MaSanPham, p => new { p.GiaNhap, p.GiaBan });

                var result = transactions.OrderByDescending(t => t.Date).Select(t => {
                    int.TryParse(t.ProductSKU, out var pId);
                    decimal giaNhap = 0;
                    decimal giaBan = 0;
                    if (pId > 0 && productsDict.TryGetValue(pId, out var prodInfo))
                    {
                        giaNhap = prodInfo.GiaNhap;
                        giaBan = prodInfo.GiaBan;
                    }

                    // Nếu có giá nhập trong Note thì ưu tiên lấy
                    if (t.Note != null && t.Note.Contains("Đơn giá nhập:"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(t.Note, @"Đơn giá nhập:\s*([\d\.,]+)");
                        if (match.Success)
                        {
                            var numStr = match.Groups[1].Value.Replace(".", "").Replace(",", "");
                            if (decimal.TryParse(numStr, out var parsedCost) && parsedCost > 0)
                            {
                                giaNhap = parsedCost;
                            }
                        }
                    }

                    return new {
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
                        t.TrangThai,
                        t.NguoiDuyet,
                        NgayDuyet = t.NgayDuyet.HasValue ? t.NgayDuyet.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
                        t.LyDoTuChoi,
                        GiaNhap = giaNhap,
                        GiaBan = giaBan,
                        ThanhTien = giaNhap * Math.Abs(t.QuantityChange),
                        t.MaYeuCauNhap,
                        t.PhanLoai
                    };
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

                decimal importCost = request.ImportPrice.HasValue && request.ImportPrice.Value > 0 
                    ? request.ImportPrice.Value 
                    : product.GiaNhap;
                decimal thanhTien = importCost * request.Quantity;

                var noteBuilder = new System.Text.StringBuilder();
                if (!string.IsNullOrEmpty(request.Note))
                {
                    noteBuilder.Append(request.Note);
                    noteBuilder.Append(" - ");
                }
                noteBuilder.Append($"Nguồn: {request.Source} | Đơn giá nhập: {importCost:N0} đ | Thành tiền: {thanhTien:N0} đ");

                var currentUser = HttpContext.Session.GetString("UserName") ?? "Thủ kho";

                // Tạo phiếu ở trạng thái "Chờ duyệt", KHÔNG tăng tồn kho và KHÔNG ghi vào báo cáo giá cho đến khi được duyệt
                var tx = new InventoryTransaction
                {
                    Code = code,
                    Type = "Nhập kho",
                    ProductSKU = product.MaSanPham.ToString(),
                    ProductName = product.TenSanPham,
                    QuantityChange = request.Quantity,
                    Creator = currentUser,
                    Date = DateTime.Now,
                    Note = noteBuilder.ToString(),
                    TrangThai = "Chờ duyệt",
                    SoLuongTruoc = null,
                    SoLuongSau = null
                };
                _context.InventoryTransactions.Add(tx);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = $"Đã tạo phiếu nhập kho ({code}) cho {request.Quantity} sản phẩm với đơn giá {importCost:N0} đ! Phiếu đang ở trạng thái 'Chờ duyệt', sau khi Quản lý phê duyệt thì hàng mới vào kho và hiển thị lên biến động giá.", 
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

                    // Trích xuất giá nhập từ Note nếu có để cập nhật lịch sử giá và sản phẩm
                    decimal importCost = product.GiaNhap;
                    if (tx.Note != null && tx.Note.Contains("Đơn giá nhập:"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(tx.Note, @"Đơn giá nhập:\s*([\d\.,]+)");
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

                    // Tự động ghi vào PhieuNhap & ChiTietPhieuNhap để đồng bộ Báo Cáo Biến Động Giá
                    var phieuNhap = new PhieuNhap
                    {
                        MaNCC = product.MaNCC > 0 ? product.MaNCC : 1,
                        MaNhanVien = 1,
                        NgayNhap = DateTime.Now
                    };
                    _context.PhieuNhaps.Add(phieuNhap);
                    await _context.SaveChangesAsync();

                    var ctPhieuNhap = new ChiTietPhieuNhap
                    {
                        MaPhieuNhap = phieuNhap.MaPhieuNhap,
                        MaSanPham = product.MaSanPham,
                        SoLuong = tx.QuantityChange,
                        GiaNhap = importCost
                    };
                    _context.ChiTietPhieuNhaps.Add(ctPhieuNhap);
                }
                else if (tx.Type == "Xuất kho")
                {
                    if (product.SoLuongTon + tx.QuantityChange < 0)
                    {
                        return BadRequest(new { message = $"Số lượng tồn kho khả dụng ({product.SoLuongTon}) không đủ để thực hiện xuất {Math.Abs(tx.QuantityChange)}!" });
                    }
                    product.SoLuongTon += tx.QuantityChange;
                }
                else if (tx.Type == "Kiểm kê")
                {
                    if (product.SoLuongTon + tx.QuantityChange < 0)
                    {
                        return BadRequest(new { message = $"Số lượng tồn kho sau kiểm kê không thể nhỏ hơn 0! Tồn kho hiện tại: {product.SoLuongTon}, Lệch điều chỉnh: {tx.QuantityChange}" });
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
                tx.NguoiDuyet = HttpContext.Session.GetString("UserName") ?? "Quản lý";
                tx.NgayDuyet = DateTime.Now;

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
                tx.NguoiDuyet = HttpContext.Session.GetString("UserName") ?? "Quản lý";
                tx.NgayDuyet = DateTime.Now;

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
                        ? $"Kiểm kê kho. Thực tế đề xuất: {request.ActualQuantity}, Lệch: {(diff >= 0 ? "+" : "")}{diff}"
                        : $"{request.Note} (Thực tế đề xuất: {request.ActualQuantity}, Lệch: {(diff >= 0 ? "+" : "")}{diff})",
                    SoLuongTruoc = null,
                    SoLuongSau = null,
                    TrangThai = "Chờ duyệt"
                };

                _context.InventoryTransactions.Add(tx);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Đã tạo phiếu kiểm kê và chờ quản lý phê duyệt.", 
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
                    SoLuongTruoc = null,
                    SoLuongSau = null,
                    TrangThai = "Chờ duyệt"
                };

                _context.InventoryTransactions.Add(tx);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Đã tạo phiếu điều chỉnh và chờ quản lý phê duyệt.", 
                    code = code 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi tạo phiếu điều chỉnh", error = ex.Message });
            }
        }

        // GET: api/inventory/adjust/pending
        [HttpGet("adjust/pending")]
        public async Task<IActionResult> GetPendingAdjustments()
        {
            try
            {
                var pending = await _context.InventoryTransactions
                    .Where(t => t.Type == "Điều chỉnh" && t.TrangThai == "Chờ duyệt")
                    .OrderByDescending(t => t.Date)
                    .Select(t => new {
                        t.Id,
                        t.Code,
                        t.Type,
                        t.ProductSKU,
                        t.ProductName,
                        t.QuantityChange,
                        t.Creator,
                        Date = t.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                        t.Note,
                        t.TrangThai
                    })
                    .ToListAsync();

                return Ok(pending);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải danh sách phiếu điều chỉnh chờ duyệt", error = ex.Message });
            }
        }

        // POST: api/inventory/adjust/approve/{id}
        [HttpPost("adjust/approve/{id}")]
        public async Task<IActionResult> ApproveAdjust(int id)
        {
            var rolesString = HttpContext.Session.GetString("UserRoles") ?? "";
            var roles = rolesString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()).ToList();
            bool isManager = roles.Contains("Super Admin") || roles.Contains("Admin") || roles.Contains("Quản lý");
            if (!isManager)
            {
                return StatusCode(403, new { message = "Bạn không có quyền thực hiện thao tác này! Chỉ Quản lý/Admin mới có quyền duyệt đơn." });
            }

            try
            {
                var tx = await _context.InventoryTransactions.FirstOrDefaultAsync(t => t.Id == id);
                if (tx == null)
                {
                    return NotFound(new { message = "Không tìm thấy giao dịch!" });
                }

                if (tx.TrangThai != "Chờ duyệt" || tx.Type != "Điều chỉnh")
                {
                    return BadRequest(new { message = "Giao dịch này không hợp lệ hoặc đã được xử lý!" });
                }

                int productId = int.Parse(tx.ProductSKU);
                var product = await _context.SanPhams.FirstOrDefaultAsync(p => p.MaSanPham == productId);
                if (product == null)
                {
                    return NotFound(new { message = "Không tìm thấy sản phẩm tương ứng!" });
                }

                int beforeQty = product.SoLuongTon;
                int afterQty = beforeQty + tx.QuantityChange;

                if (afterQty < 0)
                {
                    return BadRequest(new { message = $"Số lượng điều chỉnh giảm ({tx.QuantityChange}) làm tồn kho âm (Hiện tại: {beforeQty})!" });
                }

                // Update stock
                product.SoLuongTon = afterQty;

                // Update transaction
                tx.TrangThai = "Đã duyệt";
                tx.SoLuongTruoc = beforeQty;
                tx.SoLuongSau = afterQty;
                tx.NguoiDuyet = HttpContext.Session.GetString("UserName") ?? "Quản lý";
                tx.NgayDuyet = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Đã phê duyệt phiếu và cập nhật số lượng tồn kho thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi duyệt phiếu điều chỉnh", error = ex.Message });
            }
        }

        // POST: api/inventory/adjust/reject/{id}
        [HttpPost("adjust/reject/{id}")]
        public async Task<IActionResult> RejectAdjust(int id, [FromBody] RejectAdjustRequest request)
        {
            var rolesString = HttpContext.Session.GetString("UserRoles") ?? "";
            var roles = rolesString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()).ToList();
            bool isManager = roles.Contains("Super Admin") || roles.Contains("Admin") || roles.Contains("Quản lý");
            if (!isManager)
            {
                return StatusCode(403, new { message = "Bạn không có quyền thực hiện thao tác này! Chỉ Quản lý/Admin mới có quyền từ chối đơn." });
            }

            if (request == null || string.IsNullOrEmpty(request.LyDoTuChoi))
            {
                return BadRequest(new { message = "Bắt buộc phải nhập lý do từ chối!" });
            }

            try
            {
                var tx = await _context.InventoryTransactions.FirstOrDefaultAsync(t => t.Id == id);
                if (tx == null)
                {
                    return NotFound(new { message = "Không tìm thấy giao dịch!" });
                }

                if (tx.TrangThai != "Chờ duyệt" || tx.Type != "Điều chỉnh")
                {
                    return BadRequest(new { message = "Giao dịch này không hợp lệ hoặc đã được xử lý!" });
                }

                tx.TrangThai = "Đã từ chối";
                tx.LyDoTuChoi = request.LyDoTuChoi;
                tx.NguoiDuyet = HttpContext.Session.GetString("UserName") ?? "Quản lý";
                tx.NgayDuyet = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Đã từ chối phiếu điều chỉnh tồn kho thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi từ chối phiếu điều chỉnh", error = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════
    // 2-STEP IMPORT WORKFLOW
    // ═══════════════════════════════════════════════════════════

    // GET: api/inventory/purchase-requests
    [HttpGet("purchase-requests")]
    public async Task<IActionResult> GetPurchaseRequests(string? status)
    {
        try
        {
            var query = _context.InventoryTransactions
                .Where(t => t.PhanLoai == "YeuCauNhap")
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(t => t.TrangThai == status);

            var list = await query.OrderByDescending(t => t.Date).Select(t => new
            {
                t.Id, t.Code, t.Type, t.ProductSKU, t.ProductName,
                t.QuantityChange, t.Creator,
                Date = t.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                t.Note, t.TrangThai, t.NguoiDuyet,
                NgayDuyet = t.NgayDuyet.HasValue ? t.NgayDuyet.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
                t.LyDoTuChoi, t.PhanLoai, t.MaYeuCauNhap
            }).ToListAsync();

            return Ok(list);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi tải danh sách yêu cầu nhập", error = ex.Message });
        }
    }

    // POST: api/inventory/purchase-request  — Thủ kho gửi yêu cầu nhập hàng
    [HttpPost("purchase-request")]
    public async Task<IActionResult> CreatePurchaseRequest([FromBody] PurchaseRequestDto request)
    {
        if (request == null || request.ProductId <= 0 || request.Quantity <= 0)
            return BadRequest(new { message = "Dữ liệu yêu cầu nhập không hợp lệ!" });

        try
        {
            var product = await _context.SanPhams.FirstOrDefaultAsync(p => p.MaSanPham == request.ProductId);
            if (product == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm!" });

            int count = await _context.InventoryTransactions.CountAsync(t => t.PhanLoai == "YeuCauNhap") + 1;
            string code = "YC" + count.ToString("D6");

            decimal estPrice = request.EstimatedPrice.HasValue && request.EstimatedPrice.Value > 0
                ? request.EstimatedPrice.Value
                : product.GiaNhap;

            var note = $"Nguồn: {request.NhaCungCap} | Giá ước tính: {estPrice:N0} đ | Tổng ước tính: {estPrice * request.Quantity:N0} đ";
            if (!string.IsNullOrEmpty(request.Note))
                note = request.Note + " — " + note;

            var tx = new InventoryTransaction
            {
                Code        = code,
                Type        = "Nhập kho",
                PhanLoai    = "YeuCauNhap",
                ProductSKU  = product.MaSanPham.ToString(),
                ProductName = product.TenSanPham,
                QuantityChange = request.Quantity,
                Creator     = HttpContext.Session.GetString("UserName") ?? "Thủ kho",
                Date        = DateTime.Now,
                Note        = note,
                TrangThai   = "Chờ duyệt YC",
            };

            _context.InventoryTransactions.Add(tx);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã gửi Yêu cầu nhập hàng ({code}) cho {request.Quantity} sản phẩm. Đang chờ kế toán phê duyệt.", code });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi hệ thống khi tạo yêu cầu nhập", error = ex.Message });
        }
    }

    // POST: api/inventory/purchase-request/approve/{id}  — Kế toán duyệt yêu cầu
    [HttpPost("purchase-request/approve/{id}")]
    public async Task<IActionResult> ApprovePurchaseRequest(int id)
    {
        if (!AuthHelper.HasPermission(HttpContext, "Import_Inventory"))
            return StatusCode(403, new { message = "Bạn không có quyền duyệt yêu cầu nhập hàng!" });

        try
        {
            var tx = await _context.InventoryTransactions.FirstOrDefaultAsync(t => t.Id == id && t.PhanLoai == "YeuCauNhap");
            if (tx == null) return NotFound(new { message = "Không tìm thấy yêu cầu nhập!" });
            if (tx.TrangThai != "Chờ duyệt YC")
                return BadRequest(new { message = "Yêu cầu này đã được xử lý!" });

            tx.TrangThai   = "Đã duyệt YC";
            tx.NguoiDuyet  = HttpContext.Session.GetString("UserName") ?? "Kế toán";
            tx.NgayDuyet   = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã duyệt yêu cầu {tx.Code}. Thủ kho có thể tạo Phiếu Nhập khi hàng về." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
        }
    }

    // POST: api/inventory/purchase-request/reject/{id}  — Kế toán từ chối yêu cầu
    [HttpPost("purchase-request/reject/{id}")]
    public async Task<IActionResult> RejectPurchaseRequest(int id, [FromBody] RejectRequestDto request)
    {
        if (!AuthHelper.HasPermission(HttpContext, "Import_Inventory"))
            return StatusCode(403, new { message = "Bạn không có quyền từ chối yêu cầu nhập hàng!" });

        if (request == null || string.IsNullOrEmpty(request.LyDoTuChoi))
            return BadRequest(new { message = "Bắt buộc phải nhập lý do từ chối!" });

        try
        {
            var tx = await _context.InventoryTransactions.FirstOrDefaultAsync(t => t.Id == id && t.PhanLoai == "YeuCauNhap");
            if (tx == null) return NotFound(new { message = "Không tìm thấy yêu cầu nhập!" });
            if (tx.TrangThai != "Chờ duyệt YC")
                return BadRequest(new { message = "Yêu cầu này đã được xử lý!" });

            tx.TrangThai   = "Từ chối YC";
            tx.NguoiDuyet  = HttpContext.Session.GetString("UserName") ?? "Kế toán";
            tx.NgayDuyet   = DateTime.Now;
            tx.LyDoTuChoi  = request.LyDoTuChoi;

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã từ chối yêu cầu {tx.Code}." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
        }
    }

    // POST: api/inventory/create-from-request/{id}  — Thủ kho tạo phiếu nhập thực tế từ YC đã duyệt
    [HttpPost("create-from-request/{id}")]
    public async Task<IActionResult> CreateImportFromRequest(int id, [FromBody] CreateImportFromRequestDto request)
    {
        if (request == null || request.Quantity <= 0 || request.ImportPrice <= 0)
            return BadRequest(new { message = "Dữ liệu phiếu nhập không hợp lệ!" });

        try
        {
            var yeuCau = await _context.InventoryTransactions.FirstOrDefaultAsync(t => t.Id == id && t.PhanLoai == "YeuCauNhap");
            if (yeuCau == null) return NotFound(new { message = "Không tìm thấy yêu cầu nhập!" });
            if (yeuCau.TrangThai != "Đã duyệt YC")
                return BadRequest(new { message = "Chỉ có thể tạo phiếu nhập từ yêu cầu đã được kế toán duyệt!" });

            // Kiểm tra đã tạo phiếu nhập từ YC này chưa
            // MaYeuCauNhap is stored as NVARCHAR(50) in DB, compare as string
            string idStr = id.ToString();
            bool alreadyCreated = await _context.InventoryTransactions
                .AnyAsync(t => t.MaYeuCauNhap == idStr && t.PhanLoai == "PhieuNhap");
            if (alreadyCreated)
                return BadRequest(new { message = "Phiếu nhập đã được tạo từ yêu cầu này rồi!" });

            int count = await _context.InventoryTransactions.CountAsync(t => t.PhanLoai == "PhieuNhap" && t.Type == "Nhập kho") + 1;
            string code = "PN" + count.ToString("D6");

            decimal thanhTien = request.ImportPrice * request.Quantity;
            var note = $"Từ yêu cầu {yeuCau.Code} | Đơn giá nhập: {request.ImportPrice:N0} đ | Thành tiền: {thanhTien:N0} đ";
            if (!string.IsNullOrEmpty(request.Note))
                note = request.Note + " — " + note;

            var phieuNhap = new InventoryTransaction
            {
                Code           = code,
                Type           = "Nhập kho",
                PhanLoai       = "PhieuNhap",
                MaYeuCauNhap   = id.ToString(),
                ProductSKU     = yeuCau.ProductSKU,
                ProductName    = yeuCau.ProductName,
                QuantityChange = request.Quantity,
                Creator        = HttpContext.Session.GetString("UserName") ?? "Thủ kho",
                Date           = DateTime.Now,
                Note           = note,
                TrangThai      = "Chờ duyệt PN",
            };

            _context.InventoryTransactions.Add(phieuNhap);

            // Cập nhật trạng thái YC → "Chờ xác nhận PN" để UI biết đang chờ xác nhận hàng về
            yeuCau.TrangThai = "Chờ xác nhận PN";

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã tạo Phiếu Nhập ({code}) chờ kế toán xác nhận hàng về. Sau khi xác nhận hàng sẽ vào kho.", code });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi hệ thống khi tạo phiếu nhập", error = ex.Message });
        }
    }

    // POST: api/inventory/phieu-nhap/approve/{id}  — Kế toán xác nhận hàng đã về, hàng vào kho
    [HttpPost("phieu-nhap/approve/{id}")]
    public async Task<IActionResult> ApprovePhieuNhap(int id)
    {
        if (!AuthHelper.HasPermission(HttpContext, "Import_Inventory"))
            return StatusCode(403, new { message = "Bạn không có quyền xác nhận phiếu nhập hàng!" });

        try
        {
            var phieuNhap = await _context.InventoryTransactions
                .FirstOrDefaultAsync(t => t.Id == id && t.PhanLoai == "PhieuNhap" && t.TrangThai == "Chờ duyệt PN");
            if (phieuNhap == null)
                return NotFound(new { message = "Không tìm thấy phiếu nhập hoặc phiếu đã được xử lý!" });

            // Lấy sản phẩm
            if (!int.TryParse(phieuNhap.ProductSKU, out int productId))
                return BadRequest(new { message = "Mã sản phẩm trong phiếu nhập không hợp lệ!" });

            var product = await _context.SanPhams.FirstOrDefaultAsync(p => p.MaSanPham == productId);
            if (product == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm tương ứng!" });

            int beforeQty = product.SoLuongTon;

            // Cập nhật tồn kho
            product.SoLuongTon += phieuNhap.QuantityChange;
            int afterQty = product.SoLuongTon;

            // Trích xuất đơn giá nhập từ Note để cập nhật giá sản phẩm
            decimal importCost = product.GiaNhap;
            if (phieuNhap.Note != null && phieuNhap.Note.Contains("Đơn giá nhập:"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(phieuNhap.Note, @"Đơn giá nhập:\s*([\d\.,]+)");
                if (match.Success)
                {
                    var numStr = match.Groups[1].Value.Replace(".", "").Replace(",", "");
                    if (decimal.TryParse(numStr, out var parsedCost) && parsedCost > 0)
                        importCost = parsedCost;
                }
            }

            if (importCost > 0)
                product.GiaNhap = importCost;

            // Cập nhật phiếu nhập
            phieuNhap.TrangThai    = "Đã duyệt";
            phieuNhap.SoLuongTruoc = beforeQty;
            phieuNhap.SoLuongSau   = afterQty;
            phieuNhap.NguoiDuyet   = HttpContext.Session.GetString("UserName") ?? "Kế toán";
            phieuNhap.NgayDuyet    = DateTime.Now;

            // Cập nhật YeuCauNhap liên kết → "Hoàn thành" (để ẩn khỏi danh sách)
            if (!string.IsNullOrEmpty(phieuNhap.MaYeuCauNhap) && int.TryParse(phieuNhap.MaYeuCauNhap, out int yeuCauId))
            {
                var yeuCau = await _context.InventoryTransactions
                    .FirstOrDefaultAsync(t => t.Id == yeuCauId && t.PhanLoai == "YeuCauNhap");
                if (yeuCau != null)
                    yeuCau.TrangThai = "Hoàn thành";
            }

            // Tự động sync vào PhieuNhap & ChiTietPhieuNhap cho Báo Cáo Biến Động Giá
            var phieuNhapRecord = new PhieuNhap
            {
                MaNCC       = product.MaNCC > 0 ? product.MaNCC : 1,
                MaNhanVien  = 1,
                NgayNhap    = DateTime.Now
            };
            _context.PhieuNhaps.Add(phieuNhapRecord);
            await _context.SaveChangesAsync();

            var ctPhieuNhap = new ChiTietPhieuNhap
            {
                MaPhieuNhap = phieuNhapRecord.MaPhieuNhap,
                MaSanPham   = product.MaSanPham,
                SoLuong     = phieuNhap.QuantityChange,
                GiaNhap     = importCost
            };
            _context.ChiTietPhieuNhaps.Add(ctPhieuNhap);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã xác nhận hàng về! Tồn kho {product.TenSanPham}: {beforeQty} → {afterQty} cái." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi hệ thống khi xác nhận phiếu nhập", error = ex.Message });
        }
    }

    }

    public class ImportRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal? ImportPrice { get; set; }
        public bool UpdateProductPrice { get; set; } = true;
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

    public class RejectAdjustRequest
    {
        public string LyDoTuChoi { get; set; } = "";
    }

    public class PurchaseRequestDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal? EstimatedPrice { get; set; }
        public string NhaCungCap { get; set; } = "Nhà cung cấp";
        public string? Note { get; set; }
    }

    public class CreateImportFromRequestDto
    {
        public int Quantity { get; set; }           // Số lượng thực tế nhận được
        public decimal ImportPrice { get; set; }    // Giá nhập thực tế
        public string? Note { get; set; }
    }

    public class RejectRequestDto
    {
        public string LyDoTuChoi { get; set; } = "";
    }
}
