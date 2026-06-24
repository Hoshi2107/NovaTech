<template>
  <div class="bg-light text-dark min-vh-100 d-flex flex-column">
    <!-- Navigation -->
    <nav class="navbar navbar-expand-lg navbar-light bg-gradient-nav sticky-top py-3">
      <div class="container">
        <router-link class="navbar-brand d-flex align-items-center gap-2 text-decoration-none" to="/store">
          <i class="fa-solid fa-bolt fs-3 animate-pulse" style="color: #0c7eb6;"></i>
          <span class="logo-brand-nova">Nova</span><span class="logo-brand-tech-blue">Tech</span>
        </router-link>

        <!-- Category Mega Menu Button -->
        <div class="mega-menu-dropdown-toggle ms-2 d-none d-lg-block">
          <button class="btn btn-outline-primary rounded-pill px-3 py-2 text-xs fw-bold shadow-sm d-flex align-items-center gap-2">
            <i class="fa-solid fa-table-cells"></i> Danh mục
          </button>
          <div class="mega-menu-wrapper">
            <div class="mega-menu-left-sidebar">
              <router-link to="/store/products?category=Điện thoại" class="mega-menu-left-item" :class="{ active: activePanel === 'dt' }" @mouseenter="activePanel = 'dt'">
                <span><i class="fa-solid fa-mobile-screen-button me-2 text-primary"></i>Điện thoại</span>
                <i class="fa-solid fa-chevron-right text-xs"></i>
              </router-link>
              <router-link to="/store/products?category=Laptop" class="mega-menu-left-item" :class="{ active: activePanel === 'lt' }" @mouseenter="activePanel = 'lt'">
                <span><i class="fa-solid fa-laptop me-2 text-primary"></i>Laptop</span>
                <i class="fa-solid fa-chevron-right text-xs"></i>
              </router-link>
              <router-link to="/store/products?category=Máy tính bảng" class="mega-menu-left-item" :class="{ active: activePanel === 'mtb' }" @mouseenter="activePanel = 'mtb'">
                <span><i class="fa-solid fa-tablet-screen-button me-2 text-primary"></i>Máy tính bảng</span>
                <i class="fa-solid fa-chevron-right text-xs"></i>
              </router-link>
              <router-link to="/store/products?category=Tai nghe" class="mega-menu-left-item" :class="{ active: activePanel === 'tn' }" @mouseenter="activePanel = 'tn'">
                <span><i class="fa-solid fa-headphones me-2 text-primary"></i>Tai nghe</span>
                <i class="fa-solid fa-chevron-right text-xs"></i>
              </router-link>
              <router-link to="/store/products?category=Phụ kiện" class="mega-menu-left-item" :class="{ active: activePanel === 'pk' }" @mouseenter="activePanel = 'pk'">
                <span><i class="fa-solid fa-keyboard me-2 text-primary"></i>Phụ kiện</span>
                <i class="fa-solid fa-chevron-right text-xs"></i>
              </router-link>
              <router-link to="/store/products?category=Loa" class="mega-menu-left-item" :class="{ active: activePanel === 'loa' }" @mouseenter="activePanel = 'loa'">
                <span><i class="fa-solid fa-volume-high me-2 text-primary"></i>Loa & Âm thanh</span>
                <i class="fa-solid fa-chevron-right text-xs"></i>
              </router-link>
            </div>
            
            <!-- Right Panels -->
            <div class="mega-menu-right-panel" :class="{ active: activePanel === 'dt' }">
              <div class="row">
                <div class="col-md-7">
                  <h6 class="fw-bold text-dark mb-3">Hãng điện thoại</h6>
                  <div class="mega-menu-brand-grid">
                    <router-link to="/store/products?category=Điện thoại&brand=Apple" class="mega-menu-brand-badge">Apple</router-link>
                    <router-link to="/store/products?category=Điện thoại&brand=Samsung" class="mega-menu-brand-badge">Samsung</router-link>
                    <router-link to="/store/products?category=Điện thoại&brand=Xiaomi" class="mega-menu-brand-badge">Xiaomi</router-link>
                    <router-link to="/store/products?category=Điện thoại&brand=Oppo" class="mega-menu-brand-badge">Oppo</router-link>
                  </div>
                  <h6 class="fw-bold text-dark mt-4 mb-2">Dòng sản phẩm HOT</h6>
                  <ul class="list-unstyled text-xs text-secondary d-grid gap-2">
                    <li><router-link to="/store/products?category=Điện thoại" class="text-decoration-none text-secondary">iPhone 15 Pro Max 256GB</router-link></li>
                  </ul>
                </div>
                <div class="col-md-5 border-start border-light ps-3">
                  <h6 class="fw-bold text-dark mb-3"><i class="fa-solid fa-fire text-danger me-1"></i>Bán chạy nhất</h6>
                  <div class="p-2.5 rounded bg-light border border-light text-center">
                    <img src="https://images.unsplash.com/photo-1695048133142-1a20484d2569?q=80&w=150" class="rounded border mb-2" style="width: 60px; height: 60px; object-fit: cover;">
                    <div class="fw-bold text-dark text-xxs">iPhone 15 Pro Max</div>
                    <div class="text-danger fw-bold text-xs mt-1">27.990.000 đ</div>
                    <div class="text-xxs text-muted text-decoration-line-through">32.990.000 đ</div>
                  </div>
                </div>
              </div>
            </div>
            
            <div class="mega-menu-right-panel" :class="{ active: activePanel === 'lt' }">
              <div class="row">
                <div class="col-md-7">
                  <h6 class="fw-bold text-dark mb-3">Thương hiệu Laptop</h6>
                  <div class="mega-menu-brand-grid">
                    <router-link to="/store/products?category=Laptop&brand=Apple" class="mega-menu-brand-badge">Apple</router-link>
                    <router-link to="/store/products?category=Laptop&brand=Asus" class="mega-menu-brand-badge">Asus</router-link>
                    <router-link to="/store/products?category=Laptop&brand=Dell" class="mega-menu-brand-badge">Dell</router-link>
                  </div>
                </div>
                <div class="col-md-5 border-start border-light ps-3">
                  <h6 class="fw-bold text-dark mb-3"><i class="fa-solid fa-fire text-danger me-1"></i>Bán chạy nhất</h6>
                  <div class="p-2.5 rounded bg-light border border-light text-center">
                    <img src="https://images.unsplash.com/photo-1517336714731-489689fd1ca8?q=80&w=150" class="rounded border mb-2" style="width: 60px; height: 60px; object-fit: cover;">
                    <div class="fw-bold text-dark text-xxs">MacBook Pro 14 inch M3</div>
                    <div class="text-primary fw-bold text-xs mt-1">39.990.000 đ</div>
                  </div>
                </div>
              </div>
            </div>

            <div class="mega-menu-right-panel" :class="{ active: activePanel === 'mtb' }">
              <div class="row">
                <div class="col-md-7">
                  <h6 class="fw-bold text-dark mb-3">Hãng Máy tính bảng</h6>
                  <div class="mega-menu-brand-grid">
                    <router-link to="/store/products?category=Máy tính bảng&brand=Apple" class="mega-menu-brand-badge">iPad (Apple)</router-link>
                    <router-link to="/store/products?category=Máy tính bảng&brand=Samsung" class="mega-menu-brand-badge">Samsung Tab</router-link>
                  </div>
                </div>
                <div class="col-md-5 border-start border-light ps-3">
                  <h6 class="fw-bold text-dark mb-3"><i class="fa-solid fa-fire text-danger me-1"></i>Bán chạy nhất</h6>
                  <div class="p-2.5 rounded bg-light border border-light text-center">
                    <img src="https://images.unsplash.com/photo-1589739900243-4b52cd9b104e?q=80&w=150" class="rounded border mb-2" style="width: 60px; height: 60px; object-fit: cover;">
                    <div class="fw-bold text-dark text-xxs">Galaxy Tab S9 Ultra</div>
                    <div class="text-danger fw-bold text-xs mt-1">21.990.000 đ</div>
                  </div>
                </div>
              </div>
            </div>

            <div class="mega-menu-right-panel" :class="{ active: activePanel === 'tn' }">
              <div class="row">
                <div class="col-md-7">
                  <h6 class="fw-bold text-dark mb-3">Thương hiệu Tai nghe</h6>
                  <div class="mega-menu-brand-grid">
                    <router-link to="/store/products?category=Tai nghe&brand=Sony" class="mega-menu-brand-badge">Sony</router-link>
                    <router-link to="/store/products?category=Tai nghe&brand=Apple" class="mega-menu-brand-badge">Apple</router-link>
                  </div>
                </div>
                <div class="col-md-5 border-start border-light ps-3">
                  <h6 class="fw-bold text-dark mb-3"><i class="fa-solid fa-fire text-danger me-1"></i>Bán chạy nhất</h6>
                  <div class="p-2.5 rounded bg-light border border-light text-center">
                    <img src="https://images.unsplash.com/photo-1505740420928-5e560c06d30e?q=80&w=150" class="rounded border mb-2" style="width: 60px; height: 60px; object-fit: cover;">
                    <div class="fw-bold text-dark text-xxs">Sony WH-1000XM5</div>
                    <div class="text-danger fw-bold text-xs mt-1">6.990.000 đ</div>
                  </div>
                </div>
              </div>
            </div>

            <div class="mega-menu-right-panel" :class="{ active: activePanel === 'pk' }">
              <div class="row">
                <div class="col-md-7">
                  <h6 class="fw-bold text-dark mb-3">Phụ kiện HOT</h6>
                  <div class="mega-menu-brand-grid">
                    <router-link to="/store/products?category=Phụ kiện" class="mega-menu-brand-badge">Bàn phím</router-link>
                    <router-link to="/store/products?category=Phụ kiện" class="mega-menu-brand-badge">Chuột</router-link>
                  </div>
                </div>
                <div class="col-md-5 border-start border-light ps-3">
                  <h6 class="fw-bold text-dark mb-3"><i class="fa-solid fa-fire text-danger me-1"></i>Bán chạy nhất</h6>
                  <div class="p-2.5 rounded bg-light border border-light text-center">
                    <img src="https://images.unsplash.com/photo-1505740420928-5e560c06d30e?q=80&w=150" class="rounded border mb-2" style="width: 60px; height: 60px; object-fit: cover;">
                    <div class="fw-bold text-dark text-xxs">Sony WH-1000XM5</div>
                    <div class="text-danger fw-bold text-xs mt-1">6.990.000 đ</div>
                  </div>
                </div>
              </div>
            </div>

            <div class="mega-menu-right-panel" :class="{ active: activePanel === 'loa' }">
              <div class="row">
                <div class="col-md-7">
                  <h6 class="fw-bold text-dark mb-3">Hãng Loa di động</h6>
                  <div class="mega-menu-brand-grid">
                    <router-link to="/store/products?category=Loa&brand=Sony" class="mega-menu-brand-badge">Sony</router-link>
                  </div>
                </div>
                <div class="col-md-5 border-start border-light ps-3">
                  <h6 class="fw-bold text-dark mb-3"><i class="fa-solid fa-fire text-danger me-1"></i>Bán chạy nhất</h6>
                  <div class="p-2.5 rounded bg-light border border-light text-center">
                    <img src="https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?q=80&w=150" class="rounded border mb-2" style="width: 60px; height: 60px; object-fit: cover;">
                    <div class="fw-bold text-dark text-xxs">Sony SRS-XE200</div>
                    <div class="text-danger fw-bold text-xs mt-1">2.490.000 đ</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <button class="navbar-toggler border-0 shadow-none" type="button" data-bs-toggle="collapse" data-bs-target="#onlineNav">
          <span class="navbar-toggler-icon"></span>
        </button>

        <div class="collapse navbar-collapse" id="onlineNav">
          <ul class="navbar-nav me-auto mb-2 mb-lg-0 gap-2 ms-lg-3">
            <li class="nav-item">
              <router-link class="nav-link" to="/store" active-class="active" exact-active-class="active">Trang chủ</router-link>
            </li>
            <li class="nav-item">
              <router-link class="nav-link" to="/store/products" active-class="active">Sản phẩm</router-link>
            </li>
            <li class="nav-item">
              <a class="nav-link" href="#">Chính sách</a>
            </li>
            <li class="nav-item">
              <a class="nav-link" href="#">Hỗ trợ kỹ thuật</a>
            </li>
          </ul>
          
          <div class="d-flex align-items-center gap-3">
            <!-- Cart Icon -->
            <router-link to="/store/cart" class="btn btn-icon text-dark position-relative border-0 bg-transparent">
              <i class="fa-solid fa-cart-shopping fs-5 text-secondary"></i>
              <span class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger text-white" style="font-size: 0.65rem; padding: 0.25em 0.5em;">
                {{ cartCount }}
              </span>
            </router-link>
            
            <!-- Auth Link -->
            <div v-if="isLogged" class="dropdown">
              <button class="btn btn-outline-info rounded-pill px-4 dropdown-toggle text-dark" type="button" data-bs-toggle="dropdown">
                <i class="fa-solid fa-circle-user me-2 text-cyan"></i>{{ user.fullName }}
              </button>
              <ul class="dropdown-menu dropdown-menu-end bg-white border border-light">
                <li>
                  <router-link class="dropdown-item py-2" to="/portal">
                    <i class="fa-solid fa-network-wired me-2 text-cyan"></i>Cổng Portal / ERP
                  </router-link>
                </li>
                <li><hr class="dropdown-divider border-secondary"></li>
                <li>
                  <a class="dropdown-item py-2 text-danger" href="#" @click.prevent="logout">
                    <i class="fa-solid fa-right-from-bracket me-2"></i>Đăng xuất
                  </a>
                </li>
              </ul>
            </div>
            <button v-else type="button" class="btn btn-info rounded-pill px-4 text-white fw-bold" data-bs-toggle="modal" data-bs-target="#loginChoiceModal">
              <i class="fa-solid fa-user me-2"></i>Đăng nhập
            </button>
          </div>
        </div>
      </div>
    </nav>

    <!-- Main Content -->
    <main style="min-height: 70vh;" class="animate-fade-in flex-grow-1">
      <router-view />
    </main>

    <!-- Footer -->
    <footer class="footer py-5 mt-5">
      <div class="container">
        <div class="row g-4">
          <div class="col-md-4">
            <h5 class="text-dark fw-bold mb-3"><i class="fa-solid fa-bolt-lightning text-cyan me-2"></i>NovaTech Store</h5>
            <p class="text-xs text-muted-custom">Hệ thống bán lẻ thiết bị công nghệ hiện đại hàng đầu Việt Nam.</p>
          </div>
          <div class="col-md-4">
            <h5 class="text-dark fw-bold mb-3">Thông tin liên hệ</h5>
            <p class="text-xs mb-1 text-muted-custom"><i class="fa-solid fa-phone me-2 text-info"></i>Hotline: 1900 8198</p>
            <p class="text-xs mb-1 text-muted-custom"><i class="fa-solid fa-envelope me-2 text-info"></i>Email: contact@novatech.vn</p>
          </div>
          <div class="col-md-4">
            <h5 class="text-dark fw-bold mb-3">Chi nhánh</h5>
            <p class="text-xs text-muted-custom"><i class="fa-solid fa-map-location-dot me-2 text-info"></i>123 Đường Ba Tháng Hai, Quận 10, TP.HCM</p>
          </div>
        </div>
        <hr class="border-secondary my-4">
        <div class="d-flex justify-content-between align-items-center text-xs">
          <span>© 2026 NovaTech. All rights reserved.</span>
          <span class="text-muted">Đồ án tốt nghiệp - Xây dựng hệ thống ERP & TikTok Shop</span>
        </div>
      </div>
    </footer>

    <!-- Modal Lựa Chọn Đăng Nhập / Đăng Ký -->
    <div class="modal fade" id="loginChoiceModal" tabindex="-1" aria-labelledby="loginChoiceModalLabel" aria-hidden="true">
      <div class="modal-dialog modal-dialog-centered" style="max-width: 400px;">
        <div class="modal-content border-0 shadow-lg" style="border-radius: 20px; overflow: hidden; background: radial-gradient(circle at center, #0f172a 0%, #020617 100%); border: 1px solid rgba(255, 255, 255, 0.08);">
          <div class="modal-body p-4 text-center text-white position-relative">
            <!-- Close Button -->
            <button type="button" class="custom-close-btn position-absolute top-0 end-0 m-3" data-bs-dismiss="modal" aria-label="Close">
              <span aria-hidden="true">&times;</span>
            </button>
            
            <!-- Logo / Brand Title -->
            <div class="mt-2 mb-1">
              <h3 class="smember-title m-0">
                NovaTech Member
              </h3>
            </div>
            
            <!-- Mascot / Tech Icon -->
            <div class="my-3 text-center">
              <img :src="novatechLogo" alt="NovaTech Logo" class="modal-logo-img">
            </div>
            
            <!-- Text Content -->
            <h6 class="smember-text mb-4 px-2">
              Vui lòng đăng nhập tài khoản NovaTech để xem ưu đãi và thanh toán dễ dàng hơn.
            </h6>
            
            <!-- Option Buttons -->
            <div class="d-flex gap-3 justify-content-center mt-3">
              <button @click="goTo('/register')" class="btn btn-smember-register w-50" data-bs-dismiss="modal">
                Đăng ký
              </button>
              <button @click="goTo('/login')" class="btn btn-smember-login w-50" data-bs-dismiss="modal">
                Đăng nhập
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { authState, authService } from '../services/auth'
import { cartService } from '../services/cart'
import novatechLogo from '../assets/novatech_logo.png'

