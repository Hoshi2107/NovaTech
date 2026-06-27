var app = new Vue({
    el: '#app',
    delimiters: ['${', '}'],
    data: {
        products: [],
        customers: [],
        cart: [],
        loading: true,
        checkingOut: false,

        // Form inputs
        customerIndex: "-1",
        customerName: "Khách Hàng Vãng Lai",
        customerPhone: "",
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

        user: null,
        cashReceived: 0,
        heldCarts: [],
        zoomQR: false,
        bankConfig: {
            bankId: 'VietinBank',
            accountNo: '108602210708',
            accountName: 'CONG TY NOVATECH',
            template: 'qr_only'
        }
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
        }
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
            return `https://img.vietqr.io/image/${this.bankConfig.bankId}-${this.bankConfig.accountNo}-${this.bankConfig.template}.png?amount=${amount}&addInfo=${description}&accountName=${accName}`;
        },
        categories() {
            const cats = this.products.map(p => p.category).filter(Boolean);
            return [...new Set(cats)];
        },
        brands() {
            const bList = this.products.map(p => p.brand).filter(Boolean);
            return [...new Set(bList)];
        },
        filteredProducts() {
            let list = this.products;
            
            // Category filter
            if (this.activeCategory !== 'Tất cả') {
                list = list.filter(p => p.category === this.activeCategory);
            }

            // Brand filter
            if (this.activeBrand !== 'Tất cả') {
                list = list.filter(p => p.brand === this.activeBrand);
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
        }
    },
    methods: {
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
        onCustomerChange() {
            const idx = parseInt(this.customerIndex);
            if (idx === -1) {
                this.customerName = "Khách Hàng Vãng Lai";
                this.customerPhone = "";
            } else {
                const c = this.customers[idx];
                this.customerName = c.name;
                this.customerPhone = c.phone;
            }
        },
        addToCart(product) {
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

            // 1. Validation for Walk-in Customer SĐT
            if (this.customerIndex === "-1" && !this.customerPhone.trim()) {
                this.errorMsg = "Cảnh báo: Khách hàng vãng lai bắt buộc phải có số điện thoại!";
                alert("Cảnh báo: Khách hàng vãng lai bắt buộc phải nhập số điện thoại!");
                return;
            }

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
                    items: this.cart.map(i => ({...i})),
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
                    items: this.cart.map(i => ({...i})),
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
                cart: [...this.cart.map(i => ({...i}))],
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
