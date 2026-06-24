<template>
  <div class="row g-4">
    <!-- Create Supplier Form -->
    <div class="col-md-4" v-if="hasPerm('Create_Product')">
      <div class="card card-glass p-4">
        <h5 class="fw-bold mb-4 text-cyan">Thêm nhà cung cấp</h5>
        <form @submit.prevent="handleCreate">
          <div class="mb-3">
            <label class="form-label text-xs">Tên đối tác</label>
            <input type="text" v-model="newSupplier.name" class="form-control bg-dark border-secondary border-opacity-50 text-white" required placeholder="Nhà cung cấp LG">
          </div>
          <div class="mb-3">
            <label class="form-label text-xs">Mã định danh</label>
            <input type="text" v-model="newSupplier.code" class="form-control bg-dark border-secondary border-opacity-50 text-white" required placeholder="SUP-LG">
          </div>
          <div class="mb-3">
            <label class="form-label text-xs">Hotline liên hệ</label>
            <input type="text" v-model="newSupplier.phone" class="form-control bg-dark border-secondary border-opacity-50 text-white" required placeholder="091...">
          </div>
          <div class="mb-3">
            <label class="form-label text-xs">Địa chỉ Email</label>
            <input type="email" v-model="newSupplier.email" class="form-control bg-dark border-secondary border-opacity-50 text-white" required placeholder="partner@lg.com">
          </div>
          <div class="mb-4">
            <label class="form-label text-xs">Văn phòng / Địa chỉ</label>
            <input type="text" v-model="newSupplier.address" class="form-control bg-dark border-secondary border-opacity-50 text-white" placeholder="TP.HCM">
          </div>
          <button type="submit" class="btn btn-info rounded-pill px-4 w-100 fw-bold">Thêm nhà cung cấp</button>
        </form>
      </div>
    </div>

    <!-- Supplier List -->
    <div :class="hasPerm('Create_Product') ? 'col-md-8' : 'col-md-12'">
      <div class="card card-glass p-4">
        <h5 class="fw-bold mb-4 text-cyan">Danh sách nhà cung cấp</h5>
        <div class="table-responsive">
          <table class="table table-dark table-hover mb-0 text-xs align-middle">
            <thead>
              <tr>
                <th>Mã số</th>
                <th>Tên đối tác</th>
                <th>Số điện thoại</th>
                <th>Email</th>
                <th>Địa chỉ</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="s in suppliers" :key="s.id">
                <td><code>{{ s.code }}</code></td>
                <td class="fw-bold text-dark">{{ s.name }}</td>
                <td>{{ s.phone }}</td>
                <td>{{ s.email }}</td>
                <td>{{ s.address }}</td>
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
  name: 'Suppliers',
  setup() {
    const suppliers = ref([])
    const newSupplier = ref({
      name: '',
      code: '',
      phone: '',
      email: '',
      address: ''
    })

    const hasPerm = (p) => authService.hasPermission(p)

    const fetchSuppliers = async () => {
      try {
        const response = await axios.get('/api/GetSuppliers')
        suppliers.value = response.data
      } catch (err) {
        console.error('Error fetching suppliers:', err)
      }
    }

    const handleCreate = async () => {
      try {
        await axios.post('/api/CreateSupplier', newSupplier.value)
        fetchSuppliers()
        newSupplier.value = { name: '', code: '', phone: '', email: '', address: '' }
      } catch (err) {
        console.error('Error creating supplier:', err)
      }
    }

    onMounted(() => {
      fetchSuppliers()
    })

    return {
      suppliers,
      newSupplier,
      hasPerm,
      handleCreate
    }
  }
}
</script>