export default {
  name: 'OnlineLayout',
  setup() {
    const router = useRouter()
    const activePanel = ref('dt')

    const isLogged = computed(() => !!authState.user)
    const user = computed(() => authState.user)

    const cartCount = computed(() => cartService.count.value)

    const logout = async () => {
      await authService.logout()
      router.push('/store')
    }

    const goTo = (path) => {
      // Dismiss Bootstrap modal
      const modalElement = document.getElementById('loginChoiceModal')
      if (modalElement) {
        if (window.bootstrap && window.bootstrap.Modal) {
          const modalInstance = window.bootstrap.Modal.getInstance(modalElement)
          if (modalInstance) {
            modalInstance.hide()
          }
        }
        
        // Robust manual cleanup of modal classes and backdrop
        modalElement.classList.remove('show')
        modalElement.style.display = 'none'
        document.body.classList.remove('modal-open')
        document.body.style.overflow = ''
        document.body.style.paddingRight = ''
        const backdrop = document.querySelector('.modal-backdrop')
        if (backdrop) {
          backdrop.remove()
        }
      }
      router.push(path)
    }

    return {
      activePanel,
      isLogged,
      user,
      cartCount,
      logout,
      novatechLogo,
      goTo
    }
  }
}
</script>

<style scoped>
/* Custom Close Button */
.custom-close-btn {
  background-color: rgba(255, 255, 255, 0.1);
  border: none;
  border-radius: 50%;
  width: 28px;
  height: 28px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: rgba(255, 255, 255, 0.7);
  font-size: 20px;
  font-weight: bold;
  cursor: pointer;
  padding: 0;
  line-height: 1;
  transition: all 0.2s ease;
  z-index: 10;
}

