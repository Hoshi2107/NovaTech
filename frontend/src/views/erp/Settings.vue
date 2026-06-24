<template>
  <div class="row justify-content-center">
    <div class="col-md-8">
      <div class="card card-glass p-4 text-dark animate-fade-in">
        <h5 class="fw-bold mb-4 text-cyan"><i class="fa-solid fa-gears me-2"></i>Cài đặt cửa hàng & Cấu hình</h5>
        
        <div v-if="successMsg" class="alert alert-success text-xs py-2 px-3 mb-3">
          {{ successMsg }}
        </div>

        <div v-if="loading" class="text-center py-5">
          <div class="spinner-border text-info" role="status"></div>
        </div>

        <form @submit.prevent="handleSave" v-else>
          <div class="row g-3">
            <div class="col-md-6">
              <label class="form-label text-sm">Tên cửa hàng công nghệ</label>
              <input type="text" v-model="form.shopName" class="form-control text-xs" required :disabled="!hasPerm('Edit_Setting')">
            </div>
            <div class="col-md-6">
              <label class="form-label text-sm">Đường dây nóng (Hotline)</label>
              <input type="text" v-model="form.shopHotline" class="form-control text-xs" required :disabled="!hasPerm('Edit_Setting')">
            </div>
            <div class="col-md-6">
              <label class="form-label text-sm">Email liên hệ cửa hàng</label>
              <input type="email" v-model="form.shopEmail" class="form-control text-xs" required :disabled="!hasPerm('Edit_Setting')">
            </div>
            <div class="col-md-6">
              <label class="form-label text-sm">Hệ thống giờ mở cửa</label>
              <input type="text" v-model="form.systemConfig" class="form-control text-xs" :disabled="!hasPerm('Edit_Setting')">
            </div>
            <div class="col-12">
              <label class="form-label text-sm">Địa chỉ trụ sở</label>
              <input type="text" v-model="form.shopAddress" class="form-control text-xs" required :disabled="!hasPerm('Edit_Setting')">
            </div>
          </div>

          <hr class="border-secondary my-4">

          <button type="submit" class="btn btn-info rounded-pill px-5 py-2.5 fw-bold text-xs text-white" v-if="hasPerm('Edit_Setting')">
            Lưu cấu hình lại
          </button>
          <button class="btn btn-secondary rounded-pill px-5 py-2.5 disabled text-xs" type="button" v-else>
            Không có quyền sửa cài đặt
          </button>
        </form>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, onMounted } from 'vue'
import axios from 'axios'
import { authService } from '../../services/auth'

export default {
  name: 'Settings',
  setup() {
    const loading = ref(true)
    const successMsg = ref('')
    
    const form = ref({
      shopName: '',
      shopHotline: '',
      shopEmail: '',
      systemConfig: '',
      shopAddress: ''
    })

    const hasPerm = (p) => authService.hasPermission(p)

    const fetchSettings = async () => {
      try {
        loading.value = true
        const response = await axios.get('/api/GetSettings')
        form.value = response.data
      } catch (err) {
        console.error('Error fetching settings:', err)
      } finally {
        loading.value = false
      }
    }

    const handleSave = async () => {
      successMsg.value = ''
      try {
        const response = await axios.post('/api/UpdateSettings', form.value)
        successMsg.value = response.data.message
      } catch (err) {
        console.error('Error saving settings:', err)
      }
    }

    onMounted(() => {
      fetchSettings()
    })

    return {
      form,
      loading,
      successMsg,
      hasPerm,
      handleSave
    }
  }
}
</script>
