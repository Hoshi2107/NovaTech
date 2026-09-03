const fs = require('fs');
const path = require('path');
const { 
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell, 
  HeadingLevel, AlignmentType, BorderStyle, WidthType, ShadingType 
} = require('docx');

// Brand Colors
const COLOR_PRIMARY = "0F4C81";   // Deep Blue / Professional ERP Accent
const COLOR_SECONDARY = "008080"; // Teal Accent
const COLOR_DARK = "1E293B";      // Slate Dark Text
const COLOR_MUTED = "64748B";     // Slate Muted Text
const COLOR_BG_LIGHT = "F1F5F9";  // Light Slate Table/Box Background
const COLOR_HIGHLIGHT = "FE2C55"; // Highlight / Alert Pink
const COLOR_BORDER = "CBD5E1";    // Border Grey

function createHeaderCell(text, widthPercent) {
  return new TableCell({
    width: { size: widthPercent, type: WidthType.PERCENTAGE },
    shading: { fill: COLOR_PRIMARY, type: ShadingType.CLEAR },
    margins: { top: 120, bottom: 120, left: 150, right: 150 },
    children: [
      new Paragraph({
        alignment: AlignmentType.CENTER,
        children: [
          new TextRun({ text: text, bold: true, color: "FFFFFF", size: 21, font: "Segoe UI" })
        ]
      })
    ]
  });
}

