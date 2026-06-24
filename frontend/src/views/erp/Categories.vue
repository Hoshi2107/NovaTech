<template>
  <div class="row g-4">
    <!-- Create Category Form -->
    <div class="col-md-4" v-if="hasPerm('Create_Product')">
      <div class="card card-glass p-4">
        <h5 class="fw-bold mb-4 text-cyan">Tạo danh mục mới</h5>
        <form @submit.prevent="handleCreate">
          <div class="mb-3">
            <label class="form-label text-xs">Tên danh mục</label>
            <input type="text" v-model="newCategory.name" class="form-control bg-dark border-secondary border-opacity-50 text-white" required placeholder="Ví dụ: Máy tính bảng">
          </div>
          <div class="mb-4">
            <label class="form-label text-xs">Mô tả ngắn</label>
            <textarea v-model="newCategory.description" rows="3" class="form-control bg-dark border-secondary border-opacity-50 text-white" placeholder="Mô tả thông tin chi tiết danh mục..."></textarea>
          </div>
          <button type="submit" class="btn btn-info rounded-pill px-4 w-100 fw-bold">Tạo danh mục</button>
        </form>
      </div>
    </div>

    <!-- Category List -->
    <div :class="hasPerm('Create_Product') ? 'col-md-8' : 'col-md-12'">
      <div class="card card-glass p-4">
        <h5 class="fw-bold mb-4 text-cyan">Danh sách danh mục</h5>
        <div class="table-responsive">
          <table class="table table-dark table-hover mb-0 text-sm align-middle">
            <thead>
              <tr>
                <th>ID</th>
                <th>Tên danh mục</th>
                <th>Mô tả</th>
                <th>Số sản phẩm</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="c in categories" :key="c.id">
                <td>{{ c.id }}</td>
                <td class="fw-bold text-dark">{{ c.name }}</td>
                <td>{{ c.description || 'Chưa có mô tả' }}</td>
                <td><span class="badge bg-secondary">{{ c.productCount || 0 }} sản phẩm</span></td>
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
  name: 'Categories',
  setup() {
    const categories = ref([])
    const newCategory = ref({
      name: '',
      description: ''
    })

    const hasPerm = (p) => authService.hasPermission(p)

    const fetchCategories = async () => {
      try {
        const response = await axios.get('/api/GetCategories')
        categories.value = response.data
      } catch (err) {
        console.error('Error fetching categories:', err)
      }
    }

    const handleCreate = async () => {
      try {
        await axios.post('/api/CreateCategory', newCategory.value)
        fetchCategories()
        newCategory.value = { name: '', description: '' }
      } catch (err) {
        console.error('Error creating category:', err)
      }
    }

    onMounted(() => {
      fetchCategories()
    })

    return {
      categories,
      newCategory,
      hasPerm,
      handleCreate
    }
  }
}
</script>
