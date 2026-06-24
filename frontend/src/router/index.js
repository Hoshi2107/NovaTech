import { createRouter, createWebHashHistory } from 'vue-router'
import { authService, authState } from '../services/auth'

// Lazy loaded views
import Login from '../views/Login.vue'
import Register from '../views/Register.vue'
import ForgotPassword from '../views/ForgotPassword.vue'
import Profile from '../views/Profile.vue'
import ChangePassword from '../views/ChangePassword.vue'
import PortalSelection from '../views/PortalSelection.vue'
import POS from '../views/POS.vue'

import AdminLayout from '../components/AdminLayout.vue'
import Dashboard from '../views/erp/Dashboard.vue'
import Products from '../views/erp/Products.vue'
import Categories from '../views/erp/Categories.vue'
import Brands from '../views/erp/Brands.vue'
import Suppliers from '../views/erp/Suppliers.vue'
import Inventory from '../views/erp/Inventory.vue'
import ImportStock from '../views/erp/ImportStock.vue'
import ExportStock from '../views/erp/ExportStock.vue'
import Orders from '../views/erp/Orders.vue'
import OrderDetail from '../views/erp/OrderDetail.vue'
import Customers from '../views/erp/Customers.vue'
import Promotions from '../views/erp/Promotions.vue'
import TikTokShop from '../views/erp/TikTokShop.vue'
import AiAssistant from '../views/erp/AiAssistant.vue'
import Employees from '../views/erp/Employees.vue'
import Reports from '../views/erp/Reports.vue'
import Settings from '../views/erp/Settings.vue'

import OnlineLayout from '../components/OnlineLayout.vue'
import OnlineStorefront from '../views/store/OnlineStorefront.vue'
import OnlineProductsList from '../views/store/OnlineProductsList.vue'
import OnlineProductDetail from '../views/store/OnlineProductDetail.vue'
import OnlineCart from '../views/store/OnlineCart.vue'

const routes = [
  // Root Redirect to Storefront
  { path: '/', redirect: '/store' },
  
  // Authentication & Portal
  { path: '/portal', component: PortalSelection, meta: { requiresAuth: true } },
  { path: '/login', component: Login, meta: { guestOnly: true } },
  { path: '/register', component: Register, meta: { guestOnly: true } },
  { path: '/forgot-password', component: ForgotPassword, meta: { guestOnly: true } },
  { path: '/profile', component: Profile, meta: { requiresAuth: true } },
  { path: '/change-password', component: ChangePassword, meta: { requiresAuth: true } },
  
  // POS System
  { path: '/pos', component: POS, meta: { requiresAuth: true, permission: 'View_Order' } },

  // ERP System (Admin)
  {
    path: '/erp',
    component: AdminLayout,
    meta: { requiresAuth: true },
    children: [
      { path: '', redirect: '/erp/dashboard' },
      { path: 'dashboard', component: Dashboard },
      { path: 'products', component: Products, meta: { permission: 'View_Product' } },
      { path: 'categories', component: Categories, meta: { permission: 'View_Product' } },
      { path: 'brands', component: Brands, meta: { permission: 'View_Product' } },
      { path: 'suppliers', component: Suppliers, meta: { permission: 'View_Product' } },
      { path: 'inventory', component: Inventory, meta: { permission: 'View_Inventory' } },
      { path: 'inventory/import', component: ImportStock, meta: { permission: 'View_Inventory' } },
      { path: 'inventory/export', component: ExportStock, meta: { permission: 'View_Inventory' } },
      { path: 'orders', component: Orders, meta: { permission: 'View_Order' } },
      { path: 'orders/:id', component: OrderDetail, meta: { permission: 'View_Order' } },
      { path: 'customers', component: Customers, meta: { permission: 'View_Customer' } },
      { path: 'promotions', component: Promotions, meta: { permission: 'View_Promotion' } },
      { path: 'tiktok', component: TikTokShop, meta: { permission: 'View_TikTok' } },
      { path: 'ai', component: AiAssistant },
      { path: 'employees', component: Employees, meta: { permission: 'View_Employee' } },
      { path: 'reports', component: Reports, meta: { permission: 'View_Report' } },
      { path: 'settings', component: Settings, meta: { permission: 'View_Setting' } }
    ]
  },

  // Online Storefront (Public)
  {
    path: '/store',
    component: OnlineLayout,
    children: [
      { path: '', component: OnlineStorefront },
      { path: 'products', component: OnlineProductsList },
      { path: 'products/:id', component: OnlineProductDetail },
      { path: 'cart', component: OnlineCart }
    ]
  },

  // Fallback
  { path: '/:pathMatch(.*)*', redirect: '/' }
]

const router = createRouter({
  history: createWebHashHistory(),
  routes
})

// Navigation Guard
router.beforeEach(async (to, from, next) => {
  // Always fetch session if not yet loaded to verify login state
  if (authState.loading) {
    await authService.fetchSession()
  }

  const isLoggedIn = !!authState.user

  if (to.meta.requiresAuth && !isLoggedIn) {
    next('/login')
  } else if (to.meta.guestOnly && isLoggedIn) {
    next('/portal')
  } else if (to.meta.permission && isLoggedIn && !authService.hasPermission(to.meta.permission)) {
    alert('Bạn không có quyền truy cập vào chức năng này!')
    next('/portal')
  } else {
    next()
  }
})

export default router
