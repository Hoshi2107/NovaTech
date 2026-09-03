const fs = require('fs');
const path = require('path');
const { 
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell, 
  HeadingLevel, AlignmentType, BorderStyle, WidthType, ShadingType 
} = require('docx');

// Brand Colors
const COLOR_PRIMARY = "FE2C55"; // TikTok Pink/Red
const COLOR_SECONDARY = "25F4EE"; // TikTok Cyan
const COLOR_DARK = "1E1E2E"; // Deep Slate
const COLOR_MUTED = "555555"; // Grey
const COLOR_BG_LIGHT = "F8F9FA"; // Light grey table/box background
const COLOR_BORDER = "CCCCCC";

function createHeaderCell(text, widthPercent) {
  return new TableCell({
    width: { size: widthPercent, type: WidthType.PERCENTAGE },
    shading: { fill: COLOR_PRIMARY, type: ShadingType.CLEAR },
    margins: { top: 120, bottom: 120, left: 150, right: 150 },
    children: [
      new Paragraph({
        alignment: AlignmentType.CENTER,
        children: [
          new TextRun({ text: text, bold: true, color: "FFFFFF", size: 22, font: "Segoe UI" })
        ]
      })
    ]
  });
}

function createDataCell(text, widthPercent, isBold = false, align = AlignmentType.LEFT) {
  return new TableCell({
    width: { size: widthPercent, type: WidthType.PERCENTAGE },
    shading: { fill: "FFFFFF", type: ShadingType.CLEAR },
    margins: { top: 100, bottom: 100, left: 150, right: 150 },
    borders: {
      top: { style: BorderStyle.SINGLE, size: 4, color: COLOR_BORDER },
      bottom: { style: BorderStyle.SINGLE, size: 4, color: COLOR_BORDER },
      left: { style: BorderStyle.SINGLE, size: 4, color: COLOR_BORDER },
      right: { style: BorderStyle.SINGLE, size: 4, color: COLOR_BORDER }
    },
    children: [
      new Paragraph({
        alignment: align,
        children: [
          new TextRun({ text: text, bold: isBold, color: COLOR_DARK, size: 20, font: "Segoe UI" })
        ]
      })
    ]
  });
}

function createCalloutBox(title, text) {
  return new Table({
    width: { size: 100, type: WidthType.PERCENTAGE },
    margins: { top: 150, bottom: 150, left: 200, right: 200 },
    rows: [
      new TableRow({
        children: [
          new TableCell({
            width: { size: 100, type: WidthType.PERCENTAGE },
            shading: { fill: COLOR_BG_LIGHT, type: ShadingType.CLEAR },
            borders: {
              left: { style: BorderStyle.SINGLE, size: 24, color: COLOR_PRIMARY },
              top: { style: BorderStyle.NONE },
              right: { style: BorderStyle.NONE },
              bottom: { style: BorderStyle.NONE }
            },
            children: [
              new Paragraph({
                children: [
                  new TextRun({ text: title + " ", bold: true, color: COLOR_PRIMARY, size: 22, font: "Segoe UI" })
                ]
              }),
              new Paragraph({
                children: [
                  new TextRun({ text: text, italic: true, color: COLOR_DARK, size: 20, font: "Segoe UI" })
                ]
              })
            ]
          })
        ]
      })
    ]
  });
}

