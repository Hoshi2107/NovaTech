import axios from 'axios'

// 1. Initial Mock Database Seed
const initialDb = {
  products: [
    { 
      id: 1, 
      name: "iPhone 15 Pro Max 256GB", 
      sku: "IPHONE15PM-256", 
      barcode: "893123456789", 
      brand: "Apple", 
      category: "Điện thoại", 
      supplier: "Công ty TNHH Apple Việt Nam", 
      importPrice: 28000000, 
      price: 27990000, 
      originalPrice: 32990000,
      discountRate: 15,
      discountExpiry: new Date(Date.now() + 86400000 * 2).toISOString(),
      isBestSeller: true,
      stock: 15,
      specifications: "Màn hình: 6.7 inch Super Retina XDR OLED 120Hz\nChip: Apple A17 Pro 3nm\nRAM: 8GB\nBộ nhớ: 256GB\nCamera: Chính 48MP & 2 phụ 12MP",
      image: "https://images.unsplash.com/photo-1695048133142-1a20484d2569?q=80&w=300",
      status: "Đang bán",
      images: ["https://images.unsplash.com/photo-1695048133142-1a20484d2569?q=80&w=300"]
    },
    { 
      id: 2, 
      name: "MacBook Pro 14 inch M3", 
      sku: "MACPRO-M3", 
      barcode: "893123456790", 
      brand: "Apple", 
      category: "Laptop", 
      supplier: "Nhà phân phối FPT Synnex", 
      importPrice: 34000000, 
      price: 39990000, 
      originalPrice: 0,
      discountRate: 0,
      discountExpiry: null,
      isBestSeller: true,
      stock: 5,
      specifications: "Màn hình: 14.2 inch Liquid Retina XDR 120Hz\nChip: Apple M3 (8-core CPU, 10-core GPU)\nRAM: 8GB\nSSD: 512GB",
      image: "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?q=80&w=300",
      status: "Đang bán",
      images: ["https://images.unsplash.com/photo-1517336714731-489689fd1ca8?q=80&w=300"]
    },
    { 
      id: 3, 
      name: "iPad Pro 11 inch M2", 
      sku: "IPADPRO-M2", 
      barcode: "893123456791", 
      brand: "Apple", 
      category: "Máy tính bảng", 
      supplier: "Công ty TNHH Apple Việt Nam", 
      importPrice: 17500000, 
      price: 20990000, 
      originalPrice: 0,
      discountRate: 0,
      discountExpiry: null,
      isBestSeller: false,
      stock: 8,
      specifications: "Màn hình: 11 inch Liquid Retina ProMotion\nChip: Apple M2\nRAM: 8GB\nBộ nhớ: 128GB\nHỗ trợ S-Pen thế hệ 2",
      image: "https://images.unsplash.com/photo-1589739900243-4b52cd9b104e?q=80&w=300",
      status: "Đang bán",
      images: ["https://images.unsplash.com/photo-1589739900243-4b52cd9b104e?q=80&w=300"]
    },
    { 
      id: 4, 
      name: "Tai nghe không dây Sony WH-1000XM5", 
      sku: "SONY-WH1000XM5", 
      barcode: "893123456792", 
      brand: "Sony", 
      category: "Tai nghe", 
      supplier: "Nhà phân phối FPT Synnex", 
      importPrice: 5200000, 
      price: 6990000, 
      originalPrice: 7990000,
      discountRate: 12,
      discountExpiry: new Date(Date.now() + 86400000 * 5).toISOString(),
      isBestSeller: true,
      stock: 12,
      specifications: "Kiểu tai nghe: Chụp tai (Over-ear)\nKết nối: Bluetooth 5.2, Jack 3.5mm\nChống ồn: Active Noise Cancelling thông minh\nPin: Lên đến 30 giờ",
      image: "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?q=80&w=300",
      status: "Đang bán",
      images: ["https://images.unsplash.com/photo-1505740420928-5e560c06d30e?q=80&w=300"]
    },
    { 
      id: 5, 
      name: "Bàn phím cơ không dây Logitech MX Keys", 
      sku: "LOGI-MXKEYS", 
      barcode: "893123456793", 
      brand: "Logitech", 
      category: "Phụ kiện", 
      supplier: "Nhà phân phối FPT Synnex", 
      importPrice: 2100000, 
      price: 2990000, 
      originalPrice: 0,
      discountRate: 0,
      discountExpiry: null,
      isBestSeller: false,
      stock: 20,
      specifications: "Kết nối: Bluetooth & Logi Bolt\nĐèn nền: Tự động điều chỉnh theo môi trường\nThời lượng pin: 10 ngày hoặc lên tới 5 tháng không bật đèn nền",
      image: "https://images.unsplash.com/photo-1587829741301-dc798b83add3?q=80&w=300",
      status: "Đang bán",
      images: ["https://images.unsplash.com/photo-1587829741301-dc798b83add3?q=80&w=300"]
    },
    { 
      id: 6, 
      name: "Máy tính bảng Samsung Galaxy Tab S9 Ultra", 
      sku: "GALAXY-S9U", 
      barcode: "893123456794", 
      brand: "Samsung", 
      category: "Máy tính bảng", 
      supplier: "Nhà phân phối FPT Synnex", 
      importPrice: 16500000, 
      price: 21990000, 
      originalPrice: 0,
      discountRate: 0,
      discountExpiry: null,
      isBestSeller: false,
      stock: 4,
      specifications: "Màn hình: 14.6 inch Dynamic AMOLED 2X 120Hz\nChip: Snapdragon 8 Gen 2 for Galaxy\nRAM: 12GB\nBộ nhớ: 256GB\nKèm bút S-Pen cao cấp", 
      image: "https://images.unsplash.com/photo-1589739900243-4b52cd9b104e?q=80&w=300",
      status: "Đang bán",
      images: ["https://images.unsplash.com/photo-1589739900243-4b52cd9b104e?q=80&w=300"]
    },
    { 
      id: 7, 
      name: "Loa Bluetooth di động Sony SRS-XE200", 
      sku: "SONY-SRS-XE200", 
      barcode: "893123456795", 
      brand: "Sony", 
      category: "Loa", 
      supplier: "Nhà phân phối FPT Synnex", 
      importPrice: 1800000, 
      price: 2490000, 
      originalPrice: 2990000,
      discountRate: 16,
      discountExpiry: new Date(Date.now() + 86400000 * 1.5).toISOString(),
      isBestSeller: false,
      stock: 20, 
      specifications: "Âm thanh: Line-Shape Diffuser cho âm thanh rộng\nKháng nước/bụi: IP67\nThời lượng pin: Lên đến 16 giờ", 
      image: "https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?q=80&w=300",
      status: "Đang bán",
      images: ["https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?q=80&w=300"]
    }
  ],
  categories: [
    { id: 1, name: "Laptop", description: "Laptop văn phòng, Gaming, Đồ họa", productCount: 1 },
    { id: 2, name: "Điện thoại", description: "Smartphone chính hãng", productCount: 1 },
    { id: 3, name: "Phụ kiện", description: "Bàn phím, chuột, tai nghe", productCount: 1 },
    { id: 4, name: "Máy tính bảng", description: "Máy tính bảng iPad, Android", productCount: 2 },
    { id: 5, name: "Loa", description: "Loa Bluetooth, âm thanh", productCount: 1 },
    { id: 6, name: "Tai nghe", description: "Tai nghe chống ồn, thể thao", productCount: 1 }
  ],
  brands: [
    { id: 1, name: "Apple", description: "Thương hiệu cao cấp từ Mỹ", productCount: 3 },
    { id: 2, name: "Asus", description: "Laptop gaming & linh kiện", productCount: 0 },
    { id: 3, name: "Sony", description: "Thiết bị âm thanh chính hãng", productCount: 2 },
    { id: 4, name: "Samsung", description: "Tập đoàn công nghệ Hàn Quốc", productCount: 1 },
    { id: 5, name: "Logitech", description: "Thiết bị ngoại vi Thụy Sĩ", productCount: 1 }
  ],
  suppliers: [
    { id: 1, name: "Công ty TNHH Apple Việt Nam", code: "SUP-APL", phone: "028 3910 1818", email: "contact@apple.com.vn", address: "Q.1, TP.HCM" },
    { id: 2, name: "Nhà phân phối FPT Synnex", code: "SUP-FPT", phone: "024 7300 6666", email: "fpt.synnex@fpt.com.vn", address: "Cầu Giấy, Hà Nội" }
  ],
  employees: [
    { id: 1, fullName: "Nguyễn Văn Admin", email: "admin@novatech.vn", phone: "0901234567", password: "123", status: "Đang làm việc", roles: ["Super Admin"], joinedDate: new Date(2024, 5, 20).toISOString() },
    { id: 2, fullName: "Trần Thị Manager", email: "manager@novatech.vn", phone: "0907654321", password: "123", status: "Đang làm việc", roles: ["Quản lý cửa hàng"], joinedDate: new Date(2025, 1, 10).toISOString() },
    { id: 3, fullName: "Lê Văn Sale", email: "sale@novatech.vn", phone: "0912345678", password: "123", status: "Đang làm việc", roles: ["Nhân viên bán hàng"], joinedDate: new Date(2025, 8, 15).toISOString() },
    { id: 4, fullName: "Phạm Văn Kho", email: "kho@novatech.vn", phone: "0987654321", password: "123", status: "Đang làm việc", roles: ["Nhân viên kho"], joinedDate: new Date(2025, 10, 1).toISOString() },
    { id: 5, fullName: "Đỗ Thị Kế Toán", email: "ketoan@novatech.vn", phone: "0977665544", password: "123", status: "Đang làm việc", roles: ["Kế toán"], joinedDate: new Date(2025, 3, 20).toISOString() },
    { id: 6, fullName: "Hoàng Văn Marketing", email: "marketing@novatech.vn", phone: "0944556677", password: "123", status: "Đang làm việc", roles: ["Marketing"], joinedDate: new Date(2025, 6, 12).toISOString() }
  ],
  roles: [
    { roleName: "Super Admin", permissions: ["View_Dashboard", "View_Product", "Create_Product", "Edit_Product", "Delete_Product", "View_Inventory", "Import_Inventory", "Export_Inventory", "Inventory_Inventory", "View_Order", "Create_Order", "Edit_Order", "Delete_Order", "Approve_Order", "Export_Order", "View_Customer", "Create_Customer", "Edit_Customer", "Delete_Customer", "View_Employee", "Create_Employee", "Edit_Employee", "Delete_Employee", "View_Role", "Create_Role", "Edit_Role", "Delete_Role", "Assign_Role", "View_Setting", "Edit_Setting", "View_TikTok", "Sync_TikTok", "View_Report", "View_AI", "View_Promotion"] },
    { roleName: "Quản lý cửa hàng", permissions: ["View_Dashboard", "View_Product", "Create_Product", "Edit_Product", "Delete_Product", "View_Inventory", "Import_Inventory", "Export_Inventory", "Inventory_Inventory", "View_Order", "Create_Order", "Edit_Order", "Delete_Order", "Approve_Order", "Export_Order", "View_Customer", "Create_Customer", "Edit_Customer", "Delete_Customer", "View_Employee", "View_TikTok", "Sync_TikTok", "View_Report", "View_AI", "View_Promotion"] },
    { roleName: "Nhân viên bán hàng", permissions: ["View_Dashboard", "View_Product", "View_Order", "Create_Order", "Edit_Order", "View_Customer", "Create_Customer", "Edit_Customer", "View_AI"] },
    { roleName: "Nhân viên kho", permissions: ["View_Dashboard", "View_Product", "View_Inventory", "Import_Inventory", "Export_Inventory", "Inventory_Inventory"] },
    { roleName: "Kế toán", permissions: ["View_Dashboard", "View_Order", "Export_Order", "View_Report"] },
    { roleName: "Marketing", permissions: ["View_Dashboard", "View_Product", "View_TikTok", "Sync_TikTok", "View_Promotion", "View_Report"] }
  ],
  customers: [
    { id: 1, name: "Trần Minh Quân", phone: "0909876543", email: "minhquan@gmail.com", address: "Quận 1, TP.HCM", points: 1200, membershipRank: "Vàng", createdDate: new Date(2025, 4, 15).toISOString() },
    { id: 2, name: "Lê Hồng Thắm", phone: "0918765432", email: "hongtham@gmail.com", address: "Quận Bình Thạnh, TP.HCM", points: 450, membershipRank: "Bạc", createdDate: new Date(2025, 8, 10).toISOString() },
    { id: 3, name: "Nguyễn Hữu Khang", phone: "0923456789", email: "huukhang@gmail.com", address: "Quận 7, TP.HCM", points: 3200, membershipRank: "Kim Cương", createdDate: new Date(2024, 10, 5).toISOString() }
  ],
  customerInboxThreads: [
    {
      id: 1,
      customerId: 1,
      customerName: "Tran Minh Quan",
      customerPhone: "0909876543",
      channel: "Store",
      subject: "Warranty question for iPhone 15",
      status: "Unread",
      priority: "High",
      updatedAt: new Date(Date.now() - 3600000).toISOString(),
      messages: [
        { id: 1, sender: "customer", text: "Hi, how long is the warranty for the iPhone I bought last week?", timestamp: new Date(Date.now() - 7200000).toISOString(), isRead: false }
      ]
    },
    {
      id: 2,
      customerId: 2,
      customerName: "Le Hong Tham",
      customerPhone: "0918765432",
      channel: "Store",
      subject: "Can I change color?",
      status: "Processing",
      priority: "Medium",
      updatedAt: new Date(Date.now() - 86400000).toISOString(),
      messages: [
        { id: 1, sender: "customer", text: "Can I exchange my MacBook for another color?", timestamp: new Date(Date.now() - 86400000).toISOString(), isRead: true },
        { id: 2, sender: "staff", text: "We are checking the exchange policy for you.", timestamp: new Date(Date.now() - 86000000).toISOString(), isRead: true }
      ]
    },
    {
      id: 3,
      customerId: 3,
      customerName: "Nguyen Huu Khang",
      customerPhone: "0923456789",
      channel: "Store",
      subject: "Accessory bundle question",
      status: "Replied",
      priority: "Low",
      updatedAt: new Date(Date.now() - 172800000).toISOString(),
      messages: [
        { id: 1, sender: "customer", text: "Any keyboard + mouse bundle discount available?", timestamp: new Date(Date.now() - 172800000).toISOString(), isRead: true },
        { id: 2, sender: "staff", text: "Yes, we sent 2 suitable bundles to your email.", timestamp: new Date(Date.now() - 171500000).toISOString(), isRead: true }
      ]
    }
  ],
  orders: [
    {
      id: 1,
      orderCode: "ORD-260601001",
      customerName: "Trần Minh Quân",
      customerPhone: "0909876543",
      customerAddress: "Quận 1, TP.HCM",
      orderDate: new Date(Date.now() - 86400000 * 5).toISOString(),
      status: "Hoàn thành",
      channel: "Website",
      paymentMethod: "COD",
      note: "Giao giờ hành chính",
      items: [
        { productId: 1, productName: "iPhone 15 Pro Max 256GB", sku: "IPHONE15PM-256", image: "https://images.unsplash.com/photo-1695048133142-1a20484d2569?q=80&w=300", quantity: 1, price: 27990000 }
      ],
      discount: 0,
      subTotal: 27990000,
      total: 27990000
    },
    {
      id: 2,
      orderCode: "ORD-260602002",
      customerName: "Khách Hàng Vãng Lai",
      customerPhone: "0987654321",
      customerAddress: "Mua trực tiếp tại cửa hàng",
      orderDate: new Date(Date.now() - 86400000 * 2).toISOString(),
      status: "Hoàn thành",
      channel: "Cửa hàng",
      paymentMethod: "Tiền mặt",
      items: [
        { productId: 4, productName: "Tai nghe không dây Sony WH-1000XM5", sku: "SONY-WH1000XM5", image: "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?q=80&w=300", quantity: 2, price: 6990000 }
      ],
      discount: 0,
      subTotal: 13980000,
      total: 13980000
    }
  ],
  transactions: [
    { id: 1, code: "GRN-2606010001", type: "Nhập kho", productSku: "IPHONE15PM-256", productName: "iPhone 15 Pro Max 256GB", quantityChange: 20, creator: "Phạm Văn Kho", date: new Date(2026, 5, 1).toISOString(), note: "Nhập hàng đợt đầu tháng" }
  ],
  vouchers: [
    { id: 1, code: "NOVATECH10", value: 10, minOrderValue: 500000, startDate: new Date().toISOString(), endDate: new Date(Date.now() + 86400000 * 30).toISOString(), status: "Đang diễn ra" }
  ],
  tiktokConfig: {
    isConnected: false,
    shopName: "",
    shopId: "",
    syncStatus: "Chưa kết nối",
    lastSyncTime: null
  },
  tiktokLogs: [],
  notifications: [
    { id: 1, title: "Đồng bộ TikTok Shop", message: "Kết nối của bạn tới TikTok Shop tạm thời chưa được kích hoạt.", type: "Đồng bộ lỗi", timestamp: new Date().toISOString(), isRead: false }
  ],
  chatHistory: [],
  settings: {
    shopName: "Cửa hàng Công nghệ NovaTech",
    shopLogo: "https://images.unsplash.com/photo-1531297484001-80022131f5a1?q=80&w=300",
    shopAddress: "123 Đường Ba Tháng Hai, Quận 10, TP. Hồ Chí Minh",
    shopEmail: "contact@novatech.vn",
    shopHotline: "1900 8198",
    systemConfig: "Mở cửa: 8:00 - 22:00 hằng ngày"
  }
};

