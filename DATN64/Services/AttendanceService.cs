using System;
using System.Linq;
using System.Threading.Tasks;
using DATN64.Models;
using Microsoft.EntityFrameworkCore;

namespace DATN64.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly AppDbContext _context;

        // Constants for shift status
        public const string Active = "Đang làm";
        public const string Completed = "Hoàn thành";
        public const string ForgottenCheckout = "Quên checkout";

        public AttendanceService(AppDbContext context)
        {
            _context = context;
        }

        public async Task ProcessForgottenCheckoutAsync(int maNhanVien)
        {
            // Tìm các ca làm vẫn "Đang làm" từ ngày hôm trước trở về trước
            var forgottenShifts = await _context.ChamCongs
                .Where(c => c.MaNhanVien == maNhanVien 
                         && c.TrangThai == Active 
                         && c.NgayCham.Date < DateTime.Today)
                .ToListAsync();

            if (forgottenShifts.Any())
            {
                foreach (var shift in forgottenShifts)
                {
                    shift.TrangThai = ForgottenCheckout;
                    shift.GioRa = null;
                    shift.TongGioLam = null;
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
