<template>
  <div class="bg-light text-dark min-vh-100">
    <!-- Sidebar Layout -->
    <aside class="sidebar position-fixed top-0 bottom-0 start-0 z-3 p-3" :class="{ 'show': sidebarOpen }">
      <div class="sidebar-brand d-flex align-items-center gap-2 mb-4 pb-3 border-bottom border-secondary border-opacity-25">
        <div class="logo-icon bg-gradient-cyan text-white d-flex align-items-center justify-content-center rounded-3 shadow" style="width: 40px; height: 40px;">
          <i class="fa-solid fa-bolt-lightning fs-5"></i>
        </div>
        <div>
          <h5 class="m-0 fw-bold text-dark tracking-wider">NovaTech</h5>
          <span class="text-xs text-muted-custom">Hệ quản trị ERP v1.0</span>
        </div>
      </div>

      <div class="sidebar-menu overflow-y-auto" style="max-height: calc(100vh - 120px);">
        <ul class="nav flex-column gap-1">
          <li class="nav-item">
            <router-link to="/erp/dashboard" class="nav-link-custom d-flex align-items-center gap-3 py-2.5 px-3 rounded-3" active-class="active">
              <i class="fa-solid fa-chart-line text-cyan fs-5"></i>
              <span>Tổng quan</span>
            </router-link>
          </li>

          <!-- Hàng hóa -->
          <template v-if="hasPerm('View_Product')">
            <li class="nav-item-header text-xs text-uppercase tracking-widest text-muted-custom mt-3 mb-2 px-3">Hàng hóa</li>
            <li class="nav-item">
              <router-link to="/erp/products" class="nav-link-custom d-flex align-items-center gap-3 py-2.5 px-3 rounded-3" active-class="active">
                <i class="fa-solid fa-box text-cyan fs-5"></i>
                <span>Sản phẩm</span>
              </router-link>
            </li>
            <li class="nav-item">
              <router-link to="/erp/categories" class="nav-link-custom d-flex align-items-center gap-3 py-2.5 px-3 rounded-3" active-class="active">
                <i class="fa-solid fa-tags text-cyan fs-5"></i>
                <span>Danh mục</span>
              </router-link>
            </li>
            <li class="nav-item">
              <router-link to="/erp/brands" class="nav-link-custom d-flex align-items-center gap-3 py-2.5 px-3 rounded-3" active-class="active">
                <i class="fa-solid fa-copyright text-cyan fs-5"></i>
                <span>Thương hiệu</span>
              </router-link>
            </li>
            <li class="nav-item">
              <router-link to="/erp/suppliers" class="nav-link-custom d-flex align-items-center gap-3 py-2.5 px-3 rounded-3" active-class="active">
                <i class="fa-solid fa-truck-field text-cyan fs-5"></i>
                <span>Nhà cung cấp</span>
              </router-link>
            </li>
          </template>

          <!-- Kho bãi -->
          <template v-if="hasPerm('View_Inventory')">
            <li class="nav-item-header text-xs text-uppercase tracking-widest text-muted-custom mt-3 mb-2 px-3">Kho bãi</li>
            <li class="nav-item">
              <router-link to="/erp/inventory" class="nav-link-custom d-flex align-items-center gap-3 py-2.5 px-3 rounded-3" active-class="active">
                <i class="fa-solid fa-warehouse text-cyan fs-5"></i>
                <span>Tồn kho</span>
              </router-link>
            </li>
            <li class="nav-item">
              <router-link to="/erp/inventory/import" class="nav-link-custom d-flex align-items-center gap-3 py-2.5 px-3 rounded-3" active-class="active">
                <i class="fa-solid fa-circle-down text-cyan fs-5"></i>
                <span>Nhập kho</span>
              </router-link>
            </li>
            <li class="nav-item">
              <router-link to="/erp/inventory/export" class="nav-link-custom d-flex align-items-center gap-3 py-2.5 px-3 rounded-3" active-class="active">
                <i class="fa-solid fa-circle-up text-cyan fs-5"></i>
                <span>Xuất kho</span>
              </router-link>
            </li>
          </template>

          <!-- Bán hàng -->
          <template v-if="hasPerm('View_Order') || hasPerm('View_Customer') || hasPerm('View_Promotion')">
            <li class="nav-item-header text-xs text-uppercase tracking-widest text-muted-custom mt-3 mb-2 px-3">Bán hàng và chăm sóc khách hàng</li>
            <li class="nav-item">
              <router-link to="/erp/sales-cskh" class="nav-link-custom d-flex align-items-center gap-3 py-2.5 px-3 rounded-3" active-class="active">
                <i class="fa-solid fa-headset text-cyan fs-5"></i>
                <span>Trả lời khách hàng</span>
              </router-link>
            </li>
            <li v-if="hasPerm('View_Order')" class="nav-item">
              <router-link to="/erp/orders" class="nav-link-custom d-flex align-items-center gap-3 py-2.5 px-3 rounded-3" active-class="active">
                <i class="fa-solid fa-file-invoice-dollar text-cyan fs-5"></i>
                <span>Đơn hàng</span>
              </router-link>
            </li>
            <li v-if="hasPerm('View_Customer')" class="nav-item">
              <router-link to="/erp/customers" class="nav-link-custom d-flex align-items-center gap-3 py-2.5 px-3 rounded-3" active-class="active">
                <i class="fa-solid fa-user-group text-cyan fs-5"></i>
                <span>Khách hàng</span>
              </router-link>
            </li>
            <li v-if="hasPerm('View_Promotion')" class="nav-item">
              <router-link to="/erp/promotions" class="nav-link-custom d-flex align-items-center gap-3 py-2.5 px-3 rounded-3" active-class="active">
                <i class="fa-solid fa-gift text-cyan fs-5"></i>
                <span>Khuyến mãi</span>
              </router-link>
            </li>
          </template>

          <!-- Tích hợp -->
          <template v-if="hasPerm('View_TikTok')">
            <li class="nav-item-header text-xs text-uppercase tracking-widest text-muted-custom mt-3 mb-2 px-3">Tích hợp</li>
            <li class="nav-item">
              <router-link to="/erp/tiktok" class="nav-link-custom d-flex align-items-center gap-3 py-2.5 px-3 rounded-3" active-class="active">
                <i class="fa-brands fa-tiktok text-cyan fs-5"></i>
                <span>TikTok Shop</span>
              </router-link>
            </li>
          </template>

          <!-- Hệ thống -->
          <li class="nav-item-header text-xs text-uppercase tracking-widest text-muted-custom mt-3 mb-2 px-3">Hệ thống</li>
          <li class="nav-item">
            <router-link to="/erp/ai" class="nav-link-custom d-flex align-items-center gap-3 py-2.5 px-3 rounded-3" active-class="active">
              <i class="fa-solid fa-robot text-cyan fs-5"></i>
              <span>AI Assistant</span>
            </router-link>
          </li>
          <li v-if="hasPerm('View_Employee')" class="nav-item">
            <router-link to="/erp/employees" class="nav-link-custom d-flex align-items-center gap-3 py-2.5 px-3 rounded-3" active-class="active">
              <i class="fa-solid fa-users-gear text-cyan fs-5"></i>
              <span>Nhân sự & RBAC</span>
            </router-link>
          </li>
          <li v-if="hasPerm('View_Report')" class="nav-item">
            <router-link to="/erp/reports" class="nav-link-custom d-flex align-items-center gap-3 py-2.5 px-3 rounded-3" active-class="active">
              <i class="fa-solid fa-chart-pie text-cyan fs-5"></i>
              <span>Báo cáo</span>
            </router-link>
          </li>
          <li v-if="hasPerm('View_Setting')" class="nav-item">
            <router-link to="/erp/settings" class="nav-link-custom d-flex align-items-center gap-3 py-2.5 px-3 rounded-3" active-class="active">
              <i class="fa-solid fa-gears text-cyan fs-5"></i>
              <span>Cấu hình</span>
            </router-link>
          </li>
        </ul>
      </div>
    </aside>

    <!-- Overlay for mobile sidebar -->
    <div v-if="sidebarOpen" class="modal-backdrop fade show d-lg-none" @click="sidebarOpen = false"></div>

    <!-- Content Area wrapper -->
    <div class="main-content d-flex flex-column animate-fade-in">
      <!-- Header -->
      <header class="header position-sticky top-0 z-2 bg-glass border-bottom border-secondary border-opacity-25 px-4 py-3 d-flex align-items-center justify-content-between">
        <div class="d-flex align-items-center gap-3">
          <button class="btn btn-icon d-lg-none text-dark fs-4 border-0 bg-transparent" @click="sidebarOpen = !sidebarOpen">
            <i class="fa-solid fa-bars"></i>
          </button>
          <div class="search-box position-relative d-none d-md-block" style="width: 300px;">
            <input type="text" class="form-control bg-white border border-light text-dark rounded-pill ps-4 pe-5" placeholder="Tìm kiếm nhanh...">
            <i class="fa-solid fa-magnifying-glass position-absolute top-50 translate-middle-y end-0 me-3 text-muted"></i>
          </div>
        </div>

        <div class="d-flex align-items-center gap-3">
          <!-- Portal Selection Button -->
          <router-link to="/portal" class="btn btn-outline-info rounded-pill px-3 py-1 text-sm">
            <i class="fa-solid fa-network-wired me-2"></i>Cổng Portal
          </router-link>

          <!-- Notifications Dropdown -->
          <div class="dropdown">
            <button class="btn btn-icon text-dark position-relative border-0 bg-transparent" type="button" data-bs-toggle="dropdown">
              <i class="fa-solid fa-bell fs-5"></i>
              <span v-if="unreadCount > 0" class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger" style="font-size: 0.65rem; padding: 0.25em 0.5em;">
                {{ unreadCount }}
              </span>
            </button>
            <div class="dropdown-menu dropdown-menu-end bg-white border border-light shadow p-2" style="width: 320px; border-radius: 12px;">
              <div class="px-3 py-2 border-bottom border-light d-flex justify-content-between align-items-center text-dark">
                <span class="fw-bold">Thông báo mới</span>
                <a href="#" class="text-xs text-info text-decoration-none" @click.prevent="markAllRead">Đọc tất cả</a>
              </div>
              <div class="notification-list" style="max-height: 250px; overflow-y: auto;">
                <div v-for="n in notifications" :key="n.id" class="dropdown-item py-2.5 px-3 border-bottom border-light d-flex gap-3 align-items-start text-dark" style="white-space: normal;">
                  <div class="fs-5 mt-1">
                    <i class="fa-solid" :class="getNotifIcon(n.type)"></i>
                  </div>
                  <div>
                    <div class="fw-semibold text-sm">{{ n.title }}</div>
                    <div class="text-xs text-muted mt-0.5" style="line-height: 1.3;">{{ n.message }}</div>
                    <div class="text-xxs text-muted mt-1">{{ formatDate(n.timestamp) }}</div>
                  </div>
                </div>
                <div v-if="notifications.length === 0" class="text-center py-4 text-xs text-muted">
                  Không có thông báo mới nào
                </div>
              </div>
            </div>
          </div>

          <!-- User Profile Dropdown -->
          <div class="dropdown" v-if="user">
            <button class="btn d-flex align-items-center gap-2 text-dark border-0 bg-transparent" type="button" data-bs-toggle="dropdown">
              <div class="avatar bg-gradient-cyan text-white d-flex align-items-center justify-content-center rounded-circle shadow fw-bold" style="width: 36px; height: 36px;">
                {{ user.avatar }}
              </div>
              <span class="fw-semibold text-sm d-none d-sm-inline">{{ user.fullName }}</span>
              <i class="fa-solid fa-chevron-down text-xs text-muted"></i>
            </button>
            <ul class="dropdown-menu dropdown-menu-end bg-white border border-light shadow text-dark">
              <li>
                <div class="px-3 py-2 border-bottom border-light text-dark">
                  <span class="d-block fw-semibold text-sm">{{ user.fullName }}</span>
                  <span class="d-block text-xs text-muted" style="color: #64748b !important;">{{ user.roles.join(', ') }}</span>
                </div>
              </li>
              <li>
                <router-link class="dropdown-item py-2 text-dark" to="/profile">
                  <i class="fa-solid fa-id-card me-2 text-cyan"></i>Hồ sơ cá nhân
                </router-link>
              </li>
              <li>
                <router-link class="dropdown-item py-2 text-dark" to="/change-password">
                  <i class="fa-solid fa-key me-2 text-cyan"></i>Đổi mật khẩu
                </router-link>
              </li>
              <li><hr class="dropdown-divider border-light"></li>
              <li>
                <a class="dropdown-item py-2 text-danger" href="#" @click.prevent="logout">
                  <i class="fa-solid fa-right-from-bracket me-2"></i>Đăng xuất
                </a>
              </li>
            </ul>
          </div>
        </div>
      </header>

      <!-- Main Body -->
      <div class="container-fluid p-4 flex-grow-1">
        <!-- Render Child Route -->
        <router-view />
      </div>

      <!-- Footer -->
      <footer class="bg-glass border-top border-secondary border-opacity-10 py-3 text-center text-xs text-muted-custom">
        <span>© 2026 Cửa Hàng Công Nghệ NovaTech. Xây dựng bởi Antigravity. OS: Windows.</span>
      </footer>
    </div>
  </div>
