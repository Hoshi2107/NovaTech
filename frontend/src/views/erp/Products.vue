<template>
  <div class="card card-glass p-4">
    <div class="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-3">
      <h4 class="fw-bold m-0"><i class="fa-solid fa-boxes-stacked text-cyan me-2"></i>Danh sách sản phẩm</h4>
      <button v-if="hasPerm('Create_Product')" class="btn btn-info rounded-pill px-4" data-bs-toggle="modal" data-bs-target="#createProductModal">
        <i class="fa-solid fa-plus me-2"></i>Thêm sản phẩm mới
      </button>
    </div>

    <!-- Filter Form -->
    <form @submit.prevent="fetchProducts" class="row g-3 mb-4">
      <div class="col-md-4">
        <input type="text" v-model="filters.search" class="form-control text-xs" placeholder="Tìm kiếm theo tên hoặc SKU...">
      </div>
      <div class="col-md-3">
        <select v-model="filters.category" class="form-select text-xs">
          <option value="">-- Tất cả danh mục --</option>
          <option v-for="c in categories" :key="c.id" :value="c.name">{{ c.name }}</option>
        </select>
      </div>
      <div class="col-md-3">
        <select v-model="filters.brand" class="form-select text-xs">
          <option value="">-- Tất cả thương hiệu --</option>
          <option v-for="b in brands" :key="b.id" :value="b.name">{{ b.name }}</option>
        </select>
      </div>
      <div class="col-md-2">
        <button type="submit" class="btn btn-outline-info w-100 rounded-pill text-xs"><i class="fa-solid fa-filter me-2"></i>Lọc</button>
      </div>
    </form>

    <!-- Product Table -->
    <div class="table-responsive">
      <table class="table table-dark table-hover mb-0 text-sm align-middle">
        <thead>
          <tr>
            <th>Ảnh</th>
            <th>Tên sản phẩm</th>
            <th>SKU</th>
            <th>Phân loại</th>
            <th>Giá bán</th>
            <th>Tồn kho</th>
            <th>Trạng thái</th>
            <th class="text-end">Hành động</th>
          </tr>
        </thead>
        <tbody>
          <template v-for="p in products" :key="p.id">
            <tr>
              <td>
                <img :src="p.image" class="rounded-3" style="width: 45px; height: 45px; object-fit: cover;">
              </td>
              <td>
                <div class="fw-bold text-dark">{{ p.name }}</div>
                <span class="text-xxs text-muted">Hãng: {{ p.brand }}</span>
              </td>
              <td><code>{{ p.sku }}</code></td>
              <td>{{ p.category }}</td>
              <td class="text-primary fw-bold">
                <div v-if="isDiscountActive(p)">
                  <span class="text-danger">{{ formatMoney(p.price) }} đ</span>
                  <div class="text-xxs text-muted text-decoration-line-through">{{ formatMoney(p.originalPrice) }} đ</div>
                </div>
                <div v-else>
                  <span>{{ formatMoney(p.price) }} đ</span>
                </div>
              </td>
              <td>
                <span v-if="p.stock <= 3" class="badge bg-danger">{{ p.stock }} (Ít hàng)</span>
                <span v-else class="badge bg-success">{{ p.stock }}</span>
              </td>
              <td>
                <span v-if="p.status === 'Đang bán'" class="badge bg-success-subtle text-success border border-success border-opacity-25 rounded-pill px-2.5">{{ p.status }}</span>
                <span v-else class="badge bg-danger-subtle text-danger border border-danger border-opacity-25 rounded-pill px-2.5">{{ p.status }}</span>
              </td>
              <td class="text-end">
                <div class="d-flex justify-content-end gap-2">
                  <button class="btn btn-sm btn-outline-secondary" @click="toggleDetails(p.id)" title="Xem chi tiết"><i class="fa-solid fa-eye"></i></button>
                  <button v-if="hasPerm('Edit_Product')" class="btn btn-sm btn-outline-danger" data-bs-toggle="modal" data-bs-target="#discountModal" @click="selectProduct(p)" title="Cấu hình Flash Sale"><i class="fa-solid fa-tags"></i></button>
                  <button v-if="hasPerm('Edit_Product')" class="btn btn-sm btn-outline-info" data-bs-toggle="modal" data-bs-target="#editModal" @click="selectProduct(p)" title="Sửa sản phẩm"><i class="fa-solid fa-pen"></i></button>
                  <button v-if="hasPerm('Delete_Product')" class="btn btn-sm btn-outline-danger" @click="handleDelete(p.id)" title="Xóa sản phẩm"><i class="fa-solid fa-trash-can"></i></button>
                </div>
              </td>
            </tr>

            <!-- Expanded Details Panel -->
            <tr v-if="expandedRows.includes(p.id)">
              <td colspan="8" class="p-0">
                <div class="p-4 bg-light border-bottom border-top text-dark">
                  <div class="row g-4">
                    <div class="col-md-3 text-center">
                      <img :src="p.image" class="img-fluid rounded border border-light shadow-sm" style="max-height: 180px; object-fit: cover;">
                    </div>
                    <div class="col-md-9">
                      <h5 class="fw-bold text-dark mb-3">{{ p.name }}</h5>
                      <div class="row g-3 text-xs">
                        <div class="col-md-4"><strong>SKU:</strong> <code>{{ p.sku }}</code></div>
                        <div class="col-md-4"><strong>Barcode:</strong> {{ p.barcode || 'N/A' }}</div>
                        <div class="col-md-4"><strong>Hãng sản xuất:</strong> {{ p.brand }}</div>
                        <div class="col-md-4"><strong>Danh mục:</strong> {{ p.category }}</div>
                        <div class="col-md-4"><strong>Nhà cung cấp:</strong> {{ p.supplier }}</div>
                        <div class="col-md-4"><strong>Tồn kho:</strong> {{ p.stock }} chiếc</div>
                        <div class="col-md-4"><strong>Giá nhập:</strong> <span class="fw-bold">{{ formatMoney(p.importPrice) }} đ</span></div>
                        <div class="col-md-4">
                          <strong>Giá bán:</strong> 
                          <span class="fw-bold text-primary">{{ formatMoney(p.price) }} đ</span>
                          <span v-if="isDiscountActive(p)" class="text-xxs text-muted text-decoration-line-through ms-1">({{ formatMoney(p.originalPrice) }} đ)</span>
                        </div>
                        <div class="col-md-4">
                          <strong>Flash Sale:</strong> 
                          <span v-if="isDiscountActive(p)" class="badge bg-danger">Đang giảm {{ p.discountRate }}% (Hết hạn: {{ formatDateTime(p.discountExpiry) }})</span>
                          <span v-else class="text-muted">Không áp dụng</span>
                        </div>
                      </div>
                      <div class="mt-3 text-xs">
                        <strong>Thông số kỹ thuật:</strong>
                        <div class="p-2 bg-white rounded border border-light mt-1 text-muted" style="white-space: pre-line;">{{ p.specifications || 'Chưa cung cấp thông số' }}</div>
                      </div>
                    </div>
                  </div>
                </div>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>

    <!-- Create Product Modal -->
    <div class="modal fade" id="createProductModal" tabindex="-1" aria-hidden="true">
      <div class="modal-dialog modal-dialog-centered modal-lg">
        <div class="modal-content bg-white border border-light shadow text-dark">
          <form @submit.prevent="handleCreate">
            <div class="modal-header border-bottom border-light">
              <h5 class="modal-title fw-bold text-dark">Thêm sản phẩm mới</h5>
              <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close" ref="closeCreateBtn"></button>
            </div>
            <div class="modal-body text-xs">
              <div class="row g-3">
                <div class="col-md-6">
                  <label class="form-label text-secondary fw-semibold">Tên sản phẩm</label>
                  <input type="text" v-model="newProduct.name" class="form-control" required placeholder="Ví dụ: iPhone 15 Plus">
                </div>
                <div class="col-md-6">
                  <label class="form-label text-secondary fw-semibold">SKU</label>
                  <input type="text" v-model="newProduct.sku" class="form-control" required placeholder="IPHONE15P-128">
                </div>
                <div class="col-md-6">
                  <label class="form-label text-secondary fw-semibold">Barcode</label>
                  <input type="text" v-model="newProduct.barcode" class="form-control">
                </div>
                <div class="col-md-6">
                  <label class="form-label text-secondary fw-semibold">Phân loại danh mục</label>
                  <select v-model="newProduct.category" class="form-select">
                    <option v-for="c in categories" :key="c.id" :value="c.name">{{ c.name }}</option>
                  </select>
                </div>
                <div class="col-md-6">
                  <label class="form-label text-secondary fw-semibold">Thương hiệu</label>
                  <select v-model="newProduct.brand" class="form-select">
                    <option v-for="b in brands" :key="b.id" :value="b.name">{{ b.name }}</option>
                  </select>
                </div>
                <div class="col-md-6">
                  <label class="form-label text-secondary fw-semibold">Nhà cung cấp</label>
                  <select v-model="newProduct.supplier" class="form-select">
                    <option v-for="s in suppliers" :key="s.id" :value="s.name">{{ s.name }}</option>
                  </select>
                </div>
                <div class="col-md-6">
                  <label class="form-label text-secondary fw-semibold">Giá nhập (đ)</label>
                  <input type="number" v-model.number="newProduct.importPrice" class="form-control" required>
                </div>
                <div class="col-md-6">
                  <label class="form-label text-secondary fw-semibold">Giá bán (đ)</label>
                  <input type="number" v-model.number="newProduct.price" class="form-control" required>
                </div>
                <div class="col-md-6">
                  <label class="form-label text-secondary fw-semibold">Tồn kho ban đầu</label>
                  <input type="number" v-model.number="newProduct.stock" class="form-control" required>
                </div>
                <div class="col-md-6">
                  <label class="form-label text-secondary fw-semibold">Trạng thái bán</label>
                  <select v-model="newProduct.status" class="form-select">
                    <option value="Đang bán">Đang bán</option>
                    <option value="Ngừng bán">Ngừng bán</option>
                  </select>
                </div>
                <div class="col-12">
                  <label class="form-label text-secondary fw-semibold">Đường dẫn ảnh chính (URL)</label>
                  <input type="text" v-model="newProduct.image" class="form-control">
                </div>
                <div class="col-12">
                  <label class="form-label text-secondary fw-semibold">Thông số kỹ thuật</label>
                  <textarea v-model="newProduct.specifications" rows="3" class="form-control"></textarea>
                </div>
              </div>
            </div>
            <div class="modal-footer border-top border-light">
              <button type="button" class="btn btn-outline-secondary rounded-pill" data-bs-dismiss="modal">Đóng</button>
              <button type="submit" class="btn btn-info rounded-pill px-4">Tạo sản phẩm</button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- Edit Product Modal -->
    <div class="modal fade" id="editModal" tabindex="-1" aria-hidden="true" v-if="selectedProd">
      <div class="modal-dialog modal-dialog-centered modal-lg">
        <div class="modal-content bg-white border border-light shadow text-dark">
          <form @submit.prevent="handleEdit">
            <div class="modal-header border-bottom border-light">
              <h5 class="modal-title fw-bold text-dark">Sửa sản phẩm: {{ selectedProd.name }}</h5>
              <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close" ref="closeEditBtn"></button>
            </div>
            <div class="modal-body text-xs">
              <div class="row g-3">
                <div class="col-md-6">
                  <label class="form-label text-secondary fw-semibold">Tên sản phẩm</label>
                  <input type="text" v-model="selectedProd.name" class="form-control" required>
                </div>
                <div class="col-md-6">
                  <label class="form-label text-secondary fw-semibold">SKU</label>
                  <input type="text" v-model="selectedProd.sku" class="form-control" required>
                </div>
                <div class="col-md-6">
                  <label class="form-label text-secondary fw-semibold">Barcode</label>
                  <input type="text" v-model="selectedProd.barcode" class="form-control">
                </div>
                <div class="col-md-6">
                  <label class="form-label text-secondary fw-semibold">Phân loại danh mục</label>
                  <select v-model="selectedProd.category" class="form-select">
                    <option v-for="c in categories" :key="c.id" :value="c.name">{{ c.name }}</option>
                  </select>
                </div>
                <div class="col-md-6">
                  <label class="form-label text-secondary fw-semibold">Thương hiệu</label>
                  <select v-model="selectedProd.brand" class="form-select">
                    <option v-for="b in brands" :key="b.id" :value="b.name">{{ b.name }}</option>
                  </select>
                </div>
                <div class="col-md-6">
                  <label class="form-label text-secondary fw-semibold">Nhà cung cấp</label>
                  <select v-model="selectedProd.supplier" class="form-select">
                    <option v-for="s in suppliers" :key="s.id" :value="s.name">{{ s.name }}</option>
                  </select>
                </div>
                <div class="col-md-6">
                  <label class="form-label text-secondary fw-semibold">Giá nhập (đ)</label>
                  <input type="number" v-model.number="selectedProd.importPrice" class="form-control" required>
                </div>
                <div class="col-md-6">
                  <label class="form-label text-secondary fw-semibold">Giá bán (đ)</label>
                  <input type="number" v-model.number="selectedProd.price" class="form-control" required>
                </div>
                <div class="col-md-6">
                  <label class="form-label text-secondary fw-semibold">Tồn kho</label>
                  <input type="number" v-model.number="selectedProd.stock" class="form-control" required>
                </div>
                <div class="col-md-6">
                  <label class="form-label text-secondary fw-semibold">Trạng thái bán</label>
                  <select v-model="selectedProd.status" class="form-select">
                    <option value="Đang bán">Đang bán</option>
                    <option value="Ngừng bán">Ngừng bán</option>
                    <option value="Hết hàng">Hết hàng</option>
                  </select>
                </div>
                <div class="col-12">
                  <label class="form-label text-secondary fw-semibold">Đường dẫn ảnh chính (URL)</label>
                  <input type="text" v-model="selectedProd.image" class="form-control">
                </div>
                <div class="col-12">
                  <label class="form-label text-secondary fw-semibold">Thông số kỹ thuật</label>
                  <textarea v-model="selectedProd.specifications" rows="3" class="form-control"></textarea>
                </div>
              </div>
            </div>
            <div class="modal-footer border-top border-light">
              <button type="button" class="btn btn-outline-secondary rounded-pill" data-bs-dismiss="modal">Đóng</button>
              <button type="submit" class="btn btn-info rounded-pill px-4">Lưu thay đổi</button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- Discount Modal -->
    <div class="modal fade" id="discountModal" tabindex="-1" aria-hidden="true" v-if="selectedProd">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content bg-white border border-light shadow text-dark">
          <div class="modal-header border-bottom border-light">
            <h5 class="modal-title fw-bold text-dark"><i class="fa-solid fa-tags text-danger me-2"></i>Cấu hình giảm giá Flash Sale</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close" ref="closeDiscountBtn"></button>
          </div>
          <form @submit.prevent="handleDiscount">
            <div class="modal-body text-xs">
              <div class="mb-3">
                <label class="form-label fw-bold text-secondary">Giá bán gốc hiện tại</label>
                <div class="form-control bg-light border border-light text-dark fw-semibold">
                  {{ formatMoney(selectedProd.originalPrice || selectedProd.price) }} đ
                </div>
              </div>
              <div class="mb-3">
                <label class="form-label fw-bold text-secondary">Giá bán sau khi giảm (đ) (Nhập 0 để hủy giảm giá)</label>
                <input type="number" v-model.number="discountPrice" class="form-control" placeholder="Nhập giá giảm..." required>
              </div>
              <div class="mb-3">
                <label class="form-label fw-bold text-secondary">Thời gian áp dụng giảm giá (giờ) (Nhập 0 để hủy giảm giá)</label>
                <input type="number" v-model.number="discountHours" class="form-control" placeholder="Nhập số giờ (ví dụ: 24, 48...)" required>
              </div>
            </div>
            <div class="modal-footer border-top border-light">
              <button type="button" @click="clearDiscount" class="btn btn-outline-secondary btn-sm rounded-pill">Xóa giảm giá</button>
              <button type="submit" class="btn btn-danger btn-sm rounded-pill px-4">Lưu cấu hình</button>
            </div>
          </form>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, onMounted } from 'vue'