// 2. Helper to load and save data from/to localStorage
const normalizeDb = (db = {}) => {
  const normalized = { ...initialDb, ...db };

  Object.keys(initialDb).forEach((key) => {
    const defaultValue = initialDb[key];
    const currentValue = normalized[key];

    if (Array.isArray(defaultValue)) {
      normalized[key] = Array.isArray(currentValue) ? currentValue : [...defaultValue];
      return;
    }

    if (defaultValue && typeof defaultValue === 'object') {
      normalized[key] =
        currentValue && typeof currentValue === 'object' && !Array.isArray(currentValue)
          ? { ...defaultValue, ...currentValue }
          : { ...defaultValue };
    }
  });

  return normalized;
};

const getDb = () => {
  try {
    const data = localStorage.getItem('novatech_db');
    const parsedDb = data ? JSON.parse(data) : {};
    const normalizedDb = normalizeDb(parsedDb);

    localStorage.setItem('novatech_db', JSON.stringify(normalizedDb));
    return normalizedDb;
  } catch (err) {
    console.error('Cannot load NovaTech mock database:', err);

    const normalizedDb = normalizeDb();
    localStorage.setItem('novatech_db', JSON.stringify(normalizedDb));
    return normalizedDb;
  }
};

const saveDb = (db) => {
  localStorage.setItem('novatech_db', JSON.stringify(normalizeDb(db)));
};