</template>

<script>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { authState, authService } from '../services/auth'
import axios from 'axios'

export default {
  name: 'AdminLayout',
  setup() {
    const router = useRouter()
    const sidebarOpen = ref(false)
    const notifications = ref([])

    const user = computed(() => authState.user)

    const unreadCount = computed(() => {
      return notifications.value.filter(n => !n.isRead).length
    })

    const hasPerm = (permission) => {
      return authService.hasPermission(permission)
    }

    const fetchNotifications = async () => {
      try {
        const response = await axios.get('/api/GetDashboard')
        notifications.value = response.data.recentNotifications || []
      } catch (err) {
        console.error('Error fetching notifications:', err)
      }
    }

    const markAllRead = () => {
      notifications.value.forEach(n => n.isRead = true)
    }

    const logout = async () => {
      await authService.logout()
      router.push('/login')
    }

    const getNotifIcon = (type) => {
      if (type === 'Đơn mới') return 'fa-cart-shopping text-success'
      if (type === 'Hết hàng') return 'fa-triangle-exclamation text-warning'
      if (type === 'Đồng bộ lỗi') return 'fa-circle-xmark text-danger'
      return 'fa-circle-info text-info'
    }

    const formatDate = (dateStr) => {
      if (!dateStr) return ''
      const d = new Date(dateStr)
      return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')} - ${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}`
    }

    onMounted(() => {
      fetchNotifications()
    })

    return {
      sidebarOpen,
      notifications,
      unreadCount,
      user,
      hasPerm,
      markAllRead,
      logout,
      getNotifIcon,
      formatDate
    }
  }
}
</script>
