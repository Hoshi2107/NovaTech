<template>
  <div class="container d-flex align-items-center justify-content-center min-vh-100">
    <div class="card card-glass p-4 shadow-lg animate-fade-in" style="max-width: 420px; width: 100%;">
      <div class="text-center mb-4">
        <h4 class="fw-extrabold text-neon">KHÔI PHỤC MẬT KHẨU</h4>
        <p class="text-muted-custom text-xs">Nhập email tài khoản để nhận liên kết khôi phục mật khẩu</p>
      </div>

      <div v-if="successMsg" class="alert alert-success text-xs py-2.5 px-3 mb-3">
        <i class="fa-solid fa-circle-check me-2"></i>{{ successMsg }}
      </div>

      <form v-else @submit.prevent="handleSubmit">
        <div class="mb-4">
          <label class="form-label text-xs text-secondary fw-semibold">Địa chỉ Email</label>
          <div class="input-group">
            <span class="input-group-text bg-light"><i class="fa-solid fa-envelope text-muted text-xs"></i></span>
            <input type="email" v-model="email" class="form-control text-xs ps-1" placeholder="admin@novatech.vn" required>
          </div>
        </div>

        <button type="submit" class="btn btn-info w-100 rounded-pill py-2.5 fw-semibold text-sm shadow">
          Gửi Yêu Cầu Khôi Phục
        </button>
      </form>

      <div class="text-center mt-4 pt-3 border-top border-secondary border-opacity-10">
        <router-link to="/login" class="text-xs text-info text-decoration-none fw-semibold">
          <i class="fa-solid fa-arrow-left me-1"></i>Trở lại màn hình đăng nhập
        </router-link>
      </div>
    </div>
  </div>
</template>

<script>
import { ref } from 'vue'

export default {
  name: 'ForgotPassword',
  setup() {
    const email = ref('')
    const successMsg = ref('')

    const handleSubmit = () => {
      successMsg.value = `Một email khôi phục mật khẩu đã được gửi đến: ${email.value}`
    }

    return {
      email,
      successMsg,
      handleSubmit
    }
  }
}
</script>