.custom-close-btn:hover {
  background-color: rgba(255, 255, 255, 0.2);
  color: #ffffff;
  transform: scale(1.05);
}

/* Smember Modal Styling */
.smember-title {
  background: linear-gradient(135deg, #38bdf8 0%, #0ea5e9 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  font-weight: 800;
  font-size: 2rem;
  letter-spacing: -0.5px;
  font-family: 'Inter', system-ui, -apple-system, sans-serif;
}

.smember-text {
  font-size: 0.95rem;
  font-weight: 500;
  color: #94a3b8 !important; /* Slate-400 */
  line-height: 1.6;
  font-family: 'Inter', system-ui, -apple-system, sans-serif;
}

.modal-logo-img {
  width: 160px;
  height: 160px;
  object-fit: contain;
  filter: drop-shadow(0 0 12px rgba(56, 189, 248, 0.3));
  transition: transform 0.3s ease;
}

.modal-logo-img:hover {
  transform: scale(1.05) rotate(1deg);
}

.btn-smember-register {
  background-color: rgba(255, 255, 255, 0.03);
  color: #38bdf8;
  border: 1.5px solid rgba(56, 189, 248, 0.4);
  border-radius: 10px;
  padding: 10px 16px;
  font-weight: 700;
  font-size: 1rem;
  transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
  text-decoration: none;
}

.btn-smember-register:hover {
  background-color: rgba(56, 189, 248, 0.1);
  color: #ffffff;
  border-color: #38bdf8;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(56, 189, 248, 0.15);
}

.btn-smember-login {
  background: linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%);
  color: #ffffff;
  border: 1.5px solid transparent;
  border-radius: 10px;
  padding: 10px 16px;
  font-weight: 700;
  font-size: 1rem;
  transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
  text-decoration: none;
  box-shadow: 0 4px 12px rgba(14, 165, 233, 0.25);
}

.btn-smember-login:hover {
  background: linear-gradient(135deg, #38bdf8 0%, #0ea5e9 100%);
  color: #ffffff;
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(14, 165, 233, 0.4);
}

/* Scoped overrides to enforce Mega Menu styles from template */
.bg-gradient-nav {
  background: #ffffff;
  border-bottom: 1px solid #e2e8f0;
}
.logo-brand-nova {
  color: #0c7eb6;
  font-weight: 800;
  font-size: 1.85rem;
  letter-spacing: -0.5px;
  font-family: 'Inter', system-ui, -apple-system, sans-serif;
}
.logo-brand-tech-blue {
  color: #0c7eb6;
  font-weight: 800;
  font-size: 1.85rem;
  margin-left: 2px;
  letter-spacing: -0.5px;
  font-family: 'Inter', system-ui, -apple-system, sans-serif;
}
.footer {
  background-color: #ffffff;
  border-top: 1px solid #e2e8f0;
}
.nav-link {
  color: #475569 !important;
  font-weight: 500;
  transition: color 0.3s;
}
.nav-link:hover, .nav-link.router-link-active {
  color: #0284c7 !important;
}
</style>
