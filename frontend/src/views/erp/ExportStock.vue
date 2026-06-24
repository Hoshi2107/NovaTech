<template>
  <div class="row justify-content-center">
    <div class="col-md-6">
      <div class="card card-glass p-4 text-dark animate-fade-in">
        <h5 class="fw-bold mb-4 text-danger"><i class="fa-solid fa-circle-up me-2"></i>Tạo phiếu xuất kho</h5>
        
        <div v-if="successMsg" class="alert alert-success text-xs py-2 px-3 mb-3">
          {{ successMsg }}
        </div>
        <div v-if="errorMsg" class="alert alert-danger text-xs py-2 px-3 mb-3">
          {{ errorMsg }}
        </div>

        <form @submit.prevent="handleExport">
          <div class="mb-3">
            <label class="form-label text-sm">Chọn sản phẩm</label>
            <select v-model="form.productSKU" class="form-select text-xs" required>
              <option value="" disabled>-- Chọn sản phẩm cần xuất --</option>
              <option v-for="p in products" :key="p.id" :value="p.sku">
                {{ p.name }} (SKU: {{ p.sku }}) - Tồn hiện tại: {{ p.stock }} chiếc
              </option>
            </select>
          </div>
          
          <div class="mb-3">
            <label class="form-label text-sm">Số lượng xuất</label>
            <input type="number" v-model.number="form.quantity" class="form-control text-xs" required min="1">
          </div>

          <div class="mb-4">
            <label class="form-label text-sm">Ghi chú xuất</label>
            <input type="text" v-model="form.note" class="form-control text-xs" placeholder="Xuất hỏng, chuyển kho, v.v.">
          </div>

          <div class="d-flex gap-2">
            <router-link to="/erp/inventory" class="btn btn-outline-secondary rounded-pill px-4 w-50 fw-bold text-xs">Quay lại</router-link>
            <button type="submit" class="btn btn-danger rounded-pill px-4 w-50 fw-bold text-xs">Xác nhận xuất kho</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'

export default {
  name: 'ExportStock',
  setup() {
    const router = useRouter()
    const products = ref([])
    
    const form = ref({
      productSKU: '',
      quantity: 1,
      note: ''
    })

    const successMsg = ref('')
    const errorMsg = ref('')

    const fetchProducts = async () => {
      try {
        const response = await axios.get('/api/GetProducts')
        products.value = response.data.filter(p => p.stock > 0)
      } catch (err) {
        console.error('Error fetching products:', err)
      }
    }

    const handleExport = async () => {
      errorMsg.value = ''
      successMsg.value = ''
      try {
        const response = await axios.post('/api/ExportStock', form.value)
        successMsg.value = response.data.message || 'Xuất kho thành công!'
        setTimeout(() => {
          router.push('/erp/inventory')
        }, 1500)
      } catch (err) {
        errorMsg.value = err.response?.data?.message || 'Có lỗi xảy ra khi xuất kho.'
      }
    }

    onMounted(() => {
      fetchProducts()
    })

    return {
      products,
      form,
      successMsg,
      errorMsg,
      handleExport
    }
  }
}
</script>
