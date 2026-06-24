<template>
  <div class="row g-4">
    <!-- Create Voucher Form -->
    <div class="col-md-4" v-if="hasPerm('View_Promotion')">
      <div class="card card-glass p-4 text-dark">
        <h5 class="fw-bold mb-4 text-cyan">Tạo Voucher khuyến mãi</h5>
        <form @submit.prevent="handleCreate">
          <div class="mb-3">
            <label class="form-label text-xs">Mã giảm giá</label>
            <input type="text" v-model="newVoucher.code" class="form-control bg-dark border-secondary border-opacity-50 text-white" required placeholder="SUMMER50">
          </div>
          <div class="mb-3">
            <label class="form-label text-xs">Loại áp dụng</label>
            <select v-model="newVoucher.type" class="form-select bg-dark border-secondary border-opacity-50 text-white" required>
              <option value="Giảm %">Giảm % hóa đơn</option>
              <option value="Giảm tiền">Giảm số tiền trực tiếp</option>
            </select>
          </div>
          <div class="mb-3">
            <label class="form-label text-xs">Giá trị giảm</label>
            <input type="number" v-model.number="newVoucher.value" class="form-control bg-dark border-secondary border-opacity-50 text-white" required placeholder="10 hoặc 500000">
          </div>
          <div class="mb-3">
            <label class="form-label text-xs">Đơn tối thiểu</label>
            <input type="number" v-model.number="newVoucher.minOrderValue" class="form-control bg-dark border-secondary border-opacity-50 text-white" required placeholder="500000">
          </div>
          <div class="mb-4">
            <label class="form-label text-xs">Số lượng phát hành</label>
            <input type="number" v-model.number="newVoucher.quantity" class="form-control bg-dark border-secondary border-opacity-50 text-white" required>
          </div>
          <button type="submit" class="btn btn-info rounded-pill px-4 w-100 fw-bold">Kích hoạt Voucher</button>
        </form>
      </div>
    </div>

    <!-- Promotion List -->
    <div :class="hasPerm('View_Promotion') ? 'col-md-8' : 'col-md-12'">
      <div class="card card-glass p-4">
        <h5 class="fw-bold mb-4 text-cyan"><i class="fa-solid fa-gift me-2"></i>Chiến dịch khuyến mãi & Voucher</h5>
        
        <div v-if="loading" class="text-center py-5">
          <div class="spinner-border text-info" role="status"></div>
        </div>

        <div class="table-responsive" v-else>
          <table class="table table-dark table-hover mb-0 text-sm align-middle">
            <thead>
              <tr>
                <th>Mã Voucher</th>
                <th>Hình thức</th>
                <th>Mức Giảm</th>
                <th>Đơn hàng tối thiểu</th>
                <th>Số lượng</th>
                <th>Trạng thái</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="v in vouchers" :key="v.id">
                <td><code class="fs-6 text-warning">{{ v.code }}</code></td>
                <td>{{ v.type }}</td>
                <td>
                  <span v-if="v.type === 'Giảm %'">{{ v.value }} %</span>
                  <span v-else>{{ formatMoney(v.value) }} đ</span>
                </td>
                <td>{{ formatMoney(v.minOrderValue) }} đ</td>
                <td>{{ v.quantity }} Voucher</td>
                <td>
                  <span class="badge bg-success">{{ v.status }}</span>
                </td>
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
  name: 'Promotions',
  setup() {
    const vouchers = ref([])
    const loading = ref(true)
    
    const newVoucher = ref({
      code: '',
      type: 'Giảm %',
      value: 10,
      minOrderValue: 500000,
      quantity: 50,
      startDate: new Date().toISOString().split('T')[0],
      endDate: new Date(Date.now() + 15 * 24 * 60 * 60 * 1000).toISOString().split('T')[0] // default 15 days expiry to match MVC seed
    })

    const hasPerm = (p) => authService.hasPermission(p)

    const fetchVouchers = async () => {
      try {
        loading.value = true
        const response = await axios.get('/api/GetPromotions')
        vouchers.value = response.data
      } catch (err) {
        console.error('Error fetching promotions:', err)
      } finally {
        loading.value = false
      }
    }

    const handleCreate = async () => {
      try {
        await axios.post('/api/CreateVoucher', newVoucher.value)
        fetchVouchers()
        newVoucher.value = {
          code: '',
          type: 'Giảm %',
          value: 10,
          minOrderValue: 500000,
          quantity: 50,
          startDate: new Date().toISOString().split('T')[0],
          endDate: new Date(Date.now() + 15 * 24 * 60 * 60 * 1000).toISOString().split('T')[0]
        }
      } catch (err) {
        console.error('Error creating voucher:', err)
      }
    }

    const formatMoney = (val) => {
      if (val === undefined || val === null) return 0
      return Number(val).toLocaleString()
    }

    onMounted(() => {
      fetchVouchers()
    })

    return {
      vouchers,
      loading,
      newVoucher,
      hasPerm,
      handleCreate,
      formatMoney
    }
  }
}
</script>
