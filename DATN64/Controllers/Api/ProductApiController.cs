using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;
using DATN64.Models;
using System.Linq;
using System.Threading.Tasks;

namespace DATN64.Controllers.Api
{
    public class ProductDto
    {
        public int MaSanPham { get; set; }
        public string TenSanPham { get; set; } = "";
        public int MaDanhMuc { get; set; }
        public int MaThuongHieu { get; set; }
        public int MaNCC { get; set; }
        public decimal GiaNhap { get; set; }
        public decimal GiaBan { get; set; }
        public int SoLuongTon { get; set; }
        public string? MoTa { get; set; }
        public string? HinhAnh { get; set; }
        public string? TrangThai { get; set; }
    }

    [Route("api/product")]
    [ApiController]
    public class ProductApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts(string? search, int? category, int? brand, decimal? minPrice, decimal? maxPrice, string? status, int? quantity)
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
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.GiaBan >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.GiaBan <= maxPrice.Value);
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.TrangThai == status);
            }
            if (quantity.HasValue)
            {
                query = query.Where(p => p.SoLuongTon <= quantity.Value);
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
                NhaCungCapTen = p.NhaCungCap != null ? p.NhaCungCap.TenNCC : ""
            }).ToListAsync();

            return Ok(products);
        }

        [HttpGet("options")]
        public async Task<IActionResult> GetOptions()
        {
            var categories = await _context.DanhMucs.Select(c => new { c.MaDanhMuc, c.TenDanhMuc }).ToListAsync();
            var brands = await _context.ThuongHieus.Select(b => new { b.MaThuongHieu, b.TenThuongHieu }).ToListAsync();
            var suppliers = await _context.NhaCungCaps.Select(s => new { s.MaNCC, TenNhaCungCap = s.TenNCC }).ToListAsync();

            return Ok(new { categories, brands, suppliers });
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductDto dto)
        {
            if (string.IsNullOrEmpty(dto.HinhAnh))
            {
                dto.HinhAnh = "https://images.unsplash.com/photo-1531297484001-80022131f5a1?q=80&w=300";
            }
            var p = new SanPham
            {
                TenSanPham = dto.TenSanPham,
                MaDanhMuc = dto.MaDanhMuc,
                MaThuongHieu = dto.MaThuongHieu,
                MaNCC = dto.MaNCC,
                GiaNhap = dto.GiaNhap,
                GiaBan = dto.GiaBan,
                SoLuongTon = dto.SoLuongTon,
                MoTa = dto.MoTa,
                TrangThai = dto.TrangThai ?? "Đang bán",
                HinhAnh = dto.HinhAnh
            };
            _context.SanPhams.Add(p);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Thêm sản phẩm thành công!", product = p });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductDto p)
        {
            var target = await _context.SanPhams.FirstOrDefaultAsync(prod => prod.MaSanPham == id);
            if (target == null) return NotFound(new { message = "Không tìm thấy sản phẩm" });

            target.TenSanPham = p.TenSanPham;
            target.MaDanhMuc = p.MaDanhMuc;
            target.MaThuongHieu = p.MaThuongHieu;
            target.MaNCC = p.MaNCC;
            target.GiaNhap = p.GiaNhap;
            target.GiaBan = p.GiaBan;
            target.SoLuongTon = p.SoLuongTon;
            target.MoTa = p.MoTa;
            target.TrangThai = p.TrangThai;
            if (!string.IsNullOrEmpty(p.HinhAnh))
            {
                target.HinhAnh = p.HinhAnh;
            }
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật sản phẩm thành công!", product = target });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DisableProduct(int id)
        {
            var target = await _context.SanPhams.FirstOrDefaultAsync(p => p.MaSanPham == id);
            if (target == null) return NotFound(new { message = "Không tìm thấy sản phẩm" });

            target.TrangThai = "Ngừng kinh doanh"; // Vô hiệu hóa
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã vô hiệu hóa sản phẩm!" });
        }

        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Không có file nào được tải lên.");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var fileUrl = $"/uploads/products/{uniqueFileName}";
            return Ok(new { url = fileUrl });
        }

        // ================= CATEGORIES =================
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories(string? search)
        {
            var query = _context.DanhMucs.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.TenDanhMuc.Contains(search));
            }
            return Ok(await query.ToListAsync());
        }

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] DanhMuc c)
        {
            _context.DanhMucs.Add(c);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Thêm danh mục thành công!", category = c });
        }

        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] DanhMuc c)
        {
            var target = await _context.DanhMucs.FindAsync(id);
            if (target == null) return NotFound();
            target.TenDanhMuc = c.TenDanhMuc;
            target.MoTa = c.MoTa;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thành công!", category = target });
        }

        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var target = await _context.DanhMucs.FindAsync(id);
            if (target == null) return NotFound();
            _context.DanhMucs.Remove(target);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa danh mục!" });
        }

        // ================= BRANDS =================
        [HttpGet("brands")]
        public async Task<IActionResult> GetBrands(string? search)
        {
            var query = _context.ThuongHieus.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => b.TenThuongHieu.Contains(search));
            }
            return Ok(await query.ToListAsync());
        }

        [HttpPost("brands")]
        public async Task<IActionResult> CreateBrand([FromBody] ThuongHieu b)
        {
            _context.ThuongHieus.Add(b);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Thêm thương hiệu thành công!", brand = b });
        }

        [HttpPut("brands/{id}")]
        public async Task<IActionResult> UpdateBrand(int id, [FromBody] ThuongHieu b)
        {
            var target = await _context.ThuongHieus.FindAsync(id);
            if (target == null) return NotFound();
            target.TenThuongHieu = b.TenThuongHieu;
            target.MoTa = b.MoTa;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thành công!", brand = target });
        }

        [HttpDelete("brands/{id}")]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            var target = await _context.ThuongHieus.FindAsync(id);
            if (target == null) return NotFound();
            _context.ThuongHieus.Remove(target);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa thương hiệu!" });
        }

        // ================= SUPPLIERS =================
        [HttpGet("suppliers")]
        public async Task<IActionResult> GetSuppliers(string? search, string? address)
        {
            var query = _context.NhaCungCaps.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => s.TenNCC.Contains(search) || s.SoDienThoai.Contains(search));
            }
            if (!string.IsNullOrEmpty(address))
            {
                query = query.Where(s => s.DiaChi != null && s.DiaChi.Contains(address));
            }
            return Ok(await query.ToListAsync());
        }

        [HttpPost("suppliers")]
        public async Task<IActionResult> CreateSupplier([FromBody] NhaCungCap s)
        {
            _context.NhaCungCaps.Add(s);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Thêm nhà cung cấp thành công!", supplier = s });
        }

        [HttpPut("suppliers/{id}")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] NhaCungCap s)
        {
            var target = await _context.NhaCungCaps.FindAsync(id);
            if (target == null) return NotFound();
            target.TenNCC = s.TenNCC;
            target.SoDienThoai = s.SoDienThoai;
            target.Email = s.Email;
            target.DiaChi = s.DiaChi;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thành công!", supplier = target });
        }

        [HttpDelete("suppliers/{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var target = await _context.NhaCungCaps.FindAsync(id);
            if (target == null) return NotFound();
            _context.NhaCungCaps.Remove(target);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa nhà cung cấp!" });
        }
    }
}