const doc = new Document({
  styles: {
    default: {
      font: "Segoe UI",
      size: 22
    }
  },
  sections: [
    {
      properties: {},
      children: [
        // TITLE
        new Paragraph({
          alignment: AlignmentType.CENTER,
          spacing: { after: 100, before: 200 },
          children: [
            new TextRun({
              text: "TÀI LIỆU BẢO VỆ ĐỒ ÁN / DỰ ÁN",
              bold: true,
              size: 32,
              color: COLOR_PRIMARY,
              font: "Segoe UI"
            })
          ]
        }),
        new Paragraph({
          alignment: AlignmentType.CENTER,
          spacing: { after: 300 },
          children: [
            new TextRun({
              text: "Mô Phỏng Kênh Bán Hàng TikTok Shop Livestream & Tích Hợp Hệ Thống NovaTech ERP",
              bold: true,
              italic: true,
              size: 24,
              color: COLOR_DARK,
              font: "Segoe UI"
            })
          ]
        }),

        createCalloutBox("📌 Tóm tắt cốt lõi cho sinh viên khi lên bảng bảo vệ:", "Dự án Fake TikTok Shop đóng vai trò là kênh bán hàng bên thứ 3 (Marketplace Simulator). Hệ thống cung cấp trải nghiệm Livestream real-time (bằng SignalR WebSocket), cho phép khách hàng xem live, ghim sản phẩm, chọn biến thể và đặt hàng tức thì. Đơn hàng sau khi chốt sẽ được đẩy tự động (Auto-Push Webhook) về hệ thống NovaTech ERP để kho tiếp nhận và xử lý."),

        new Paragraph({ text: "", spacing: { after: 200 } }),

        // SECTION 1
        new Paragraph({
          heading: HeadingLevel.HEADING_1,
          spacing: { before: 300, after: 150 },
          children: [
            new TextRun({ text: "1. TỔNG QUAN DỰ ÁN & MỤC TIÊU CỐT LÕI", bold: true, color: COLOR_PRIMARY, size: 26 })
          ]
        }),
        new Paragraph({
          spacing: { after: 100 },
          children: [
            new TextRun({ text: "• Tên phân hệ: ", bold: true }),
            new TextRun({ text: "Fake TikTok Shop Simulator for NovaTech (ASP.NET Core 8.0 MVC)" })
          ]
        }),
        new Paragraph({
          spacing: { after: 100 },
          children: [
            new TextRun({ text: "• Mục tiêu thiết kế: ", bold: true }),
            new TextRun({ text: "Xây dựng trình mô phỏng kênh bán hàng TikTok Shop thực tế nhằm chứng minh khả năng mở rộng hệ thống (System Scalability) và khả năng tích hợp đa kênh (Omnichannel Integration) của hệ thống quản trị doanh nghiệp NovaTech ERP." })
          ]
        }),
        new Paragraph({
          spacing: { after: 200 },
          children: [
            new TextRun({ text: "• Vai trò trong Đồ Án: ", bold: true }),
            new TextRun({ text: "Giúp kiểm thử luồng đồng bộ danh mục sản phẩm (PULL) và luồng xử lý đơn hàng tự động từ kênh ngoài về kho (PUSH Webhook)." })
          ]
        }),

        // SECTION 2
        new Paragraph({
          heading: HeadingLevel.HEADING_1,
          spacing: { before: 300, after: 150 },
          children: [
            new TextRun({ text: "2. KIẾN TRÚC CÔNG NGHỆ (TECH STACK DETAIL)", bold: true, color: COLOR_PRIMARY, size: 26 })
          ]
        }),

        // TABLE OF TECH STACK
        new Table({
          width: { size: 100, type: WidthType.PERCENTAGE },
          rows: [
            new TableRow({
              children: [
                createHeaderCell("Thành phần", 25),
                createHeaderCell("Công nghệ / Thư viện", 35),
                createHeaderCell("Vai trò & Lý do lựa chọn", 40)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("Backend Engine", 25, true),
                createDataCell("ASP.NET Core 8.0 MVC (C#)", 35),
                createDataCell("Xử lý logic server, routing RESTful API, quản lý controller & lifecycle ứng dụng.", 40)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("Real-Time Comm", 25, true),
                createDataCell("ASP.NET Core SignalR (WebSocket)", 35),
                createDataCell("Truyền tải khung hình video stream, comment, tim, ghim sản phẩm real-time không cần HTTP Polling.", 40)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("Database & ORM", 25, true),
                createDataCell("SQLite + Entity Framework Core 8", 35),
                createDataCell("Cơ sở dữ liệu nhẹ, tự tạo bảng (`EnsureCreated`), lưu trữ sản phẩm cache, đơn hàng, webhook log.", 40)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("Optimization", 25, true),
                createDataCell("Brotli & Gzip Response Compression", 35),
                createDataCell("Nén dữ liệu JSON/HTML/JS mức `Fastest` giúp tối ưu tốc độ load trên thiết bị di động.", 40)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("Frontend UI/UX", 25, true),
                createDataCell("HTML5, CSS3, JavaScript ES6+", 35),
                createDataCell("Giao diện Mobile-First chuẩn TikTok Shop, tích hợp HTML5 Canvas & MediaDevices API capture webcam.", 40)
              ]
            })
          ]
        }),

        new Paragraph({ text: "", spacing: { after: 200 } }),

        // SECTION 3
        new Paragraph({
          heading: HeadingLevel.HEADING_1,
          spacing: { before: 300, after: 150 },
          children: [
            new TextRun({ text: "3. CÁC PHÂN HỆ VÀ LUỒNG HOẠT ĐỘNG CHI TIẾT", bold: true, color: COLOR_PRIMARY, size: 26 })
          ]
        }),

        new Paragraph({
          heading: HeadingLevel.HEADING_2,
          spacing: { before: 150, after: 100 },
          children: [
            new TextRun({ text: "3.1. Phân hệ Livestream Real-Time (`LivestreamHub` & `Livestream.cshtml`)", bold: true, color: COLOR_DARK, size: 23 })
          ]
        }),
        new Paragraph({
          spacing: { after: 100 },
          children: [
            new TextRun({ text: "• Kênh Host (Streamer): ", bold: true }),
            new TextRun({ text: "Sử dụng camera thiết bị chụp ảnh từng frame (Canvas API) và gửi qua SignalR Hub hàm `SendFrame(frameData)`. Host có quyền chọn sản phẩm trong giỏ và ấn 'Ghim', server lập tức gọi `Clients.All.SendAsync('ProductsUpdated')` tới toàn bộ viewer." })
          ]
        }),
        new Paragraph({
          spacing: { after: 100 },
          children: [
            new TextRun({ text: "• Kênh Viewer (`LiveViewer.cshtml`): ", bold: true }),
            new TextRun({ text: "Lắng nghe sự kiện `ReceiveFrame` để render luồng video trực tiếp. Nhận thông báo sản phẩm ghim nổi bật trên màn hình kèm nút 'Mua Ngay'." })
          ]
        }),
        new Paragraph({
          spacing: { after: 200 },
          children: [
            new TextRun({ text: "• Quản lý đếm lượt xem (Viewer Tracking): ", bold: true }),
            new TextRun({ text: "Sử dụng `ConcurrentDictionary<string, string>` trong `LivestreamHub.cs` để đếm chính xác số kết nối client thực tế (loại trừ kết nối của Host)." })
          ]
        }),

        new Paragraph({
          heading: HeadingLevel.HEADING_2,
          spacing: { before: 150, after: 100 },
          children: [
            new TextRun({ text: "3.2. Phân hệ Đặt Hàng & Biến Thể Sản Phẩm (`StreamApiController.cs`)", bold: true, color: COLOR_DARK, size: 23 })
          ]
        }),
        new Paragraph({
          spacing: { after: 100 },
          children: [
            new TextRun({ text: "• Xử lý biến thể thông minh: ", bold: true }),
            new TextRun({ text: "Hàm `GetProductVariants` sử dụng thuật toán `FilterRelatedVariants` và `ExtractVariantLabel` tách dòng sản phẩm (ví dụ: iPhone 15 Pro Max) ra các lựa chọn dung lượng (128GB, 256GB...) và màu sắc (Titan Tự Nhiên, Titan Xanh...)." })
          ]
        }),
        new Paragraph({
          spacing: { after: 200 },
          children: [
            new TextRun({ text: "• Kiểm tra & Trừ tồn kho: ", bold: true }),
            new TextRun({ text: "Khi khách bấm mua (`CreateOrder`), hệ thống kiểm tra tồn kho tại `ProductCaches`. Nếu thỏa mãn, tồn kho lập tức giảm (`cachedProd.Stock -= item.Quantity`) và đơn hàng lưu vào SQLite dưới mã `SS-YYYYMMDDxxxx`." })
          ]
        }),

        new Paragraph({
          heading: HeadingLevel.HEADING_2,
          spacing: { before: 150, after: 100 },
          children: [
            new TextRun({ text: "3.3. Phân hệ Tích Hợp Tự Động Với NovaTech ERP (Integration Layer)", bold: true, color: COLOR_DARK, size: 23 })
          ]
        }),
        new Paragraph({
          spacing: { after: 100 },
          children: [
            new TextRun({ text: "1. Đồng bộ danh mục (PULL): ", bold: true }),
            new TextRun({ text: "API `/api/stream/sync-products` gọi sang NovaTech ERP (`/api/product`) để lấy toàn bộ dữ liệu mã SKU, tên, giá bán, tồn kho về lưu cache tại TikTok Shop." })
          ]
        }),
        new Paragraph({
          spacing: { after: 100 },
          children: [
            new TextRun({ text: "2. Đẩy đơn tự động (PUSH Webhook): ", bold: true }),
            new TextRun({ text: "Ngay khi có đơn hàng mới (`CreateOrder`), hàm `PushWebhookInternalAsync` được kích hoạt, đóng gói Payload JSON chuẩn chứa thông tin khách hàng & sản phẩm gửi POST tới `/api/stream/webhook` của NovaTech ERP." })
          ]
        }),
        new Paragraph({
          spacing: { after: 200 },
          children: [
            new TextRun({ text: "3. Theo dõi nhật ký (Webhook Logs): ", bold: true }),
            new TextRun({ text: "Mọi yêu cầu Webhook đều được lưu lại bảng `WebhookLogs` với HTTP Status Code (200 OK, 500, 404...) và thông báo phản hồi từ NovaTech để phục vụ tra cứu/tra soát lỗi." })
          ]
        }),

        // SECTION 4 - Q&A FOR DEFENSE
        new Paragraph({
          heading: HeadingLevel.HEADING_1,
          spacing: { before: 300, after: 150 },
          children: [
            new TextRun({ text: "4. BỘ CÂU HỎI VÀ CÂU TRẢ LỜI MẪU KHI BẢO VỆ HỘI ĐỒNG (Q&A)", bold: true, color: COLOR_PRIMARY, size: 26 })
          ]
        }),

        // Q1
        new Paragraph({
          spacing: { before: 100, after: 50 },
          children: [
            new TextRun({ text: "Câu 1: Tại sao em lại dùng SignalR mà không dùng HTTP Polling để làm tính năng Livestream?", bold: true, color: COLOR_DARK })
          ]
        }),
        new Paragraph({
          spacing: { after: 150 },
          children: [
            new TextRun({ text: "Trả lời: ", bold: true, italic: true, color: COLOR_PRIMARY }),
            new TextRun({ text: "HTTP Polling bắt trình duyệt liên tục gửi request 1-2s/lần gây quá tải server và trễ (latency) cao. SignalR thiết lập kết nối WebSocket 2 chiều duy nhất (Bi-directional persistent connection), giúp push dữ liệu frame video và comment tức thì tới hàng trăm client với độ trễ tiệm cận 0ms và tiết kiệm tối đa tài nguyên server." })
          ]
        }),

        // Q2
        new Paragraph({
          spacing: { before: 100, after: 50 },
          children: [
            new TextRun({ text: "Câu 2: Làm thế nào em đảm bảo khi hàng nghìn người cùng xem Live bấm mua thì không bị oversell (bán vượt tồn kho)?", bold: true, color: COLOR_DARK })
          ]
        }),
        new Paragraph({
          spacing: { after: 150 },
          children: [
            new TextRun({ text: "Trả lời: ", bold: true, italic: true, color: COLOR_PRIMARY }),
            new TextRun({ text: "Trong API `CreateOrder`, em thực hiện validate số lượng tồn kho `cachedProd.Stock < item.Quantity` trước khi trừ kho. Nếu tồn kho không đủ, API sẽ từ chối giao dịch ngay lập tức và trả về lỗi BadRequest. Ngoài ra, khi đơn hàng bị hủy (`Status == Cancelled`), hệ thống có cơ chế `Restore stock` tự động cộng trả lại tồn kho." })
          ]
        }),

        // Q3
        new Paragraph({
          spacing: { before: 100, after: 50 },
          children: [
            new TextRun({ text: "Câu 3: Kiến trúc đồng bộ giữa TikTok Shop và NovaTech ERP được thiết kế theo mô hình nào?", bold: true, color: COLOR_DARK })
          ]
        }),
        new Paragraph({
          spacing: { after: 200 },
          children: [
            new TextRun({ text: "Trả lời: ", bold: true, italic: true, color: COLOR_PRIMARY }),
            new TextRun({ text: "Kiến trúc được thiết kế theo mô hình Event-Driven Architecture kết hợp RESTful Webhook. Khi có sự kiện phát sinh (đơn hàng mới `order_created` hoặc đổi trạng thái `order_status_changed`), TikTok Shop đóng vai trò Publisher chủ động bắn thông báo Webhook (Payload JSON) tới Subscriber là hệ thống NovaTech ERP." })
          ]
        }),

        // FOOTER NOTE
        new Paragraph({ text: "", spacing: { after: 300 } }),
        createCalloutBox("✨ Lời chúc bảo vệ thành công:", "Chúc bạn tự tin bảo vệ đồ án và đạt điểm tối đa! Hãy nắm chắc các từ khóa: ASP.NET Core 8, SignalR WebSocket, Webhook Push/Pull, SQLite EF Core, Event-Driven Architecture.")
      ]
    }
  ]
});

const outputPath = path.join('c:', 'Users', 'Admin', 'Desktop', 'NovaTech', 'Huong_Dan_Bao_Ve_Fake_TikTok_Shop.docx');

Packer.toBuffer(doc).then((buffer) => {
  fs.writeFileSync(outputPath, buffer);
  console.log("Document generated successfully at: " + outputPath);
}).catch((err) => {
  console.error("Error generating document: ", err);
});
