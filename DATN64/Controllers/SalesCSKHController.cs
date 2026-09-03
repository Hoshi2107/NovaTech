using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using DATN64.Models;
using DATN64.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace DATN64.Controllers
{
    public class SalesCSKHController : Controller
    {
        private readonly AppDbContext _context;

        public SalesCSKHController(AppDbContext context)
        {
            _context = context;
        }

        [HasPermission("View_Order")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Bán hàng & Chăm sóc khách hàng";
            return View();
        }

        [HttpGet]
        [HasPermission("View_Order")]
        public IActionResult GetCustomerInbox(string? keyword, string? status, int page = 1, int pageSize = 20)
        {
            if (page < 1)
            {
                page = 1;
            }

            var allowedPageSizes = new[] { 10, 20, 50 };

            if (!allowedPageSizes.Contains(pageSize))
            {
                pageSize = 20;
            }

            keyword = (keyword ?? string.Empty).Trim();
            status = (status ?? string.Empty).Trim();

            var query = _context.CustomerInboxThreads
                .Include(t => t.Messages)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(t =>
                    (t.CustomerName != null && t.CustomerName.Contains(keyword)) ||
                    (t.CustomerPhone != null && t.CustomerPhone.Contains(keyword)) ||
                    (t.Subject != null && t.Subject.Contains(keyword)) ||
                    t.Messages.Any(m => m.Text.Contains(keyword))
                );
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }

            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var threads = query
                .OrderByDescending(t => t.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            foreach (var thread in threads)
            {
                TryAttachCorrectCustomerProfileToThread(thread);
            }

            _context.SaveChanges();

            var result = threads
                .Select(ToThreadDto)
                .ToList();

            return Json(new
            {
                items = result,
                currentPage = page,
                pageSize = pageSize,
                totalItems = totalItems,
                totalPages = totalPages
            });
        }

        [HttpGet]
        public IActionResult GetCustomerThread(int id)
        {
            var thread = _context.CustomerInboxThreads
                .Include(t => t.Messages)
                .FirstOrDefault(t => t.Id == id);

            if (thread == null)
            {
                return NotFound(new { message = "Không tìm thấy hội thoại." });
            }

            TryAttachCorrectCustomerProfileToThread(thread);
            _context.SaveChanges();

            return Json(ToThreadDto(thread));
        }

        [HttpPost]
        [HasPermission("View_Order")]
        public IActionResult MarkCustomerThreadRead([FromBody] ThreadIdRequest request)
        {
            if (request == null || request.ThreadId <= 0)
            {
                return BadRequest(new { message = "Hội thoại không hợp lệ." });
            }

            var thread = _context.CustomerInboxThreads
                .Include(t => t.Messages)
                .FirstOrDefault(t => t.Id == request.ThreadId);

            if (thread == null)
            {
                return NotFound(new { message = "Không tìm thấy hội thoại." });
            }

            foreach (var message in thread.Messages)
            {
                if (message.Sender == "customer")
                {
                    message.IsRead = true;
                }
            }

            if (thread.Status == "Unread")
            {
                thread.Status = "Processing";
            }

            TryAttachCorrectCustomerProfileToThread(thread);

            // Không cập nhật UpdatedAt ở đây để danh sách không bị đảo vị trí khi chỉ bấm xem.
            _context.SaveChanges();

            return Json(new
            {
                message = "Đã đánh dấu đã đọc.",
                thread = ToThreadDto(thread)
            });
        }

        [HttpPost]
        [HasPermission("View_Order")]
        public IActionResult ReplyCustomerMessage([FromBody] ReplyCustomerMessageRequest request)
        {
            if (request == null || request.ThreadId <= 0)
            {
                return BadRequest(new { message = "Hội thoại không hợp lệ." });
            }

            var thread = _context.CustomerInboxThreads
                .Include(t => t.Messages)
                .FirstOrDefault(t => t.Id == request.ThreadId);

            var messageText = (request.Message ?? "").Trim();
            var imageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();

            if (thread == null)
            {
                return NotFound(new { message = "Không tìm thấy hội thoại." });
            }

            if (thread.Status == "Closed")
            {
                return BadRequest(new { message = "Hội thoại đã đóng. Không thể gửi phản hồi mới." });
            }

            if (string.IsNullOrWhiteSpace(messageText) && string.IsNullOrWhiteSpace(imageUrl))
            {
                return BadRequest(new { message = "Vui lòng nhập nội dung phản hồi hoặc đính kèm ảnh." });
            }

            TryAttachCorrectCustomerProfileToThread(thread);

            thread.Messages.Add(new CustomerInboxMessage
            {
                Sender = "staff",
                Text = messageText,
                ImageUrl = imageUrl,
                Timestamp = DateTime.Now,
                IsRead = true
            });

            foreach (var message in thread.Messages)
            {
                if (message.Sender == "customer")
                {
                    message.IsRead = true;
                }
            }

            thread.Status = string.IsNullOrWhiteSpace(request.Status) ? "Replied" : request.Status;
            thread.UpdatedAt = DateTime.Now;

            _context.SaveChanges();

            return Json(new
            {
                message = "Đã gửi phản hồi cho khách hàng.",
                thread = ToThreadDto(thread)
            });
        }

        [HttpPost]
        [HasPermission("View_Order")]
        public IActionResult CloseCustomerThread([FromBody] ThreadIdRequest request)
        {
            if (request == null || request.ThreadId <= 0)
            {
                return BadRequest(new { message = "Hội thoại không hợp lệ." });
            }

            var thread = _context.CustomerInboxThreads
                .Include(t => t.Messages)
                .FirstOrDefault(t => t.Id == request.ThreadId);

            if (thread == null)
            {
                return NotFound(new { message = "Không tìm thấy hội thoại." });
            }

            TryAttachCorrectCustomerProfileToThread(thread);

            thread.Status = "Closed";
            thread.UpdatedAt = DateTime.Now;

            foreach (var message in thread.Messages)
            {
                if (message.Sender == "customer")
                {
                    message.IsRead = true;
                }
            }

            _context.SaveChanges();

            return Json(new
            {
                message = "Đã đóng hội thoại.",
                thread = ToThreadDto(thread)
            });
        }

        [HttpPost]
        [HasPermission("View_Order")]
        public IActionResult DeleteCustomerThread([FromBody] ThreadIdRequest request)
        {
            if (request == null || request.ThreadId <= 0)
            {
                return BadRequest(new { message = "Hội thoại không hợp lệ." });
            }

            var thread = _context.CustomerInboxThreads
                .Include(t => t.Messages)
                .FirstOrDefault(t => t.Id == request.ThreadId);

            if (thread == null)
            {
                return NotFound(new { message = "Không tìm thấy hội thoại." });
            }

            if (thread.Messages != null && thread.Messages.Any())
            {
                _context.RemoveRange(thread.Messages);
            }

            _context.CustomerInboxThreads.Remove(thread);
            _context.SaveChanges();

            return Json(new
            {
                message = "Đã xóa hội thoại."
            });
        }

        [HttpPost]
        public async Task<IActionResult> UploadChatImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Không có file được tải lên." });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Chỉ cho phép tải lên ảnh (.jpg, .jpeg, .png, .webp, .gif)." });
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { message = "Ảnh quá lớn. Vui lòng chọn ảnh dưới 5MB." });
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "cskh");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var imageUrl = $"/uploads/cskh/{uniqueFileName}";

            return Json(new { imageUrl = imageUrl });
        }

        [HttpPost]
        public IActionResult CreateCustomerInquiry([FromBody] CreateInquiryRequest request)
        {
            var customerName = NormalizeText(request.CustomerName);
            if (string.IsNullOrWhiteSpace(customerName))
            {
                customerName = "Khách vãng lai";
            }

            var customerPhone = NormalizePhone(request.CustomerPhone);
            var customerEmail = NormalizeEmail(request.CustomerEmail);
            var sessionEmail = NormalizeEmail(HttpContext.Session.GetString("UserEmail"));

            var subjectText = string.IsNullOrWhiteSpace(request.Subject)
                ? "Chat hỗ trợ NovaTech"
                : request.Subject.Trim();

            var messageText = NormalizeText(request.Message);
            var imageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();

            if (string.IsNullOrWhiteSpace(messageText) && string.IsNullOrWhiteSpace(imageUrl))
            {
                return BadRequest(new { message = "Vui lòng nhập nội dung câu hỏi." });
            }

            var customer = FindCustomerForNewInquiry(customerEmail, customerPhone, sessionEmail, customerName);

            if (customer != null)
            {
                customerName = string.IsNullOrWhiteSpace(customer.HoTen) ? customerName : customer.HoTen.Trim();
                customerPhone = string.IsNullOrWhiteSpace(customer.SoDienThoai) ? customerPhone : customer.SoDienThoai.Trim();
            }

            var now = DateTime.Now;

            var thread = new CustomerInboxThread
            {
                CustomerId = customer?.MaKhachHang ?? 0,
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                Channel = "Website",
                Subject = subjectText,
                Status = "Unread",
                Priority = "Medium",
                UpdatedAt = now,
                Messages = new List<CustomerInboxMessage>
                {
                    new CustomerInboxMessage
                    {
                        Sender = "customer",
                        Text = messageText,
                        ImageUrl = imageUrl,
                        Timestamp = now,
                        IsRead = false
                    },
                    new CustomerInboxMessage
                    {
                        Sender = "staff",
                        Text = "Anh/chị vui lòng chờ, admin sẽ phản hồi lại sau vài phút.",
                        Timestamp = now.AddSeconds(1),
                        IsRead = true,
                        IsAutoReply = true
                    }
                }
            };

            _context.CustomerInboxThreads.Add(thread);
            _context.SaveChanges();

            if (thread.CustomerId == 0 && (string.IsNullOrWhiteSpace(thread.CustomerName) || thread.CustomerName == "Khách vãng lai"))
            {
                thread.CustomerName = $"Khách vãng lai #{thread.Id}";
                _context.SaveChanges();
            }

            return Json(new
            {
                message = "Đã gửi tin nhắn thành công.",
                thread = ToThreadDto(thread)
            });
        }

        [HttpPost]
        public IActionResult AddCustomerInquiryMessage([FromBody] AddInquiryMessageRequest request)
        {
            var thread = _context.CustomerInboxThreads
                .Include(t => t.Messages)
                .FirstOrDefault(t => t.Id == request.ThreadId);

            var messageText = NormalizeText(request.Message);
            var imageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();

            if (thread == null)
            {
                return NotFound(new { message = "Không tìm thấy hội thoại." });
            }

            if (string.IsNullOrWhiteSpace(messageText) && string.IsNullOrWhiteSpace(imageUrl))
            {
                return BadRequest(new { message = "Vui lòng nhập nội dung tin nhắn hoặc đính kèm ảnh." });
            }

            TryAttachCorrectCustomerProfileToThread(thread);

            thread.Messages.Add(new CustomerInboxMessage
            {
                Sender = "customer",
                Text = messageText,
                ImageUrl = imageUrl,
                Timestamp = DateTime.Now,
                IsRead = false
            });

            thread.Status = "Unread";
            thread.UpdatedAt = DateTime.Now;

            _context.SaveChanges();

            return Json(new
            {
                message = "Đã gửi tin nhắn.",
                thread = ToThreadDto(thread)
            });
        }

        private bool IsUserLoggedIn()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            return !string.IsNullOrWhiteSpace(email);
        }

        private KhachHang? FindCustomerForNewInquiry(string? requestEmail, string? requestPhone, string? sessionEmail, string? requestName)
        {
            requestEmail = NormalizeEmail(requestEmail);
            requestPhone = NormalizePhone(requestPhone);
            sessionEmail = NormalizeEmail(sessionEmail);
            requestName = NormalizeText(requestName);

            KhachHang? customer = null;

            if (!string.IsNullOrWhiteSpace(requestEmail))
            {
                customer = _context.KhachHangs
                    .FirstOrDefault(k =>
                        k.Email != null &&
                        k.Email.ToLower() == requestEmail);
            }

            if (customer == null && !string.IsNullOrWhiteSpace(requestPhone))
            {
                customer = _context.KhachHangs
                    .FirstOrDefault(k =>
                        k.SoDienThoai != null &&
                        k.SoDienThoai == requestPhone);
            }

            if (customer == null && !string.IsNullOrWhiteSpace(sessionEmail))
            {
                customer = _context.KhachHangs
                    .FirstOrDefault(k =>
                        k.Email != null &&
                        k.Email.ToLower() == sessionEmail);
            }

            if (customer == null && !string.IsNullOrWhiteSpace(requestName))
            {
                customer = FindUniqueCustomerByName(requestName);
            }

            return customer;
        }

        private void TryAttachCorrectCustomerProfileToThread(CustomerInboxThread thread)
        {
            var matchedCustomer = ResolveCorrectCustomerForThread(thread);

            if (matchedCustomer == null)
            {
                thread.CustomerId = 0;
                return;
            }

            thread.CustomerId = matchedCustomer.MaKhachHang;

            if (string.IsNullOrWhiteSpace(thread.CustomerPhone))
            {
                thread.CustomerPhone = matchedCustomer.SoDienThoai;
            }

            if (string.IsNullOrWhiteSpace(thread.CustomerName) ||
                IsLikelyWrongCustomerName(thread.CustomerName, matchedCustomer.HoTen))
            {
                thread.CustomerName = matchedCustomer.HoTen;
            }
        }

        private KhachHang? ResolveCorrectCustomerForThread(CustomerInboxThread thread)
        {
            var threadName = NormalizeText(thread.CustomerName);
            var threadPhone = NormalizePhone(thread.CustomerPhone);

            if (!string.IsNullOrWhiteSpace(threadPhone))
            {
                var byPhone = _context.KhachHangs
                    .FirstOrDefault(k =>
                        k.SoDienThoai != null &&
                        k.SoDienThoai == threadPhone);

                if (byPhone != null)
                {
                    return byPhone;
                }
            }

            if (!string.IsNullOrWhiteSpace(threadName))
            {
                var byUniqueName = FindUniqueCustomerByName(threadName);

                if (byUniqueName != null)
                {
                    return byUniqueName;
                }
            }

            if (thread.CustomerId > 0)
            {
                var byId = _context.KhachHangs
                    .FirstOrDefault(k => k.MaKhachHang == thread.CustomerId);

                if (byId != null && IsThreadCompatibleWithCustomer(thread, byId))
                {
                    return byId;
                }
            }

            return null;
        }

        private KhachHang? FindUniqueCustomerByName(string name)
        {
            name = NormalizeText(name);

            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var matched = _context.KhachHangs
                .Where(k =>
                    k.HoTen != null &&
                    k.HoTen.Trim().ToLower() == name.ToLower())
                .Take(2)
                .ToList();

            if (matched.Count == 1)
            {
                return matched[0];
            }

            return null;
        }

        private bool IsThreadCompatibleWithCustomer(CustomerInboxThread thread, KhachHang customer)
        {
            var threadName = NormalizeText(thread.CustomerName);
            var threadPhone = NormalizePhone(thread.CustomerPhone);
            var customerName = NormalizeText(customer.HoTen);
            var customerPhone = NormalizePhone(customer.SoDienThoai);

            if (!string.IsNullOrWhiteSpace(threadPhone) &&
                !string.IsNullOrWhiteSpace(customerPhone) &&
                threadPhone == customerPhone)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(threadName) &&
                !string.IsNullOrWhiteSpace(customerName) &&
                threadName.Equals(customerName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private bool IsLikelyWrongCustomerName(string threadName, string? customerName)
        {
            threadName = NormalizeText(threadName);
            customerName = NormalizeText(customerName);

            if (string.IsNullOrWhiteSpace(threadName) || string.IsNullOrWhiteSpace(customerName))
            {
                return false;
            }

            return !threadName.Equals(customerName, StringComparison.OrdinalIgnoreCase);
        }

        private object ToThreadDto(CustomerInboxThread thread)
        {
            var messages = thread.Messages
                .OrderBy(m => m.Timestamp)
                .ToList();

            var lastMessage = messages.LastOrDefault();

            var unreadCount = messages.Count(m =>
                m.Sender == "customer" &&
                !m.IsRead);

            var customer = ResolveCorrectCustomerForThread(thread);

            var finalCustomerId = customer?.MaKhachHang ?? 0;

            var customerName = !string.IsNullOrWhiteSpace(thread.CustomerName) && thread.CustomerName != "Khách vãng lai"
                ? thread.CustomerName
                : (customer != null && !string.IsNullOrWhiteSpace(customer.HoTen) ? customer.HoTen : $"Khách vãng lai #{thread.Id}");

            var customerPhone = !string.IsNullOrWhiteSpace(thread.CustomerPhone)
                ? thread.CustomerPhone
                : customer?.SoDienThoai ?? "";

            return new
            {
                id = thread.Id,
                customerId = finalCustomerId,
                customerName = customerName,
                customerPhone = customerPhone,
                customerEmail = customer?.Email ?? "",
                customerAddress = customer?.DiaChi ?? "",
                customerPoints = customer?.DiemTichLuy ?? 0,
                customerStatus = customer?.TrangThai ?? "",
                canOpenCustomerProfile = finalCustomerId > 0,
                channel = string.IsNullOrWhiteSpace(thread.Channel) ? "Website" : thread.Channel,
                subject = thread.Subject,
                status = string.IsNullOrWhiteSpace(thread.Status) ? "Unread" : thread.Status,
                priority = thread.Priority,
                updatedAt = thread.UpdatedAt,
                lastMessage = lastMessage?.Text ?? "",
                unreadCount = unreadCount,
                messages = messages.Select(m => new
                {
                    id = m.Id,
                    threadId = m.ThreadId,
                    sender = m.Sender,
                    text = m.Text,
                    imageUrl = m.ImageUrl,
                    timestamp = m.Timestamp,
                    isRead = m.IsRead,
                    isAutoReply = m.IsAutoReply
                }).ToList()
            };
        }

        private static string NormalizeEmail(string? email)
        {
            return string.IsNullOrWhiteSpace(email)
                ? ""
                : email.Trim().ToLowerInvariant();
        }

        private static string NormalizePhone(string? phone)
        {
            return string.IsNullOrWhiteSpace(phone)
                ? ""
                : phone.Trim();
        }

        private static string NormalizeText(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? ""
                : value.Trim();
        }

        public class ThreadIdRequest
        {
            public int ThreadId { get; set; }
        }

        public class ReplyCustomerMessageRequest
        {
            public int ThreadId { get; set; }
            public string? Message { get; set; }
            public string? Status { get; set; }
            public string? ImageUrl { get; set; }
        }

        public class CreateInquiryRequest
        {
            public string? CustomerName { get; set; }
            public string? CustomerPhone { get; set; }
            public string? CustomerEmail { get; set; }
            public string? Subject { get; set; }
            public string? Message { get; set; }
            public string? ImageUrl { get; set; }
        }

        public class AddInquiryMessageRequest
        {
            public int ThreadId { get; set; }
            public string? Message { get; set; }
            public string? ImageUrl { get; set; }
        }
    }
}