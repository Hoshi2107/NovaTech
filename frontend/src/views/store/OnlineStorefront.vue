<template>
  <div>
    <!-- Hero Banner Section -->
    <div class="p-5 mb-5 rounded-4 shadow-sm" style="background: linear-gradient(135deg, rgba(255, 255, 255, 0.95) 0%, rgba(241, 245, 249, 0.9) 100%), url('https://images.unsplash.com/photo-1531297484001-80022131f5a1?q=80&w=1200&auto=format&fit=crop'); background-blend-mode: overlay; background-size: cover; border: 1px solid #e2e8f0;">
      <div class="container-fluid py-5">
        <h1 class="display-4 fw-extrabold text-neon">Siêu Thị Công Nghệ NovaTech</h1>
        <p class="col-md-8 fs-5 text-dark" style="line-height: 1.6;">Chào mừng đến với cửa hàng bán lẻ thiết bị công nghệ uy tín. Tận hưởng ưu đãi độc quyền tại cửa hàng trực tuyến tích hợp của chúng tôi.</p>
        <div class="d-flex gap-3 mt-4">
          <router-link to="/store/products" class="btn btn-info rounded-pill px-4 text-white fw-bold shadow-sm">Mua Sắm Ngay <i class="fa-solid fa-arrow-right ms-2"></i></router-link>
          <a href="#" class="btn btn-outline-secondary rounded-pill px-4">Tìm hiểu thêm</a>
        </div>
      </div>
    </div>

    <!-- Category Section (ChoTot Style) -->
    <div class="container my-5 animate-fade-in text-dark">
      <h5 class="fw-bold mb-4 text-dark"><i class="fa-solid fa-layer-group text-primary me-2"></i>Danh mục sản phẩm điện tử</h5>
      <div class="d-flex flex-wrap justify-content-center justify-content-md-between gap-3">
        <div class="chotot-cat-card" @click="goToCategory('Điện thoại')">
          <div class="chotot-cat-icon"><i class="fa-solid fa-mobile-screen-button"></i></div>
          <div class="chotot-cat-title">Điện thoại</div>
        </div>
        <div class="chotot-cat-card" @click="goToCategory('Laptop')">
          <div class="chotot-cat-icon"><i class="fa-solid fa-laptop"></i></div>
          <div class="chotot-cat-title">Laptop</div>
        </div>
        <div class="chotot-cat-card" @click="goToCategory('Máy tính bảng')">
          <div class="chotot-cat-icon"><i class="fa-solid fa-tablet-screen-button"></i></div>
          <div class="chotot-cat-title">Máy tính bảng</div>
        </div>
        <div class="chotot-cat-card" @click="goToCategory('Tai nghe')">
          <div class="chotot-cat-icon"><i class="fa-solid fa-headphones"></i></div>
          <div class="chotot-cat-title">Tai nghe</div>
        </div>
        <div class="chotot-cat-card" @click="goToCategory('Phụ kiện')">
          <div class="chotot-cat-icon"><i class="fa-solid fa-keyboard"></i></div>
          <div class="chotot-cat-title">Phụ kiện</div>
        </div>
        <div class="chotot-cat-card" @click="goToCategory('Loa')">
          <div class="chotot-cat-icon"><i class="fa-solid fa-volume-high"></i></div>
          <div class="chotot-cat-title">Loa & Âm thanh</div>
        </div>
      </div>
    </div>

    <!-- FLASH SALE COUNTDOWN SECTION -->
    <div class="container my-5 animate-fade-in p-4 rounded-4" style="background: linear-gradient(135deg, #fff5f5 0%, #fff 100%); border: 1px solid #fecaca;" v-if="flashSaleProducts.length > 0">
      <div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
        <div class="d-flex align-items-center gap-2">
          <span class="flash-sale-badge"><i class="fa-solid fa-bolt-lightning me-1"></i>FLASH SALE</span>
          <h3 class="fw-bold text-dark m-0">Giá sốc hôm nay</h3>
        </div>
        <div class="countdown-box">
          <i class="fa-regular fa-clock"></i> Kết thúc sau: <span>{{ globalCountdownText }}</span>
        </div>
      </div>
      
      <div class="row row-cols-1 row-cols-md-4 g-4">
        <div class="col" v-for="p in flashSaleProducts" :key="p.id">
          <div class="card card-product h-100 p-3 shadow-sm border border-light bg-white">
            <span class="badge bg-danger badge-top-right text-xxs px-2 py-1 shadow-sm">Giảm {{ p.discountRate }}%</span>
            
            <img :src="p.image" class="card-img-top rounded-3 mb-3" :alt="p.name" style="height: 180px; object-fit: cover;">
            <div class="card-body p-0 d-flex flex-column justify-content-between">
              <div>
                <span class="badge bg-light text-primary border border-info border-opacity-25 mb-2 text-xxs">{{ p.category }}</span>
                <h5 class="card-title fw-bold text-dark fs-6 mb-1">{{ p.name }}</h5>
                <p class="text-xxs text-muted mb-2">Thương hiệu: {{ p.brand }}</p>
                
                <div class="mb-3">
                  <span class="card-text text-danger fw-bold fs-5">{{ formatMoney(p.price) }} đ</span>
                  <span class="text-xxs text-muted text-decoration-line-through ms-1">{{ formatMoney(p.originalPrice) }} đ</span>
                </div>
              </div>
              
              <div class="d-grid gap-2">
                <button class="btn btn-outline-danger rounded-pill text-xs py-2 fw-semibold" type="button" @click="toggleDetails('fs-' + p.id)">
                  Xem Chi Tiết
                </button>
                <button class="btn btn-danger rounded-pill text-xs py-2 fw-semibold text-white shadow-sm" type="button" @click="addToCart(p)">
                  <i class="fa-solid fa-cart-plus me-1"></i>Thêm Vào Giỏ
                </button>
              </div>
            </div>

            <!-- Collapsible inline details -->
            <div class="mt-3" v-if="expandedDetails.includes('fs-' + p.id)">
              <hr class="my-2 border-secondary border-opacity-10">
              <div class="p-3 bg-light rounded-3 text-xs text-dark border">
                <div class="mb-1.5"><strong>Mã SKU:</strong> <code>{{ p.sku }}</code></div>
                <div class="mb-1.5"><strong>Hãng:</strong> {{ p.brand }}</div>
                <div class="mb-2"><strong>Thông số kỹ thuật:</strong></div>
                <div class="bg-white p-2 rounded text-xxs text-secondary border border-opacity-25" style="white-space: pre-line; max-height: 120px; overflow-y: auto;">
                  {{ p.specifications }}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- BEST SELLERS SECTION -->
    <div class="container my-5 text-dark">
      <div class="d-flex justify-content-between align-items-center mb-4">
        <h3 class="fw-bold text-dark m-0"><i class="fa-solid fa-crown text-warning me-2"></i>Sản phẩm bán chạy nhất</h3>
        <router-link to="/store/products" class="text-primary text-decoration-none fw-semibold">Xem tất cả <i class="fa-solid fa-angles-right ms-1"></i></router-link>
      </div>

      <div v-if="loading" class="text-center py-5">
        <div class="spinner-border text-info" role="status"></div>
      </div>

      <div class="row row-cols-1 row-cols-md-4 g-4" v-else>
        <div class="col" v-for="p in bestSellers" :key="p.id">
          <div class="card card-product h-100 p-3 shadow-sm border border-light bg-white">
            <span v-if="isDiscountActive(p)" class="badge bg-danger badge-top-right text-xxs px-2 py-1 shadow-sm">Giảm {{ p.discountRate }}%</span>
            <span v-else class="badge bg-warning text-dark badge-top-left text-xxs px-2 py-1 shadow-sm"><i class="fa-solid fa-crown me-1"></i>Best Seller</span>

            <img :src="p.image" class="card-img-top rounded-3 mb-3" :alt="p.name" style="height: 180px; object-fit: cover;">
            <div class="card-body p-0 d-flex flex-column justify-content-between">
              <div>
                <span class="badge bg-light text-primary border border-info border-opacity-25 mb-2 text-xxs">{{ p.category }}</span>
                <h5 class="card-title fw-bold text-dark fs-6 mb-1">{{ p.name }}</h5>
                <p class="text-xxs text-muted mb-2">Thương hiệu: {{ p.brand }}</p>
                
                <div class="mb-3">
                  <div v-if="isDiscountActive(p)">
                    <span class="card-text text-danger fw-bold fs-5">{{ formatMoney(p.price) }} đ</span>
                    <span class="text-xxs text-muted text-decoration-line-through ms-1">{{ formatMoney(p.originalPrice) }} đ</span>
                  </div>
                  <div v-else>
                    <span class="card-text text-primary fw-bold fs-5">{{ formatMoney(p.price) }} đ</span>
                  </div>
                </div>
              </div>
              
              <div class="d-grid gap-2">
                <button class="btn btn-outline-primary rounded-pill text-xs py-2 fw-semibold" type="button" @click="toggleDetails('bs-' + p.id)">
                  Xem Chi Tiết
                </button>
                <button class="btn btn-primary rounded-pill text-xs py-2 fw-semibold text-white shadow-sm" type="button" @click="addToCart(p)" :disabled="p.stock <= 0">
                  <i class="fa-solid fa-cart-plus me-1"></i>Thêm Vào Giỏ
                </button>
              </div>
            </div>

            <!-- Collapsible inline details -->
            <div class="mt-3" v-if="expandedDetails.includes('bs-' + p.id)">
              <hr class="my-2 border-secondary border-opacity-10">
              <div class="p-3 bg-light rounded-3 text-xs text-dark border">
                <div class="mb-1.5"><strong>Mã SKU:</strong> <code>{{ p.sku }}</code></div>
                <div class="mb-1.5"><strong>Hãng:</strong> {{ p.brand }}</div>
                <div class="mb-2"><strong>Thông số kỹ thuật:</strong></div>
                <div class="bg-white p-2 rounded text-xxs text-secondary border border-opacity-25" style="white-space: pre-line; max-height: 120px; overflow-y: auto;">
                  {{ p.specifications }}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, onMounted, computed, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'
