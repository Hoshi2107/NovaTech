using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using DATN64.Helpers;
using System.Linq;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Identity;

namespace DATN64.Controllers
{
    [HasPermission("View_Employee")]
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;

        public EmployeeController(AppDbContext context)
        {
            _context = context;
        }

        // ═══════════════════════════════════════════════════════════════
        // GET: /Employee/Index — Trang chính với 4 tabs
        // ═══════════════════════════════════════════════════════════════
        public IActionResult Index(
            string activeTab = "nhanvien",
            int? filterMaNV = null,
            int? filterThang = null,
            int? filterNam = null,
            string? tuNgay = null,
            string? denNgay = null)
        {
            var now = DateTime.Now;
            int thang = filterThang ?? now.Month;
            int nam = filterNam ?? now.Year;

            // ── TAB 1: NHÂN VIÊN ─────────────────────────────────────
            var employees = _context.NhanViens.ToList();
            var roles = _context.Roles.ToList();
            var rolePermissions = _context.RolePermissions.ToList();

            var employeeRoles = (from nr in _context.NhanVienRoles
                                 join r in _context.Roles on nr.RoleId equals r.Id
                                 select new { nr.MaNhanVien, nr.RoleId, r.Name }).ToList();

            var empRolesDict = new Dictionary<int, List<Role>>();
            foreach (var mapping in employeeRoles)
            {
                var roleObj = roles.FirstOrDefault(r => r.Id == mapping.RoleId);
                if (roleObj != null)
                {
                    if (!empRolesDict.ContainsKey(mapping.MaNhanVien))
                        empRolesDict[mapping.MaNhanVien] = new List<Role>();
                    empRolesDict[mapping.MaNhanVien].Add(roleObj);
                }
            }

            // ── TAB 2: CHẤM CÔNG ─────────────────────────────────────
            var allChamCong = _context.ChamCongs.ToList();

            // Bộ lọc chấm công
            var chamCongQuery = allChamCong.AsQueryable();

            if (filterMaNV.HasValue && filterMaNV > 0)
                chamCongQuery = chamCongQuery.Where(c => c.MaNhanVien == filterMaNV.Value);

            DateTime? tuNgayDT = null, denNgayDT = null;
            if (!string.IsNullOrEmpty(tuNgay) && DateTime.TryParse(tuNgay, out var td))
                tuNgayDT = td;
            if (!string.IsNullOrEmpty(denNgay) && DateTime.TryParse(denNgay, out var dd))
                denNgayDT = dd;

            if (tuNgayDT.HasValue)
                chamCongQuery = chamCongQuery.Where(c => c.NgayCham >= tuNgayDT.Value);
            if (denNgayDT.HasValue)
                chamCongQuery = chamCongQuery.Where(c => c.NgayCham <= denNgayDT.Value);

            if (!tuNgayDT.HasValue && !denNgayDT.HasValue)
            {
                chamCongQuery = chamCongQuery.Where(c => c.NgayCham.Month == thang && c.NgayCham.Year == nam);
            }

            var chamCongList = chamCongQuery.OrderByDescending(c => c.NgayCham).ThenByDescending(c => c.GioVao).ToList();

            // Thống kê nhân viên được chọn
            NhanVienChamCongStats? selectedEmpStats = null;
            if (filterMaNV.HasValue && filterMaNV > 0)
            {
                var selectedEmp = employees.FirstOrDefault(e => e.MaNhanVien == filterMaNV.Value);
                if (selectedEmp != null)
                {
                    var thangChamCong = allChamCong.Where(c => c.MaNhanVien == filterMaNV.Value
                        && c.NgayCham.Month == thang && c.NgayCham.Year == nam).ToList();

                    int soNgayLamViec = DateTime.DaysInMonth(nam, thang);
                    int soNgayNghi = soNgayLamViec - thangChamCong.Count(c => c.TrangThai == "Hoàn thành");
                    int diMuon = thangChamCong.Count(c => c.GioVao.HasValue && c.GioVao.Value.Hour > 8
                                                          || (c.GioVao.HasValue && c.GioVao.Value.Hour == 8 && c.GioVao.Value.Minute > 30));
                    int chuaCheckOut = thangChamCong.Count(c => !c.GioRa.HasValue && c.TrangThai == "Đang làm");

                    selectedEmpStats = new NhanVienChamCongStats
                    {
                        NhanVien = selectedEmp,
                        TongGioThang = Math.Round(thangChamCong.Sum(c => c.TongGioLam ?? 0), 2),
                        SoNgayLam = thangChamCong.Count(c => c.TrangThai == "Hoàn thành"),
                        SoNgayNghi = soNgayNghi > 0 ? soNgayNghi : 0,
                        SoLanDiMuon = diMuon,
                        SoLanChuaCheckOut = chuaCheckOut
                    };
                }
            }

            // ── TAB 3: LƯƠNG ─────────────────────────────────────────
            var luongThangQuery = allChamCong
                .Where(c => c.NgayCham.Month == (filterThang ?? now.Month)
                         && c.NgayCham.Year == (filterNam ?? now.Year)
                         && c.TrangThai == "Hoàn thành");

            if (filterMaNV.HasValue && filterMaNV > 0)
                luongThangQuery = luongThangQuery.Where(c => c.MaNhanVien == filterMaNV.Value);

            var luongData = luongThangQuery
                .GroupBy(c => c.MaNhanVien)
                .Select(g => new
                {
                    MaNhanVien = g.Key,
                    TongGio = Math.Round(g.Sum(c => c.TongGioLam ?? 0), 2)
                })
                .ToList();

            var luongList = new List<LuongNhanVienViewModel>();
            foreach (var item in luongData)
            {
                var emp = employees.FirstOrDefault(e => e.MaNhanVien == item.MaNhanVien);
                if (emp != null)
                {
                    var luongTheoGio = emp.LuongTheoGio ?? 0;
                    luongList.Add(new LuongNhanVienViewModel
                    {
                        NhanVien = emp,
                        TongGio = item.TongGio,
                        LuongTheoGio = luongTheoGio,
                        LuongThang = Math.Round((double)luongTheoGio * item.TongGio, 0)
                    });
                }
            }
            luongList = luongList.OrderByDescending(l => l.LuongThang).ToList();

            // Thống kê lương
            ViewBag.TongChiPhiLuong = luongList.Sum(l => l.LuongThang);
            ViewBag.NvLamNhieuGio = luongList.OrderByDescending(l => l.TongGio).FirstOrDefault()?.NhanVien?.HoTen ?? "—";
            ViewBag.NvLuongCao = luongList.OrderByDescending(l => l.LuongThang).FirstOrDefault()?.NhanVien?.HoTen ?? "—";

            // ── RBAC: danh sách permissions cho từng role ─────────────
            var allPermissions = new List<string>
            {
                "View_Product","Create_Product","Edit_Product","Delete_Product",
                "View_Order","Create_Order","Edit_Order","Delete_Order",
                "View_Customer","Create_Customer","Edit_Customer","Delete_Customer",
                "View_Inventory","Create_Inventory","Edit_Inventory",
                "View_Employee","Create_Employee","Assign_Role",
                "View_Report","View_TikTok","View_Promotion","View_Setting"
            };

            // ── PASS TO VIEW ──────────────────────────────────────────
            ViewBag.RolesList = roles;
            ViewBag.RolePermissions = rolePermissions;
            ViewBag.EmployeeRolesDict = empRolesDict;
            ViewBag.ChamCongList = chamCongList;
            ViewBag.SelectedEmpStats = selectedEmpStats;
            ViewBag.LuongList = luongList;
            ViewBag.AllPermissions = allPermissions;
            ViewBag.ActiveTab = activeTab;
            ViewBag.FilterMaNV = filterMaNV;
            ViewBag.FilterThang = thang;
            ViewBag.FilterNam = nam;
            ViewBag.TuNgay = tuNgay;
            ViewBag.DenNgay = denNgay;

            return View(employees);
        }

