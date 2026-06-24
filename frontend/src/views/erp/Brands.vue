<template>
  <div class="row g-4">
    <!-- Create Brand Form -->
    <div class="col-md-4" v-if="hasPerm('Create_Product')">
      <div class="card card-glass p-4">
        <h5 class="fw-bold mb-4 text-cyan">Tạo thương hiệu mới</h5>
        <form @submit.prevent="handleCreate">
          <div class="mb-3">
            <label class="form-label text-xs">Tên thương hiệu</label>
            <input type="text" v-model="newBrand.name" class="form-control bg-dark border-secondary border-opacity-50 text-white" required placeholder="Ví dụ: Samsung">
          </div>
          <div class="mb-4">
            <label class="form-label text-xs">Mô tả ngắn</label>
            <textarea v-model="newBrand.description" rows="3" class="form-control bg-dark border-secondary border-opacity-50 text-white" placeholder="Thương hiệu cao cấp từ Hàn Quốc..."></textarea>
          </div>
          <button type="submit" class="btn btn-info rounded-pill px-4 w-100 fw-bold">Tạo thương hiệu</button>
        </form>
      </div>
    </div>

    <!-- Brand List -->
    <div :class="hasPerm('Create_Product') ? 'col-md-8' : 'col-md-12'">
      <div class="card card-glass p-4">
        <h5 class="fw-bold mb-4 text-cyan">Danh sách thương hiệu</h5>
        <div class="table-responsive">
          <table class="table table-dark table-hover mb-0 text-sm align-middle">
            <thead>
              <tr>
                <th>ID</th>
                <th>Tên thương hiệu</th>
                <th>Mô tả</th>
                <th>Số sản phẩm</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="b in brands" :key="b.id">
                <td>{{ b.id }}</td>
                <td class="fw-bold text-dark">{{ b.name }}</td>
                <td>{{ b.description || 'Chưa có mô tả' }}</td>
                <td><span class="badge bg-secondary">{{ b.productCount || 0 }} sản phẩm</span></td>
              </tr>
            </tbody>
          </table>
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
  name: 'Brands',
  setup() {
    const brands = ref([])
    const newBrand = ref({
      name: '',
      description: ''
    })

    const hasPerm = (p) => authService.hasPermission(p)

    const fetchBrands = async () => {
      try {
        const response = await axios.get('/api/GetBrands')
        brands.value = response.data
      } catch (err) {
        console.error('Error fetching brands:', err)
      }
    }

    const handleCreate = async () => {
      try {
        await axios.post('/api/CreateBrand', newBrand.value)
        fetchBrands()
        newBrand.value = { name: '', description: '' }
      } catch (err) {
        console.error('Error creating brand:', err)
      }
    }

    onMounted(() => {
      fetchBrands()
    })

    return {
      brands,
      newBrand,
      hasPerm,
      handleCreate
    }
  }
}
</script>