import { cartService } from '../../services/cart'

export default {
  name: 'OnlineStorefront',
  setup() {
    const router = useRouter()
    const products = ref([])
    const loading = ref(true)
    const expandedDetails = ref([])
    const now = ref(new Date())
    let countdownInterval = null

    const fetchProducts = async () => {
      try {
        loading.value = true
        const response = await axios.get('/api/GetProducts')
        products.value = response.data || []
      } catch (err) {
        console.error('Error fetching storefront products:', err)
      } finally {
        loading.value = false
      }
    }

    const flashSaleProducts = computed(() => {
      return products.value.filter(p => p.discountExpiry && new Date(p.discountExpiry) > now.value)
    })

    const bestSellers = computed(() => {
      return products.value.filter(p => p.isBestSeller)
    })

    const maxExpiry = computed(() => {
      if (flashSaleProducts.value.length === 0) return null
      const dates = flashSaleProducts.value.map(p => new Date(p.discountExpiry))
      return new Date(Math.max(...dates))
    })

    const globalCountdownText = computed(() => {
      if (!maxExpiry.value) return '--:--:--'
      const diff = maxExpiry.value - now.value
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

    const isDiscountActive = (p) => {
      return p.discountExpiry && new Date(p.discountExpiry) > now.value
    }

    const goToCategory = (cat) => {
      router.push(`/store/products?category=${cat}`)
    }

    const addToCart = (p) => {
      cartService.addToCart(p)
      alert('Đã thêm sản phẩm vào giỏ hàng!')
    }

    const formatMoney = (val) => {
      if (val === undefined || val === null) return 0
      return Number(val).toLocaleString()
    }

    const toggleDetails = (key) => {
      const idx = expandedDetails.value.indexOf(key)
      if (idx > -1) {
        expandedDetails.value.splice(idx, 1)
      } else {
        expandedDetails.value.push(key)
      }
    }

    onMounted(() => {
      fetchProducts()
      countdownInterval = setInterval(() => {
        now.value = new Date()
      }, 1000)
    })

    onUnmounted(() => {
      if (countdownInterval) clearInterval(countdownInterval)
    })

    return {
      products,
      loading,
      flashSaleProducts,
      bestSellers,
      globalCountdownText,
      isDiscountActive,
      goToCategory,
      addToCart,
      formatMoney,
      expandedDetails,
      toggleDetails
    }
  }
}
</script>
