using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using DATN64.Models;
using System.Linq;

namespace DATN64.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var emp = _context.NhanViens.FirstOrDefault(e => e.Email != null && e.Email.Equals(email));

            if (emp != null)
            {
                if (emp.TrangThai != "Hoạt động")
                {
                    ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa hoặc ngừng hoạt động.");
                    return View();
                }

                var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<NhanVien>();
                bool isPasswordValid = false;
                bool shouldUpdateHash = false;

                try
                {
                    var verificationResult = hasher.VerifyHashedPassword(emp, emp.MatKhau, password);
                    if (verificationResult == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success)
                    {
                        isPasswordValid = true;
                    }
                    else if (verificationResult == Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        isPasswordValid = true;
                        shouldUpdateHash = true;
                    }
                    else
                    {
                        if (emp.MatKhau == password)
                        {
                            isPasswordValid = true;
                            shouldUpdateHash = true;
                        }
                    }
                }
                catch (System.FormatException)
                {
                    if (emp.MatKhau == password)
                    {
                        isPasswordValid = true;
                        shouldUpdateHash = true;
                    }
                }

                if (!isPasswordValid)
                {
                    ModelState.AddModelError("", "Thông tin đăng nhập không chính xác hoặc tài khoản không tồn tại.");
                    return View();
                }

                if (shouldUpdateHash && !string.IsNullOrEmpty(password))
                {
                    emp.MatKhau = hasher.HashPassword(emp, password);
                    _context.SaveChanges();
                }

                // Retrieve roles dynamically from NhanVienRole junction table
                var roles = (from nr in _context.NhanVienRoles
                             join r in _context.Roles on nr.RoleId equals r.Id
                             where nr.MaNhanVien == emp.MaNhanVien
                             select r.Name).ToList();

                var rolesStr = string.Join(",", roles);

                // Retrieve distinct permissions associated with these roles
                var permissions = _context.RolePermissions
                    .Where(rp => roles.Contains(rp.RoleName))
                    .Select(rp => rp.PermissionName)
                    .Distinct()
                    .ToList();

                var permissionsStr = string.Join(",", permissions);

                HttpContext.Session.SetString("UserEmail", emp.Email ?? "");
                HttpContext.Session.SetString("UserName", emp.HoTen);
                HttpContext.Session.SetString("UserRoles", rolesStr);
                HttpContext.Session.SetString("UserPermissions", permissionsStr);

                // Ensure CustomerId is set when the logged-in user has role "Khách hàng"
                // so Online cart can show checkout form.
                if (roles.Contains("Khách hàng"))
                {
                    var empCustomer = _context.KhachHangs.FirstOrDefault(k => k.Email != null && k.Email.ToLower() == (emp.Email ?? "").ToLower());
                    if (empCustomer != null)
                    {
                        HttpContext.Session.SetString("CustomerId", empCustomer.MaKhachHang.ToString());
                    }
                }



                TempData["ToastMessage"] = "Đăng nhập thành công!";

                TempData["ToastType"] = "success";

                if (roles.Contains("Khách hàng"))
                {
                    return RedirectToAction("Index", "Online");
                }
                else
                {
                    return RedirectToAction("Selection", "Portal");
                }
            }

            // Customer check (database lookup by email or phone)
            var customer = _context.KhachHangs.FirstOrDefault(c => (c.Email != null && c.Email.Equals(email)) || (c.SoDienThoai != null && c.SoDienThoai.Equals(email)));
            if (customer != null)
            {
                var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<KhachHang>();
                bool isPasswordValid = false;
                bool shouldUpdateHash = false;

                if (!string.IsNullOrEmpty(customer.MatKhau))
                {
                    try
                    {
                        // 1. Try checking if it's already hashed
                        var verificationResult = hasher.VerifyHashedPassword(customer, customer.MatKhau, password);
                        if (verificationResult == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success)
                        {
                            isPasswordValid = true;
                        }
                        else if (verificationResult == Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded)
                        {
                            isPasswordValid = true;
                            shouldUpdateHash = true;
                        }
                        else
                        {
                            // 2. Fallback: if verification failed, check if database password matches the plain text password
                            if (customer.MatKhau == password)
                            {
                                isPasswordValid = true;
                                shouldUpdateHash = true; // Mark to hash it
                            }
                        }
                    }
                    catch (System.FormatException)
                    {
                        // The stored password is not a valid Base-64 string, so check if it's plain text
                        if (customer.MatKhau == password)
                        {
                            isPasswordValid = true;
                            shouldUpdateHash = true; // Mark to hash it
                        }
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(password))
                    {
                        isPasswordValid = true;
                    }
                }

                if (!isPasswordValid)
                {
                    ModelState.AddModelError("", "Thông tin đăng nhập không chính xác hoặc tài khoản không tồn tại.");
                    return View();
                }

                // If password matches plain text, update to hashed version automatically
                if (shouldUpdateHash && !string.IsNullOrEmpty(password))
                {
                    customer.MatKhau = hasher.HashPassword(customer, password);
                    _context.SaveChanges();
                }

                // Merge guest/other orders matching this customer's email or phone
                var otherCustomers = _context.KhachHangs
                    .Where(k => k.MaKhachHang != customer.MaKhachHang && 
                               ((!string.IsNullOrEmpty(k.Email) && !string.IsNullOrEmpty(customer.Email) && k.Email.ToLower() == customer.Email.ToLower()) || 
                                (!string.IsNullOrEmpty(k.SoDienThoai) && !string.IsNullOrEmpty(customer.SoDienThoai) && k.SoDienThoai == customer.SoDienThoai)))
                    .ToList();

                if (otherCustomers.Any())
                {
                    var otherIds = otherCustomers.Select(o => o.MaKhachHang).ToList();
                    var ordersToMerge = _context.DonHangs
                        .Where(o => o.MaKhachHang.HasValue && otherIds.Contains(o.MaKhachHang.Value))
                        .ToList();

                    foreach (var o in ordersToMerge)
                    {
                        o.MaKhachHang = customer.MaKhachHang;
                    }
                    _context.SaveChanges();
                }

                HttpContext.Session.SetString("UserEmail", customer.Email ?? "");
                HttpContext.Session.SetString("UserName", customer.HoTen);
                HttpContext.Session.SetString("UserRoles", "Khách hàng");
                HttpContext.Session.SetString("UserPermissions", "");
                HttpContext.Session.SetString("CustomerId", customer.MaKhachHang.ToString());

                TempData["ToastMessage"] = "Chào mừng bạn đến với NovaTech Store!";
                TempData["ToastType"] = "success";
                return RedirectToAction("Index", "Online");
            }

            // Customer check (simulated fallback)
            if (email.Equals("customer@gmail.com", System.StringComparison.OrdinalIgnoreCase))
            {
                HttpContext.Session.SetString("UserEmail", "customer@gmail.com");
                HttpContext.Session.SetString("UserName", "Khách Hàng Demo");
                HttpContext.Session.SetString("UserRoles", "Khách hàng");
                HttpContext.Session.SetString("UserPermissions", "");

                var demoCustomer = _context.KhachHangs.FirstOrDefault(k => k.Email != null && k.Email.ToLower() == "customer@gmail.com");
                if (demoCustomer == null)
                {
                    demoCustomer = new KhachHang
                    {
                        HoTen = "Khách Hàng Demo",
                        Email = "customer@gmail.com",
                        SoDienThoai = "0900000000",
                        DiaChi = "123 Đường Demo, Quận 1, TP.HCM",
                        DiemTichLuy = 0
                    };
                    _context.KhachHangs.Add(demoCustomer);
                    _context.SaveChanges();
                }

                // Merge guest/other orders matching this customer's email or phone
                var otherCustomers = _context.KhachHangs
                    .Where(k => k.MaKhachHang != demoCustomer.MaKhachHang && 
                               ((!string.IsNullOrEmpty(k.Email) && !string.IsNullOrEmpty(demoCustomer.Email) && k.Email.ToLower() == demoCustomer.Email.ToLower()) || 
                                (!string.IsNullOrEmpty(k.SoDienThoai) && !string.IsNullOrEmpty(demoCustomer.SoDienThoai) && k.SoDienThoai == demoCustomer.SoDienThoai)))
                    .ToList();

                if (otherCustomers.Any())
                {
                    var otherIds = otherCustomers.Select(o => o.MaKhachHang).ToList();
                    var ordersToMerge = _context.DonHangs
                        .Where(o => o.MaKhachHang.HasValue && otherIds.Contains(o.MaKhachHang.Value))
                        .ToList();

                    foreach (var o in ordersToMerge)
                    {
                        o.MaKhachHang = demoCustomer.MaKhachHang;
                    }
                    _context.SaveChanges();
                }

                HttpContext.Session.SetString("CustomerId", demoCustomer.MaKhachHang.ToString());

                TempData["ToastMessage"] = "Chào mừng bạn đến với NovaTech Store!";
                return RedirectToAction("Index", "Online");
            }

            ModelState.AddModelError("", "Thông tin đăng nhập không chính xác hoặc tài khoản không tồn tại.");
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Online");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            ViewBag.SuccessMessage = "Một email khôi phục mật khẩu đã được gửi đến " + email;
            return View();
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ChangePassword(string oldPassword, string newPassword)
        {
            TempData["ToastMessage"] = "Đổi mật khẩu thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Selection", "Portal");
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");

            var emp = _context.NhanViens.FirstOrDefault(e => e.Email == email);
            if (emp == null)
            {
                ViewBag.FullName = HttpContext.Session.GetString("UserName");
                ViewBag.Email = email;
                ViewBag.Phone = "0900000000";
                ViewBag.Roles = "Khách hàng";

                // Mapping for Profile.cshtml fields
                ViewBag.TenNhanVien = HttpContext.Session.GetString("UserName") ?? "Khách hàng";
                ViewBag.SoDienThoai = "0900000000";
                ViewBag.ChucVu = "Khách hàng";
            }
            else
            {
                // Đồng bộ ngược lại Session phòng trường hợp dữ liệu đã đổi ở màn quản trị
                HttpContext.Session.SetString("UserName", emp.HoTen);
                HttpContext.Session.SetString("UserRoles", emp.VaiTro);

                ViewBag.FullName = emp.HoTen;
                ViewBag.Email = emp.Email;
                ViewBag.Phone = emp.SoDienThoai;
                ViewBag.Roles = emp.VaiTro;

                // Mapping for Profile.cshtml fields
                ViewBag.TenNhanVien = emp.HoTen;
                ViewBag.SoDienThoai = emp.SoDienThoai;
                ViewBag.ChucVu = emp.VaiTro;
            }

            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (string.IsNullOrEmpty(model.FullName))
            {
                ModelState.AddModelError("", "Họ và tên không được để trống.");
                return View(model);
            }

            if (string.IsNullOrEmpty(model.Email) && string.IsNullOrEmpty(model.Phone))
            {
                ModelState.AddModelError("", "Vui lòng nhập Email hoặc Số điện thoại để đăng ký.");
                return View(model);
            }

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu và xác nhận mật khẩu không trùng khớp.");
                return View(model);
            }

            // Check email uniqueness
            if (!string.IsNullOrEmpty(model.Email))
            {
                var existingEmail = _context.KhachHangs.FirstOrDefault(k => k.Email != null && k.Email.ToLower() == model.Email.ToLower());
                if (existingEmail != null)
                {
                    ModelState.AddModelError("", "Email này đã được sử dụng bởi tài khoản khác.");
                    return View(model);
                }
            }

            // Check phone uniqueness
            if (!string.IsNullOrEmpty(model.Phone))
            {
                var existingPhone = _context.KhachHangs.FirstOrDefault(k => k.SoDienThoai == model.Phone);
                if (existingPhone != null)
                {
                    ModelState.AddModelError("", "Số điện thoại này đã được sử dụng bởi tài khoản khác.");
                    return View(model);
                }
            }

            var customer = new KhachHang
            {
                HoTen = model.FullName,
                Email = string.IsNullOrEmpty(model.Email) ? null : model.Email,
                SoDienThoai = model.Phone,
                DiaChi = model.Address ?? "",
                DiemTichLuy = 0,
                TrangThai = "Hoạt động",
                NgayTao = System.DateTime.Now
            };

            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<KhachHang>();
            customer.MatKhau = hasher.HashPassword(customer, model.Password);

            _context.KhachHangs.Add(customer);
            _context.SaveChanges();

            // Set session variables to automatically log in the registered customer
            HttpContext.Session.SetString("UserEmail", customer.Email ?? customer.SoDienThoai ?? "");
            HttpContext.Session.SetString("UserName", customer.HoTen);
            HttpContext.Session.SetString("UserRoles", "Khách hàng");
            HttpContext.Session.SetString("UserPermissions", "");
            HttpContext.Session.SetString("CustomerId", customer.MaKhachHang.ToString());

            TempData["ToastMessage"] = "Đăng ký tài khoản thành công!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index", "Online");
        }
    }
}
