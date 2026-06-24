<template>
  <div class="container py-5 d-flex justify-content-center">
    <div class="card card-glass p-4 shadow animate-fade-in" style="max-width: 450px; width: 100%;">
      <h4 class="fw-bold mb-4 text-cyan"><i class="fa-solid fa-key me-2"></i>Đổi mật khẩu</h4>

      <div v-if="successMsg" class="alert alert-success text-xs py-2 px-3 mb-3">
        {{ successMsg }}
      </div>
      <div v-if="errorMsg" class="alert alert-danger text-xs py-2 px-3 mb-3">
        {{ errorMsg }}
      </div>

      <form @submit.prevent="handleChangePassword" v-if="!successMsg">
        <div class="mb-3">
          <label class="form-label text-xs text-secondary fw-semibold">Mật khẩu hiện tại</label>
          <input type="password" v-model="oldPassword" class="form-control text-xs" placeholder="Nhập mật khẩu hiện tại" required>
        </div>

        <div class="mb-3">
          <label class="form-label text-xs text-secondary fw-semibold">Mật khẩu mới</label>
          <input type="password" v-model="newPassword" class="form-control text-xs" placeholder="Nhập mật khẩu mới" required>
        </div>

        <div class="mb-4">
          <label class="form-label text-xs text-secondary fw-semibold">Xác nhận mật khẩu mới</label>
          <input type="password" v-model="confirmPassword" class="form-control text-xs" placeholder="Nhập lại mật khẩu mới" required>
        </div>

        <div class="d-flex gap-2 justify-content-end">
          <router-link to="/portal" class="btn btn-outline-secondary rounded-pill px-4 text-xs">Hủy</router-link>
          <button type="submit" class="btn btn-info rounded-pill px-4 text-white text-xs">Cập nhật mật khẩu</button>
        </div>
      </form>
      
      <div class="text-center mt-3" v-else>
        <router-link to="/portal" class="btn btn-info rounded-pill px-4 text-white text-xs">Quay lại trang chủ</router-link>
      </div>
    </div>
  </div>
</template>

<script>
import { ref } from 'vue'
import axios from 'axios'

export default {
  name: 'ChangePassword',
  setup() {
    const oldPassword = ref('')
    const newPassword = ref('')
    const confirmPassword = ref('')
    const successMsg = ref('')
    const errorMsg = ref('')

    const handleChangePassword = async () => {
      errorMsg.value = ''
      successMsg.value = ''
      
      if (newPassword.value !== confirmPassword.value) {
        errorMsg.value = 'Mật khẩu xác nhận không khớp!'
        return
      }

      try {
        const response = await axios.post('/api/ChangePassword', {
          oldPassword: oldPassword.value,
          newPassword: newPassword.value
        })
        successMsg.value = response.data.message || 'Thay đổi mật khẩu thành công!'
      } catch (err) {
        errorMsg.value = err.response?.data?.message || 'Có lỗi xảy ra khi đổi mật khẩu.'
      }
    }

    return {
      oldPassword,
      newPassword,
      confirmPassword,
      successMsg,
      errorMsg,
      handleChangePassword
    }
  }
}
</script>
