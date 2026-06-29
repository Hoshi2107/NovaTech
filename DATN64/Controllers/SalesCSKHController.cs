using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using DATN64.Models;
using DATN64.Helpers;

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
        public IActionResult GetCustomerInbox()
        {
            var threads = _context.CustomerInboxThreads
                .Include(t => t.Messages)
                .OrderByDescending(t => t.UpdatedAt)
                .ToList()
                .Select(t =>
                {
                    var messages = t.Messages.OrderBy(m => m.Timestamp).ToList();

                    return new
                    {
                        id = t.Id,
                        customerId = t.CustomerId,
                        customerName = t.CustomerName,
                        customerPhone = t.CustomerPhone,
                        channel = t.Channel,
                        subject = t.Subject,
                        status = t.Status,
                        priority = t.Priority,
                        updatedAt = t.UpdatedAt,
                        unreadCount = messages.Count(m => m.Sender == "customer" && !m.IsRead),
                        lastMessage = messages.LastOrDefault()?.Text ?? ""
                    };
                });

            return Json(threads);
        }

        [HttpGet]
        public IActionResult GetCustomerThread(int id)
        {
            if (!IsUserLoggedIn())
            {
                return Unauthorized(new { message = "Vui lòng đăng nhập để sử dụng chat." });
            }

            var thread = _context.CustomerInboxThreads
                .Include(t => t.Messages)
                .FirstOrDefault(t => t.Id == id);

            if (thread == null)
            {
                return NotFound(new { message = "Không tìm thấy hội thoại." });
            }

            return Json(ToThreadDto(thread));
        }

      [HttpPost]
[HasPermission("View_Order")]
public IActionResult MarkCustomerThreadRead([FromBody] ThreadIdRequest request)
{
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

    // Không cập nhật UpdatedAt ở đây.
    // Nếu cập nhật UpdatedAt khi chỉ bấm xem chat,
    // danh sách hộp thư sẽ bị đảo vị trí.
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
            var thread = _context.CustomerInboxThreads
                .Include(t => t.Messages)
                .FirstOrDefault(t => t.Id == request.ThreadId);

            var messageText = (request.Message ?? "").Trim();

            if (thread == null)
            {
                return NotFound(new { message = "Không tìm thấy hội thoại." });
            }

            if (string.IsNullOrWhiteSpace(messageText))
            {
                return BadRequest(new { message = "Vui lòng nhập nội dung phản hồi." });
            }

            thread.Messages.Add(new CustomerInboxMessage
            {
                Sender = "staff",
                Text = messageText,
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
           

            _context.SaveChanges();

            return Json(new
            {
                message = "Đã gửi phản hồi cho khách hàng.",
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
        public IActionResult CreateCustomerInquiry([FromBody] CreateInquiryRequest request)
        {
            if (!IsUserLoggedIn())
            {
                return Unauthorized(new { message = "Vui lòng đăng nhập để sử dụng chat." });
            }

            var customerName = (request.CustomerName ?? "").Trim();
            var customerPhone = (request.CustomerPhone ?? "").Trim();
            var subjectText = string.IsNullOrWhiteSpace(request.Subject)
                ? "Chat hỗ trợ NovaTech"
                : request.Subject.Trim();
            var messageText = (request.Message ?? "").Trim();

            if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(messageText))
            {
                return BadRequest(new { message = "Vui lòng nhập nội dung câu hỏi." });
            }

            KhachHang? customer = null;

            if (!string.IsNullOrWhiteSpace(customerPhone))
            {
                customer = _context.KhachHangs.FirstOrDefault(k => k.SoDienThoai == customerPhone);
            }

            var now = DateTime.Now;

            var thread = new CustomerInboxThread
            {
                CustomerId = customer?.MaKhachHang ?? 0,
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                Channel = "Store",
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
                        Timestamp = now,
                        IsRead = false
                    },
                    new CustomerInboxMessage
                    {
                        Sender = "staff",
                        Text = "Anh/chị vui lòng chờ, admin sẽ phản hồi lại sau 10 phút.",
                        Timestamp = now.AddSeconds(1),
                        IsRead = true,
                        IsAutoReply = true
                    }
                }
            };

            _context.CustomerInboxThreads.Add(thread);
            _context.SaveChanges();

            return Json(new
            {
                message = "Đã gửi tin nhắn thành công.",
                thread = ToThreadDto(thread)
            });
        }

        [HttpPost]
        public IActionResult AddCustomerInquiryMessage([FromBody] AddInquiryMessageRequest request)
        {
            if (!IsUserLoggedIn())
            {
                return Unauthorized(new { message = "Vui lòng đăng nhập để sử dụng chat." });
            }

            var thread = _context.CustomerInboxThreads
                .Include(t => t.Messages)
                .FirstOrDefault(t => t.Id == request.ThreadId);

            var messageText = (request.Message ?? "").Trim();

            if (thread == null)
            {
                return NotFound(new { message = "Không tìm thấy hội thoại." });
            }

            if (string.IsNullOrWhiteSpace(messageText))
            {
                return BadRequest(new { message = "Vui lòng nhập nội dung tin nhắn." });
            }

            thread.Messages.Add(new CustomerInboxMessage
            {
                Sender = "customer",
                Text = messageText,
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

        private object ToThreadDto(CustomerInboxThread thread)
        {
            var messages = thread.Messages
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    id = m.Id,
                    threadId = m.ThreadId,
                    sender = m.Sender,
                    text = m.Text,
                    timestamp = m.Timestamp,
                    isRead = m.IsRead,
                    isAutoReply = m.IsAutoReply
                })
                .ToList();

            return new
            {
                id = thread.Id,
                customerId = thread.CustomerId,
                customerName = thread.CustomerName,
                customerPhone = thread.CustomerPhone,
                channel = thread.Channel,
                subject = thread.Subject,
                status = thread.Status,
                priority = thread.Priority,
                updatedAt = thread.UpdatedAt,
                messages = messages
            };
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
        }

        public class CreateInquiryRequest
        {
            public string? CustomerName { get; set; }
            public string? CustomerPhone { get; set; }
            public string? Subject { get; set; }
            public string? Message { get; set; }
        }

        public class AddInquiryMessageRequest
        {
            public int ThreadId { get; set; }
            public string? Message { get; set; }
        }
    }
}