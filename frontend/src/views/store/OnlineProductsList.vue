<template>
  <div class="container my-5 animate-fade-in text-dark">
    <div class="row g-4">
      <!-- Sidebar Filters (Left Side) -->
      <div class="col-lg-3">
        <div class="card shadow-sm p-4 border border-light bg-white rounded-4 sticky-lg-top" style="top: 90px; z-index: 10;">
          <div class="d-flex justify-content-between align-items-center mb-4">
            <h5 class="fw-bold m-0 text-dark"><i class="fa-solid fa-filter text-primary me-2"></i>Bộ lọc tìm kiếm</h5>
            <button type="button" class="btn btn-sm btn-link text-decoration-none text-xs p-0 border-0 bg-transparent" @click="resetAllFilters">Xóa bộ lọc</button>
          </div>
          
          <!-- 1. Search Box -->
          <div class="mb-4">
            <label class="form-label fw-bold text-secondary text-xs text-uppercase mb-2">Tìm kiếm nhanh</label>
            <div class="position-relative">
              <input type="text" v-model="searchQuery" class="form-control text-sm ps-3 pe-5" placeholder="Nhập tên sản phẩm...">
              <i class="fa-solid fa-magnifying-glass position-absolute top-50 translate-middle-y end-0 me-3 text-muted"></i>
            </div>
          </div>

          <!-- 2. Categories -->
          <div class="mb-4">
            <label class="form-label fw-bold text-secondary text-xs text-uppercase mb-2">Thiết bị điện tử</label>
            <div class="d-flex flex-column gap-1.5">
              <button type="button" class="btn btn-sm text-start py-1.5 px-2.5 rounded border-0 fw-medium text-xs w-100" 
                      :class="selectedCategory === '' ? 'btn-primary text-white' : 'btn-light text-dark bg-transparent hover-bg-light'" 
                      @click="selectedCategory = ''">
                Tất cả thiết bị
              </button>
              <button type="button" v-for="catName in categoryNames" :key="catName"
                      class="btn btn-sm text-start py-1.5 px-2.5 rounded border-0 fw-medium text-xs w-100" 
                      :class="selectedCategory === catName ? 'btn-primary text-white' : 'btn-light text-dark bg-transparent hover-bg-light'" 
                      @click="selectedCategory = catName">
                {{ catName === 'Loa' ? 'Loa & Âm thanh' : catName }}
              </button>
            </div>
          </div>

          <!-- 3. Brands Checkbox List -->
          <div class="mb-4">
            <label class="form-label fw-bold text-secondary text-xs text-uppercase mb-2">Thương hiệu</label>
            <input type="text" v-model="brandSearchQuery" class="form-control text-xs mb-2 py-1 px-2.5" placeholder="Tìm nhanh hãng...">
            <div class="brand-checkbox-list" style="max-height: 180px; overflow-y: auto;">
              <div class="form-check mb-1.5" v-for="b in filteredBrands" :key="b">
                <input class="form-check-input" type="checkbox" :value="b" v-model="checkedBrands" :id="'brand-chk-' + b">
                <label class="form-check-label text-xs cursor-pointer text-dark" :for="'brand-chk-' + b">{{ b }}</label>
              </div>
            </div>
          </div>

          <!-- 4. Price Ranges -->
          <div class="mb-2">
            <label class="form-label fw-bold text-secondary text-xs text-uppercase mb-2">Mức giá</label>
            <div class="d-flex flex-column gap-1.5">
              <div class="form-check">
                <input class="form-check-input" type="checkbox" value="under-5" v-model="checkedPrices" id="price-under-5">
                <label class="form-check-label text-xs cursor-pointer text-dark" for="price-under-5">Dưới 5 triệu</label>
              </div>
              <div class="form-check">
                <input class="form-check-input" type="checkbox" value="5-15" v-model="checkedPrices" id="price-5-15">
                <label class="form-check-label text-xs cursor-pointer text-dark" for="price-5-15">5 triệu - 15 triệu</label>
              </div>
              <div class="form-check">
                <input class="form-check-input" type="checkbox" value="15-30" v-model="checkedPrices" id="price-15-30">
                <label class="form-check-label text-xs cursor-pointer text-dark" for="price-15-30">15 triệu - 30 triệu</label>
              </div>
              <div class="form-check">
                <input class="form-check-input" type="checkbox" value="over-30" v-model="checkedPrices" id="price-over-30">
                <label class="form-check-label text-xs cursor-pointer text-dark" for="price-over-30">Trên 30 triệu</label>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Right Side Products Grid -->
      <div class="col-lg-9">
        <div class="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-3">
          <div>
            <h4 class="fw-bold text-dark m-0">
              {{ selectedCategory === '' ? 'Tất cả sản phẩm NovaTech' : 'Thiết bị: ' + (selectedCategory === 'Loa' ? 'Loa & Âm thanh' : selectedCategory) }}
            </h4>
            <span class="text-xs text-muted">Tìm thấy <strong class="text-dark">{{ sortedProducts.length }}</strong> sản phẩm</span>
          </div>

          <!-- Sorting Select -->
          <div class="d-flex align-items-center gap-2">
            <span class="text-xs text-muted whitespace-nowrap">Sắp xếp:</span>
            <select v-model="sortBy" class="form-select text-xs py-1.5 px-3 rounded-pill bg-white border" style="width: 160px;">
              <option value="default">Mới nhất</option>
              <option value="price-asc">Giá tăng dần</option>
              <option value="price-desc">Giá giảm dần</option>
              <option value="name-asc">Tên A-Z</option>
            </select>
          </div>
        </div>

        <!-- Empty State -->
        <div class="text-center py-5 bg-white rounded-4 shadow-sm border border-light" v-if="sortedProducts.length === 0">
          <div class="fs-1 text-muted mb-3"><i class="fa-regular fa-folder-open"></i></div>
          <h5 class="text-secondary fw-semibold">Không tìm thấy sản phẩm nào</h5>
          <p class="text-xs text-muted-custom">Vui lòng điều chỉnh hoặc xóa bộ lọc hiện tại.</p>
        </div>

        <!-- Product Grid -->
        <div class="row row-cols-1 row-cols-md-3 g-4" v-else>
          <div class="col animate-fade-in" v-for="p in sortedProducts" :key="p.id">
            <div class="card card-product h-100 p-3 shadow-sm border border-light">
              <!-- Badges -->
              <span v-if="p.isBestSeller" class="badge bg-warning text-dark badge-top-left text-xxs px-2 py-1 shadow-sm">
                <i class="fa-solid fa-crown me-1"></i>Bán chạy nhất
              </span>
              <span v-if="isDiscountActive(p)" class="badge bg-danger badge-top-right text-xxs px-2 py-1 shadow-sm">
                Giảm {{ p.discountRate }}%
              </span>

              <img :src="p.image" class="card-img-top rounded-3 mb-3" :alt="p.name" style="height: 180px; object-fit: cover;">
              
              <div class="card-body p-0 d-flex flex-column justify-content-between text-dark">
                <div>
                  <span class="badge bg-light text-primary border border-info border-opacity-25 mb-2 text-xxs">{{ p.category }}</span>
                  <h5 class="card-title fw-bold fs-6 mb-1">{{ p.name }}</h5>
                  <p class="text-xxs text-muted mb-2">Thương hiệu: {{ p.brand }}</p>
                  
                  <!-- Countdown Timer -->
                  <div v-if="isDiscountActive(p)" class="discount-countdown text-danger fw-bold mb-2 text-xxs">
                    <i class="fa-regular fa-clock me-1"></i>Còn: <span>{{ getCountdownText(p.discountExpiry) }}</span>
                  </div>

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
                  <button class="btn btn-outline-primary rounded-pill text-xs py-2 fw-semibold" type="button" @click="toggleDetails(p.id)">
                    Xem Chi Tiết
                  </button>
                  <button @click="addToCart(p)" class="btn btn-primary rounded-pill text-xs py-2 fw-semibold text-white shadow-sm" type="button" :disabled="p.stock <= 0">
                    <i class="fa-solid fa-cart-plus me-1"></i>Thêm Vào Giỏ
                  </button>
                </div>
              </div>

              <!-- Expandable Inline Product Details Collapse -->
              <div v-if="expandedDetails.includes(p.id)" class="mt-3">
                <hr class="my-2 border-secondary border-opacity-10">
                <div class="p-3 bg-light rounded-3 text-xs text-dark border">
                  <div class="mb-1.5"><strong>Mã SKU:</strong> <code>{{ p.sku }}</code></div>
                  <div class="mb-1.5"><strong>Hãng sản xuất:</strong> {{ p.brand }}</div>
                  <div class="mb-2"><strong>Thông số kỹ thuật:</strong></div>
                  <div class="bg-white p-2 rounded text-xxs text-secondary border border-opacity-25" style="white-space: pre-line; max-height: 120px; overflow-y: auto; line-height: 1.5;">
                    {{ p.specifications }}
                  </div>
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
import { useRoute } from 'vue-router'
import axios from 'axios'
import { cartService } from '../../services/cart'