import axios from 'axios'
import { authService } from '../../services/auth'

export default {
  name: 'Products',
  setup() {
    const products = ref([])
    const categories = ref([])
    const brands = ref([])
    const suppliers = ref([])
    const expandedRows = ref([])
    
    const filters = ref({
      search: '',
      category: '',
      brand: ''
    })

    const newProduct = ref({
      name: '',
      sku: '',
      barcode: '',
      category: '',
      brand: '',
      supplier: '',
      importPrice: 0,
      price: 0,
      stock: 10,
      status: 'Đang bán',
      image: '',
      specifications: ''
    })

    const selectedProd = ref(null)
    const discountPrice = ref(0)
    const discountHours = ref(24)

    // Modal close refs
    const closeCreateBtn = ref(null)
    const closeEditBtn = ref(null)
    const closeDiscountBtn = ref(null)

    const hasPerm = (p) => authService.hasPermission(p)

    const fetchProducts = async () => {
      try {
        const response = await axios.get('/api/GetProducts', { params: filters.value })
        products.value = response.data
      } catch (err) {
        console.error('Error fetching products:', err)
      }
    }

    const fetchMetadata = async () => {
      try {
        const catRes = await axios.get('/api/GetCategories')
        categories.value = catRes.data
        if (categories.value.length > 0) newProduct.value.category = categories.value[0].name

        const brandRes = await axios.get('/api/GetBrands')
        brands.value = brandRes.data
        if (brands.value.length > 0) newProduct.value.brand = brands.value[0].name

        const supRes = await axios.get('/api/GetSuppliers')
        suppliers.value = supRes.data
        if (suppliers.value.length > 0) newProduct.value.supplier = suppliers.value[0].name
      } catch (err) {
        console.error('Error fetching metadata:', err)
      }
    }

    const handleCreate = async () => {
      try {
        await axios.post('/api/CreateProduct', newProduct.value)
        fetchProducts()
        
        // Reset form
        newProduct.value = {
          name: '',
          sku: '',
          barcode: '',
          category: categories.value[0]?.name || '',
          brand: brands.value[0]?.name || '',
          supplier: suppliers.value[0]?.name || '',
          importPrice: 0,
          price: 0,
          stock: 10,
          status: 'Đang bán',
          image: '',
          specifications: ''
        }

        closeCreateBtn.value.click()
      } catch (err) {
        console.error('Error creating product:', err)
      }
    }

    const selectProduct = (p) => {
      selectedProd.value = { ...p }
      discountPrice.value = p.price
      discountHours.value = 24
    }

    const handleEdit = async () => {
      try {
        await axios.post('/api/EditProduct', selectedProd.value)
        fetchProducts()
        closeEditBtn.value.click()
      } catch (err) {
        console.error('Error editing product:', err)
      }
    }

    const handleDelete = async (id) => {
      if (!confirm('Bạn thực sự muốn xóa sản phẩm này?')) return
      try {
        await axios.post('/api/DeleteProduct', { id })
        fetchProducts()
      } catch (err) {
        console.error('Error deleting product:', err)
      }
    }

    const handleDiscount = async () => {
      try {
        await axios.post('/api/UpdateDiscount', {
          id: selectedProd.value.id,
          discountPrice: discountPrice.value,
          hours: discountHours.value
        })
        fetchProducts()
        closeDiscountBtn.value.click()
      } catch (err) {
        console.error('Error setting discount:', err)
      }
    }

    const clearDiscount = async () => {
      discountPrice.value = 0
      discountHours.value = 0
      await handleDiscount()
    }

    const isDiscountActive = (p) => {
      return p.discountExpiry && new Date(p.discountExpiry) > new Date()
    }

    const toggleDetails = (id) => {
      const index = expandedRows.value.indexOf(id)
      if (index > -1) {
        expandedRows.value.splice(index, 1)
      } else {
        expandedRows.value.push(id)
      }
    }

    const formatMoney = (val) => {
      if (val === undefined || val === null) return 0
      return Number(val).toLocaleString()
    }

    const formatDateTime = (dateStr) => {
      if (!dateStr) return ''
      const d = new Date(dateStr)
      return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')} ${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}`
    }

    onMounted(() => {
      fetchProducts()
      fetchMetadata()
    })

    return {
      products,
      categories,
      brands,
      suppliers,
      filters,
      newProduct,
      selectedProd,
      discountPrice,
      discountHours,
      expandedRows,
      closeCreateBtn,
      closeEditBtn,
      closeDiscountBtn,
      hasPerm,
      fetchProducts,
      handleCreate,
      selectProduct,
      handleEdit,
      handleDelete,
      handleDiscount,
      clearDiscount,
      isDiscountActive,
      toggleDetails,
      formatMoney,
      formatDateTime
    }
  }
}
</script>
