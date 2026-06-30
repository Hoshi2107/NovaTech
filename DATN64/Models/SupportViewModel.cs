using System;
using System.Collections.Generic;

namespace DATN64.Models
{
    public class SupportViewModel
    {
        public string ActiveTab { get; set; } = "warranty";
        public WarrantyCheckModel WarrantyCheck { get; set; } = new WarrantyCheckModel();
        public RepairRequestModel RepairRequest { get; set; } = new RepairRequestModel();
    }

    public class WarrantyCheckModel
    {
        public string Query { get; set; } = string.Empty;
        public bool Found { get; set; }
        public bool HasResult { get; set; }
        public int? OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? OrderDate { get; set; }
        public decimal? TotalAmount { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ChiTietDonHang>? OrderItems { get; set; }
    }

    public class RepairRequestModel
    {
        public string Query { get; set; } = string.Empty;
        public string Issue { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string PreferredTime { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public bool Success { get; set; }
        public bool HasAttempted { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
    }
}
