<template>
  <div class="login-page">
    <div class="login-card animate-fade-in" style="max-width: 480px;">
      <!-- Header / Logo -->
      <div class="text-center mb-4">
        <div class="logo-container">
          <i class="fa-solid fa-bolt fs-2 me-2" style="color: #0c7eb6;"></i>
          <span class="logo-nova">Nova</span><span class="logo-tech">Tech</span>
        </div>
        <p class="logo-desc">Đăng ký tài khoản khách hàng NovaMember</p>
      </div>

      <!-- Success Msg -->
      <div v-if="successMsg" class="alert alert-success text-xs py-2 px-3 mb-3">
        <i class="fa-solid fa-circle-check me-2"></i>{{ successMsg }}
      </div>

      <!-- Error Msg -->
      <div v-if="error" class="alert alert-danger text-xs py-2 px-3 mb-3">
        <i class="fa-solid fa-circle-exclamation me-2"></i>{{ error }}
      </div>

      <!-- Form -->
      <form @submit.prevent="handleRegister" v-if="!successMsg">
        <div class="mb-3">
          <label class="form-label input-label">Họ và tên</label>
          <div class="input-group custom-input-group">
            <span class="custom-input-text"><i class="fa-solid fa-user"></i></span>
            <input type="text" v-model="fullName" class="form-control custom-input-control" placeholder="Nguyễn Văn A" required>
          </div>
        </div>

        <div class="mb-3">
          <label class="form-label input-label">Email đăng ký</label>
          <div class="input-group custom-input-group">
            <span class="custom-input-text"><i class="fa-solid fa-envelope"></i></span>
            <input type="email" v-model="email" class="form-control custom-input-control" placeholder="nguyenvana@gmail.com" required>
          </div>
        </div>

        <div class="mb-3">
          <label class="form-label input-label">Số điện thoại</label>
          <div class="input-group custom-input-group">
            <span class="custom-input-text"><i class="fa-solid fa-phone"></i></span>
            <input type="text" v-model="phone" class="form-control custom-input-control" placeholder="0901234567" required>
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

        <div class="mb-4">
          <label class="form-label input-label">Xác nhận mật khẩu</label>
          <div class="input-group custom-input-group">
            <span class="custom-input-text"><i class="fa-solid fa-lock"></i></span>
            <input :type="showPassword ? 'text' : 'password'" v-model="confirmPassword" class="form-control custom-input-control" placeholder="••••••••" required>
          </div>
        </div>

        <button type="submit" class="btn btn-login w-100 shadow-sm d-flex align-items-center justify-content-center" :disabled="loading">
          <span v-if="loading" class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
          <i class="fa-solid fa-user-plus me-2" v-else></i>
          Đăng ký ngay
        </button>
      </form>

      <!-- Back to Login -->
      <div class="text-center mt-4 pt-3 border-top border-light">
        <p class="text-secondary text-xs mb-2">
          Đã có tài khoản? 
          <router-link to="/login" class="text-link fw-bold ms-1">Đăng nhập tại đây</router-link>
        </p>
        <div class="mt-2">
          <router-link to="/store" class="back-home-link">
            <i class="fa-solid fa-house"></i> Quay lại trang chủ
          </router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'

export default {
  name: 'Register',
  setup() {
    const router = useRouter()
    const fullName = ref('')
    const email = ref('')
    const phone = ref('')
    const password = ref('')
    const confirmPassword = ref('')
    const showPassword = ref(false)
    const error = ref('')
    const successMsg = ref('')
    const loading = ref(false)

    const handleRegister = async () => {
      error.value = ''
      successMsg.value = ''

      if (password.value !== confirmPassword.value) {
        error.value = 'Mật khẩu xác nhận không khớp!'
        return
      }

      loading.value = true
      try {
        // Send register request to simulated/real api
        const response = await axios.post('/api/CreateCustomer', {
          name: fullName.value,
          email: email.value,
          phone: phone.value,
          address: 'Đăng ký trực tuyến'
        })
        
        // Also register in mock DB as credential if in mock mode
        // For simulation, we can add it to employees list or local mock database
        const dbStr = localStorage.getItem('novatech_db')
        if (dbStr) {
          const db = JSON.parse(dbStr)
          // Add this as customer login credential
          db.employees.push({
            id: db.employees.length + 1,
            fullName: fullName.value,
            email: email.value,
            phone: phone.value,
            password: password.value,
            status: 'Đang làm việc',
            roles: ['Khách hàng'],
            joinedDate: new Date().toISOString()
          })
          localStorage.setItem('novatech_db', JSON.stringify(db))
        }

        successMsg.value = 'Đăng ký tài khoản thành công! Đang chuyển hướng sang Đăng nhập...'
        setTimeout(() => {
          router.push('/login')
        }, 2000)
      } catch (err) {
        error.value = err.response?.data?.message || 'Đăng ký thất bại. Vui lòng kiểm tra lại.'
      } finally {
        loading.value = false
      }
    }

    return {
      fullName,
      email,
      phone,
      password,
      confirmPassword,
      showPassword,
      error,
      successMsg,
      loading,
      handleRegister
    }
  }
}
</script>

<style scoped>
/* Inherit same classes from Login.vue styles */
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
</style>
