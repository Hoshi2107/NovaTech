<template>
  <div class="row g-4">
    <!-- Tồn kho tổng hợp -->
    <div class="col-md-7">
      <div class="card card-glass p-4 h-100">
        <h5 class="fw-bold mb-4 text-cyan"><i class="fa-solid fa-boxes-stacked me-2"></i>Bảng Tồn Kho Sản Phẩm</h5>
        
        <div v-if="loading" class="text-center py-5">
          <div class="spinner-border text-info" role="status"></div>
        </div>

        <div class="table-responsive" v-else>
          <table class="table table-dark table-hover mb-0 text-sm align-middle">
            <thead>
              <tr>
                <th>Mã SKU</th>
                <th>Tên sản phẩm</th>
                <th>Giá trị</th>
                <th>Tồn kho</th>
                <th>Trạng thái</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="p in products" :key="p.id">
                <td><code>{{ p.sku }}</code></td>
                <td class="fw-bold text-dark">{{ p.name }}</td>
                <td>{{ formatMoney(p.price) }} đ</td>
                <td class="fw-bold">{{ p.stock }} chiếc</td>
                <td>
                  <span v-if="p.stock === 0" class="badge bg-danger">Hết hàng</span>
                  <span v-else-if="p.stock <= 3" class="badge bg-warning text-dark">Sắp hết hàng</span>
                  <span v-else class="badge bg-success">An toàn</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Nhập xuất nhanh -->
    <div class="col-md-5">
      <div class="card card-glass p-4 mb-4" v-if="hasPerm('View_Inventory')">
        <h5 class="fw-bold mb-3 text-cyan">Tác vụ kho nhanh</h5>
        <div class="d-grid gap-2">
          <router-link to="/erp/inventory/import" class="btn btn-outline-success rounded-pill py-2.5 text-xs">
            <i class="fa-solid fa-circle-down me-2"></i>Tạo Phiếu Nhập Kho
          </router-link>
          <router-link to="/erp/inventory/export" class="btn btn-outline-danger rounded-pill py-2.5 text-xs">
            <i class="fa-solid fa-circle-up me-2"></i>Tạo Phiếu Xuất Kho
          </router-link>
        </div>
      </div>

      <div class="card card-glass p-4">
        <h5 class="fw-bold mb-3 text-cyan"><i class="fa-solid fa-clock-rotate-left me-2"></i>Lịch sử nhập xuất</h5>
        
        <div v-if="loadingHistory" class="text-center py-5">
          <div class="spinner-border text-info" role="status"></div>
        </div>

        <div class="overflow-y-auto" style="max-height: 250px;" v-else>
          <div v-for="tx in transactions" :key="tx.id" class="p-2 mb-2 rounded bg-light border border-light text-xs text-dark">
            <div class="d-flex justify-content-between mb-1">
              <span class="badge" :class="tx.type === 'Nhập kho' ? 'bg-success' : 'bg-danger'">
                {{ tx.type }} ({{ tx.code }})
              </span>
              <span class="text-muted">{{ formatDate(tx.date) }}</span>
            </div>
            <p class="mb-0.5 fw-bold">{{ tx.productName }}</p>
            <p class="mb-0 text-muted-custom">
              Số lượng: <strong>{{ tx.quantityChange }}</strong> | Người thực hiện: {{ tx.creator }}
            </p>
            <p class="mb-0 text-xxs text-muted" v-if="tx.note">Ghi chú: {{ tx.note }}</p>
          </div>
          <div v-if="transactions.length === 0" class="text-center text-muted py-4 text-xs">
            Chưa có giao dịch kho nào
          </div>
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
  name: 'Inventory',
  setup() {
    const products = ref([])
    const transactions = ref([])
    const loading = ref(true)
    const loadingHistory = ref(true)

    const hasPerm = (p) => authService.hasPermission(p)

    const fetchProducts = async () => {
      try {
        loading.value = true
        const response = await axios.get('/api/GetProducts')
        products.value = response.data
      } catch (err) {
        console.error('Error fetching products:', err)
      } finally {
        loading.value = false
      }
    }

    const fetchTransactions = async () => {
      try {
        loadingHistory.value = true
        const response = await axios.get('/api/GetInventoryTransactions')
        transactions.value = response.data
      } catch (err) {
        console.error('Error fetching transactions:', err)
      } finally {
        loadingHistory.value = false
      }
    }

    const formatMoney = (val) => {
      if (val === undefined || val === null) return 0
      return Number(val).toLocaleString()
    }

    const formatDate = (dateStr) => {
      if (!dateStr) return ''
      const d = new Date(dateStr)
      return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')} - ${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}`
    }

    onMounted(() => {
      fetchProducts()
      fetchTransactions()
    })

    return {
      products,
      transactions,
      loading,
      loadingHistory,
      hasPerm,
      formatMoney,
      formatDate
    }
  }
}
</script>
