<template>
  <div>
    <!-- Stat Cards -->
    <div class="row g-3 mb-4" v-if="data">
      <div class="col-md-3">
        <div class="card card-glass p-3 text-white">
          <div class="d-flex justify-content-between align-items-center mb-2">
            <span class="text-xs text-muted-custom text-uppercase">Doanh Thu Hoàn Thành</span>
            <span class="badge bg-success bg-opacity-25 text-success rounded-pill px-2.5 py-1 text-xs">
              <i class="fa-solid fa-arrow-trend-up"></i> +12%
            </span>
          </div>
          <h3 class="fw-bold mb-1 text-cyan">{{ formatMoney(data.totalRevenue) }} đ</h3>
          <span class="text-xxs text-muted">Hôm nay: 18,500,000 đ</span>
        </div>
      </div>
      <div class="col-md-3">
        <div class="card card-glass p-3 text-white">
          <div class="d-flex justify-content-between align-items-center mb-2">
            <span class="text-xs text-muted-custom text-uppercase">Tổng số đơn hàng</span>
            <span class="badge bg-info bg-opacity-25 text-info rounded-pill px-2.5 py-1 text-xs">Ổn định</span>
          </div>
          <h3 class="fw-bold mb-1 text-dark">{{ data.totalOrders }} đơn</h3>
          <span class="text-xxs text-muted">Đơn chờ xử lý: {{ data.pendingOrders }} đơn</span>
        </div>
      </div>
      <div class="col-md-3">
        <div class="card card-glass p-3 text-white">
          <div class="d-flex justify-content-between align-items-center mb-2">
            <span class="text-xs text-muted-custom text-uppercase">Sản phẩm quản lý</span>
            <span class="badge bg-warning bg-opacity-25 text-warning rounded-pill px-2.5 py-1 text-xs">Cần kiểm kho</span>
          </div>
          <h3 class="fw-bold mb-1 text-dark">{{ data.totalProducts }} dòng</h3>
          <span class="text-xxs text-muted">Sắp hết hàng: {{ data.lowStockProducts.length }} sản phẩm</span>
        </div>
      </div>
      <div class="col-md-3">
        <div class="card card-glass p-3 text-white">
          <div class="d-flex justify-content-between align-items-center mb-2">
            <span class="text-xs text-muted-custom text-uppercase">Khách hàng thành viên</span>
            <span class="badge bg-primary bg-opacity-25 text-primary rounded-pill px-2.5 py-1 text-xs">
              <i class="fa-solid fa-user-plus"></i> +3 mới
            </span>
          </div>
          <h3 class="fw-bold mb-1 text-neon">{{ data.customers.length }} TV</h3>
          <span class="text-xxs text-muted">Lượt mua trung bình: 1.5 lần</span>
        </div>
      </div>
    </div>

    <!-- Charts Section -->
    <div class="row g-4 mb-4" v-if="data">
      <div class="col-md-8">
        <div class="card card-glass p-4 h-100">
          <h5 class="fw-bold mb-3"><i class="fa-solid fa-chart-area text-cyan me-2"></i>Biểu đồ Doanh Thu & Đơn Hàng (7 ngày qua)</h5>
          <canvas ref="chartCanvas" style="max-height: 280px;"></canvas>
        </div>
      </div>
      <div class="col-md-4">
        <div class="card card-glass p-4 h-100">
          <h5 class="fw-bold mb-3"><i class="fa-solid fa-triangle-exclamation text-warning me-2"></i>Cảnh báo Kho & Hệ thống</h5>
          
          <div class="d-flex flex-column gap-2.5">
            <!-- Low stock alert -->
            <div v-for="p in data.lowStockProducts" :key="p.id" class="p-2.5 rounded-3 bg-danger bg-opacity-10 border border-danger border-opacity-25 d-flex justify-content-between align-items-center text-dark">
              <div>
                <span class="text-xs fw-bold text-danger d-block">Sắp hết hàng</span>
                <span class="text-xs text-secondary">{{ p.name }}</span>
              </div>
              <span class="badge bg-danger">Còn {{ p.stock }} chiếc</span>
            </div>
            
            <!-- Recent notifications -->
            <div v-for="n in data.recentNotifications" :key="n.id" class="p-2.5 rounded-3 bg-secondary bg-opacity-10 border border-secondary border-opacity-25 text-dark">
              <div class="d-flex justify-content-between text-xs mb-1">
                <strong class="text-info">{{ n.title }}</strong>
                <span class="text-xxs text-muted">{{ formatTime(n.timestamp) }}</span>
              </div>
              <p class="text-xs text-muted-custom mb-0">{{ n.message }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Top lists tables -->
    <div class="row g-4" v-if="data">
      <div class="col-md-6">
        <div class="card card-glass p-4">
          <h5 class="fw-bold mb-3"><i class="fa-solid fa-crown text-warning me-2"></i>Sản phẩm nổi bật</h5>
          <div class="table-responsive">
            <table class="table table-dark table-hover mb-0 text-xs align-middle">
              <thead>
                <tr>
                  <th>Hình</th>
                  <th>Tên Sản Phẩm</th>
                  <th>SKU</th>
                  <th>Giá Bán</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="p in data.topProducts" :key="p.id">
                  <td><img :src="p.image" class="rounded-3" style="width: 32px; height: 32px; object-fit: cover;"></td>
                  <td class="fw-bold text-dark">{{ p.name }}</td>
                  <td><code>{{ p.sku }}</code></td>
                  <td class="text-info fw-bold">{{ formatMoney(p.price) }} đ</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
      
      <div class="col-md-6">
        <div class="card card-glass p-4">
          <h5 class="fw-bold mb-3"><i class="fa-solid fa-users text-info me-2"></i>Khách hàng mua nhiều</h5>
          <div class="table-responsive">
            <table class="table table-dark table-hover mb-0 text-xs align-middle">
              <thead>
                <tr>
                  <th>Tên Khách Hàng</th>
                  <th>SĐT</th>
                  <th>Tích Điểm</th>
                  <th>Hạng</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="c in data.customers" :key="c.id">
                  <td class="fw-bold text-dark">{{ c.name }}</td>
                  <td>{{ c.phone }}</td>
                  <td>{{ c.points }} điểm</td>
                  <td><span class="badge bg-gradient-cyan text-dark rounded-pill">{{ c.membershipRank }}</span></td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, onMounted } from 'vue'
