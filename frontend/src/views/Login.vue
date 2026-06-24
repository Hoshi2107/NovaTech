<template>
  <div class="login-page">
    <div class="login-card animate-fade-in">
      <!-- Header / Logo -->
      <div class="text-center mb-4">
        <div class="logo-container">
          <i class="fa-solid fa-bolt fs-2 me-2" style="color: #0c7eb6;"></i>
          <span class="logo-nova">Nova</span><span class="logo-tech">Tech</span>
        </div>
        <p class="logo-desc">Hệ thống quản lý bán hàng đa kênh</p>
      </div>

      <!-- Error Message -->
      <div v-if="error" class="alert alert-danger text-xs py-2 px-3 mb-3">
        <i class="fa-solid fa-circle-exclamation me-2"></i>{{ error }}
      </div>

      <!-- Form -->
      <form @submit.prevent="handleLogin">
        <div class="mb-3">
          <label class="form-label input-label">Email đăng nhập</label>
          <div class="input-group custom-input-group">
            <span class="custom-input-text"><i class="fa-solid fa-envelope"></i></span>
            <input type="email" v-model="email" class="form-control custom-input-control" placeholder="admin@novatech.vn" required>
          </div>
        </div>

        <div class="mb-3">
          <label class="form-label input-label">Mật khẩu</label>
          <div class="input-group custom-input-group">
            <span class="custom-input-text"><i class="fa-solid fa-lock"></i></span>
            <input :type="showPassword ? 'text' : 'password'" v-model="password" class="form-control custom-input-control" placeholder="••••••••" required>
            <button class="btn btn-link bg-white text-secondary border-0 px-3" type="button" @click="showPassword = !showPassword" style="box-shadow: none;">
              <i class="fa-solid" :class="showPassword ? 'fa-eye-slash' : 'fa-eye'"></i>
            </button>
          </div>
        </div>

        <div class="d-flex justify-content-between align-items-center mb-4 text-xs">
          <div class="form-check">
            <input class="form-check-input" type="checkbox" id="rememberMe">
            <label class="form-check-label text-secondary cursor-pointer" for="rememberMe">Ghi nhớ đăng nhập</label>
          </div>
          <router-link to="/forgot-password" class="text-link">Quên mật khẩu?</router-link>
        </div>

        <button type="submit" class="btn btn-login w-100 shadow-sm d-flex align-items-center justify-content-center" :disabled="loading">
          <span v-if="loading" class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
          <i class="fa-solid fa-right-to-bracket me-2" v-else></i>
          Đăng nhập
        </button>
      </form>

      <!-- Register Link -->
      <div class="text-center mt-4 pt-3 border-top border-light">
        <p class="text-secondary text-xs mb-2">
          Chưa có tài khoản? 
          <router-link to="/register" class="text-link fw-bold ms-1">Đăng ký tại đây</router-link>
        </p>
        <div class="mt-2">
          <router-link to="/store" class="back-home-link">
            <i class="fa-solid fa-house"></i> Quay lại trang chủ
          </router-link>
        </div>
      </div>

      <!-- Demo Accounts -->
      <div class="text-center mt-4 pt-3 border-top border-light">
        <p class="text-secondary text-xs mb-2.5">Tài khoản Demo hệ thống:</p>
        <div class="d-flex flex-wrap justify-content-center gap-2 mb-3">
          <span class="demo-badge" @click="quickLogin('admin@novatech.vn')">Super Admin</span>
          <span class="demo-badge" @click="quickLogin('sale@novatech.vn')">Bán hàng</span>
          <span class="demo-badge" @click="quickLogin('kho@novatech.vn')">Nhân viên kho</span>
          <span class="demo-badge" @click="quickLogin('customer@gmail.com')">Khách hàng</span>
        </div>
        <p class="text-muted text-xxs mb-0">(Mật khẩu mặc định là: 123)</p>
      </div>
    </div>
  </div>
</template>

<script>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { authService } from '../services/auth'

export default {
  name: 'Login',
  setup() {
    const router = useRouter()
    const email = ref('')
    const password = ref('')
    const showPassword = ref(false)
    const error = ref('')
    const loading = ref(false)

    const handleLogin = async () => {
      error.value = ''
      loading.value = true
      const result = await authService.login(email.value, password.value)
      loading.value = false
      if (result.success) {
        router.push('/portal')
      } else {
        error.value = result.message
      }
    }

    const quickLogin = (userEmail) => {
      email.value = userEmail
      password.value = '123'
    }

    return {
      email,
      password,
      showPassword,
      error,
      loading,
      handleLogin,
      quickLogin
    }
  }
}
</script>

<style scoped>
.login-page {
  background-color: #e9eff4;
  min-height: 100vh;
  width: 100vw;
  display: flex;
  align-items: center;
  justify-content: center;
  position: absolute;
  top: 0;
  left: 0;
  z-index: 999;
}
.login-card {
  background-color: #ffffff;
  border-radius: 20px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.05);
  border: none;
  max-width: 440px;
  width: 90%;
  padding: 3rem 2.5rem;
}
.logo-container {
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 0.5rem;
}
.logo-nova {
  color: #0c7eb6;
  font-weight: 800;
  font-size: 2.2rem;
  letter-spacing: -0.5px;
}
.logo-tech {
  background-color: #0c7eb6;
  color: #ffffff;
  font-weight: 800;
  font-size: 2.2rem;
  padding: 2px 8px;
  border-radius: 4px;
  margin-left: 2px;
  letter-spacing: -0.5px;
}
.logo-desc {
  color: #64748b;
  font-size: 0.95rem;
  margin-top: 0.5rem;
}
.input-label {
  color: #64748b;
  font-weight: 600;
  font-size: 0.85rem;
  margin-bottom: 0.5rem;
  text-align: left;
  display: block;
}
.custom-input-group {
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  overflow: hidden;
  background-color: #ffffff;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}
.custom-input-group:focus-within {
  border-color: #0c7eb6;
  box-shadow: 0 0 0 3px rgba(12, 126, 182, 0.15);
}
.custom-input-text {
  background-color: #f8fafc;
  border: none;
  border-right: 1px solid #e2e8f0;
  color: #64748b;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 46px;
  font-size: 0.9rem;
}
.custom-input-control {
  border: none !important;
  box-shadow: none !important;
  padding: 0.65rem 0.75rem;
  font-size: 0.9rem;
}
.btn-login {
  background-color: #0c7eb6;
  border-color: #0c7eb6;
  color: #ffffff;
  font-weight: bold;
  font-size: 0.95rem;
  padding: 0.75rem;
  border-radius: 8px;
  transition: all 0.2s ease;
}
.btn-login:hover {
  background-color: #0a6998;
  border-color: #0a6998;
  color: #ffffff;
}
.text-link {
  color: #0c7eb6;
  text-decoration: none;
  font-weight: 600;
}
.text-link:hover {
  text-decoration: underline;
}
.back-home-link {
  color: #64748b;
  text-decoration: none;
  font-weight: 600;
  font-size: 0.8rem;
  transition: color 0.2s ease;
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
}
.back-home-link:hover {
  color: #0c7eb6;
}
.demo-badge {
  background-color: #e9eff4;
  color: #334155;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  font-size: 0.75rem;
  font-weight: 600;
  padding: 0.35rem 0.65rem;
  cursor: pointer;
  transition: all 0.15s ease;
}
.demo-badge:hover {
  background-color: #d1dfeb;
  border-color: #94a3b8;
}
</style>
