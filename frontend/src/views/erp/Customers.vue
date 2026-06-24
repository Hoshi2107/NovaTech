<template>
  <div class="row g-4">
    <!-- Create Customer Form -->
    <div class="col-md-4" v-if="hasPerm('Create_Customer')">
      <div class="card card-glass p-4">
        <h5 class="fw-bold mb-4 text-cyan">Đăng ký khách hàng mới</h5>
        <form @submit.prevent="handleCreate">
          <div class="mb-3">
            <label class="form-label text-xs">Họ và tên</label>
            <input type="text" v-model="newCustomer.name" class="form-control bg-dark border-secondary border-opacity-50 text-white" required placeholder="Nguyễn Văn A">
          </div>
          <div class="mb-3">
            <label class="form-label text-xs">Số điện thoại</label>
            <input type="text" v-model="newCustomer.phone" class="form-control bg-dark border-secondary border-opacity-50 text-white" required placeholder="09...">
          </div>
          <div class="mb-3">
            <label class="form-label text-xs">Địa chỉ Email</label>
            <input type="email" v-model="newCustomer.email" class="form-control bg-dark border-secondary border-opacity-50 text-white" required placeholder="khachhang@gmail.com">
          </div>
          <div class="mb-4">
            <label class="form-label text-xs">Địa chỉ thường trú</label>
            <input type="text" v-model="newCustomer.address" class="form-control bg-dark border-secondary border-opacity-50 text-white" placeholder="TP. Hồ Chí Minh">
          </div>
          <button type="submit" class="btn btn-info rounded-pill px-4 w-100 fw-bold">Tạo tài khoản thành viên</button>
        </form>
      </div>
    </div>

    <!-- Customer List -->
    <div :class="hasPerm('Create_Customer') ? 'col-md-8' : 'col-md-12'">
      <div class="card card-glass p-4">
        <h5 class="fw-bold mb-4 text-cyan"><i class="fa-solid fa-user-group me-2"></i>Thành viên mua sắm</h5>
        
        <div v-if="loading" class="text-center py-5">
          <div class="spinner-border text-info" role="status"></div>
        </div>

        <div class="table-responsive" v-else>
          <table class="table table-dark table-hover mb-0 text-sm align-middle">
            <thead>
              <tr>
                <th>Khách hàng</th>
                <th>Liên hệ</th>
                <th>Tích điểm</th>
                <th>Phân hạng</th>
                <th>Ngày gia nhập</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="c in customers" :key="c.id">
                <td class="fw-bold text-dark">{{ c.name }}</td>
                <td>
                  <div class="text-xs">{{ c.phone }}</div>
                  <div class="text-xxs text-muted">{{ c.email }}</div>
                </td>
                <td class="fw-bold">{{ c.points }} điểm</td>
                <td>
                  <span class="badge bg-info text-dark rounded-pill px-2.5">{{ c.membershipRank }}</span>
                </td>
                <td>{{ formatDate(c.createdDate) }}</td>
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
  name: 'Customers',
  setup() {
    const customers = ref([])
    const loading = ref(true)
    const newCustomer = ref({
      name: '',
      phone: '',
      email: '',
      address: ''
    })

    const hasPerm = (p) => authService.hasPermission(p)

    const fetchCustomers = async () => {
      try {
        loading.value = true
        const response = await axios.get('/api/GetCustomers')
        customers.value = response.data
      } catch (err) {
        console.error('Error fetching customers:', err)
      } finally {
        loading.value = false
      }
    }

    const handleCreate = async () => {
      try {
        await axios.post('/api/CreateCustomer', newCustomer.value)
        fetchCustomers()
        newCustomer.value = { name: '', phone: '', email: '', address: '' }
      } catch (err) {
        console.error('Error creating customer:', err)
      }
    }

    const formatDate = (dateStr) => {
      if (!dateStr) return ''
      const d = new Date(dateStr)
      return `${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}/${d.getFullYear()}`
    }

    onMounted(() => {
      fetchCustomers()
    })

    return {
      customers,
      loading,
      newCustomer,
      hasPerm,
      handleCreate,
      formatDate
    }
  }
}
</script>