const getSession = () => {
  const sess = localStorage.getItem('novatech_session');
  return sess ? JSON.parse(sess) : null;
};

const saveSession = (sess) => {
  if (sess) {
    localStorage.setItem('novatech_session', JSON.stringify(sess));
  } else {
    localStorage.removeItem('novatech_session');
  }
};

// 3. Request router to handle mock responses
const handleMockRequest = async (endpoint, method, body, config) => {
  const db = getDb();
  let status = 200;
  let data = null;

  try {
    if (endpoint === 'GetSession' && method === 'GET') {
      const sess = getSession();
      if (!sess) {
        status = 401;
        data = { message: "Chưa đăng nhập phiên làm việc." };
      } else {
        data = sess;
      }
    } 
    else if (endpoint === 'Login' && method === 'POST') {
      const { email, password } = body || {};
      const emp = db.employees.find(e => e.email.toLowerCase() === email.toLowerCase() && e.password === password);
      
      if (emp) {
        if (emp.status !== "Đang làm việc") {
          status = 400;
          data = { message: "Tài khoản của bạn đang bị khóa." };
        } else {
          let permissions = [];
          emp.roles.forEach(r => {
            const roleObj = db.roles.find(rp => rp.roleName === r);
            if (roleObj) {
              permissions = [...permissions, ...roleObj.permissions];
            }
          });
          permissions = [...new Set(permissions)];

          const userSession = {
            fullName: emp.fullName,
            email: emp.email,
            roles: emp.roles,
            permissions: permissions,
            avatar: emp.fullName.substring(0, 1).toUpperCase()
          };
          saveSession(userSession);
          data = userSession;
        }
      } 
      else if (email.toLowerCase() === 'customer@gmail.com' && password === '123') {
        const userSession = {
          fullName: "Khách Hàng Demo",
          email: "customer@gmail.com",
          roles: ["Khách hàng"],
          permissions: [],
          avatar: "K"
        };
        saveSession(userSession);
        data = userSession;
      } 
      else {
        status = 400;
        data = { message: "Tài khoản hoặc mật khẩu không chính xác." };
      }
    } 
    else if (endpoint === 'Logout' && method === 'POST') {
      saveSession(null);
      data = { message: "Đã đăng xuất thành công." };
    } 
    else if (endpoint === 'ChangePassword' && method === 'POST') {
      const sess = getSession();
      if (!sess) {
        status = 401;
        data = { message: "Chưa đăng nhập." };
      } else {
        const emp = db.employees.find(e => e.email === sess.email);
        if (emp) {
          if (emp.password !== body.oldPassword) {
            status = 400;
            data = { message: "Mật khẩu cũ không chính xác." };
          } else {
            emp.password = body.newPassword;
            saveDb(db);
            data = { message: "Đổi mật khẩu thành công!" };
          }
        } else {
          status = 400;
          data = { message: "Không thể đổi mật khẩu cho tài khoản này." };
        }
      }
    } 
    else if (endpoint === 'Profile' && method === 'GET') {
      const sess = getSession();
      if (!sess) {
        status = 401;
        data = { message: "Chưa đăng nhập." };
      } else {
        const emp = db.employees.find(e => e.email === sess.email);
        if (emp) {
          data = {
            fullName: emp.fullName,
            email: emp.email,
            phone: emp.phone,
            roles: emp.roles,
            joinedDate: emp.joinedDate
          };
        } else {
          data = {
            fullName: sess.fullName,
            email: sess.email,
            phone: "0900000000",
            roles: sess.roles
          };
        }
      }
    } 
    else if (endpoint === 'GetDashboard' && method === 'GET') {
      const completedOrders = db.orders.filter(o => o.status === "Hoàn thành");
      const totalRevenue = completedOrders.reduce((sum, o) => sum + o.total, 0);
      const totalOrders = db.orders.length;
      const pendingOrders = db.orders.filter(o => o.status === "Đơn mới" || o.status === "Đã xác nhận").length;
      const totalProducts = db.products.length;
      const lowStockProducts = db.products.filter(p => p.stock <= 3);
      const recentNotifications = db.notifications;
      const topProducts = db.products.filter(p => p.isBestSeller);

      data = {
        totalRevenue,
        totalOrders,
        pendingOrders,
        totalProducts,
        lowStockProducts,
        recentNotifications,
        topProducts,
        customers: db.customers
      };
    } 
    else if (endpoint.startsWith('GetProducts') && method === 'GET') {
      const urlObj = new URL(config.url, 'http://localhost');
      const search = urlObj.searchParams.get('search');
      const category = urlObj.searchParams.get('category');
      const brand = urlObj.searchParams.get('brand');

      let list = [...db.products];
      if (search) {
        list = list.filter(p => p.name.toLowerCase().includes(search.toLowerCase()) || p.sku.toLowerCase().includes(search.toLowerCase()));
      }
      if (category) {
        list = list.filter(p => p.category === category);
      }
      if (brand) {
        list = list.filter(p => p.brand === brand);
      }
      data = list;
    } 
    else if (endpoint === 'CreateProduct' && method === 'POST') {
      const p = body;
      p.id = db.products.length > 0 ? Math.max(...db.products.map(prod => prod.id)) + 1 : 1;
      p.image = p.image || "https://images.unsplash.com/photo-1531297484001-80022131f5a1?q=80&w=300";
      p.images = [p.image];
      p.originalPrice = p.price;
      p.discountRate = 0;
      p.discountExpiry = null;
      db.products.push(p);
      saveDb(db);
      data = { message: "Thêm sản phẩm thành công!", product: p };
    } 
    else if (endpoint === 'EditProduct' && method === 'POST') {
      const p = body;
      const target = db.products.find(prod => prod.id === p.id);
      if (!target) {
        status = 404;
        data = { message: "Không tìm thấy sản phẩm." };
      } else {
        Object.assign(target, p);
        saveDb(db);
        data = { message: "Cập nhật sản phẩm thành công!", product: target };
      }
    } 
    else if (endpoint === 'DeleteProduct' && method === 'POST') {
      const { id } = body;
      const idx = db.products.findIndex(p => p.id === id);
      if (idx === -1) {
        status = 404;
        data = { message: "Không tìm thấy sản phẩm." };
      } else {
        db.products.splice(idx, 1);
        saveDb(db);
        data = { message: "Đã xóa sản phẩm thành công." };
      }
    } 
    else if (endpoint === 'UpdateDiscount' && method === 'POST') {
      const { id, discountPrice, hours } = body;
      const target = db.products.find(p => p.id === id);
      if (!target) {
        status = 404;
        data = { message: "Không tìm thấy sản phẩm." };
      } else {
        if (discountPrice > 0 && hours > 0) {
          if (!target.originalPrice) {
            target.originalPrice = target.price;
          }
          target.price = discountPrice;
          target.discountExpiry = new Date(Date.now() + hours * 3600000).toISOString();
          target.discountRate = Math.round((1 - (discountPrice / target.originalPrice)) * 100);
        } else {
          if (target.originalPrice > 0) {
            target.price = target.originalPrice;
            target.originalPrice = 0;
          }
          target.discountExpiry = null;
          target.discountRate = 0;
        }
        saveDb(db);
        data = { message: "Cập nhật giảm giá thành công!", product: target };
      }
    } 
    else if (endpoint === 'GetCategories' && method === 'GET') {
      data = db.categories;
    } 
    else if (endpoint === 'CreateCategory' && method === 'POST') {
      const c = body;
      c.id = db.categories.length > 0 ? Math.max(...db.categories.map(cat => cat.id)) + 1 : 1;
      c.productCount = 0;
      db.categories.push(c);
      saveDb(db);
      data = { message: "Thêm danh mục thành công!", category: c };
    } 
    else if (endpoint === 'GetBrands' && method === 'GET') {
      data = db.brands;
    } 
    else if (endpoint === 'CreateBrand' && method === 'POST') {
      const b = body;
      b.id = db.brands.length > 0 ? Math.max(...db.brands.map(br => br.id)) + 1 : 1;
      b.productCount = 0;
      db.brands.push(b);
      saveDb(db);
      data = { message: "Thêm thương hiệu thành công!", brand: b };
    } 
    else if (endpoint === 'GetSuppliers' && method === 'GET') {
      data = db.suppliers;
    } 
    else if (endpoint === 'CreateSupplier' && method === 'POST') {
      const s = body;
      s.id = db.suppliers.length > 0 ? Math.max(...db.suppliers.map(sup => sup.id)) + 1 : 1;
      db.suppliers.push(s);
      saveDb(db);
      data = { message: "Thêm nhà cung cấp thành công!", supplier: s };
    } 
    else if (endpoint === 'GetInventoryTransactions' && method === 'GET') {
      data = [...db.transactions].sort((a, b) => new Date(b.date) - new Date(a.date));
    } 
    else if (endpoint === 'ImportStock' && method === 'POST') {
      const { productSKU, quantity, note } = body;
      const p = db.products.find(prod => prod.sku === productSKU);
      if (!p) {
        status = 404;
        data = { message: "Không tìm thấy sản phẩm có SKU tương ứng." };
      } else {
        p.stock += quantity;
        const trans = {
          id: db.transactions.length > 0 ? Math.max(...db.transactions.map(t => t.id)) + 1 : 1,
          code: "GRN-" + Date.now(),
          type: "Nhập kho",
          productSku: productSKU,
          productName: p.name,
          quantityChange: quantity,
          creator: getSession()?.fullName || "Hệ thống (Mock)",
          date: new Date().toISOString(),
          note: note
        };
        db.transactions.push(trans);
        saveDb(db);
        data = { message: "Nhập kho thành công!", product: p, transaction: trans };
      }
    } 
    else if (endpoint === 'ExportStock' && method === 'POST') {
      const { productSKU, quantity, note } = body;
      const p = db.products.find(prod => prod.sku === productSKU);
      if (!p) {
        status = 404;
        data = { message: "Không tìm thấy sản phẩm." };
      } else if (p.stock < quantity) {
        status = 400;
        data = { message: "Tồn kho không đủ để xuất!" };
      } else {
        p.stock -= quantity;
        const trans = {
          id: db.transactions.length > 0 ? Math.max(...db.transactions.map(t => t.id)) + 1 : 1,
          code: "GIN-" + Date.now(),
          type: "Xuất kho",
          productSku: productSKU,
          productName: p.name,
          quantityChange: -quantity,
          creator: getSession()?.fullName || "Hệ thống (Mock)",
          date: new Date().toISOString(),
          note: note
        };
        db.transactions.push(trans);
        saveDb(db);
        data = { message: "Xuất kho thành công!", product: p, transaction: trans };
      }
    } 
    else if (endpoint === 'GetOrders' && method === 'GET') {
      data = [...db.orders].sort((a, b) => new Date(b.orderDate) - new Date(a.orderDate));
    } 
    else if (endpoint.startsWith('GetOrderDetail') && method === 'GET') {
      const urlObj = new URL(config.url, 'http://localhost');
      const id = parseInt(urlObj.searchParams.get('id'));
      const order = db.orders.find(o => o.id === id);
      if (!order) {
        status = 404;
        data = { message: "Không tìm thấy đơn hàng." };
      } else {
        data = order;
      }
    } 
    else if (endpoint === 'CreateOrderPOS' && method === 'POST') {
      const { productIds, quantities, customerName, customerPhone, paymentMethod } = body;
      if (!productIds || productIds.length === 0) {
        status = 400;
        data = { message: "Giỏ hàng rỗng." };
      } else {
        const orderItems = [];
        let errorStock = false;
        let errorName = "";

        for (let i = 0; i < productIds.length; i++) {
          const pid = productIds[i];
          const qty = quantities[i];
          const prod = db.products.find(p => p.id === pid);
          if (prod) {
            if (prod.stock < qty) {
              errorStock = true;
              errorName = prod.name;
              break;
            }
            prod.stock -= qty;
            orderItems.push({
              productId: prod.id,
              productName: prod.name,
              sku: prod.sku,
              image: prod.image,
              quantity: qty,
              price: prod.price
            });
          }
        }

        if (errorStock) {
          status = 400;
          data = { message: `Sản phẩm '${errorName}' không đủ tồn kho.` };
        } else {
          const subTotal = orderItems.reduce((sum, item) => sum + (item.price * item.quantity), 0);
          const newOrder = {
            id: db.orders.length > 0 ? Math.max(...db.orders.map(o => o.id)) + 1 : 1,
            orderCode: "ORD-POS-" + Date.now().toString().substring(6),
            customerName: customerName || "Khách Hàng Vãng Lai",
            customerPhone: customerPhone || "",
            customerAddress: "Mua trực tiếp tại cửa hàng",
            orderDate: new Date().toISOString(),
            status: "Hoàn thành",
            channel: "Cửa hàng",
            paymentMethod: paymentMethod || "Tiền mặt",
            items: orderItems,
            discount: 0,
            subTotal: subTotal,
            total: subTotal
          };
          db.orders.push(newOrder);

          db.notifications.unshift({
            id: db.notifications.length > 0 ? Math.max(...db.notifications.map(n => n.id)) + 1 : 1,
            title: "Đơn hàng POS mới",
            message: `Đơn hàng ${newOrder.orderCode} vừa thanh toán thành công ${newOrder.total.toLocaleString()} đ.`,
            type: "Đơn mới",
            timestamp: new Date().toISOString(),
            isRead: false
          });

          saveDb(db);
          data = { message: "Đơn hàng POS đã được xác nhận & in hóa đơn thành công!", order: newOrder };
        }
      }
    } 
    else if (endpoint === 'CreateOrderOnline' && method === 'POST') {
      const { customerName, customerPhone, customerAddress, paymentMethod, note, items } = body;
      if (!items || items.length === 0) {
        status = 400;
        data = { message: "Giỏ hàng trống." };
      } else {
        const orderItems = [];
        let stockError = false;
        
        for (const item of items) {
          const prod = db.products.find(p => p.id === item.productId);
          if (prod) {
            if (prod.stock < item.quantity) {
              stockError = true;
              break;
            }
            prod.stock -= item.quantity;
            orderItems.push({
              productId: prod.id,
              productName: prod.name,
              sku: prod.sku,
              image: prod.image,
              quantity: item.quantity,
              price: prod.price
            });
          }
        }

        if (stockError) {
          status = 400;
          data = { message: "Có sản phẩm không đủ số lượng tồn kho." };
        } else {
          const subTotal = orderItems.reduce((sum, i) => sum + (i.price * i.quantity), 0);
          const newOrder = {
            id: db.orders.length > 0 ? Math.max(...db.orders.map(o => o.id)) + 1 : 1,
            orderCode: "ORD-ONL-" + Date.now().toString().substring(5),
            customerName: customerName,
            customerPhone: customerPhone,
            customerAddress: customerAddress,
            orderDate: new Date().toISOString(),
            status: "Đơn mới",
            channel: "Website",
            paymentMethod: paymentMethod || "COD",
            items: orderItems,
            discount: 0,
            subTotal: subTotal,
            total: subTotal,
            note: note || ""
          };
          db.orders.push(newOrder);

          db.notifications.unshift({
            id: db.notifications.length > 0 ? Math.max(...db.notifications.map(n => n.id)) + 1 : 1,
            title: "Đơn Online mới",
            message: `Đơn hàng trực tuyến ${newOrder.orderCode} vừa đặt hàng thành công.`,
            type: "Đơn mới",
            timestamp: new Date().toISOString(),
            isRead: false
          });

          saveDb(db);
          data = { message: "Đặt hàng trực tuyến thành công!", order: newOrder };
        }
      }
    } 
    else if (endpoint === 'UpdateOrderStatus' && method === 'POST') {
      const { id, status: newStatus } = body;
      const target = db.orders.find(o => o.id === id);
      if (!target) {
        status = 404;
        data = { message: "Không tìm thấy đơn hàng." };
      } else {
        target.status = newStatus;
        saveDb(db);
        data = { message: "Cập nhật trạng thái đơn hàng thành công!", order: target };
      }
    } 
    else if (endpoint === 'GetCustomers' && method === 'GET') {
      data = db.customers;
    } 
    else if (endpoint === 'CreateCustomer' && method === 'POST') {
      const c = body;
      c.id = db.customers.length > 0 ? Math.max(...db.customers.map(cust => cust.id)) + 1 : 1;
      c.createdDate = new Date().toISOString();
      c.points = 0;
      c.membershipRank = "Đồng";
      db.customers.push(c);
      saveDb(db);
      data = { message: "Thêm khách hàng thành công!", customer: c };
    } 
    else if (endpoint === 'GetPromotions' && method === 'GET') {
      data = db.vouchers;
    } 
    else if (endpoint === 'CreateVoucher' && method === 'POST') {
      const v = body;
      v.id = db.vouchers.length > 0 ? Math.max(...db.vouchers.map(voc => voc.id)) + 1 : 1;
      v.status = "Đang diễn ra";
      db.vouchers.push(v);
      saveDb(db);
      data = { message: "Tạo mã khuyến mãi thành công!", voucher: v };
    } 
    else if (endpoint === 'GetTiktokConfig' && method === 'GET') {
      data = db.tiktokConfig;
    } 
    else if (endpoint === 'SaveTiktokConfig' && method === 'POST') {
      const cfg = body;
      db.tiktokConfig.isConnected = cfg.isConnected;
      db.tiktokConfig.shopName = cfg.shopName;
      db.tiktokConfig.shopId = cfg.shopId;
      db.tiktokConfig.syncStatus = cfg.isConnected ? "Bình thường" : "Chưa kết nối";
      db.tiktokConfig.lastSyncTime = new Date().toISOString();
      saveDb(db);
      data = { message: "Lưu cấu hình TikTok Shop thành công!", config: db.tiktokConfig };
    } 
    else if (endpoint === 'SyncTiktok' && method === 'POST') {
      if (!db.tiktokConfig.isConnected) {
        status = 400;
        data = { message: "Vui lòng kết nối TikTok Shop trước!" };
      } else {
        db.tiktokConfig.lastSyncTime = new Date().toISOString();
        const log = {
          id: db.tiktokLogs.length > 0 ? Math.max(...db.tiktokLogs.map(l => l.id)) + 1 : 1,
          type: "Đồng bộ",
          message: `Đồng bộ thành công ${db.products.length} sản phẩm và tồn kho lên TikTok Shop.`,
          status: "Thành công",
          timestamp: new Date().toISOString()
        };
        db.tiktokLogs.unshift(log);
        saveDb(db);
        data = { message: "Đồng bộ TikTok Shop thành công!", lastSyncTime: db.tiktokConfig.lastSyncTime, log: log };
      }
    } 
    else if (endpoint === 'GetTiktokLogs' && method === 'GET') {
      data = db.tiktokLogs;
    } 
    else if (endpoint === 'GetCustomerInbox' && method === 'GET') {
      db.customerInboxThreads = Array.isArray(db.customerInboxThreads) ? db.customerInboxThreads : [];

      const threads = db.customerInboxThreads.map(thread => {
        const messages = Array.isArray(thread.messages) ? thread.messages : [];

        return {
          id: thread.id,
          customerId: thread.customerId,
          customerName: thread.customerName,
          customerPhone: thread.customerPhone,
          channel: thread.channel,
          subject: thread.subject,
          status: thread.status,
          priority: thread.priority,
          updatedAt: thread.updatedAt,
          unreadCount: messages.filter(message => message.sender === 'customer' && !message.isRead).length,
          lastMessage: messages[messages.length - 1]?.text || ''
        };
      }).sort((a, b) => new Date(b.updatedAt) - new Date(a.updatedAt));

      data = threads;
    }
    else if (endpoint === 'GetCustomerThread' && method === 'GET') {
      db.customerInboxThreads = Array.isArray(db.customerInboxThreads) ? db.customerInboxThreads : [];

      const urlObj = new URL(config.url, 'http://localhost');
      const id = Number(urlObj.searchParams.get('id'));

      const thread = db.customerInboxThreads.find(item => Number(item.id) === id);

      if (!thread) {
        status = 404;
        data = { message: 'Không tìm thấy hội thoại.' };
      } else {
        thread.messages = Array.isArray(thread.messages) ? thread.messages : [];
        data = thread;
      }
    }
    else if (endpoint === 'MarkCustomerThreadRead' && method === 'POST') {
      db.customerInboxThreads = Array.isArray(db.customerInboxThreads) ? db.customerInboxThreads : [];

      const thread = db.customerInboxThreads.find(item => Number(item.id) === Number(body?.threadId));

      if (!thread) {
        status = 404;
        data = { message: 'Không tìm thấy hội thoại.' };
      } else {
        thread.messages = Array.isArray(thread.messages) ? thread.messages : [];

        thread.messages.forEach(item => {
          if (item.sender === 'customer') {
            item.isRead = true;
          }
        });

        thread.status = thread.status === 'Unread' ? 'Processing' : thread.status;
        thread.updatedAt = new Date().toISOString();

        saveDb(db);

        data = {
          message: 'Đã đánh dấu đã đọc.',
          thread
        };
      }
    }
    else if (endpoint === 'ReplyCustomerMessage' && method === 'POST') {
      db.customerInboxThreads = Array.isArray(db.customerInboxThreads) ? db.customerInboxThreads : [];

      const thread = db.customerInboxThreads.find(item => Number(item.id) === Number(body?.threadId));
      const messageText = (body?.message || '').trim();

      if (!thread) {
        status = 404;
        data = { message: 'Không tìm thấy hội thoại.' };
      } else if (!messageText) {
        status = 400;
        data = { message: 'Vui lòng nhập nội dung phản hồi.' };
      } else {
        thread.messages = Array.isArray(thread.messages) ? thread.messages : [];

        const message = {
          id: thread.messages.length > 0
            ? Math.max(...thread.messages.map(item => Number(item.id) || 0)) + 1
            : 1,
          sender: 'staff',
          text: messageText,
          timestamp: new Date().toISOString(),
          isRead: true
        };

        thread.messages.push(message);
        thread.status = body?.status || 'Replied';
        thread.updatedAt = new Date().toISOString();

        thread.messages.forEach(item => {
          if (item.sender === 'customer') {
            item.isRead = true;
          }
        });

        saveDb(db);

        data = {
          message: 'Đã gửi phản hồi cho khách hàng.',
          thread
        };
      }
    }
    else if (endpoint === 'CreateCustomerInquiry' && method === 'POST') {
      db.customers = Array.isArray(db.customers) ? db.customers : [];
      db.customerInboxThreads = Array.isArray(db.customerInboxThreads) ? db.customerInboxThreads : [];

      const customerName = (body?.customerName || '').trim();
      const customerPhone = (body?.customerPhone || '').trim();
      const subjectText = (body?.subject || 'Hỏi về sản phẩm').trim() || 'Hỏi về sản phẩm';
      const messageText = (body?.message || '').trim();

      if (!customerName || !messageText) {
        status = 400;
        data = { message: 'Vui lòng nhập họ tên và nội dung câu hỏi.' };
      } else {
        const customer = db.customers.find(item => item.phone === customerPhone) || null;

        const thread = {
          id: db.customerInboxThreads.length > 0
            ? Math.max(...db.customerInboxThreads.map(item => Number(item.id) || 0)) + 1
            : 1,
          customerId: customer?.id || 0,
          customerName: customerName,
          customerPhone: customerPhone,
          channel: 'Store',
          subject: subjectText,
          status: 'Unread',
          priority: 'Medium',
          updatedAt: new Date().toISOString(),
          messages: [
            {
              id: 1,
              sender: 'customer',
              text: messageText,
              timestamp: new Date().toISOString(),
              isRead: false
            }
          ]
        };

        db.customerInboxThreads.unshift(thread);
        saveDb(db);

        data = {
          message: 'Đã gửi câu hỏi thành công.',
          thread
        };
      }
    }
    else if (endpoint === 'AddCustomerInquiryMessage' && method === 'POST') {
      db.customerInboxThreads = Array.isArray(db.customerInboxThreads) ? db.customerInboxThreads : [];

      const threadId = Number(body?.threadId);
      const messageText = (body?.message || '').trim();

      const thread = db.customerInboxThreads.find(item => Number(item.id) === threadId);

      if (!thread) {
        status = 404;
        data = { message: 'Không tìm thấy hội thoại.' };
      } else if (!messageText) {
        status = 400;
        data = { message: 'Vui lòng nhập nội dung tin nhắn.' };
      } else {
        thread.messages = Array.isArray(thread.messages) ? thread.messages : [];

        const message = {
          id: thread.messages.length > 0
            ? Math.max(...thread.messages.map(item => Number(item.id) || 0)) + 1
            : 1,
          sender: 'customer',
          text: messageText,
          timestamp: new Date().toISOString(),
          isRead: false
        };

        thread.messages.push(message);
        thread.status = 'Unread';
        thread.updatedAt = new Date().toISOString();

        saveDb(db);

        data = {
          message: 'Đã gửi tin nhắn.',
          thread
        };
      }
    }
    else if (endpoint === 'GetChatHistory' && method === 'GET') {
      data = db.chatHistory;
    } 
    else if (endpoint === 'AskAi' && method === 'POST') {
      const { question } = body;
      db.chatHistory.push({
        sender: "User",
        message: question,
        timestamp: new Date().toISOString()
      });

      let reply = "Tôi đã tiếp nhận câu hỏi của bạn. Hệ thống ERP NovaTech đang hiển thị doanh thu là " + 
                  db.orders.filter(o => o.status === "Hoàn thành").reduce((sum, o) => sum + o.total, 0).toLocaleString() + 
                  " đ và có " + db.products.filter(p => p.stock <= 3).length + " sản phẩm sắp hết hàng cần bạn xử lý.";
      
      if (question.toLowerCase().includes("kho")) {
        reply = "Báo cáo kho hiện tại: " + db.products.map(p => `${p.name} (Tồn: ${p.stock})`).join(", ");
      } else if (question.toLowerCase().includes("khuyến mãi")) {
        reply = "Mã voucher hoạt động tốt nhất hiện nay là: NOVATECH10 (Giảm 10% đơn từ 500k).";
      }

      const aiMsg = {
        sender: "AI",
        message: reply,
        timestamp: new Date(Date.now() + 1000).toISOString()
      };
      db.chatHistory.push(aiMsg);
      saveDb(db);
      data = aiMsg;
    } 
    else if (endpoint === 'GetEmployees' && method === 'GET') {
      data = db.employees;
    } 
    else if (endpoint === 'CreateEmployee' && method === 'POST') {
      const emp = body;
      emp.id = db.employees.length > 0 ? Math.max(...db.employees.map(e => e.id)) + 1 : 1;
      emp.joinedDate = new Date().toISOString();
      db.employees.push(emp);
      saveDb(db);
      data = { message: "Tạo nhân viên mới thành công!", employee: emp };
    } 
    else if (endpoint === 'EditEmployee' && method === 'POST') {
      const emp = body;
      const target = db.employees.find(e => e.id === emp.id);
      if (!target) {
        status = 404;
        data = { message: "Không tìm thấy nhân viên." };
      } else {
        Object.assign(target, emp);
        saveDb(db);
        data = { message: "Cập nhật nhân sự thành công!", employee: target };
      }
    } 
    else if (endpoint === 'DeleteEmployee' && method === 'POST') {
      const { id } = body;
      const idx = db.employees.findIndex(e => e.id === id);
      if (idx === -1) {
        status = 404;
        data = { message: "Không tìm thấy nhân viên." };
      } else {
        db.employees.splice(idx, 1);
        saveDb(db);
        data = { message: "Đã xóa nhân sự thành công." };
      }
    } 
    else if (endpoint === 'GetRoles' && method === 'GET') {
      data = db.roles;
    } 
    else if (endpoint === 'GetReports' && method === 'GET') {
      const orders = db.orders;
      const totalOrders = orders.length;
      const totalRevenue = orders.filter(o => o.status === "Hoàn thành").reduce((sum, o) => sum + o.total, 0);
      const completedCount = orders.filter(o => o.status === "Hoàn thành").length;

      const channelStatsMap = {};
      orders.forEach(o => {
        const channelName = o.channel || 'Cửa hàng';
        if (!channelStatsMap[channelName]) {
          channelStatsMap[channelName] = { channel: channelName, count: 0, total: 0 };
        }
        channelStatsMap[channelName].count += 1;
        channelStatsMap[channelName].total += o.total;
      });
      const channelStats = Object.values(channelStatsMap);

      data = {
        totalOrders,
        totalRevenue,
        completedCount,
        channelStats
      };
    } 
    else if (endpoint === 'GetSettings' && method === 'GET') {
      data = db.settings;
    } 
    else if (endpoint === 'UpdateSettings' && method === 'POST') {
      Object.assign(db.settings, body);
      saveDb(db);
      data = { message: "Cập nhật cấu hình cửa hàng thành công!" };
    } 
    else {
      status = 404;
      data = { message: `Mock API: Endpoint '/api/${endpoint}' not found.` };
    }
  } catch (err) {
    console.error("Mock Request Error:", err);
    status = 500;
    data = { message: "Có lỗi xảy ra trong hệ thống mock API.", error: err.message };
  }

  return {
    data: data,
    status: status,
    statusText: status === 200 ? 'OK' : 'Bad Request',
    headers: { 'content-type': 'application/json' },
    config: config
  };
};

// 4. Override Axios default adapter
const originalAdapter = axios.defaults.adapter;

axios.defaults.adapter = async function (config) {
  const { url, method, data } = config;
  
  if (url && url.startsWith('/api/')) {
    const endpoint = url.split('?')[0].substring(5);
    let body = data;
    if (typeof data === 'string') {
      try {
        body = JSON.parse(data);
      } catch (e) {
        // Not a JSON string
      }
    }
    
    await new Promise(resolve => setTimeout(resolve, 200));
    
    const mockRes = await handleMockRequest(endpoint, method.toUpperCase(), body, config);
    if (mockRes.status >= 200 && mockRes.status < 300) {
      return mockRes;
    } else {
      const error = new Error(mockRes.data?.message || 'Mock Request Error');
      error.response = mockRes;
      throw error;
    }
  }

  if (originalAdapter) {
    return originalAdapter(config);
  }
  throw new Error("No default adapter available.");
};

console.log("🚀 NovaTech Mock API Interceptor activated successfully!");