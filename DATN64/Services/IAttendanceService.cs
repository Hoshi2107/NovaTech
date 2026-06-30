using System.Threading.Tasks;

namespace DATN64.Services
{
    public interface IAttendanceService
    {
        Task ProcessForgottenCheckoutAsync(int maNhanVien);
    }
}
