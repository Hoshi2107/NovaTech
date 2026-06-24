<template>
  <div class="container my-5 animate-fade-in bg-white p-5 rounded-4 border border-light shadow-sm text-dark" v-if="product">
    <div class="row g-5">
      <div class="col-md-6">
        <img :src="product.image" class="img-fluid rounded-4 shadow-sm border" :alt="product.name" style="width: 100%; max-height: 450px; object-fit: cover;">
      </div>
      <div class="col-md-6">
        <span class="badge bg-light text-primary border border-info border-opacity-25 rounded-pill px-3 py-1.5 mb-3 text-xs">{{ product.category }}</span>
        
        <span v-if="product.isBestSeller" class="badge bg-warning text-dark rounded-pill px-3 py-1.5 mb-3 text-xs ms-1">
          <i class="fa-solid fa-crown me-1"></i>Bán chạy nhất
        </span>

        <h1 class="fw-bold mb-2 text-dark">{{ product.name }}</h1>
        <p class="text-xs text-muted mb-1">Mã sản phẩm SKU: <strong class="text-secondary">{{ product.sku }}</strong></p>
        <p class="text-xs text-muted mb-3">Hãng sản xuất: <strong class="text-secondary">{{ product.brand }}</strong></p>
        
        <!-- Countdown Timer -->
        <div v-if="isDiscountActive" class="discount-countdown text-danger fw-bold mb-3 text-xs">
          <i class="fa-regular fa-clock me-1"></i>Flash Sale kết thúc sau: <span class="countdown-text">{{ countdownText }}</span>
        </div>
        
        <div class="my-4">
          <h3 class="text-danger fw-bold" v-if="isDiscountActive">
            {{ formatMoney(product.price) }} đ
            <span class="text-sm text-muted text-decoration-line-through fw-normal ms-2">{{ formatMoney(product.originalPrice) }} đ</span>
            <span class="badge bg-danger text-white text-xxs ms-2 align-middle">Giảm {{ product.discountRate }}%</span>
          </h3>
          <h3 class="text-primary fw-bold" v-else>
            {{ formatMoney(product.price) }} đ
          </h3>
        </div>
        
        <hr class="border-secondary border-opacity-10 my-4">
        
        <div class="mb-4">
          <label class="form-label text-xs fw-bold text-secondary mb-2">Số lượng</label>
          <div class="quantity-picker">
            <button type="button" @click="changeQty(-1)">-</button>
            <input type="text" readonly :value="quantity">
            <button type="button" @click="changeQty(1)">+</button>
          </div>
        </div>

        <h5 class="fw-bold text-dark mb-2">Thông số kỹ thuật</h5>
        <p class="text-xs text-secondary" style="white-space: pre-line; line-height: 1.6;">{{ product.specifications || 'Chưa cung cấp' }}</p>

        <button @click="addToCart" class="btn btn-primary rounded-pill px-5 py-3 text-white fw-bold mt-4 shadow-sm" :disabled="product.stock <= 0">
          <i class="fa-solid fa-cart-plus me-2"></i>{{ product.stock > 0 ? 'Thêm Vào Giỏ Hàng' : 'Hết hàng' }}
        </button>
      </div>
    </div>
  </div>
  
  <div class="text-center py-5" v-else>
    <div class="spinner-border text-info" role="status"></div>
  </div>
</template>

<script>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute } from 'vue-router'
import axios from 'axios'
import { cartService } from '../../services/cart'

export default {
  name: 'OnlineProductDetail',
  setup() {
    const route = useRoute()
    const product = ref(null)
    const quantity = ref(1)
    const now = ref(new Date())
    let timer = null

    const fetchProduct = async () => {
      try {
        const id = route.params.id
        const response = await axios.get('/api/GetProducts')
        // Find product by id
        const prod = response.data.find(p => p.id === parseInt(id))
        product.value = prod
      } catch (err) {
        console.error('Error fetching product detail:', err)
      }
    }

    const changeQty = (val) => {
      if (!product.value) return
      const nextVal = quantity.value + val
      quantity.value = Math.max(1, Math.min(product.value.stock, nextVal))
    }

    const addToCart = () => {
      if (!product.value) return
      cartService.addToCart(product.value, quantity.value)
      alert(`Đã thêm ${quantity.value} sản phẩm vào giỏ hàng thành công!`)
    }

    const isDiscountActive = computed(() => {
      if (!product.value) return false
      return product.value.discountExpiry && new Date(product.value.discountExpiry) > now.value
    })

    const countdownText = computed(() => {
      if (!product.value || !product.value.discountExpiry) return ''
      const expiry = new Date(product.value.discountExpiry)
      const diff = expiry - now.value
      if (diff <= 0) return 'Đã kết thúc'

      const days = Math.floor(diff / (1000 * 60 * 60 * 24))
      const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60))
      const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60))
      const seconds = Math.floor((diff % (1000 * 60)) / 1000)

      let txt = ''
      if (days > 0) txt += `${days}d `
      txt += `${String(hours).padStart(2, '0')}h ${String(minutes).padStart(2, '0')}m ${String(seconds).padStart(2, '0')}s`
      return txt
    })

    const formatMoney = (val) => {
      if (val === undefined || val === null) return 0
      return Number(val).toLocaleString()
    }

    onMounted(() => {
      fetchProduct()
      timer = setInterval(() => {
        now.value = new Date()
      }, 1000)
    })

    onUnmounted(() => {
      if (timer) clearInterval(timer)
    })

    return {
      product,
      quantity,
      changeQty,
      addToCart,
      isDiscountActive,
      countdownText,
      formatMoney
    }
  }
}
</script>
