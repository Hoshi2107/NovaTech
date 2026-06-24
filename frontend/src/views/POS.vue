<template>
  <div class="pos-wrapper min-vh-100 w-100 d-flex flex-column text-white">
    <!-- POS Header -->
    <header class="pos-header d-flex justify-content-between align-items-center px-4 py-3">
      <div class="d-flex align-items-center gap-2">
        <h4 class="fw-bold m-0 text-cyan d-flex align-items-center gap-2">
          <i class="fa-solid fa-cash-register animate-pulse text-cyan"></i> NovaTech POS
        </h4>
      </div>
      
      <!-- Mobile Tabs Navigation (Visible under 992px) -->
      <div class="pos-mobile-tabs d-lg-none d-flex rounded-pill p-1 bg-dark-custom border border-slate-700">
        <button type="button" class="btn btn-sm rounded-pill px-4 text-xs fw-semibold" :class="{ 'btn-info text-white': activeTab === 'products', 'text-slate-400': activeTab !== 'products' }" @click="activeTab = 'products'">
          Sản phẩm
        </button>
        <button type="button" class="btn btn-sm rounded-pill px-4 text-xs fw-semibold position-relative" :class="{ 'btn-info text-white': activeTab === 'cart', 'text-slate-400': activeTab !== 'cart' }" @click="activeTab = 'cart'">
          Giỏ hàng
          <span class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger" style="font-size: 0.65rem;" v-if="cart.length > 0">
            {{ cart.reduce((sum, item) => sum + item.quantity, 0) }}
          </span>
        </button>
      </div>

      <!-- Cashier info & Back button -->
      <div class="d-flex align-items-center gap-3">
        <div class="d-none d-md-flex align-items-center gap-2 text-xs text-slate-400">
          <i class="fa-solid fa-circle-user text-cyan"></i>
          <span>Thu ngân: <strong class="text-white" v-if="user">{{ user.fullName }}</strong></span>
        </div>
        <router-link to="/portal" class="btn btn-back rounded-pill px-3.5 text-xs d-flex align-items-center gap-1.5">
          <i class="fa-solid fa-arrow-left"></i> Trở lại Cổng
        </router-link>
      </div>
    </header>

    <!-- Welcome Marquee Banner -->
    <div class="welcome-ticker-wrap">
      <div class="welcome-ticker-inner"><span class="welcome-ticker-text">🎉 Chào mừng Quý khách đến với NovaTech! &nbsp;&nbsp;✨ Chúc Quý khách mua sắm vui vẻ! &nbsp;&nbsp;🛒 Hàng ngàn sản phẩm chính hãng đang chờ bạn! &nbsp;&nbsp;💎 Ưu đãi hấp dẫn mỗi ngày tại NovaTech! &nbsp;&nbsp;🚀 Dịch vụ tận tâm – Chất lượng hàng đầu! &nbsp;&nbsp;🎁 Quý khách có thể hỏi nhân viên để được hỗ trợ tốt nhất! &nbsp;&nbsp;</span><span class="welcome-ticker-text" aria-hidden="true">🎉 Chào mừng Quý khách đến với NovaTech! &nbsp;&nbsp;✨ Chúc Quý khách mua sắm vui vẻ! &nbsp;&nbsp;🛒 Hàng ngàn sản phẩm chính hãng đang chờ bạn! &nbsp;&nbsp;💎 Ưu đãi hấp dẫn mỗi ngày tại NovaTech! &nbsp;&nbsp;🚀 Dịch vụ tận tâm – Chất lượng hàng đầu! &nbsp;&nbsp;🎁 Quý khách có thể hỏi nhân viên để được hỗ trợ tốt nhất! &nbsp;&nbsp;</span></div>
    </div>

    <!-- Main Dashboard -->
    <div class="pos-main-container flex-grow-1 row g-0 overflow-hidden">
      
      <!-- Left Panel: Product Catalog -->
      <div class="col-lg-8 d-flex flex-column h-100 p-4 border-end border-slate-800 overflow-hidden" :class="{ 'd-none d-lg-flex': activeTab !== 'products' }">
        
        <!-- Search and Catalog Filter -->
        <div class="d-flex flex-column flex-sm-row gap-3 mb-4 flex-shrink-0">
          <div class="search-box-pos position-relative flex-grow-1">
            <input type="text" v-model="searchQuery" class="form-control text-xs rounded-pill" placeholder="Tìm tên sản phẩm hoặc mã SKU...">
            <i class="fa-solid fa-magnifying-glass position-absolute top-50 translate-middle-y end-0 me-3 text-muted"></i>
          </div>
          <div class="category-filter-pos d-flex gap-2 overflow-x-auto pb-1" style="max-height: 48px;">
            <button type="button" class="btn btn-xs rounded-pill px-3 py-1.5 text-xxs fw-bold" :class="{ 'btn-info text-white': activeCategory === 'Tất cả', 'btn-outline-slate text-slate-400': activeCategory !== 'Tất cả' }" @click="activeCategory = 'Tất cả'">Tất cả</button>
            <button type="button" v-for="cat in categories" :key="cat" class="btn btn-xs rounded-pill px-3 py-1.5 text-xxs fw-bold" :class="{ 'btn-info text-white': activeCategory === cat, 'btn-outline-slate text-slate-400': activeCategory !== cat }" @click="activeCategory = cat">{{ cat }}</button>
          </div>
        </div>

        <div v-if="loading" class="d-flex align-items-center justify-content-center flex-grow-1">
          <div class="spinner-border text-info" role="status"></div>
        </div>

        <!-- Scrollable Products Grid -->
        <div class="flex-grow-1 overflow-y-auto pe-2 products-grid-container" v-else>
          <div class="row row-cols-2 row-cols-sm-3 row-cols-md-4 g-3" v-if="filteredProducts.length > 0">
            <div class="col" v-for="p in filteredProducts" :key="p.id">
              <div class="card product-card h-100 cursor-pointer" @click="addToCart(p)">
                <div class="position-relative overflow-hidden rounded-top-3">
                  <img :src="p.image" class="card-img-top" :alt="p.name">
                  <span class="position-absolute top-0 start-0 m-2 badge" :class="p.stock > 5 ? 'bg-success' : p.stock > 0 ? 'bg-warning text-dark' : 'bg-danger'">
                    Tồn: {{ p.stock }}
                  </span>
                </div>
                <div class="card-body p-2.5 d-flex flex-column justify-content-between">
                  <h6 class="text-xs text-truncate mb-1 fw-bold product-name-highlight" :title="p.name">{{ p.name }}</h6>
                  <div class="text-xxs mb-2 sku-highlight">{{ p.sku }}</div>
                  <div class="text-xs text-emerald fw-extrabold">{{ formatMoney(p.price) }} đ</div>
                </div>
              </div>
            </div>
          </div>
          <div class="d-flex flex-column align-items-center justify-content-center text-muted-custom py-5 flex-grow-1" v-else>
            <i class="fa-solid fa-box-open fs-1 mb-3 text-slate-600"></i>
            <p class="text-xs text-slate-400">Không tìm thấy sản phẩm nào phù hợp</p>
          </div>
        </div>
      </div>

      <!-- Right Panel: Cart & Checkout Form -->
      <div class="col-lg-4 d-flex flex-column h-100 p-4 pos-checkout-panel overflow-hidden" :class="{ 'd-none d-lg-flex': activeTab !== 'cart' }">
        <h5 class="fw-bold mb-3 d-flex align-items-center gap-2 text-cyan flex-shrink-0">
          <i class="fa-solid fa-basket-shopping text-cyan"></i> Giỏ hàng thanh toán
        </h5>

        <!-- Cart items list (Scrollable) -->
        <div class="cart-items-wrapper flex-grow-1 overflow-y-auto mb-3 pe-1">
          <div class="d-flex flex-column align-items-center justify-content-center text-muted-custom py-5 h-100" v-if="cart.length === 0">
            <i class="fa-solid fa-cart-flatbed fs-2 mb-2 text-slate-500 animate-bounce"></i>
            <p class="text-xs text-slate-400">Chưa có sản phẩm nào được chọn</p>
          </div>

          <div class="d-flex flex-column gap-2" v-else>
            <div v-for="item in cart" :key="item.id" class="cart-item d-flex align-items-center justify-content-between p-2.5 rounded-3">
              <div class="flex-grow-1 min-w-0 me-2">
                <div class="text-xs fw-bold text-slate-100 text-truncate" :title="item.name">{{ item.name }}</div>
                <div class="text-xxs text-slate-400 mt-0.5">{{ item.sku }} | {{ formatMoney(item.price) }} đ</div>
              </div>
              
              <!-- Quantity Controls -->
              <div class="d-flex align-items-center gap-2 flex-shrink-0">
                <button type="button" class="qty-btn" @click="updateQty(item.id, -1)">
                  <i class="fa-solid fa-minus"></i>
                </button>
                <span class="text-xs fw-bold text-slate-100 px-1" style="min-width: 15px; text-align: center;">{{ item.quantity }}</span>
                <button type="button" class="qty-btn" @click="updateQty(item.id, 1)">
                  <i class="fa-solid fa-plus"></i>
                </button>
                <button type="button" class="trash-btn ms-1" @click="removeFromCart(item.id)">
                  <i class="fa-solid fa-trash-can"></i>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Checkout Form & Pay Info -->
        <form @submit.prevent="handleCheckout" class="checkout-form border-top border-slate-800 pt-3 flex-shrink-0">
          <div v-if="successMsg" class="alert alert-success text-xs py-2 px-3 mb-3 d-flex align-items-center gap-2">
            <i class="fa-solid fa-circle-check fs-6"></i>
            <span>{{ successMsg }}</span>
          </div>
          <div v-if="errorMsg" class="alert alert-danger text-xs py-2 px-3 mb-3 d-flex align-items-center gap-2">
            <i class="fa-solid fa-circle-exclamation fs-6"></i>
            <span>{{ errorMsg }}</span>
          </div>

          <div class="row g-2 mb-3">
            <div class="col-6">
              <label class="form-label text-xxs text-slate-400 fw-semibold mb-1.5">Khách hàng</label>
              <select v-model="customerIndex" class="form-select text-xs input-pos" @change="onCustomerChange">
                <option value="-1">Khách Hàng Vãng Lai</option>
                <option v-for="(c, idx) in customers" :key="c.id" :value="idx">{{ c.name }}</option>
              </select>
            </div>
            <div class="col-6">
              <label class="form-label text-xxs text-slate-400 fw-semibold mb-1.5">Số điện thoại</label>
              <input type="text" v-model="customerPhone" class="form-control text-xs input-pos" placeholder="Nhập SĐT...">
            </div>
          </div>

          <!-- Pay Option Cards -->
          <div class="mb-3">
            <label class="form-label text-xxs text-slate-400 fw-semibold mb-1.5">Phương thức thanh toán</label>
            <div class="d-flex gap-2">
              <div class="payment-card flex-grow-1 text-center" :class="{ active: paymentMethod === 'Tiền mặt' }" @click="paymentMethod = 'Tiền mặt'">
                <i class="fa-solid fa-money-bill-1-wave fs-6 mb-1"></i>
                <span class="text-xxs fw-bold d-block">Tiền mặt</span>
              </div>
              <div class="payment-card flex-grow-1 text-center" :class="{ active: paymentMethod === 'Chuyển khoản' }" @click="paymentMethod = 'Chuyển khoản'">
                <i class="fa-solid fa-credit-card fs-6 mb-1"></i>
                <span class="text-xxs fw-bold d-block">Chuyển khoản</span>
              </div>
            </div>
          </div>

          <!-- Total Bill -->
          <div class="billing-summary d-flex justify-content-between align-items-center p-3 rounded-3 mb-3">
            <span class="text-xs fw-bold text-slate-300">Tổng tiền thanh toán</span>
            <span class="fs-4 text-emerald fw-extrabold">{{ formatMoney(totalAmount) }} đ</span>
          </div>

          <button type="submit" class="btn btn-checkout w-100 py-2.5 fw-bold text-sm" :disabled="cart.length === 0 || checkingOut">
            <span v-if="checkingOut" class="spinner-border spinner-border-sm me-2"></span>
            <i class="fa-solid fa-circle-check me-2" v-if="!checkingOut"></i> Xác nhận & In hóa đơn
          </button>
        </form>
      </div>
    </div>

    <!-- Printable Receipt Modal -->
    <div class="modal fade" id="receiptModal" tabindex="-1" aria-hidden="true" :class="{ show: showReceiptModal, 'd-block': showReceiptModal }" style="background: rgba(0,0,0,0.75);" v-if="showReceiptModal">
      <div class="modal-dialog modal-dialog-centered" style="max-width: 420px;">
        <div class="modal-content border-0 text-dark bg-white shadow-lg" style="border-radius: 16px;">
          <div class="modal-body p-4" id="printable-receipt">
            <div class="text-center mb-3">
              <h5 class="fw-bold m-0 text-dark">HÓA ĐƠN BÁN LẺ</h5>
              <div class="text-xs text-muted mt-1">NovaTech Store - Hệ thống POS thông minh</div>
              <div class="text-xxs text-muted">Đ/c: 123 Đường Ba Tháng Hai, Quận 10, TP.HCM</div>
            </div>
            
            <hr class="border-secondary border-dashed my-2">
            
            <div class="text-xs mb-3 d-grid gap-1">
              <div class="d-flex justify-content-between">
                <span class="text-muted">Mã hóa đơn:</span>
                <span class="fw-bold text-dark">{{ lastOrder.orderCode }}</span>
              </div>
              <div class="d-flex justify-content-between">
                <span class="text-muted">Thời gian:</span>
                <span class="text-dark">{{ lastOrder.date }}</span>
              </div>
              <div class="d-flex justify-content-between">
                <span class="text-muted">Thu ngân:</span>
                <span class="text-dark">{{ lastOrder.cashier }}</span>
              </div>
              <div class="d-flex justify-content-between">
                <span class="text-muted">Khách hàng:</span>
                <span class="text-dark">{{ lastOrder.customerName }}</span>
              </div>
              <div class="d-flex justify-content-between" v-if="lastOrder.customerPhone">
                <span class="text-muted">SĐT:</span>
                <span class="text-dark">{{ lastOrder.customerPhone }}</span>
              </div>
            </div>

            <hr class="border-secondary border-dashed my-2">

            <!-- Items table -->
            <div class="mb-3">
              <table class="w-100 text-xs">
                <thead>
                  <tr class="border-bottom text-muted">
                    <th class="py-1 text-start">Sản phẩm</th>
                    <th class="py-1 text-center" style="width: 50px;">SL</th>
                    <th class="py-1 text-end" style="width: 100px;">Thành tiền</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="item in lastOrder.items" :key="item.id" class="border-bottom">
                    <td class="py-2">
                      <div class="fw-semibold text-dark">{{ item.name }}</div>
                      <div class="text-xxs text-muted">{{ formatMoney(item.price) }} đ</div>
                    </td>
                    <td class="py-2 text-center text-dark">{{ item.quantity }}</td>
                    <td class="py-2 text-end text-dark fw-bold">{{ formatMoney(item.price * item.quantity) }} đ</td>
                  </tr>
                </tbody>
              </table>
            </div>

            <div class="d-grid gap-1 pt-2">
              <div class="d-flex justify-content-between text-xs">
                <span class="text-muted">Hình thức:</span>
                <span class="text-dark fw-bold">{{ lastOrder.paymentMethod }}</span>
              </div>
              <div class="d-flex justify-content-between align-items-center text-sm border-top border-secondary border-dashed pt-2 mt-1">
                <span class="fw-bold text-dark">TỔNG CỘNG:</span>
                <span class="fs-5 fw-extrabold text-danger">{{ formatMoney(lastOrder.totalAmount) }} đ</span>
              </div>
            </div>
            
            <div class="text-center text-muted text-xxs mt-4">
              Cảm ơn Quý khách. Hẹn gặp lại!
            </div>
          </div>
          <div class="modal-footer border-0 p-3 bg-light d-flex justify-content-end gap-2" style="border-bottom-left-radius: 16px; border-bottom-right-radius: 16px;">
            <button type="button" class="btn btn-outline-secondary btn-sm px-3" @click="showReceiptModal = false">Đóng</button>
            <button type="button" class="btn btn-info text-white btn-sm px-4 fw-semibold" @click="printReceipt">In hóa đơn</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'
