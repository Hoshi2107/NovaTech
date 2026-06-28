using System.ComponentModel.DataAnnotations;

namespace DATN64.Models
{
    public class ProfileViewModel
    {
        public int MaKhachHang { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(150)]
        public string HoTen { get; set; } = "";

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20)]
        public string? SoDienThoai { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(255)]
        public string? DiaChi { get; set; }

        public int DiemTichLuy { get; set; }
        public string? TrangThai { get; set; }
        public System.DateTime NgayTao { get; set; }

        // Password change
        public string? MatKhauCu { get; set; }

        [StringLength(255, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải ít nhất 6 ký tự")]
        public string? MatKhauMoi { get; set; }

        public string? XacNhanMatKhau { get; set; }

        // Order history
        public List<DonHang> DonHangs { get; set; } = new();
    }
}
