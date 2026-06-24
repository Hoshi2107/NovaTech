<template>
  <div class="row g-4">
    <!-- Config and Sync Action panel -->
    <div class="col-md-5" v-if="config">
      <div class="card card-glass p-4 mb-4 text-dark">
        <h5 class="fw-bold mb-4 text-cyan"><i class="fa-brands fa-tiktok me-2"></i>Cấu hình kết nối TikTok</h5>
        
        <div v-if="successMsg" class="alert alert-success text-xs py-2 px-3 mb-3">
          {{ successMsg }}
        </div>

        <form @submit.prevent="handleSaveConfig" class="mb-4">
          <div class="mb-3">
            <label class="form-check-label d-flex align-items-center gap-2 mb-2 cursor-pointer fw-semibold text-xs text-secondary">
              <input type="checkbox" v-model="config.isConnected" class="form-check-input">
              Kích hoạt kết nối với TikTok Shop API
            </label>
          </div>
          
          <div class="mb-3">
            <label class="form-label text-xs text-secondary fw-semibold">Tên gian hàng (Shop Name)</label>
            <input type="text" v-model="config.shopName" class="form-control text-xs" required :disabled="!config.isConnected">
          </div>

          <div class="mb-4">
            <label class="form-label text-xs text-secondary fw-semibold">ID cửa hàng (Shop ID)</label>
            <input type="text" v-model="config.shopId" class="form-control text-xs" required :disabled="!config.isConnected">
          </div>

          <button type="submit" class="btn btn-info rounded-pill py-2 px-4 w-100 fw-bold text-xs text-white">Lưu cấu hình</button>
        </form>

        <hr class="border-secondary border-opacity-10 my-4">

        <h6 class="fw-bold text-xs text-muted-custom uppercase mb-3">TÁC VỤ ĐỒNG BỘ DỮ LIỆU</h6>
        <div class="d-grid gap-2">
          <button type="button" @click="handleSync('Sản phẩm')" class="btn btn-outline-info rounded-pill py-2.5 text-xs text-start px-4" :disabled="!config.isConnected || syncing">
            <i class="fa-solid fa-arrows-rotate me-2 animate-spin"></i>Đồng bộ danh mục Sản phẩm
          </button>
          <button type="button" @click="handleSync('Đơn hàng')" class="btn btn-outline-warning text-dark rounded-pill py-2.5 text-xs text-start px-4" :disabled="!config.isConnected || syncing">
            <i class="fa-solid fa-cart-flatbed-suitcase me-2"></i>Đồng bộ Đơn hàng mới
          </button>
          <button type="button" @click="handleSync('Tồn kho')" class="btn btn-outline-success rounded-pill py-2.5 text-xs text-start px-4" :disabled="!config.isConnected || syncing">
            <i class="fa-solid fa-warehouse-full me-2"></i>Cập nhật số lượng Kho
          </button>
        </div>
      </div>

      <!-- Sync Status Card -->
      <div class="card card-glass p-4 text-dark" v-if="config">
        <h5 class="fw-bold mb-3 text-cyan">Trạng thái đồng bộ</h5>
        <div class="text-xs">
          <div class="d-flex justify-content-between mb-2">
            <span class="text-muted-custom">Trạng thái:</span>
            <span class="badge" :class="config.isConnected ? 'bg-success' : 'bg-secondary'">
              {{ config.isConnected ? 'Đã liên kết' : 'Chưa kết nối' }}
            </span>
          </div>
          <div class="d-flex justify-content-between">
            <span class="text-muted-custom">Đồng bộ gần nhất:</span>
            <span class="fw-bold">{{ formatDate(config.lastSyncTime) }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Sync Log panel -->
    <div class="col-md-7">
      <div class="card card-glass p-4">
        <h5 class="fw-bold mb-4 text-cyan"><i class="fa-solid fa-book me-2"></i>Nhật ký đồng bộ hệ thống</h5>
        
        <div v-if="loadingLogs" class="text-center py-5">
          <div class="spinner-border text-info" role="status"></div>
        </div>

        <div class="table-responsive" style="max-height: 520px; overflow-y: auto;" v-else>
          <table class="table table-dark table-hover mb-0 text-xs align-middle">
            <thead>
              <tr>
                <th>Phân loại</th>
                <th>Thông tin mô tả</th>
                <th>Kết quả</th>
                <th>Thời gian</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="l in logs" :key="l.id">
                <td><span class="badge bg-secondary">{{ l.type }}</span></td>
                <td>{{ l.message }}</td>
                <td>
                  <span v-if="l.status === 'Thành công'" class="badge bg-success-subtle text-success border border-success border-opacity-25 rounded-pill px-2">
                    {{ l.status }}
                  </span>
                  <span v-else class="badge bg-danger-subtle text-danger border border-danger border-opacity-25 rounded-pill px-2">
                    {{ l.status }}
                  </span>
                </td>
                <td>{{ formatLogDate(l.timestamp) }}</td>
              </tr>
              <tr v-if="logs.length === 0">
                <td colspan="4" class="text-center py-4 text-muted">Chưa có nhật ký đồng bộ nào</td>
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

export default {
  name: 'TikTokShop',
  setup() {
    const config = ref({
      isConnected: false,
      shopName: '',
      shopId: '',
      syncStatus: '',
      lastSyncTime: ''
    })

    const logs = ref([])
    const loadingLogs = ref(true)
    const syncing = ref(false)
    const successMsg = ref('')

    const fetchConfig = async () => {
      try {
        const response = await axios.get('/api/GetTiktokConfig')
        config.value = response.data
      } catch (err) {
        console.error('Error fetching TikTok config:', err)
      }
    }

    const fetchLogs = async () => {
      try {
        loadingLogs.value = true
        const response = await axios.get('/api/GetTiktokLogs')
        logs.value = response.data
      } catch (err) {
        console.error('Error fetching TikTok logs:', err)
      } finally {
        loadingLogs.value = false
      }
    }

    const handleSaveConfig = async () => {
      successMsg.value = ''
      try {
        const response = await axios.post('/api/SaveTiktokConfig', config.value)
        config.value = response.data.config
        successMsg.value = response.data.message
        fetchLogs()
      } catch (err) {
        console.error('Error saving config:', err)
      }
    }

    const handleSync = async (syncType) => {
      syncing.value = true
      successMsg.value = ''
      try {
        const response = await axios.post('/api/SyncTiktok', { syncType: syncType })
        config.value.lastSyncTime = response.data.lastSyncTime
        successMsg.value = response.data.message
        fetchLogs()
      } catch (err) {
        console.error('Error syncing:', err)
      } finally {
        syncing.value = false
      }
    }

    const formatDate = (dateStr) => {
      if (!dateStr) return 'Chưa từng đồng bộ'
      const d = new Date(dateStr)
      return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')} - ${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}/${d.getFullYear()}`
    }

    const formatLogDate = (dateStr) => {
      if (!dateStr) return ''
      const d = new Date(dateStr)
      return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}:${String(d.getSeconds()).padStart(2, '0')} ${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}`
    }

    onMounted(() => {
      fetchConfig()
      fetchLogs()
    })

    return {
      config,
      logs,
      loadingLogs,
      syncing,
      successMsg,
      handleSaveConfig,
      handleSync,
      formatDate,
      formatLogDate
    }
  }
}
</script>
