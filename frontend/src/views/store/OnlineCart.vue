<template>
  <div class="container my-5 text-dark animate-fade-in">
    <h3 class="fw-bold mb-4 text-dark"><i class="fa-solid fa-cart-shopping text-primary me-2"></i>Giỏ hàng của bạn</h3>
    
    <div v-if="successMsg" class="card card-glass p-5 text-center bg-white shadow-sm border border-light">
      <div class="fs-1 text-success mb-3"><i class="fa-solid fa-circle-check"></i></div>
      <h4 class="fw-bold text-success">{{ successMsg }}</h4>
      <p class="text-xs text-muted-custom">Đơn hàng của bạn đang được hệ thống xử lý. Xin cảm ơn!</p>
      <div class="mt-4">
        <router-link to="/store" class="btn btn-primary rounded-pill px-5 py-2 text-xs text-white">Quay lại trang chủ</router-link>
      </div>
    </div>

    <div class="row g-4" v-else-if="cartItems.length > 0">
      <!-- Left Side: Products List Table -->
      <div class="col-lg-7 col-xl-8">
        <div class="card border border-light shadow-sm p-4 bg-white rounded-4 mb-4">
          <h5 class="fw-bold text-dark mb-4">Danh sách sản phẩm ({{ totalItemsCount }})</h5>
          
          <div class="table-responsive">
            <table class="table align-middle table-borderless">
              <thead>
                <tr class="border-bottom border-light" style="font-size: 13px; color: #64748b;">
                  <th colspan="2">Sản phẩm</th>
                  <th>Giá</th>
                  <th>Số lượng</th>
                  <th class="text-end">Tổng</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="item in cartItems" :key="item.productId" class="border-bottom border-light">
                  <td style="width: 70px;">
                    <img :src="item.image" class="rounded-3 border" style="width: 60px; height: 60px; object-fit: cover;">
                  </td>
                  <td>
                    <div class="fw-bold text-dark text-sm">{{ item.name }}</div>
                    <span v-if="item.isDiscounted" class="badge bg-danger-subtle text-danger text-xxs border border-danger border-opacity-10 mt-1">
                      <i class="fa-solid fa-bolt me-1"></i>Flash Sale
                    </span>
                  </td>
                  <td>
                    <div class="text-sm fw-bold">{{ formatMoney(item.price) }} đ</div>
                    <div v-if="item.isDiscounted" class="text-xxs text-muted text-decoration-line-through">
                      {{ formatMoney(item.originalPrice) }} đ
                    </div>
                  </td>
                  <td>
                    <div class="quantity-picker">
                      <button type="button" @click="changeQty(item.productId, -1)">-</button>
                      <input type="text" :value="item.quantity" readonly>
                      <button type="button" @click="changeQty(item.productId, 1)">+</button>
                    </div>
                  </td>
                  <td class="text-end fw-bold text-dark text-sm">
                    <span>{{ formatMoney(item.price * item.quantity) }}</span> đ
                    <button type="button" class="btn btn-sm btn-link text-danger p-0 ms-3" @click="removeFromCart(item.productId)" title="Xóa">
                      <i class="fa-solid fa-trash-can"></i>
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div class="d-flex justify-content-between">
          <router-link to="/store/products" class="btn btn-outline-secondary rounded-pill px-4 text-xs">
            <i class="fa-solid fa-arrow-left me-2"></i>Tiếp tục mua hàng
          </router-link>
        </div>
      </div>

      <!-- Right Side: Order Summary & Checkout Form -->
      <div class="col-lg-5 col-xl-4">
        <div class="card border border-light shadow-sm p-4 bg-white rounded-4">
          <h5 class="fw-bold text-dark mb-4">Thông tin thanh toán</h5>

          <!-- Order Summary -->
          <div class="p-3 bg-light rounded-3 mb-4 text-xs">
            <div class="d-flex justify-content-between mb-2">
              <span class="text-secondary">Tạm tính:</span>
              <span class="fw-bold text-dark">{{ formatMoney(cartTotal) }} đ</span>
            </div>
            <div class="d-flex justify-content-between mb-2 text-danger">
              <span>Giảm giá Voucher:</span>
              <span class="fw-bold">- {{ formatMoney(discountVal) }} đ</span>
            </div>
            <hr class="border-secondary border-opacity-10 my-2">
            <div class="d-flex justify-content-between align-items-center">
              <span class="fw-bold text-dark text-sm">Tổng cộng:</span>
              <span class="fw-extrabold text-primary text-base">{{ formatMoney(checkoutTotal) }} đ</span>
            </div>
          </div>

          <!-- Voucher Input -->
          <div class="mb-4">
            <label class="form-label fw-bold text-secondary text-xs text-uppercase mb-2">Mã giảm giá</label>
            <div class="input-group">
              <input type="text" v-model="voucherInput" class="form-control text-xs" placeholder="Nhập mã (Ví dụ: NOVATECH10)...">
              <button class="btn btn-outline-primary text-xs px-3" type="button" @click="applyVoucher">Áp dụng</button>
            </div>
            <div class="text-xxs text-muted mt-2" style="line-height: 1.4;">
              <i class="fa-solid fa-circle-info me-1 text-info"></i>Mã giảm giá chỉ được tính trên sản phẩm giá thường, không áp dụng cho sản phẩm đang giảm giá Flash Sale.
            </div>
            <div v-if="voucherMsg" class="text-xxs mt-2 fw-bold" :class="voucherSuccess ? 'text-success' : 'text-danger'">
              {{ voucherMsg }}
            </div>

            <!-- Available Vouchers List -->
            <div v-if="availableVouchers.length > 0" class="mt-3">
              <span class="text-xxs fw-bold text-secondary">MÃ GIẢM GIÁ ĐANG CÓ:</span>
              <div class="d-flex flex-wrap gap-2 mt-1.5">
                <span v-for="v in availableVouchers" :key="v.id" 
                      class="badge bg-light text-primary border border-info border-opacity-25 cursor-pointer text-xxs py-1.5 px-2"
                      @click="useVoucherCode(v.code)"
                      :title="v.type + ': Giảm ' + formatMoney(v.value) + (v.type === 'Giảm %' ? '%' : 'đ') + ' (Đơn từ ' + formatMoney(v.minOrderValue) + 'đ)'">
                  {{ v.code }}
                </span>
              </div>
            </div>
          </div>

          <!-- Checkout Form -->
          <form @submit.prevent="handleCheckout">
            <div v-if="errorMsg" class="alert alert-danger text-xs py-2 px-3 mb-3">
              {{ errorMsg }}
            </div>

            <div class="mb-3">
              <label class="form-label fw-bold text-secondary text-xs">Họ và tên người nhận</label>
              <input type="text" v-model="form.customerName" class="form-control" required placeholder="Nguyễn Văn A...">
            </div>
            <div class="mb-3">
              <label class="form-label fw-bold text-secondary text-xs">Số điện thoại liên hệ</label>
              <input type="tel" v-model="form.customerPhone" class="form-control" required placeholder="0901234567...">
            </div>
            <div class="mb-3">
              <label class="form-label fw-bold text-secondary text-xs">Địa chỉ giao hàng</label>
              <textarea v-model="form.customerAddress" class="form-control" rows="2" required placeholder="Số nhà, tên đường, phường/xã, quận/huyện..."></textarea>
            </div>
            <div class="mb-4">
              <label class="form-label fw-bold text-secondary text-xs">Phương thức thanh toán</label>
              <select v-model="form.paymentMethod" class="form-select">
                <option value="COD">Thanh toán khi nhận hàng (COD)</option>
                <option value="Chuyển khoản">Chuyển khoản Ngân hàng</option>
                <option value="Tiền mặt">Đến nhận tại cửa hàng (Tiền mặt)</option>
              </select>
            </div>
            <div class="mb-4">
              <label class="form-label fw-bold text-secondary text-xs">Ghi chú đặt hàng (nếu có)</label>
              <textarea v-model="form.note" class="form-control" rows="2" placeholder="Ghi chú thêm cho shipper hoặc cửa hàng..."></textarea>
            </div>

            <button type="submit" class="btn btn-primary rounded-pill w-100 py-3 text-xs fw-bold shadow-sm" :disabled="submitting">
              <span v-if="submitting" class="spinner-border spinner-border-sm me-2"></span>
              <i class="fa-solid fa-credit-card me-2" v-else></i>ĐẶT HÀNG NGAY
            </button>
          </form>
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div class="text-center py-5 bg-white rounded-4 shadow-sm border border-light" v-else>
      <div class="fs-1 text-muted mb-3"><i class="fa-solid fa-basket-shopping"></i></div>
      <h4 class="text-secondary fw-semibold">Giỏ hàng đang trống</h4>
      <p class="text-xs text-muted mb-4">Vui lòng quay lại cửa hàng để chọn sản phẩm.</p>
      <router-link to="/store/products" class="btn btn-primary rounded-pill px-5 py-2.5 text-xs fw-bold text-white">Tiếp tục mua sắm</router-link>
    </div>
  </div>
