import { reactive } from 'vue'
import axios from 'axios'

export const authState = reactive({
  user: null, // user session: { fullName, email, roles, permissions, avatar }
  loading: true,
  error: null
})

export const authService = {
  async fetchSession() {
    try {
      authState.loading = true
      const response = await axios.get('/api/GetSession')
      authState.user = response.data
      authState.error = null
    } catch (err) {
      authState.user = null
      if (err.response && err.response.status !== 401) {
        authState.error = err.response.data?.message || 'Lỗi xác thực hệ thống.'
      }
    } finally {
      authState.loading = false
    }
    return authState.user
  },

  async login(email, password) {
    try {
      const response = await axios.post('/api/Login', { email, password })
      authState.user = response.data
      authState.error = null
      return { success: true }
    } catch (err) {
      const msg = err.response?.data?.message || 'Đăng nhập thất bại. Vui lòng kiểm tra lại.'
      authState.error = msg
      return { success: false, message: msg }
    }
  },

  async logout() {
    try {
      await axios.post('/api/Logout')
    } catch (err) {
      console.error('Logout error:', err)
    } finally {
      authState.user = null
      authState.error = null
    }
  },

  hasPermission(permission) {
    if (!authState.user) return false
    if (authState.user.roles.includes('Super Admin')) return true
    return authState.user.permissions.includes(permission)
  },

  hasRole(role) {
    if (!authState.user) return false
    return authState.user.roles.includes(role)
  }
}
