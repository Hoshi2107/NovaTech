<template>
  <div class="container py-5 d-flex justify-content-center">
    <div class="card card-glass p-4 shadow animate-fade-in" style="max-width: 500px; width: 100%;">
      <h4 class="fw-bold mb-4 text-cyan"><i class="fa-solid fa-id-card me-2"></i>Hồ sơ cá nhân</h4>
      
      <div v-if="loading" class="text-center py-5">
        <div class="spinner-border text-info" role="status"></div>
      </div>
      
      <div v-else-if="profile" class="text-xs">
        <div class="text-center mb-4">
          <div class="avatar bg-gradient-cyan text-white d-inline-flex align-items-center justify-content-center rounded-circle shadow fw-bold mb-2" style="width: 72px; height: 72px; font-size: 2rem;">
            {{ profile.fullName ? profile.fullName.substring(0, 1).toUpperCase() : 'U' }}
          </div>
          <h5 class="fw-bold text-dark mb-0">{{ profile.fullName }}</h5>
          <span class="badge bg-gradient-purple text-white mt-1">{{ profile.roles ? profile.roles.join(', ') : 'Khách' }}</span>
        </div>

        <ul class="list-group list-group-flush mb-4 text-dark">
          <li class="list-group-item bg-transparent d-flex justify-content-between py-3 border-secondary border-opacity-10">
            <span class="text-muted-custom fw-semibold">Địa chỉ Email:</span>
            <span class="fw-semibold">{{ profile.email }}</span>
          </li>
          <li class="list-group-item bg-transparent d-flex justify-content-between py-3 border-secondary border-opacity-10">
            <span class="text-muted-custom fw-semibold">Số điện thoại:</span>
            <span class="fw-semibold">{{ profile.phone || 'Chưa cung cấp' }}</span>
          </li>
          <li class="list-group-item bg-transparent d-flex justify-content-between py-3 border-secondary border-opacity-10" v-if="profile.joinedDate">
            <span class="text-muted-custom fw-semibold">Ngày gia nhập:</span>
            <span class="fw-semibold">{{ formatDate(profile.joinedDate) }}</span>
          </li>
        </ul>

        <div class="d-flex gap-2 justify-content-center">
          <router-link to="/portal" class="btn btn-outline-secondary rounded-pill px-4">Quay lại</router-link>
          <router-link to="/change-password" class="btn btn-info rounded-pill px-4 text-white">Đổi mật khẩu</router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, onMounted } from 'vue'
import axios from 'axios'

export default {
  name: 'Profile',
  setup() {
    const profile = ref(null)
    const loading = ref(true)

    const fetchProfile = async () => {
      try {
        loading.value = true
        const response = await axios.get('/api/Profile')
        profile.value = response.data
      } catch (err) {
        console.error('Error fetching profile:', err)
      } finally {
        loading.value = false
      }
    }

    const formatDate = (dateStr) => {
      if (!dateStr) return ''
      const d = new Date(dateStr)
      return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`
    }

    onMounted(() => {
      fetchProfile()
    })

    return {
      profile,
      loading,
      formatDate
    }
  }
}
</script>
