using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using DATN64.Models;
using DATN64.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

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
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(t =>
                    t.CustomerName.Contains(keyword) ||
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
                .Select(t => new
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

                    lastMessage = t.Messages
                        .OrderByDescending(m => m.Timestamp)
                        .Select(m => m.Text)
                        .FirstOrDefault(),

                    unreadCount = t.Messages
                        .Count(m => m.Sender == "customer" && !m.IsRead)
                })
                .ToList();

            return Json(new
            {
                items = threads,
                currentPage = page,
                pageSize = pageSize,
                totalItems = totalItems,
                totalPages = totalPages
            });
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

            // Không cập nhật UpdatedAt ở đây.
            // Nếu chỉ bấm xem chat mà cập nhật UpdatedAt thì danh sách sẽ bị đảo vị trí.
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

            if (thread == null)
            {
                return NotFound(new { message = "Không tìm thấy hội thoại." });
            }

            if (string.IsNullOrWhiteSpace(messageText))
            {
                return BadRequest(new { message = "Vui lòng nhập nội dung phản hồi." });
            }

            var now = DateTime.Now;

            thread.Messages.Add(new CustomerInboxMessage
            {
                Sender = "staff",
                Text = messageText,
                Timestamp = now,
                IsRead = true
            });

            foreach (var message in thread.Messages)
            {
                if (message.Sender == "customer")
                {
                    message.IsRead = true;
                }
            }

            thread.Status = string.IsNullOrWhiteSpace(request.Status)
                ? "Replied"
                : request.Status;

            // Có phản hồi mới thật sự thì cập nhật UpdatedAt là hợp lý.
            thread.UpdatedAt = now;

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

            if (request == null)
            {
                return BadRequest(new { message = "Dữ liệu không hợp lệ." });
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
                customer = _context.KhachHangs
                    .FirstOrDefault(k => k.SoDienThoai == customerPhone);
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
                        Text = "Anh/chị vui lòng chờ, admin sẽ phản hồi lại sau vài phút.",
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

            if (request == null || request.ThreadId <= 0)
            {
                return BadRequest(new { message = "Hội thoại không hợp lệ." });
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

            var now = DateTime.Now;

            thread.Messages.Add(new CustomerInboxMessage
            {
                Sender = "customer",
                Text = messageText,
                Timestamp = now,
                IsRead = false
            });

            thread.Status = "Unread";
            thread.UpdatedAt = now;

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

            var lastMessage = messages
                .OrderByDescending(m => m.timestamp)
                .Select(m => m.text)
                .FirstOrDefault();

            var unreadCount = messages
                .Count(m => m.sender == "customer" && !m.isRead);

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
                lastMessage = lastMessage,
                unreadCount = unreadCount,
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