import { authState } from '../services/auth'

export default {
  name: 'POS',
  setup() {
    const products = ref([])
    const customers = ref([])
    const cart = ref([])
    const loading = ref(true)
    const checkingOut = ref(false)

    // Form inputs
    const customerIndex = ref("-1")
    const customerName = ref("Khách Hàng Vãng Lai")
    const customerPhone = ref("")
    const paymentMethod = ref("Tiền mặt")

    const successMsg = ref("")
    const errorMsg = ref("")

    // Audit / Receipt popup variables
    const showReceiptModal = ref(false)
    const lastOrder = ref(null)

    // Search and Responsive active tab
    const searchQuery = ref('')
    const activeCategory = ref('Tất cả')
    const activeTab = ref('products')

    const user = computed(() => authState.user)

    const totalAmount = computed(() => {
      return cart.value.reduce((sum, item) => sum + (item.price * item.quantity), 0)
    })

    const categories = computed(() => {
      const cats = products.value.map(p => p.category).filter(Boolean)
      return [...new Set(cats)]
    })

    const filteredProducts = computed(() => {
      let list = products.value
      
      // Category filter
      if (activeCategory.value !== 'Tất cả') {
        list = list.filter(p => p.category === activeCategory.value)
      }
      
      // Search filter
      if (searchQuery.value.trim() !== '') {
        const query = searchQuery.value.toLowerCase().trim()
        list = list.filter(p => 
          p.name.toLowerCase().includes(query) || 
          p.sku.toLowerCase().includes(query)
        )
      }
      
      return list
    })

    const fetchProducts = async () => {
      try {
        const response = await axios.get('/api/GetProducts')
        products.value = response.data.filter(p => p.status === 'Đang bán')
      } catch (err) {
        console.error('Error fetching POS products:', err)
      }
    }

    const fetchCustomers = async () => {
      try {
        const response = await axios.get('/api/GetCustomers')
        customers.value = response.data
      } catch (err) {
        console.error('Error fetching POS customers:', err)
      }
    }

    const onCustomerChange = () => {
      const idx = parseInt(customerIndex.value)
      if (idx === -1) {
        customerName.value = "Khách Hàng Vãng Lai"
        customerPhone.value = ""
      } else {
        const c = customers.value[idx]
        customerName.value = c.name
        customerPhone.value = c.phone
      }
    }

    const addToCart = (product) => {
      if (product.stock <= 0) {
        alert('Sản phẩm đã hết hàng trong kho!')
        return
      }

      const existing = cart.value.find(i => i.id === product.id)
      if (existing) {
        if (existing.quantity >= product.stock) {
          alert('Số lượng chọn vượt quá tồn kho khả dụng!')
          return
        }
        existing.quantity++
      } else {
        cart.value.push({
          id: product.id,
          name: product.name,
          sku: product.sku,
          price: product.price,
          quantity: 1,
          maxStock: product.stock
        })
      }
    }

    const removeFromCart = (id) => {
      cart.value = cart.value.filter(i => i.id !== id)
    }

    const updateQty = (id, change) => {
      const item = cart.value.find(i => i.id === id)
      if (item) {
        item.quantity += change
        if (item.quantity > item.maxStock) {
          alert('Số lượng chọn vượt quá tồn kho khả dụng!')
          item.quantity = item.maxStock
        }
        if (item.quantity <= 0) {
          removeFromCart(id)
        }
      }
    }

    const handleCheckout = async () => {
      if (cart.value.length === 0) return
      checkingOut.value = true
      errorMsg.value = ""
      successMsg.value = ""

      const productIds = cart.value.map(i => i.id)
      const quantities = cart.value.map(i => i.quantity)

      try {
        const response = await axios.post('/api/CreateOrderPOS', {
          productIds,
          quantities,
          customerName: customerName.value,
          customerPhone: customerPhone.value,
          paymentMethod: paymentMethod.value
        })

        // Capture checkout values for the printable modal
        lastOrder.value = {
          orderCode: response.data.orderCode || 'POS-' + Math.floor(100000 + Math.random() * 900000),
          cashier: user.value?.fullName || 'Nhân viên',
          customerName: customerName.value,
          customerPhone: customerPhone.value,
          paymentMethod: paymentMethod.value,
          items: cart.value.map(i => ({...i})),
          totalAmount: totalAmount.value,
          date: new Date().toLocaleString('vi-VN')
        }
        showReceiptModal.value = true

        successMsg.value = response.data.message || 'Đơn hàng POS thanh toán thành công!'
        cart.value = []
        customerIndex.value = "-1"
        customerName.value = "Khách Hàng Vãng Lai"
        customerPhone.value = ""
        
        // Refresh products catalog stock count
        await fetchProducts()
      } catch (err) {
        errorMsg.value = err.response?.data?.message || 'Có lỗi xảy ra khi xác nhận thanh toán.'
      } finally {
        checkingOut.value = false
      }
    }

    const printReceipt = () => {
      const printContent = document.getElementById('printable-receipt').innerHTML
      const printWindow = window.open('about:blank', 'PrintWindow', 'left=500,top=50,width=450,height=700')
      
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
      `)
      printWindow.document.close()
      printWindow.focus()
    }

    const formatMoney = (val) => {
      if (val === undefined || val === null) return 0
      return Number(val).toLocaleString()
    }

    onMounted(async () => {
      loading.value = true
      await Promise.all([fetchProducts(), fetchCustomers()])
      loading.value = false
    })

    return {
      products,
      customers,
      cart,
      loading,
      checkingOut,
      customerIndex,
      customerPhone,
      paymentMethod,
      successMsg,
      errorMsg,
      user,
      totalAmount,
      categories,
      filteredProducts,
      searchQuery,
      activeCategory,
      activeTab,
      showReceiptModal,
      lastOrder,
      onCustomerChange,
      addToCart,
      removeFromCart,
      updateQty,
      handleCheckout,
      formatMoney,
      printReceipt
    }
  }
}
</script>

<style scoped>
.pos-wrapper {
  background-color: #0b0f19;
  height: 100vh;
  overflow: hidden;
  font-family: 'Inter', system-ui, -apple-system, sans-serif;
}

.pos-header {
  background-color: #0f172a;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  height: 65px;
}

.text-cyan {
  color: #38bdf8 !important;
}

.text-emerald {
  color: #34d399 !important;
}

.bg-dark-custom {
  background-color: rgba(255, 255, 255, 0.03);
}

.btn-back {
  background-color: rgba(255, 255, 255, 0.06);
  border: 1px solid rgba(255, 255, 255, 0.1);
  color: #f1f5f9;
  transition: all 0.2s ease;
}

.btn-back:hover {
  background-color: rgba(255, 255, 255, 0.12);
  color: #fff;
}

.pos-main-container {
  height: calc(100vh - 65px);
}

.search-box-pos input {
  background-color: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.1);
  color: #fff;
  padding: 0.55rem 1rem 0.55rem 2.2rem;
  transition: all 0.2s ease;
}

.search-box-pos input:focus {
  background-color: rgba(255, 255, 255, 0.05);
  border-color: #0c7eb6;
  box-shadow: 0 0 0 3px rgba(12, 126, 182, 0.15);
}

.category-filter-pos {
  scrollbar-width: none;
}

.category-filter-pos::-webkit-scrollbar {
  display: none;
}

.btn-outline-slate {
  background-color: rgba(255, 255, 255, 0.02);
  border: 1px solid rgba(255, 255, 255, 0.08);
  color: #94a3b8;
  transition: all 0.2s ease;
}

.btn-outline-slate:hover {
  background-color: rgba(255, 255, 255, 0.06);
  border-color: rgba(255, 255, 255, 0.15);
  color: #f1f5f9;
}

/* Products Grid and Card styling */
.products-grid-container {
  height: 100%;
}

.product-card {
  background-color: rgba(255, 255, 255, 0.02);
  border: 1px solid rgba(255, 255, 255, 0.06);
  border-radius: 12px;
  overflow: hidden;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
}

.product-card img {
  height: 110px;
  object-fit: cover;
  transition: transform 0.3s ease;
}

.product-card:hover {
  transform: translateY(-4px);
  border-color: rgba(56, 189, 248, 0.3);
  box-shadow: 0 10px 20px -5px rgba(56, 189, 248, 0.1);
}

.product-card:hover img {
  transform: scale(1.05);
}

/* POS Checkout Panel (Right side) */
.pos-checkout-panel {
  background-color: #0f172a;
}

/* Cart list styling */
.cart-items-wrapper {
  max-height: calc(100% - 380px);
}

.cart-item {
  background-color: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.06);
  transition: all 0.2s ease;
}

.cart-item:hover {
  background-color: rgba(255, 255, 255, 0.05);
  border-color: rgba(255, 255, 255, 0.1);
}

.qty-btn {
  background-color: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.1);
  color: #cbd5e1;
  width: 22px;
  height: 22px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 0.65rem;
  cursor: pointer;
  transition: all 0.15s ease;
  padding: 0;
}

.qty-btn:hover {
  background-color: #0c7eb6;
  border-color: #0c7eb6;
  color: #fff;
}

.trash-btn {
  background: transparent;
  border: none;
  color: #f43f5e;
  font-size: 0.85rem;
  cursor: pointer;
  transition: all 0.15s ease;
  padding: 2px 6px;
}

.trash-btn:hover {
  color: #e11d48;
  transform: scale(1.1);
}

/* Checkout Form inputs */
.input-pos {
  background-color: rgba(255, 255, 255, 0.03) !important;
  border: 1px solid rgba(255, 255, 255, 0.1) !important;
  color: #fff !important;
  border-radius: 8px;
}

.input-pos option {
  color: #0f172a !important;
  background-color: #ffffff !important;
}

.input-pos:focus {
  border-color: #0c7eb6 !important;
  box-shadow: 0 0 0 3px rgba(12, 126, 182, 0.15) !important;
}

.payment-card {
  background-color: rgba(255, 255, 255, 0.02);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 12px;
  padding: 10px;
  cursor: pointer;
  color: #94a3b8;
  transition: all 0.2s ease;
}

.payment-card:hover {
  background-color: rgba(255, 255, 255, 0.06);
  border-color: rgba(255, 255, 255, 0.15);
  color: #fff;
}

.payment-card.active {
  background-color: rgba(56, 189, 248, 0.1);
  border-color: #38bdf8;
  color: #38bdf8;
  box-shadow: 0 0 10px rgba(56, 189, 248, 0.15);
}

.billing-summary {
  background: linear-gradient(135deg, rgba(56, 189, 248, 0.05) 0%, rgba(52, 211, 153, 0.05) 100%);
  border: 1px solid rgba(56, 189, 248, 0.1);
}

.btn-checkout {
  background: linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%);
  color: #fff;
  border: none;
  border-radius: 30px;
  transition: all 0.3s ease;
  box-shadow: 0 4px 15px rgba(2, 132, 199, 0.25);
}

.btn-checkout:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 6px 20px rgba(2, 132, 199, 0.35);
}

.btn-checkout:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  box-shadow: none;
}

/* Custom scrollbars */
::-webkit-scrollbar {
  width: 6px;
  height: 6px;
}

::-webkit-scrollbar-track {
  background: rgba(255, 255, 255, 0.01);
}

::-webkit-scrollbar-thumb {
  background: rgba(255, 255, 255, 0.1);
  border-radius: 3px;
}

::-webkit-scrollbar-thumb:hover {
  background: rgba(255, 255, 255, 0.2);
}

/* Responsive Grid styling */
@media (max-width: 991.98px) {
  .pos-main-container {
    overflow-y: auto;
  }
  
  .products-grid-container {
    overflow-y: visible;
  }
}

/* Product name highlight */
.product-name-highlight {
  color: #ffffff;
  text-shadow: 0 0 8px rgba(56, 189, 248, 0.5), 0 1px 3px rgba(0, 0, 0, 0.6);
  letter-spacing: 0.01em;
  transition: color 0.2s ease, text-shadow 0.2s ease;
}

.product-card:hover .product-name-highlight {
  color: #7dd3fc;
  text-shadow: 0 0 14px rgba(125, 211, 252, 0.8), 0 1px 4px rgba(0, 0, 0, 0.6);
}

/* SKU code highlight */
.sku-highlight {
  color: #67e8f9;
  font-style: italic;
  letter-spacing: 0.04em;
  opacity: 0.85;
  transition: opacity 0.2s ease, color 0.2s ease;
}

.product-card:hover .sku-highlight {
  opacity: 1;
  color: #a5f3fc;
}

/* Welcome Ticker / Marquee */
.welcome-ticker-wrap {
  width: 100%;
  overflow: hidden;
  background-color: rgba(14, 165, 233, 0.06);
  border-top: 1px solid rgba(56, 189, 248, 0.18);
  border-bottom: 1px solid rgba(56, 189, 248, 0.18);
  padding: 5px 0;
  flex-shrink: 0;
  position: relative;
}

.welcome-ticker-wrap::before,
.welcome-ticker-wrap::after {
  content: '';
  position: absolute;
  top: 0;
  bottom: 0;
  width: 60px;
  z-index: 2;
  pointer-events: none;
}

.welcome-ticker-wrap::before {
  left: 0;
  background: linear-gradient(to right, #0d1b2a, transparent);
}

.welcome-ticker-wrap::after {
  right: 0;
  background: linear-gradient(to left, #0d1b2a, transparent);
}

.welcome-ticker-inner {
  display: flex;
  white-space: nowrap;
  will-change: transform;
  animation: ticker-scroll 32s linear infinite;
}

.welcome-ticker-text {
  display: inline-block;
  flex-shrink: 0;
  font-size: 0.72rem;
  font-weight: 500;
  letter-spacing: 0.04em;
  color: #38bdf8;
}

@keyframes ticker-scroll {
  from { transform: translateX(0); }
  to   { transform: translateX(-50%); }
}
</style>