export default {
  name: 'OnlineProductsList',
  setup() {
    const route = useRoute()
    const products = ref([])
    const categoryNames = ref(['Điện thoại', 'Laptop', 'Máy tính bảng', 'Tai nghe', 'Phụ kiện', 'Loa'])
    const expandedDetails = ref([])
    
    // Filters
    const searchQuery = ref('')
    const selectedCategory = ref('')
    const brandSearchQuery = ref('')
    const checkedBrands = ref([])
    const checkedPrices = ref([])
    const sortBy = ref('default')

    const now = ref(new Date())
    let timer = null

    const fetchProducts = async () => {
      try {
        const response = await axios.get('/api/GetProducts')
        products.value = response.data
      } catch (err) {
        console.error('Error fetching products catalog:', err)
      }
    }

    const availableBrands = computed(() => {
      const bSet = new Set(products.value.map(p => p.brand))
      return Array.from(bSet).filter(Boolean)
    })

    const filteredBrands = computed(() => {
      const q = brandSearchQuery.value.toLowerCase().trim()
      return availableBrands.value.filter(b => b.toLowerCase().includes(q))
    })

    const filteredProducts = computed(() => {
      return products.value.filter(p => {
        // 1. Search Query
        const matchSearch = searchQuery.value === '' || p.name.toLowerCase().includes(searchQuery.value.toLowerCase().trim())
        
        // 2. Category
        const matchCat = selectedCategory.value === '' || p.category === selectedCategory.value
        
        // 3. Brands checkbox
        const matchBrand = checkedBrands.value.length === 0 || checkedBrands.value.includes(p.brand)

        // 4. Price checkbox
        let matchPrice = true
        if (checkedPrices.value.length > 0) {
          matchPrice = false
          for (let pr of checkedPrices.value) {
            if (pr === 'under-5' && p.price < 5000000) matchPrice = true
            else if (pr === '5-15' && p.price >= 5000000 && p.price <= 15000000) matchPrice = true
            else if (pr === '15-30' && p.price >= 15000000 && p.price <= 30000000) matchPrice = true
            else if (pr === 'over-30' && p.price > 30000000) matchPrice = true
          }
        }

        return matchSearch && matchCat && matchBrand && matchPrice
      })
    })

    const sortedProducts = computed(() => {
      const list = [...filteredProducts.value]
      if (sortBy.value === 'price-asc') {
        return list.sort((a, b) => a.price - b.price)
      }
      if (sortBy.value === 'price-desc') {
        return list.sort((a, b) => b.price - a.price)
      }
      if (sortBy.value === 'name-asc') {
        return list.sort((a, b) => a.name.localeCompare(b.name))
      }
      return list // default = newest/original API order
    })

    const isDiscountActive = (p) => {
      return p.discountExpiry && new Date(p.discountExpiry) > now.value
    }

    const getCountdownText = (expiryStr) => {
      if (!expiryStr) return ''
      const expiry = new Date(expiryStr)
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
    }

    const resetAllFilters = () => {
      searchQuery.value = ''
      brandSearchQuery.value = ''
      checkedBrands.value = []
      checkedPrices.value = []
      sortBy.value = 'default'
      selectedCategory.value = ''
    }

    const addToCart = (p) => {
      cartService.addToCart(p)
      // Mirroring the showToast alert message from MVC view
      alert('Đã thêm sản phẩm vào giỏ hàng!')
    }

    const formatMoney = (val) => {
      if (val === undefined || val === null) return 0
      return Number(val).toLocaleString()
    }

    const toggleDetails = (id) => {
      const idx = expandedDetails.value.indexOf(id)
      if (idx > -1) {
        expandedDetails.value.splice(idx, 1)
      } else {
        expandedDetails.value.push(id)
      }
    }

    onMounted(() => {
      fetchProducts()
      
      // Parse category parameter from URL query if present
      if (route.query.category) {
        selectedCategory.value = route.query.category
      }

      timer = setInterval(() => {
        now.value = new Date()
      }, 1000)
    })

    onUnmounted(() => {
      if (timer) clearInterval(timer)
    })

    return {
      products,
      categoryNames,
      searchQuery,
      selectedCategory,
      brandSearchQuery,
      checkedBrands,
      checkedPrices,
      sortBy,
      filteredBrands,
      sortedProducts,
      isDiscountActive,
      getCountdownText,
      resetAllFilters,
      addToCart,
      formatMoney,
      expandedDetails,
      toggleDetails
    }
  }
}
</script>