</template>

<script>
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'
import { cartService } from '../../services/cart'

export default {
  name: 'OnlineCart',
  setup() {
    const submitting = ref(false)
    const successMsg = ref('')
    const errorMsg = ref('')
    
    // Voucher States
    const voucherInput = ref('')
    const voucherMsg = ref('')
    const voucherSuccess = ref(false)
    const discountVal = ref(0)
    const appliedVoucherCode = ref('')
    const availableVouchers = ref([])

    const form = ref({
      customerName: '',
      customerPhone: '',
      customerAddress: '',
      paymentMethod: 'COD',
      note: ''
    })

    const cartItems = computed(() => cartService.items.value)
    
    const totalItemsCount = computed(() => {
      return cartItems.value.reduce((sum, item) => sum + item.quantity, 0)
    })

    const cartTotal = computed(() => cartService.total.value)

    const checkoutTotal = computed(() => {
      const total = cartTotal.value - discountVal.value
      return total < 0 ? 0 : total
    })

    const fetchAvailableVouchers = async () => {
      try {
        const response = await axios.get('/api/GetVouchers')
        availableVouchers.value = response.data || []
      } catch (err) {
        console.error('Error fetching vouchers:', err)
      }
    }

    const changeQty = (productId, delta) => {
      const item = cartItems.value.find(i => i.productId === productId)
      if (!item) return

      const val = item.quantity + delta
      if (val <= 0) {
        if (confirm('Bạn muốn xóa sản phẩm này khỏi giỏ hàng?')) {
          cartService.removeFromCart(productId)
          // Recalculate voucher if any is applied
          if (appliedVoucherCode.value) {
            applyVoucher()
          }
        }
        return
      }

      cartService.updateQuantity(productId, val)
      // Recalculate voucher if one is applied
      if (appliedVoucherCode.value) {
        applyVoucher()
      }
    }

    const removeFromCart = (id) => {
      if (confirm('Bạn muốn xóa sản phẩm này khỏi giỏ hàng?')) {
        cartService.removeFromCart(id)
        if (appliedVoucherCode.value) {
          applyVoucher()
        }
      }
    }

    const useVoucherCode = (code) => {
      voucherInput.value = code
      applyVoucher()
    }

    const applyVoucher = async () => {
      const code = voucherInput.value.trim()
      if (code === '') return

      try {
        const payloadItems = cartItems.value.map(i => ({
          productId: i.productId,
          quantity: i.quantity
        }))

        const response = await axios.post('/api/ApplyVoucher', {
          code: code,
          cartItems: payloadItems
        })

        if (response.data.success) {
          discountVal.value = response.data.discount
          appliedVoucherCode.value = code
          voucherSuccess.value = true
          voucherMsg.value = response.data.message
        } else {
          resetDiscount()
          voucherSuccess.value = false
          voucherMsg.value = response.data.message || 'Mã giảm giá không hợp lệ!'
        }
      } catch (err) {
        resetDiscount()
        voucherSuccess.value = false
        voucherMsg.value = err.response?.data?.message || 'Không thể áp dụng mã giảm giá!'
      }
    }

    const resetDiscount = () => {
      discountVal.value = 0
      appliedVoucherCode.value = ''
    }

    const handleCheckout = async () => {
      if (cartItems.value.length === 0) return
      submitting.value = true
      errorMsg.value = ''
      successMsg.value = ''

      const itemsPayload = cartItems.value.map(i => ({
        productId: i.productId,
        quantity: i.quantity
      }))

      try {
        const response = await axios.post('/api/CreateOrderOnline', {
          ...form.value,
          items: itemsPayload,
          voucherCode: appliedVoucherCode.value,
          discount: discountVal.value
        })
        
        successMsg.value = response.data.message || 'Đặt hàng thành công!'
        cartService.clearCart()
        resetDiscount()
      } catch (err) {
        errorMsg.value = err.response?.data?.message || 'Có lỗi xảy ra khi xác nhận đặt hàng.'
      } finally {
        submitting.value = false
      }
    }

    const formatMoney = (val) => {
      if (val === undefined || val === null) return 0
      return Number(val).toLocaleString()
    }

    onMounted(() => {
      fetchAvailableVouchers()
    })

    return {
      form,
      submitting,
      successMsg,
      errorMsg,
      cartItems,
      totalItemsCount,
      cartTotal,
      checkoutTotal,
      voucherInput,
      voucherMsg,
      voucherSuccess,
      discountVal,
      availableVouchers,
      changeQty,
      removeFromCart,
      useVoucherCode,
      applyVoucher,
      handleCheckout,
      formatMoney
    }
  }
}
</script>
