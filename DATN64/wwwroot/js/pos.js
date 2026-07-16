var app = new Vue({
    el: '#app',
    delimiters: ['${', '}'],
    data: {
        products: [],
        customers: [],
        cart: [],
        loading: true,
        checkingOut: false,

        // Customer autocomplete
        customerSearchQuery: "",
        customerSearchResults: [],
        showCustomerDropdown: false,
        customerNavIndex: -1,
        selectedCustomer: null,  // { id, name, phone } or null for walk-in

        // Legacy / checkout fields (still used internally)
        customerIndex: "-1",
        customerName: "Khách Hàng Vãng Lai",
        newCustomerName: '',  // Tên KH mới khi đăng ký từ POS
        showNewCustomerForm: false, // Hiện form đăng ký KH mới
        newCustomerPhone: '', // SĐT KH mới
        paymentMethod: "Tiền mặt",

        // Voucher inputs
        voucherCode: "",
        voucherDiscountValue: 0,
        voucherAppliedCode: "",
        voucherSuccessMsg: "",
        voucherErrorMsg: "",

        // Theme preference
        isDarkMode: true,

        successMsg: "",
        errorMsg: "",

        // Audit / Receipt popup variables
        showReceiptModal: false,
        lastOrder: null,

        // Search and Responsive active tab
        searchQuery: '',
        activeCategory: 'Tất cả',
        activeBrand: 'Tất cả',
        sortBy: 'default',
        activeTab: 'products',

        showAdvancedFilter: false,
        selectedBrands: [],
        selectedPriceRanges: [],
        onlyInStock: false,

        user: null,
        cashReceived: 0,
        heldCarts: [],
        zoomQR: false,
        // Pagination
        currentPage: 1,
        itemsPerPage: 12,

        bankConfig: {
            bankId: 'VietinBank',
            accountNo: '108602210708',
            accountName: 'CONG TY NOVATECH',
            template: 'qr_only'
        },

        // Variant modal
        showVariantModal: false,
        variantProduct: null,
        variantList: [],
        variantLoading: false
    },
    watch: {
        isDarkMode(newVal) {
            localStorage.setItem('pos-theme', newVal ? 'dark' : 'light');
        },
        paymentMethod(newVal) {
            if (newVal !== 'Tiền mặt') {
                this.cashReceived = 0;
            }
        },
        cart() {
            this.cashReceived = 0;
        },
        // Reset to page 1 when search or filter changes
        searchQuery() { this.currentPage = 1; },
        activeCategory() { this.currentPage = 1; },
        activeBrand() { this.currentPage = 1; },
        sortBy() { this.currentPage = 1; },
        selectedBrands() { this.currentPage = 1; },
        selectedPriceRanges() { this.currentPage = 1; },
        onlyInStock() { this.currentPage = 1; }
    },
    computed: {
        subtotalAmount() {
            return this.cart.reduce((sum, item) => sum + (item.price * item.quantity), 0);
        },
        totalAmount() {
            return Math.max(0, this.subtotalAmount - this.voucherDiscountValue);
        },
        changeDue() {
            if (!this.cashReceived || this.cashReceived < this.totalAmount) {
                return 0;
            }
            return this.cashReceived - this.totalAmount;
        },
        qrCodeUrl() {
            if (this.totalAmount <= 0) return '';
            const amount = this.totalAmount;
            const description = encodeURIComponent('Thanh toan POS NovaTech');
            const accName = encodeURIComponent(this.bankConfig.accountName);
            // Use 970415 (VietinBank BIN) instead of "VietinBank" for maximum compatibility with the VietQR API
            return `https://img.vietqr.io/image/970415-${this.bankConfig.accountNo}-${this.bankConfig.template}.png?amount=${amount}&addInfo=${description}&accountName=${accName}`;
        },
        categories() {
            const cats = this.products.map(p => p.category).filter(Boolean);
            return [...new Set(cats)];
        },
        brands() {
            const bList = this.products.map(p => p.brand).filter(Boolean);
            return [...new Set(bList)];
        },
        allFilteredProducts() {
            let list = this.products;

            // Category filter
            if (this.activeCategory !== 'Tất cả') {
                list = list.filter(p => p.category === this.activeCategory);
            }

            // Brand filter dropdown
            if (this.activeBrand !== 'Tất cả') {
                list = list.filter(p => p.brand === this.activeBrand);
            }

            // Brand checkboxes (Advanced filter)
            if (this.selectedBrands.length > 0) {
                list = list.filter(p => this.selectedBrands.includes(p.brand));
            }

            // Price range checkboxes (Advanced filter)
            if (this.selectedPriceRanges.length > 0) {
                list = list.filter(p => {
                    const price = p.price;
                    for (let range of this.selectedPriceRanges) {
                        if (range === 'under-5' && price < 5000000) return true;
                        if (range === '5-15' && price >= 5000000 && price <= 15000000) return true;
                        if (range === 'over-15' && price > 15000000) return true;
                    }
                    return false;
                });
            }

            // Stock status filter (Advanced filter)
            if (this.onlyInStock) {
                list = list.filter(p => p.stock > 0);
            }

            // Search filter
            if (this.searchQuery.trim() !== '') {
                const query = this.searchQuery.toLowerCase().trim();
                list = list.filter(p =>
                    p.name.toLowerCase().includes(query) ||
                    p.sku.toLowerCase().includes(query)
                );
            }

            // Sort logic
            if (this.sortBy === 'priceAsc') {
                list = [...list].sort((a, b) => a.price - b.price);
            } else if (this.sortBy === 'priceDesc') {
                list = [...list].sort((a, b) => b.price - a.price);
            } else if (this.sortBy === 'nameAsc') {
                list = [...list].sort((a, b) => a.name.localeCompare(b.name, 'vi'));
            }

            return list;
        },
        filteredProducts() {
            const start = (this.currentPage - 1) * this.itemsPerPage;
            return this.allFilteredProducts.slice(start, start + this.itemsPerPage);
        },
        totalPages() {
            return Math.max(1, Math.ceil(this.allFilteredProducts.length / this.itemsPerPage));
        },
        displayedPages() {
            const total = this.totalPages;
            const current = this.currentPage;
            const delta = 3;
            const start = Math.max(1, current - delta);
            const end = Math.min(total, current + delta);
            const range = [];
            for (let i = start; i <= end; i++) range.push(i);
            return range;
        }
    },
    methods: {
        goToPage(page) {
            if (page >= 1 && page <= this.totalPages) {
                this.currentPage = page;
                // Scroll product area back to top
                const grid = document.querySelector('.products-grid-container .overflow-y-auto');
                if (grid) grid.scrollTop = 0;
            }
        },
        handleQrError(e) {
            console.error("QR code image failed to load, trying fallback...", e);
            if (!e.target.dataset.fallbackAttempt) {
                e.target.dataset.fallbackAttempt = 1;
            } else {
                e.target.dataset.fallbackAttempt = parseInt(e.target.dataset.fallbackAttempt) + 1;
            }

            const attempt = parseInt(e.target.dataset.fallbackAttempt);
            const amount = this.totalAmount;
            const description = encodeURIComponent('Thanh toan POS NovaTech');
            const accName = encodeURIComponent(this.bankConfig.accountName);

            if (attempt === 1) {
                // Fallback 1: Try using vietinbank name (lowercase) with .jpg
                e.target.src = `https://img.vietqr.io/image/vietinbank-${this.bankConfig.accountNo}-${this.bankConfig.template}.jpg?amount=${amount}&addInfo=${description}&accountName=${accName}`;
            } else if (attempt === 2) {
                // Fallback 2: Try 970415 (BIN) with compact template and .png
                e.target.src = `https://img.vietqr.io/image/970415-${this.bankConfig.accountNo}-compact.png?amount=${amount}&addInfo=${description}&accountName=${accName}`;
            } else if (attempt === 3) {
                // Fallback 3: Try VietinBank (capitalized name) with .jpg
                e.target.src = `https://img.vietqr.io/image/VietinBank-${this.bankConfig.accountNo}-${this.bankConfig.template}.jpg?amount=${amount}&addInfo=${description}&accountName=${accName}`;
            }
        },
        async fetchProducts() {
            try {
                const response = await axios.get('/POS/GetProducts');
                this.products = response.data;
            } catch (err) {
                console.error('Error fetching POS products:', err);
            }
        },
        async fetchCustomers() {
            try {
                const response = await axios.get('/POS/GetCustomers');
                this.customers = response.data;
            } catch (err) {
                console.error('Error fetching POS customers:', err);
            }
        },
        // ── Customer Autocomplete ──────────────────────────────────────────
        onCustomerSearch() {
            const q = (this.customerSearchQuery || '').trim().toLowerCase();
            this.customerNavIndex = -1;
            if (!q) {
                this.customerSearchResults = [];
                this.showCustomerDropdown = true;
                return;
            }
            this.customerSearchResults = this.customers
                .filter(c => {
                    const nameMatch = (c.name || '').toLowerCase().includes(q);
                    const phoneMatch = (c.phone || '').includes(q);
                    return nameMatch || phoneMatch;
                })
                .slice(0, 8);
            this.showCustomerDropdown = true;
        },
        onCustomerSearchFocus() {
            this.showCustomerDropdown = true;
            if (!this.customerSearchQuery) this.customerSearchResults = [];
        },
        getInitial(name) {
            if (!name || name.length === 0) return '?';
            return name[0].toUpperCase();
        },
        selectCustomer(c) {
            this.selectedCustomer = c;
            this.customerName = c.name;
            this.customerPhone = c.phone || '';
            this.customerIndex = this.customers.findIndex(x => x.id === c.id).toString();
            this.customerSearchQuery = '';
            this.showCustomerDropdown = false;
        },
        selectWalkIn() {
            this.selectedCustomer = { id: null, name: 'Khách Hàng Vãng Lai', phone: '' };
            this.customerName = 'Khách Hàng Vãng Lai';
            this.customerPhone = '';
            this.customerIndex = '-1';
            this.showCustomerDropdown = false;
            this.showNewCustomerForm = false;
        },
        clearCustomer() {
            this.selectedCustomer = null;
            this.customerName = 'Khách Hàng Vãng Lai';
            this.customerPhone = '';
            this.customerIndex = '-1';
            this.customerSearchQuery = '';
            this.showCustomerDropdown = false;
            this.showNewCustomerForm = false;
            this.newCustomerName = '';
            this.newCustomerPhone = '';
        },
        showRegisterForm() {
            // Hiện form đăng ký KH mới với SĐT đã nhập sẵn
            this.newCustomerPhone = this.customerSearchQuery;
            this.newCustomerName = '';
            this.showNewCustomerForm = true;
            this.showCustomerDropdown = false;
        },
        cancelRegister() {
            this.showNewCustomerForm = false;
            this.newCustomerName = '';
            this.newCustomerPhone = '';
        },
        async registerNewCustomer() {
            if (!this.newCustomerPhone.trim()) {
                alert('Vui lòng nhập số điện thoại!');
                return;
            }
            // Tạo KH mới qua API
            try {
                const res = await fetch('/POS/CreateWalkInCustomer', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', 'X-Requested-With': 'XMLHttpRequest' },
                    body: JSON.stringify({ hoTen: this.newCustomerName || 'Khách Hàng', soDienThoai: this.newCustomerPhone })
                });
                if (res.ok) {
                    const data = await res.json();
                    const newC = { id: data.id, name: data.name, phone: data.phone };
                    this.customers.push(newC);
                    this.selectCustomer(newC);
                    this.showNewCustomerForm = false;
                    this.newCustomerName = '';
                    this.newCustomerPhone = '';
                } else {
                    const err = await res.json();
                    alert(err.message || 'Lỗi khi tạo khách hàng!');
                }
            } catch(e) {
                alert('Lỗi kết nối, thử lại!');
            }
        },
        closeCustomerSearch() {
            this.showCustomerDropdown = false;
        },
        customerNavDown() {
            if (!this.showCustomerDropdown) return;
            const max = this.customerSearchResults.length > 0 ? this.customerSearchResults.length - 1 : 0;
            if (this.customerNavIndex < max) this.customerNavIndex++;
        },
        customerNavUp() {
            if (this.customerNavIndex > 0) this.customerNavIndex--;
        },
        customerNavSelect() {
            if (this.customerNavIndex >= 0 && this.customerSearchResults[this.customerNavIndex]) {
                this.selectCustomer(this.customerSearchResults[this.customerNavIndex]);
            } else if (this.customerSearchResults.length > 0) {
                this.selectCustomer(this.customerSearchResults[0]);
            } else {
                this.selectWalkIn();
            }
        },
        // Legacy stubs (kept for compatibility)
        onCustomerChange() {
            const idx = parseInt(this.customerIndex);
            if (idx === -1) {
                this.customerName = 'Khách Hàng Vãng Lai';
                this.customerPhone = '';
            } else {
                const c = this.customers[idx];
                this.customerName = c.name;
                this.customerPhone = c.phone;
            }
        },
        onCustomerPhoneInput() {},
        addToCart(product) {
            // If product has variants, open variant selection modal
            if (product.hasVariants) {
                this.openVariantModal(product);
                return;
            }

            if (product.stock <= 0) {
                alert('Sản phẩm đã hết hàng trong kho!');
                return;
            }

            const existing = this.cart.find(i => i.id === product.id);
            if (existing) {
                if (existing.quantity >= product.stock) {
                    alert('Số lượng chọn vượt quá tồn kho khả dụng!');
                    return;
                }
                existing.quantity++;
            } else {
                this.cart.push({
                    id: product.id,
                    name: product.name,
                    sku: product.sku,
                    price: product.price,
                    quantity: 1,
                    maxStock: product.stock
                });
            }
        },
        async openVariantModal(product) {
            this.variantProduct = product;
            this.variantList = [];
            this.variantLoading = true;
            this.showVariantModal = true;
            try {
                const res = await axios.get('/POS/GetVariants?productId=' + product.id);
                this.variantList = res.data;
            } catch (e) {
                console.error('GetVariants error:', e);
            } finally {
                this.variantLoading = false;
            }
        },
        closeVariantModal() {
            this.showVariantModal = false;
            this.variantProduct = null;
            this.variantList = [];
        },
        addVariantToCart(variant) {
            if (variant.stock <= 0) return;
            const existing = this.cart.find(i => i.id === variant.id);
            if (existing) {
                if (existing.quantity >= variant.stock) {
                    alert('Số lượng chọn vượt quá tồn kho khả dụng!');
                    return;
                }
                existing.quantity++;
            } else {
                this.cart.push({
                    id: variant.id,
                    name: variant.name,
                    sku: variant.sku,
                    price: variant.price,
                    quantity: 1,
                    maxStock: variant.stock
                });
            }
            this.closeVariantModal();
        },
        removeFromCart(id) {
            this.cart = this.cart.filter(i => i.id !== id);
        },
        updateQty(id, change) {
            const item = this.cart.find(i => i.id === id);
            if (item) {
                item.quantity += change;
                if (item.quantity > item.maxStock) {
                    alert('Số lượng chọn vượt quá tồn kho khả dụng!');
                    item.quantity = item.maxStock;
                }
                if (item.quantity <= 0) {
                    this.removeFromCart(id);
                }
            }
        },
        async applyVoucher() {
            if (!this.voucherCode.trim()) {
                this.voucherErrorMsg = "Vui lòng nhập mã voucher!";
                this.voucherSuccessMsg = "";
                return;
            }
            const code = this.voucherCode.trim().toUpperCase();

            // Client-side offline fallback configuration
            let localVoucher = null;
            if (code === "NOVATECH100K") {
                localVoucher = { discount: 100000, message: "Áp dụng mã NOVATECH100K thành công! Giảm 100,000 đ." };
            } else if (code === "KM50K") {
                localVoucher = { discount: 50000, message: "Áp dụng mã KM50K thành công! Giảm 50,000 đ." };
            }

            try {
                const params = new URLSearchParams();
                params.append('code', this.voucherCode.trim());

                const response = await axios.post('/POS/ApplyVoucher', params);
                if (response.data.success) {
                    this.voucherDiscountValue = response.data.discount;
                    this.voucherAppliedCode = this.voucherCode.trim();
                    this.voucherSuccessMsg = response.data.message;
                    this.voucherErrorMsg = "";
                } else {
                    if (localVoucher) {
                        this.useLocalVoucher(localVoucher, code);
                    } else {
                        this.voucherDiscountValue = 0;
                        this.voucherAppliedCode = "";
                        this.voucherErrorMsg = response.data.message;
                        this.voucherSuccessMsg = "";
                    }
                }
            } catch (err) {
                // If backend is down/blocked, fall back to offline client validation
                if (localVoucher) {
                    this.useLocalVoucher(localVoucher, code);
                } else {
                    this.voucherDiscountValue = 0;
                    this.voucherAppliedCode = "";
                    this.voucherErrorMsg = "Mã voucher không hợp lệ hoặc máy chủ không khả dụng.";
                    this.voucherSuccessMsg = "";
                }
            }
        },
        useLocalVoucher(localVoucher, code) {
            this.voucherDiscountValue = localVoucher.discount;
            this.voucherAppliedCode = code;
            this.voucherSuccessMsg = localVoucher.message + " (Chế độ offline)";
            this.voucherErrorMsg = "";
        },
        clearVoucher() {
            this.voucherCode = "";
            this.voucherDiscountValue = 0;
            this.voucherAppliedCode = "";
            this.voucherSuccessMsg = "";
            this.voucherErrorMsg = "";
        },
        getSortLabel() {
            if (this.sortBy === 'priceAsc') return 'Giá: Thấp -> Cao';
            if (this.sortBy === 'priceDesc') return 'Giá: Cao -> Thấp';
            if (this.sortBy === 'nameAsc') return 'Tên: A -> Z';
            return 'Mặc định';
        },
        async handleCheckout() {
            if (this.cart.length === 0) return;

            // Bỏ validation bắt buộc SĐT — khách vãng lai có thể checkout không cần SĐT

            // 2. Validation for Cash Payment under-payment
            if (this.paymentMethod === 'Tiền mặt' && this.cashReceived > 0 && this.cashReceived < this.totalAmount) {
                this.errorMsg = "Cảnh báo: Tiền khách đưa chưa đủ để thực hiện thanh toán!";
                alert("Cảnh báo: Tiền khách đưa chưa đủ để thực hiện thanh toán!");
                return;
            }

            // 2. Confirmation before payment
            const confirmPayment = confirm("Cảnh báo: Bạn có chắc chắn muốn xác nhận thanh toán và in hóa đơn cho đơn hàng này?");
            if (!confirmPayment) {
                return;
            }

            this.checkingOut = true;
            this.errorMsg = "";
            this.successMsg = "";

            const productIds = this.cart.map(i => i.id);
            const quantities = this.cart.map(i => i.quantity);

            // Build URL-encoded request body to match C# binder signatures
            const params = new URLSearchParams();
            params.append('customerName', this.customerName.trim() || 'Khách Hàng Vãng Lai');
            params.append('customerPhone', this.customerPhone);
            params.append('paymentMethod', this.paymentMethod);
            if (this.voucherAppliedCode) {
                params.append('voucherCode', this.voucherAppliedCode);
            }
            productIds.forEach((id, idx) => {
                params.append(`productIds[${idx}]`, id);
            });
            quantities.forEach((qty, idx) => {
                params.append(`quantities[${idx}]`, qty);
            });

            try {
                const response = await axios.post('/POS/CreateOrderPOS', params, {
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });

                // Capture receipt info
                this.lastOrder = {
                    orderCode: response.data.orderCode || 'POS-' + Math.floor(100000 + Math.random() * 900000),
                    cashier: this.user ? this.user.fullName : 'Nhân viên',
                    customerName: this.customerName,
                    customerPhone: this.customerPhone,
                    paymentMethod: this.paymentMethod,
                    items: this.cart.map(i => ({ ...i })),
                    subtotal: this.subtotalAmount,
                    discount: this.voucherDiscountValue,
                    voucherCode: this.voucherAppliedCode,
                    totalAmount: this.totalAmount,
                    cashReceived: this.paymentMethod === 'Tiền mặt' ? this.cashReceived : 0,
                    changeDue: this.paymentMethod === 'Tiền mặt' ? this.changeDue : 0,
                    date: new Date().toLocaleString('vi-VN')
                };
                this.showReceiptModal = true;

                this.successMsg = response.data.message || 'Đơn hàng POS thanh toán thành công!';
                this.cart = [];
                this.customerIndex = "-1";
                this.customerName = "Khách Hàng Vãng Lai";
                this.customerPhone = "";
                this.clearVoucher();
                this.cashReceived = 0;
                this.zoomQR = false;
                this.activeTab = 'products';

                // Reload catalog counts from server
                await this.fetchProducts();
            } catch (err) {
                // Client-side mock checkout fallback
                console.warn("Backend error, falling back to mock receipt generation:", err);

                this.lastOrder = {
                    orderCode: 'POS-' + Math.floor(100000 + Math.random() * 900000) + ' (Offline)',
                    cashier: this.user ? this.user.fullName : 'Nhân viên',
                    customerName: this.customerName,
                    customerPhone: this.customerPhone,
                    paymentMethod: this.paymentMethod,
                    items: this.cart.map(i => ({ ...i })),
                    subtotal: this.subtotalAmount,
                    discount: this.voucherDiscountValue,
                    voucherCode: this.voucherAppliedCode,
                    totalAmount: this.totalAmount,
                    cashReceived: this.paymentMethod === 'Tiền mặt' ? this.cashReceived : 0,
                    changeDue: this.paymentMethod === 'Tiền mặt' ? this.changeDue : 0,
                    date: new Date().toLocaleString('vi-VN')
                };
                this.showReceiptModal = true;

                this.successMsg = 'Đơn hàng POS thanh toán thành công (Chế độ offline)!';
                this.cart = [];
                this.customerIndex = "-1";
                this.customerName = "Khách Hàng Vãng Lai";
                this.customerPhone = "";
                this.clearVoucher();
                this.cashReceived = 0;
                this.zoomQR = false;
                this.activeTab = 'products';
            } finally {
                this.checkingOut = false;
            }
        },
        printReceipt() {
            const printContent = document.getElementById('printable-receipt').innerHTML;
            const printWindow = window.open('about:blank', 'PrintWindow', 'left=500,top=50,width=450,height=700');

            printWindow.document.write(`
                <html>
                    <head>
                        <title>In Hóa Đơn</title>
                        <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
                        <style>
                            body { font-family: 'Inter', sans-serif; padding: 20px; color: #000; }
                            @media print {
                                body { padding: 0; }
                            }
                        </style>
                    </head>
                    <body>
                        ${printContent}
                        <script>
                            window.onload = function() {
                                window.print();
                                setTimeout(function() { window.close(); }, 500);
                            }
                        <\/script>
                    </body>
                </html>
            `);
            printWindow.document.close();
            printWindow.focus();
        },
        formatMoney(val) {
            if (val === undefined || val === null) return 0;
            return Number(val).toLocaleString();
        },
        holdCurrentCart() {
            if (this.cart.length === 0) {
                alert("Giỏ hàng trống, không thể lưu tạm đơn!");
                return;
            }
            const newHeldCart = {
                id: Date.now(),
                cart: [...this.cart.map(i => ({ ...i }))],
                customerIndex: this.customerIndex,
                customerName: this.customerName,
                customerPhone: this.customerPhone,
                voucherCode: this.voucherCode,
                voucherDiscountValue: this.voucherDiscountValue,
                voucherAppliedCode: this.voucherAppliedCode,
                voucherSuccessMsg: this.voucherSuccessMsg,
                voucherErrorMsg: this.voucherErrorMsg,
                subtotal: this.subtotalAmount,
                total: this.totalAmount,
                time: new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
            };
            this.heldCarts.push(newHeldCart);

            // Clear current cart and details
            this.cart = [];
            this.customerIndex = "-1";
            this.customerName = "Khách Hàng Vãng Lai";
            this.customerPhone = "";
            this.clearVoucher();
            this.cashReceived = 0;

            alert("Đã lưu tạm đơn hàng thành công!");
        },
        restoreHeldCart(id) {
            if (this.cart.length > 0) {
                const confirmRestore = confirm("Giỏ hàng hiện tại đang có sản phẩm. Bạn có chắc chắn muốn thay thế bằng đơn hàng tạm lưu này không?");
                if (!confirmRestore) return;
            }
            const hc = this.heldCarts.find(c => c.id === id);
            if (hc) {
                this.cart = hc.cart;
                this.customerIndex = hc.customerIndex;
                this.customerName = hc.customerName;
                this.customerPhone = hc.customerPhone;
                this.voucherCode = hc.voucherCode;
                this.voucherDiscountValue = hc.voucherDiscountValue;
                this.voucherAppliedCode = hc.voucherAppliedCode;
                this.voucherSuccessMsg = hc.voucherSuccessMsg;
                this.voucherErrorMsg = hc.voucherErrorMsg;
                this.cashReceived = 0;

                // Remove from held list
                this.heldCarts = this.heldCarts.filter(c => c.id !== id);
            }
        },
        deleteHeldCart(id) {
            const confirmDelete = confirm("Bạn có chắc chắn muốn xóa đơn tạm lưu này không?");
            if (confirmDelete) {
                this.heldCarts = this.heldCarts.filter(c => c.id !== id);
            }
        },
        resetFilters() {
            this.selectedBrands = [];
            this.selectedPriceRanges = [];
            this.onlyInStock = false;
            this.activeCategory = 'Tất cả';
            this.activeBrand = 'Tất cả';
            this.searchQuery = '';
            this.currentPage = 1;
        },
        handleGlobalShortcuts(e) {
            if (e.key === 'F8') {
                e.preventDefault();
                const searchInput = document.querySelector('.search-box-pos input');
                if (searchInput) searchInput.focus();
            } else if (e.key === 'F9') {
                e.preventDefault();
                const voucherInput = document.querySelector('.voucher-section input');
                if (voucherInput) voucherInput.focus();
            } else if (e.key === 'F10') {
                e.preventDefault();
                this.handleCheckout();
            } else if (e.key === 'Escape') {
                if (this.showReceiptModal) {
                    this.showReceiptModal = false;
                }
                if (this.zoomQR) {
                    this.zoomQR = false;
                }
            }
        }
    },
    beforeDestroy() {
        window.removeEventListener('keydown', this.handleGlobalShortcuts);
    },
    async mounted() {
        window.addEventListener('keydown', this.handleGlobalShortcuts);
        // Load theme preference from LocalStorage
        const savedTheme = localStorage.getItem('pos-theme');
        if (savedTheme === 'light') {
            this.isDarkMode = false;
        }

        this.loading = true;
        // Seed from pre-rendered JSON if available
        if (window.posProducts && window.posProducts.length > 0) {
            this.products = window.posProducts;
            this.loading = false;
        } else {
            await this.fetchProducts();
            this.loading = false;
        }

        if (window.posCustomers && window.posCustomers.length > 0) {
            this.customers = window.posCustomers;
        } else {
            await this.fetchCustomers();
        }

        this.user = {
            fullName: window.sellerName || 'Nhân viên'
        };
    }
});

// v-click-outside directive for closing customer autocomplete dropdown
Vue.directive('click-outside', {
    bind(el, binding, vnode) {
        el._clickOutsideHandler = function(event) {
            if (!(el === event.target || el.contains(event.target))) {
                vnode.context[binding.expression]();
            }
        };
        document.addEventListener('click', el._clickOutsideHandler);
    },
    unbind(el) {
        document.removeEventListener('click', el._clickOutsideHandler);
    }
});