        // ═══════════════════════════════════════════════════════════════
        // POST: Thêm nhân viên mới
        // ═══════════════════════════════════════════════════════════════
        [HttpPost]
        [HasPermission("Create_Employee")]
        public IActionResult Create(NhanVien emp, List<int> selectedRoleIds)
        {
            if (string.IsNullOrEmpty(emp.HoTen) || string.IsNullOrEmpty(emp.MatKhau))
            {
                TempData["ToastMessage"] = "Họ tên và Mật khẩu không được để trống!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            if (!string.IsNullOrEmpty(emp.Email) && _context.NhanViens.Any(e => e.Email == emp.Email))
            {
                TempData["ToastMessage"] = "Email đã được sử dụng bởi nhân viên khác!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            var hasher = new PasswordHasher<NhanVien>();
            emp.MatKhau = hasher.HashPassword(emp, emp.MatKhau);
            emp.TrangThai = emp.TrangThai ?? "Hoạt động";

            _context.NhanViens.Add(emp);
            _context.SaveChanges();

            var rolesInDb = _context.Roles.Where(r => selectedRoleIds.Contains(r.Id)).ToList();
            foreach (var role in rolesInDb)
            {
                _context.NhanVienRoles.Add(new NhanVienRole
                {
                    MaNhanVien = emp.MaNhanVien,
                    RoleId = role.Id
                });
            }

            emp.VaiTro = rolesInDb.Any() ? string.Join(", ", rolesInDb.Select(r => r.Name)) : "Nhân viên bán hàng";
            _context.SaveChanges();

            TempData["ToastMessage"] = "Thêm nhân viên mới thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }

        // ═══════════════════════════════════════════════════════════════
        // POST: Sửa nhân viên
        // ═══════════════════════════════════════════════════════════════
        [HttpPost]
        [HasPermission("Assign_Role")]
        public IActionResult Edit(int maNhanVien, string hoTen, string email, string soDienThoai, string? matKhau, string trangThai, List<int> selectedRoleIds, decimal? luongTheoGio)
        {
            var emp = _context.NhanViens.FirstOrDefault(e => e.MaNhanVien == maNhanVien);
            if (emp == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy nhân viên!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(hoTen))
            {
                TempData["ToastMessage"] = "Họ tên không được để trống!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            if (!string.IsNullOrEmpty(email) && _context.NhanViens.Any(e => e.Email == email && e.MaNhanVien != maNhanVien))
            {
                TempData["ToastMessage"] = "Email đã được sử dụng bởi nhân viên khác!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            emp.HoTen = hoTen;
            emp.Email = email;
            emp.SoDienThoai = soDienThoai;
            emp.TrangThai = trangThai ?? "Hoạt động";
            emp.LuongTheoGio = luongTheoGio;

            if (!string.IsNullOrEmpty(matKhau))
            {
                var hasher = new PasswordHasher<NhanVien>();
                emp.MatKhau = hasher.HashPassword(emp, matKhau);
            }

            var oldRoles = _context.NhanVienRoles.Where(nr => nr.MaNhanVien == maNhanVien);
            _context.NhanVienRoles.RemoveRange(oldRoles);

            var rolesInDb = _context.Roles.Where(r => selectedRoleIds.Contains(r.Id)).ToList();
            foreach (var role in rolesInDb)
            {
                _context.NhanVienRoles.Add(new NhanVienRole
                {
                    MaNhanVien = maNhanVien,
                    RoleId = role.Id
                });
            }

            emp.VaiTro = rolesInDb.Any() ? string.Join(", ", rolesInDb.Select(r => r.Name)) : "Nhân viên bán hàng";
            _context.SaveChanges();

            TempData["ToastMessage"] = "Cập nhật nhân viên thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }

        // ═══════════════════════════════════════════════════════════════
        // POST: Khóa/Mở khóa tài khoản
        // ═══════════════════════════════════════════════════════════════
        [HttpPost]
        [HasPermission("Assign_Role")]
        public IActionResult ToggleStatus(int id)
        {
            var target = _context.NhanViens.FirstOrDefault(e => e.MaNhanVien == id);
            if (target == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy nhân viên!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            if (target.TrangThai == "Hoạt động")
            {
                var adminRoleId = _context.Roles.FirstOrDefault(r => r.Name == "Admin")?.Id;
                if (adminRoleId != null)
                {
                    bool isAdmin = _context.NhanVienRoles.Any(nr => nr.MaNhanVien == id && nr.RoleId == adminRoleId);
                    if (isAdmin)
                    {
                        int activeAdmins = (from n in _context.NhanViens
                                            join nr in _context.NhanVienRoles on n.MaNhanVien equals nr.MaNhanVien
                                            where nr.RoleId == adminRoleId && n.TrangThai == "Hoạt động"
                                            select n).Count();

                        if (activeAdmins <= 1)
                        {
                            TempData["ToastMessage"] = "Không thể vô hiệu hóa tài khoản Admin duy nhất!";
                            TempData["ToastType"] = "danger";
                            return RedirectToAction("Index");
                        }
                    }
                }

                target.TrangThai = "Bị khóa";
                TempData["ToastMessage"] = $"Đã vô hiệu hóa tài khoản \"{target.HoTen}\"!";
                TempData["ToastType"] = "warning";
            }
            else
            {
                target.TrangThai = "Hoạt động";
                TempData["ToastMessage"] = $"Đã kích hoạt lại tài khoản \"{target.HoTen}\"!";
                TempData["ToastType"] = "success";
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        // ═══════════════════════════════════════════════════════════════
        // POST: Lưu phân quyền (RBAC tab)
        // ═══════════════════════════════════════════════════════════════
        [HttpPost]
        [HasPermission("Assign_Role")]
        public IActionResult SaveRolePermissions(int roleId, List<string> selectedPermissions)
        {
            var role = _context.Roles.FirstOrDefault(r => r.Id == roleId);
            if (role == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy vai trò!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index", new { activeTab = "rbac" });
            }

            // Xoá quyền cũ của role này
            var oldPerms = _context.RolePermissions.Where(rp => rp.RoleName == role.Name).ToList();
            _context.RolePermissions.RemoveRange(oldPerms);

            // Thêm quyền mới
            if (selectedPermissions != null)
            {
                foreach (var perm in selectedPermissions)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleName = role.Name,
                        PermissionName = perm
                    });
                }
            }

            _context.SaveChanges();

            TempData["ToastMessage"] = $"Đã cập nhật quyền cho vai trò \"{role.Name}\"!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index", new { activeTab = "rbac" });
        }

        // ═══════════════════════════════════════════════════════════════
        // POST: Thêm vai trò mới
        // ═══════════════════════════════════════════════════════════════
        [HttpPost]
        [HasPermission("Assign_Role")]
        public IActionResult CreateRole(string roleName, string? roleDescription)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                TempData["ToastMessage"] = "Tên vai trò không được để trống!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index", new { activeTab = "rbac" });
            }

            if (_context.Roles.Any(r => r.Name == roleName))
            {
                TempData["ToastMessage"] = "Vai trò này đã tồn tại!";
                TempData["ToastType"] = "warning";
                return RedirectToAction("Index", new { activeTab = "rbac" });
            }

            _context.Roles.Add(new Role { Name = roleName, Description = roleDescription });
            _context.SaveChanges();

            TempData["ToastMessage"] = $"Đã thêm vai trò \"{roleName}\" thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index", new { activeTab = "rbac" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // ViewModels dùng nội bộ
    // ═══════════════════════════════════════════════════════════════════
    public class NhanVienChamCongStats
    {
        public NhanVien NhanVien { get; set; } = null!;
        public double TongGioThang { get; set; }
        public int SoNgayLam { get; set; }
        public int SoNgayNghi { get; set; }
        public int SoLanDiMuon { get; set; }
        public int SoLanChuaCheckOut { get; set; }
    }

    public class LuongNhanVienViewModel
    {
        public NhanVien NhanVien { get; set; } = null!;
        public double TongGio { get; set; }
        public decimal LuongTheoGio { get; set; }
        public double LuongThang { get; set; }
    }
}