import axios from 'axios'
import { Chart, registerables } from 'chart.js'
Chart.register(...registerables)

export default {
  name: 'Dashboard',
  setup() {
    const data = ref(null)
    const chartCanvas = ref(null)
    let chartInstance = null

    const fetchData = async () => {
      try {
        const response = await axios.get('/api/GetDashboard')
        data.value = response.data
        
        // Render chart on next tick
        setTimeout(renderChart, 100)
      } catch (err) {
        console.error('Error fetching dashboard:', err)
      }
    }

    const renderChart = () => {
      if (!chartCanvas.value) return
      
      if (chartInstance) {
        chartInstance.destroy()
      }

      const ctx = chartCanvas.value.getContext('2d')
      chartInstance = new Chart(ctx, {
        type: 'line',
        data: {
          labels: ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'Chủ nhật'],
          datasets: [{
            label: 'Doanh thu (triệu đ)',
            data: [12, 19, 15, 25, 22, 30, 45],
            borderColor: '#0284c7',
            backgroundColor: 'rgba(2, 132, 199, 0.1)',
            borderWidth: 3,
            fill: true,
            tension: 0.4
          }, {
            label: 'Đơn hàng (chiếc)',
            data: [5, 12, 8, 15, 10, 18, 24],
            borderColor: '#f59e0b',
            backgroundColor: 'transparent',
            borderWidth: 2,
            tension: 0.3
          }]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: {
              labels: {
                color: '#64748b'
              }
            }
          },
          scales: {
            x: {
              grid: { color: 'rgba(0, 0, 0, 0.05)' },
              ticks: { color: '#64748b' }
            },
            y: {
              grid: { color: 'rgba(0, 0, 0, 0.05)' },
              ticks: { color: '#64748b' }
            }
          }
        }
      })
    }

    const formatMoney = (val) => {
      if (val === undefined || val === null) return 0
      return Number(val).toLocaleString()
    }

    const formatTime = (dateStr) => {
      if (!dateStr) return ''
      const d = new Date(dateStr)
      return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
    }

    onMounted(() => {
      fetchData()
    })

    return {
      data,
      chartCanvas,
      formatMoney,
      formatTime
    }
  }
}
</script>