function createDataCell(text, widthPercent, isBold = false, align = AlignmentType.LEFT, fillBg = "FFFFFF") {
  return new TableCell({
    width: { size: widthPercent, type: WidthType.PERCENTAGE },
    shading: { fill: fillBg, type: ShadingType.CLEAR },
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

function createCalloutBox(title, text, isAlert = false) {
  const borderColor = isAlert ? COLOR_HIGHLIGHT : COLOR_PRIMARY;
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
              left: { style: BorderStyle.SINGLE, size: 24, color: borderColor },
              top: { style: BorderStyle.NONE },
              right: { style: BorderStyle.NONE },
              bottom: { style: BorderStyle.NONE }
            },
            children: [
              new Paragraph({
                spacing: { after: 50 },
                children: [
                  new TextRun({ text: title + " ", bold: true, color: borderColor, size: 22, font: "Segoe UI" })
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

function createCodeBlock(codeText) {
  const lines = codeText.split('\n');
  const paragraphs = lines.map(line => 
    new Paragraph({
      spacing: { line: 240 },
      children: [
        new TextRun({ text: line, font: "Consolas", size: 18, color: "0F172A" })
      ]
    })
  );

  return new Table({
    width: { size: 100, type: WidthType.PERCENTAGE },
    margins: { top: 100, bottom: 100, left: 150, right: 150 },
    rows: [
      new TableRow({
        children: [
          new TableCell({
            width: { size: 100, type: WidthType.PERCENTAGE },
            shading: { fill: "F8FAFC", type: ShadingType.CLEAR },
            borders: {
              top: { style: BorderStyle.SINGLE, size: 4, color: COLOR_BORDER },
              bottom: { style: BorderStyle.SINGLE, size: 4, color: COLOR_BORDER },
              left: { style: BorderStyle.SINGLE, size: 12, color: COLOR_SECONDARY },
              right: { style: BorderStyle.SINGLE, size: 4, color: COLOR_BORDER }
            },
            children: paragraphs
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
      size: 21
    }
  },
  sections: [
    {
      properties: {},
      children: [
        // DOCUMENT TITLE
        new Paragraph({
          alignment: AlignmentType.CENTER,
          spacing: { after: 100, before: 100 },
          children: [
            new TextRun({
              text: "🚀 TÀI LIỆU KỊCH BẢN BẢO VỆ ĐỒ ÁN NOVATECH",
              bold: true,
              size: 32,
              color: COLOR_PRIMARY,
              font: "Segoe UI"
            })
          ]
        }),
        new Paragraph({
          alignment: AlignmentType.CENTER,
          spacing: { after: 100 },
          children: [
            new TextRun({
              text: "Hệ Thống Quản Trị & Bán Hàng Đa Kênh NovaTech (ERP & E-Commerce)",
              bold: true,
              italic: true,
              size: 24,
              color: COLOR_DARK,
              font: "Segoe UI"
            })
          ]
        }),
        new Paragraph({
          alignment: AlignmentType.CENTER,
          spacing: { after: 300 },
          children: [
            new TextRun({ text: "📅 Ngày bảo vệ: 04/09/2026  |  🎯 Phân hệ phụ trách: Kế toán (Chuyên sâu), Trợ lý AI, TikTok Shop, Dashboard", italic: true, color: COLOR_MUTED, size: 20 })
          ]
        }),

        createCalloutBox("📌 TÓM TẮT MỤC TIÊU BẢO VỆ:", "Tài liệu này được biên soạn chuyên sâu dành cho sinh viên bảo vệ đồ án tốt nghiệp NovaTech. Tập trung làm rõ kiến trúc C# ASP.NET Core 8.0 MVC, Entity Framework Core, AI Agent (Gemini API), quy trình đồng bộ dữ liệu TikTok Shop, và các thuật toán tính toán dòng tiền P&L trong Module Kế toán."),

        new Paragraph({ text: "", spacing: { after: 200 } }),

        // SECTION 1
        new Paragraph({
          heading: HeadingLevel.HEADING_1,
          spacing: { before: 300, after: 150 },
          children: [
            new TextRun({ text: "1. TỔNG QUAN KIẾN TRÚC & CÔNG NGHỆ HỆ THỐNG", bold: true, color: COLOR_PRIMARY, size: 26 })
          ]
        }),

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
                createDataCell("C# ASP.NET Core MVC (.NET 8.0)", 35),
                createDataCell("Xử lý nghiệp vụ ERP, Routing RESTful Controllers, quản lý Session & Filter Permissions.", 40)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("Database ORM", 25, true),
                createDataCell("SQL Server + Entity Framework Core 8", 35),
                createDataCell("Quản lý CSDL quan hệ với LINQ query tối ưu, hỗ trợ Migration và Async LINQ.", 40)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("AI Integration", 25, true),
                createDataCell("Google Gemini API (GeminiService)", 35),
                createDataCell("Kiến trúc Autonomous Agent trả về Agentic JSON Response xử lý hành động tự động.", 40)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("Communication", 25, true),
                createDataCell("MailKit / SmtpClient (EmailService)", 35),
                createDataCell("Gửi Email HTML tri ân VIP và voucher ưu đãi qua giao thức SMTP thực tế.", 40)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("Frontend & Visual", 25, true),
                createDataCell("Razor Views, HTML5/CSS3, Chart.js", 35),
                createDataCell("Giao diện quản trị ERP hiện đại, trực quan hóa biểu đồ dòng tiền & P&L 12 tháng.", 40)
              ]
            })
          ]
        }),

        new Paragraph({ text: "", spacing: { after: 200 } }),

        // SECTION 2 - ACCOUNTING
        new Paragraph({
          heading: HeadingLevel.HEADING_1,
          spacing: { before: 300, after: 150 },
          children: [
            new TextRun({ text: "2. MODULE 1: KẾ TOÁN & QUẢN LÝ DÒNG TIỀN (TRỌNG TÂM CHI TIẾT)", bold: true, color: COLOR_PRIMARY, size: 26 })
          ]
        }),

        new Paragraph({
          heading: HeadingLevel.HEADING_2,
          spacing: { before: 150, after: 100 },
          children: [
            new TextRun({ text: "2.1. Đặt Vấn Đề & Mục Tiêu Phân Hệ Kế Toán", bold: true, color: COLOR_DARK, size: 23 })
          ]
        }),
        new Paragraph({
          spacing: { after: 150 },
          children: [
            new TextRun({ text: "Trong mô hình bán hàng đa kênh (Omnichannel ERP), việc ghi nhận doanh thu, chi phí và công nợ thủ công rất dễ gây thất thoát và sai lệch dòng tiền. Module Kế toán NovaTech được xây dựng với cơ chế " }),
            new TextRun({ text: "Tự động hóa đồng bộ gối đầu (Auto Synchronization)", bold: true, color: COLOR_PRIMARY }),
            new TextRun({ text: " giữa Bán hàng, Kho hàng và Sổ quỹ Kế toán." })
          ]
        }),

        new Paragraph({
          heading: HeadingLevel.HEADING_2,
          spacing: { before: 150, after: 100 },
          children: [
            new TextRun({ text: "2.2. Chi Tiết Các Chức Năng Cốt Lõi (Code-Level Logic)", bold: true, color: COLOR_DARK, size: 23 })
          ]
        }),

        new Paragraph({
          spacing: { before: 100, after: 50 },
          children: [
            new TextRun({ text: "A. Cơ chế Tự Động Đồng Bộ Dữ Liệu (AutoSyncDataAsync)", bold: true, color: COLOR_SECONDARY, size: 22 })
          ]
        }),
        new Paragraph({
          spacing: { after: 100 },
          children: [
            new TextRun({ text: "Mỗi khi truy cập Dashboard Kế toán (AccountingController.Index), hệ thống tự động kích hoạt hàm AutoSyncDataAsync() để cập nhật 2 luồng dữ liệu chính:" })
          ]
        }),
        new Paragraph({
          spacing: { after: 100 },
          children: [
            new TextRun({ text: "1. Đồng bộ Doanh Thu Đơn Hàng Hoàn Thành vào Sổ Quỹ (SoQuy): ", bold: true }),
            new TextRun({ text: "Quét đơn hàng 'Hoàn thành' chưa có trong Sổ Quỹ -> tự động phát sinh Phiếu Thu (PT-xxxxx), nhận diện kênh tự động (TikTok Shop / POS / Online Web)." })
          ]
        }),
        new Paragraph({
          spacing: { after: 150 },
          children: [
            new TextRun({ text: "2. Đồng bộ Nhập Kho vào Công Nợ Nhà Cung Cấp (CongNoNCC): ", bold: true }),
            new TextRun({ text: "Quét phiếu nhập kho 'Đã duyệt' -> tự động tạo PhieuNhap và CongNoNCC với thời hạn thanh toán 30 ngày." })
          ]
        }),

        createCodeBlock(
`// Trích đoạn logic đồng bộ Công nợ NCC từ Giao dịch Kho đã duyệt
var congNo = new CongNoNCC
{
    MaNCC = product.MaNCC,
    MaPhieuNhap = pn.MaPhieuNhap,
    TongTien = tx.QuantityChange * product.GiaNhap,
    DaThanhToan = 0,
    NgayTao = tx.Date,
    HanThanhToan = tx.Date.AddDays(30),
    TrangThai = "ChuaThanhToan"
};
_context.CongNoNCCs.Add(congNo);`
        ),

        new Paragraph({ text: "", spacing: { after: 150 } }),

        new Paragraph({
          spacing: { before: 100, after: 50 },
          children: [
            new TextRun({ text: "B. Sổ Quỹ Thu Chi & Điều Chỉnh Dòng Tiền (Cash Journal & Audit)", bold: true, color: COLOR_SECONDARY, size: 22 })
          ]
        }),
        new Paragraph({
          spacing: { after: 100 },
          children: [
            new TextRun({ text: "• Tổng Thu (TongThu): ", bold: true }),
            new TextRun({ text: "Tổng tiền Phiếu Thu không bị hủy." })
          ]
        }),
        new Paragraph({
          spacing: { after: 100 },
          children: [
            new TextRun({ text: "• Tổng Chi (TongChi): ", bold: true }),
            new TextRun({ text: "Tổng tiền Phiếu Chi không bị hủy." })
          ]
        }),
        new Paragraph({
          spacing: { after: 100 },
          children: [
            new TextRun({ text: "• Số Dư Quỹ (SoDuQuy): ", bold: true }),
            new TextRun({ text: "Số Dư = Tổng Thu - Tổng Chi." })
          ]
        }),
        new Paragraph({
          spacing: { after: 150 },
          children: [
            new TextRun({ text: "• Hủy Phiếu / Void Giao Dịch (VoidTransaction): ", bold: true }),
            new TextRun({ text: "Không xóa cứng trong DB để đảm bảo tính Audit Trail (Vết toán). Chuyển trạng thái sang 'Đã hủy' và ghi nối lý do vào GhiChu." })
          ]
        }),

        new Paragraph({
          spacing: { before: 100, after: 50 },
          children: [
            new TextRun({ text: "C. Báo Cáo Kết Quả Kinh Doanh P&L (Profit & Loss Statement)", bold: true, color: COLOR_SECONDARY, size: 22 })
          ]
        }),

        new Table({
          width: { size: 100, type: WidthType.PERCENTAGE },
          rows: [
            new TableRow({
              children: [
                createHeaderCell("Chỉ số P&L", 30),
                createHeaderCell("Công thức tính toán trong Code C#", 70)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("Doanh Thu Thuần", 30, true),
                createDataCell("Sum(TongTien) các Đơn hàng trạng thái 'Hoàn thành' trong kỳ lọc", 70)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("Giá Vốn Hàng Bán (COGS)", 30, true),
                createDataCell("Sum(ChiTietDonHang.SoLuong * SanPham.GiaNhap)", 70)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("Lợi Nhuận Gộp", 30, true),
                createDataCell("Lợi Nhuận Gộp = Doanh Thu Thuần - Giá Vốn Hàng Bán", 70)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("Chi Phí Nhân Sự (HR)", 30, true),
                createDataCell("Sum(ChamCong.TongGioLam * NhanVien.LuongTheoGio) từ phân hệ Chấm công", 70)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("Chi Phí Vận Hành Khác", 30, true),
                createDataCell("Sum(SoQuy.SoTien) loại Chi (ngoại trừ nhóm 'Trả lương')", 70)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("Lợi Nhuận Ròng (Net Profit)", 30, true, AlignmentType.LEFT, COLOR_BG_LIGHT),
                createDataCell("Lợi Nhuận Ròng = Lợi Nhuận Gộp - (Chi Phí Nhân Sự + Chi Phí Vận Hành Khác)", 70, true, AlignmentType.LEFT, COLOR_BG_LIGHT)
              ]
            })
          ]
        }),

        new Paragraph({ text: "", spacing: { after: 200 } }),

        // SECTION 3 - AI AGENT
        new Paragraph({
          heading: HeadingLevel.HEADING_1,
          spacing: { before: 300, after: 150 },
          children: [
            new TextRun({ text: "3. MODULE 2: TRỢ LÝ AI AUTONOMOUS AGENT (GEMINI API)", bold: true, color: COLOR_PRIMARY, size: 26 })
          ]
        }),

        new Paragraph({
          spacing: { after: 150 },
          children: [
            new TextRun({ text: "Trợ lý AI của NovaTech không chỉ dừng lại ở chatbot trả lời câu hỏi, mà đóng vai trò là một " }),
            new TextRun({ text: "Autonomous Agent", bold: true, color: COLOR_PRIMARY }),
            new TextRun({ text: " có khả năng phân tích dữ liệu kinh doanh real-time và tự động thực thi tác vụ ERP." })
          ]
        }),

        new Table({
          width: { size: 100, type: WidthType.PERCENTAGE },
          rows: [
            new TableRow({
              children: [
                createHeaderCell("Action Type", 30),
                createHeaderCell("Mô Tả Tự Động Hóa", 35),
                createHeaderCell("Hệ Thống & DB Thay Đổi", 35)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("CREATE_PRODUCT_AND_IMPORT", 30, true),
                createDataCell("AI gợi ý SP hot trend & tạo phiếu nhập hàng", 35),
                createDataCell("1. Tạo SanPham mới\n2. Tạo PhieuNhap & ChiTietPhieuNhap\n3. Tạo InventoryTransaction ('Chờ duyệt')\n4. Bắn SystemNotification", 35)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("CREATE_PROMOTION_CAMPAIGN", 30, true),
                createDataCell("Tự động khởi tạo chiến dịch Khuyến mãi", 35),
                createDataCell("1. Thêm Voucher mới vào DB\n2. Bắn SystemNotification toàn hệ thống", 35)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("SEND_VIP_REWARD", 30, true),
                createDataCell("Lọc VIP & gửi email tri ân ưu đãi", 35),
                createDataCell("1. Lọc VIP (Điểm >= 500 hoặc Target Email)\n2. Sinh Voucher VIP\n3. Gửi Email HTML qua MailKit SMTP", 35)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("SEND_PROMO_EMAIL_DONG_PLUS", 30, true),
                createDataCell("Xả hàng tồn bán chậm tự động", 35),
                createDataCell("1. Quét Top 3 SP tồn cao/bán chậm\n2. Sinh Voucher giảm giá\n3. Gửi Email HTML hiển thị Giá gốc vs Giá giảm", 35)
              ]
            })
          ]
        }),

        new Paragraph({ text: "", spacing: { after: 200 } }),

        // SECTION 4 - TIKTOK SHOP
        new Paragraph({
          heading: HeadingLevel.HEADING_1,
          spacing: { before: 300, after: 150 },
          children: [
            new TextRun({ text: "4. MODULE 3 & 4: TÍCH HỢP & QUY TRÌNH XỬ LÝ TIKTOK SHOP", bold: true, color: COLOR_PRIMARY, size: 26 })
          ]
        }),

        new Paragraph({
          spacing: { after: 100 },
          children: [
            new TextRun({ text: "• Phương thức kết nối: ", bold: true }),
            new TextRun({ text: "RESTful HTTP Client kết nối trực tiếp Trình giả lập TikTok Shop (http://localhost:6060/api/stream/orders)." })
          ]
        }),
        new Paragraph({
          spacing: { after: 100 },
          children: [
            new TextRun({ text: "• Chống trùng đơn (Idempotency): ", bold: true }),
            new TextRun({ text: "Đánh dấu tag [TikTokShop#{OrderId}] trong GhiChu. Kiểm tra trùng trước khi khởi tạo." })
          ]
        }),
        new Paragraph({
          spacing: { after: 150 },
          children: [
            new TextRun({ text: "• Xử lý SP không tồn tại: ", bold: true }),
            new TextRun({ text: "Vòng lặp check SKU/ProductId. Nếu SP chưa có ở NovaTech -> tự động Skip và ghi log vào TikTokSyncLog." })
          ]
        }),

        new Table({
          width: { size: 100, type: WidthType.PERCENTAGE },
          rows: [
            new TableRow({
              children: [
                createHeaderCell("TikTok Status (API Source)", 35),
                createHeaderCell("NovaTech Status (Internal)", 30),
                createHeaderCell("Ý Nghĩa Nghiệp Vụ", 35)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("Awaiting Shipment, Pending", 35),
                createDataCell("Chờ duyệt", 30, true),
                createDataCell("Đơn mới tinh, chờ nhân viên xác nhận", 35)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("Paid", 35),
                createDataCell("Đã thanh toán", 30, true),
                createDataCell("Đã thanh toán qua TikTok Pay", 35)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("Ready To Ship", 35),
                createDataCell("Đang đóng gói", 30, true),
                createDataCell("Kho đang đóng gói in phiếu giao hàng", 35)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("Shipped, In Transit", 35),
                createDataCell("Đang giao", 30, true),
                createDataCell("Đã bàn giao cho đơn vị vận chuyển", 35)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("Delivered, Completed, Received", 35),
                createDataCell("Hoàn thành", 30, true),
                createDataCell("Khách đã nhận (Kế toán tự động ghi nhận thu)", 35)
              ]
            }),
            new TableRow({
              children: [
                createDataCell("Cancelled, Canceled", 35),
                createDataCell("Đã hủy", 30, true),
                createDataCell("Đơn bị hủy bởi người mua hoặc sàn", 35)
              ]
            })
          ]
        }),

        new Paragraph({ text: "", spacing: { after: 200 } }),

        // SECTION 5 - DASHBOARD
        new Paragraph({
          heading: HeadingLevel.HEADING_1,
          spacing: { before: 300, after: 150 },
          children: [
            new TextRun({ text: "5. MODULE 5: DASHBOARD (TỔNG QUAN HỆ THỐNG & HR KPIS)", bold: true, color: COLOR_PRIMARY, size: 26 })
          ]
        }),

        new Paragraph({
          spacing: { after: 100 },
          children: [
            new TextRun({ text: "• Business KPIs: ", bold: true }),
            new TextRun({ text: "Tổng Doanh Thu, Doanh Thu Hôm Nay, Tổng Số Đơn, Đơn Chờ Duyệt, Đơn Hoàn Thành, Cảnh Báo Tồn Kho LowStock (<= 3)." })
          ]
        }),
        new Paragraph({
          spacing: { after: 100 },
          children: [
            new TextRun({ text: "• HR KPIs: ", bold: true }),
            new TextRun({ text: "Tổng giờ làm tháng này (TongGioLamThang), Tổng chi phí lương (TongChiPhiLuongThang = Sum(TongGio * LuongTheoGio)), Vinh danh NV làm nhiều giờ nhất." })
          ]
        }),
        new Paragraph({
          spacing: { after: 150 },
          children: [
            new TextRun({ text: "• Top Selling & Charts: ", bold: true }),
            new TextRun({ text: "Top 5 Sản phẩm bán chạy (TopProducts), Top 5 Khách hàng chi tiêu cao nhất, Biểu đồ Chart.js biến động doanh thu 7 ngày (T2 -> CN)." })
          ]
        }),

        new Paragraph({ text: "", spacing: { after: 200 } }),

        // SECTION 6 - Q&A DEFENSE
        new Paragraph({
          heading: HeadingLevel.HEADING_1,
          spacing: { before: 300, after: 150 },
          children: [
            new TextRun({ text: "6. BỘ CÂU HỎI PHẢN BIỆN TỪ HỘI ĐỒNG & KỊCH BẢN TRẢ LỜI 'BAO ĐỖ'", bold: true, color: COLOR_PRIMARY, size: 26 })
          ]
        }),

        // Q1
        new Paragraph({
          spacing: { before: 100, after: 50 },
          children: [
            new TextRun({ text: "❓ Câu 1: Tại sao trong Module Kế toán lại chọn cơ chế tự động đồng bộ gối đầu (AutoSyncDataAsync) thay vì cập nhật trực tiếp ngay khi bấm hoàn thành đơn hàng?", bold: true, color: COLOR_DARK, size: 22 })
          ]
        }),
        createCalloutBox("💡 Kịch bản trả lời bao đỗ:",
"Thưa thầy/cô, việc chọn kiến trúc Event-Driven / Pull Synchronization (AutoSyncDataAsync) mang lại 3 ưu điểm vượt trội:\n" +
"1. Tính độc lập & Giảm Coupling (Loose Coupling): Phân hệ Bán hàng/Kho và Kế toán làm việc độc lập. Nếu phân hệ kế toán có bảo trì hay cập nhật logic, luồng đặt hàng online vẫn diễn ra mượt mà.\n" +
"2. Tính toàn vẹn & Chống sót dữ liệu (Data Integrity): Khi người dùng mở Dashboard Kế toán, hệ thống sẽ rà soát quét toàn bộ đơn hàng và phiếu nhập phát sinh trước đó (bao gồm đơn nhập từ TikTok hay POS) để không bỏ sót giao dịch nào.\n" +
"3. Hiệu năng: Tránh việc phải chèn quá nhiều transaction DB cùng một lúc khi khách hàng bấm mua hàng ở Frontend."
        ),

        new Paragraph({ text: "", spacing: { after: 150 } }),

        // Q2
        new Paragraph({
          spacing: { before: 100, after: 50 },
          children: [
            new TextRun({ text: "❓ Câu 2: Cơ chế tính Giá vốn hàng bán (COGS) và Lợi nhuận P&L trong hệ thống được xử lý như thế nào nếu giá nhập sản phẩm biến động?", bold: true, color: COLOR_DARK, size: 22 })
          ]
        }),
        createCalloutBox("💡 Kịch bản trả lời bao đỗ:",
"Thưa thầy/cô, Giá vốn hàng bán được tính toán chính xác dựa trên công thức COGS = Sum(SoLuong * GiaNhap tại thời điểm lưu của Sản phẩm).\n" +
"Đồng thời, hệ thống NovaTech có lưu vết GiaNiemYetLucNhap và GiaNhap trong bảng ChiTietPhieuNhap. Khi tính P&L, hệ thống bóc tách rõ Doanh thu thuần, Giá vốn, Chi phí lương nhân sự từ bảng Chấm công (ChamCong.TongGioLam * NhanVien.LuongTheoGio) và Chi phí vận hành từ Sổ Quỹ Chi để đưa ra Lợi nhuận ròng (Net Profit) thực tế nhất."
        ),

        new Paragraph({ text: "", spacing: { after: 150 } }),

        // Q3
        new Paragraph({
          spacing: { before: 100, after: 50 },
          children: [
            new TextRun({ text: "❓ Câu 3: Trợ lý AI của em đóng vai trò gì? Nếu Gemini API trả về sai định dạng JSON thì hệ thống xử lý ra sao để không bị crash?", bold: true, color: COLOR_DARK, size: 22 })
          ]
        }),
        createCalloutBox("💡 Kịch bản trả lời bao đỗ:",
"Thưa thầy/cô, Trợ lý AI của NovaTech là một Autonomous Agent. Em bơm Real-time DB Snapshot vào Prompt để AI tự đưa ra quyết định (Action Type) như: nhập hàng hot trend, tạo voucher, gửi email tri ân VIP.\n" +
"Về xử lý lỗi: Em xây dựng cơ chế Fallback 3 lớp trong AiController.cs:\n" +
"1. Dùng Regex làm sạch chuỗi markdown code block ```json ... ```\n" +
"2. JsonSerializer.Deserialize với PropertyNameCaseInsensitive = true.\n" +
"3. Nếu Deserialize thất bại, catch block tự động chuyển sang chế độ trả về tin nhắn văn bản thuần (HasAction = false), giao diện UI luôn mượt mà và không bao giờ bị Crash 500."
        ),

        new Paragraph({ text: "", spacing: { after: 150 } }),

        // Q4
        new Paragraph({
          spacing: { before: 100, after: 50 },
          children: [
            new TextRun({ text: "❓ Câu 4: Khi đồng bộ đơn hàng từ TikTok Shop về NovaTech, em xử lý trường hợp trùng đơn hàng hoặc sản phẩm trong đơn không tồn tại trên hệ thống như thế nào?", bold: true, color: COLOR_DARK, size: 22 })
          ]
        }),
        createCalloutBox("💡 Kịch bản trả lời bao đỗ:",
"Thưa thầy/cô, em đã giải quyết triệt để 2 rủi ro này bằng các thuật toán nghiệp vụ:\n" +
"1. Chống trùng đơn: Mỗi đơn TikTok có mã OrderId duy nhất. Em gán identifier [TikTokShop#{OrderId}] vào trường GhiChu để truy vấn kiểm tra trùng lặp. Đơn trùng chỉ update trạng thái.\n" +
"2. Xử lý SP không tồn tại: Vòng lặp check SKU/ProductId trước khi tạo đơn. Nếu SP chưa có ở NovaTech, hệ thống sẽ Skip đơn đó và ghi log lỗi vào TikTokSyncLog yêu cầu quản trị viên đồng bộ danh mục trước."
        ),

        new Paragraph({ text: "", spacing: { after: 150 } }),

        // Q5
        new Paragraph({
          spacing: { before: 100, after: 50 },
          children: [
            new TextRun({ text: "❓ Câu 5: Quy trình void (hủy) một phiếu thu/chi trong Sổ Quỹ Kế toán hoạt động như thế nào?", bold: true, color: COLOR_DARK, size: 22 })
          ]
        }),
        createCalloutBox("💡 Kịch bản trả lời bao đỗ:",
"Thưa thầy/cô, trong kế toán tài chính, dùng lệnh DELETE xóa bỏ hoàn toàn giao dịch là vi phạm nguyên tắc quản trị. Trong hàm VoidTransaction, em áp dụng cơ chế Soft Cancel & Audit Trail:\n" +
"- Chuyển trạng thái phiếu thành 'Đã hủy'.\n" +
"- Ghi nối thông tin kiểm toán: [HỦY PHIẾU - {Thời gian}] {Lý do hủy} vào ghi chú.\n" +
"- Công thức tính Số dư quỹ tự động loại trừ các phiếu 'Đã hủy'. Đảm bảo tính minh bạch và truy vết dễ dàng."
        ),

        new Paragraph({ text: "", spacing: { after: 300 } }),
        createCalloutBox("🎉 TỰ TIN BẢO VỆ ĐỒ ÁN THÀNH CÔNG RỰC RỠ!", "Chúc bạn thuyết trình bình tĩnh, tự tin và đạt điểm cao nhất trong buổi bảo vệ đồ án NovaTech ngày mai!", false)
      ]
    }
  ]
});

const outputPath = path.join('c:', 'Users', 'Admin', 'Desktop', 'NovaTech', 'KICH_BAN_BAO_VE_NOVATECH.docx');

Packer.toBuffer(doc).then((buffer) => {
  fs.writeFileSync(outputPath, buffer);
  console.log("SUCCESS: Document exported to " + outputPath);
}).catch((err) => {
  console.error("ERROR: Failed to export docx: ", err);
});
