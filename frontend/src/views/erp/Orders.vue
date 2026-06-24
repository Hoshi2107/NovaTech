<template>
  <div class="card card-glass p-4">
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h4 class="fw-bold m-0"><i class="fa-solid fa-file-invoice-dollar text-cyan me-2"></i>Danh sách đơn hàng đa kênh</h4>
    </div>

    <div v-if="loading" class="text-center py-5">
      <div class="spinner-border text-info" role="status"></div>
    </div>

    <div class="table-responsive" v-else>
      <table class="table table-dark table-hover mb-0 text-sm align-middle">
        <thead>
          <tr>
            <th>Mã Đơn</th>
            <th>Khách hàng</th>
            <th>Ngày đặt</th>
            <th>Kênh bán</th>
            <th>Tổng thanh toán</th>
            <th>Trạng thái</th>
            <th class="text-end">Hành động</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="o in orders" :key="o.id">
            <td><code>{{ o.orderCode }}</code></td>
            <td>
              <div class="fw-bold text-dark">{{ o.customerName }}</div>
              <span class="text-xs text-muted">{{ o.customerPhone }}</span>
            </td>
            <td>{{ formatDate(o.orderDate) }}</td>
            <td>
              <i class="fa-solid" :class="getChannelIcon(o.channel)"></i>
              <span class="ms-1.5">{{ o.channel }}</span>
            </td>
            <td class="text-info fw-bold">{{ formatMoney(o.total) }} đ</td>
            <td>
              <span class="badge px-2.5 py-1" :class="getStatusBadge(o.status)">{{ o.status }}</span>
            </td>
            <td class="text-end">
              <router-link :to="'/erp/orders/' + o.id" class="btn btn-sm btn-outline-info rounded-pill px-3 text-xs">
                <i class="fa-solid fa-circle-info me-1"></i>Chi tiết
              </router-link>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script>
import { ref, onMounted } from 'vue'
import axios from 'axios'

export default {
  name: 'Orders',
  setup() {
    const orders = ref([])
    const loading = ref(true)

    const fetchOrders = async () => {
      try {
        loading.value = true
        const response = await axios.get('/api/GetOrders')
        orders.value = response.data
      } catch (err) {
        console.error('Error fetching orders:', err)
      } finally {
        loading.value = false
      }
    }

    const formatMoney = (val) => {
      if (val === undefined || val === null) return 0
      return Number(val).toLocaleString()
    }

    const formatDate = (dateStr) => {
      if (!dateStr) return ''
      const d = new Date(dateStr)
      return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')} - ${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}/${d.getFullYear()}`
    }

    const getChannelIcon = (channel) => {
      if (channel === 'Website') return 'fa-globe text-primary'
      if (channel === 'TikTok Shop') return 'fa-brands fa-tiktok text-dark'
      return 'fa-store text-warning'
    }

    const getStatusBadge = (status) => {
      if (status === 'Đơn mới') return 'bg-info text-dark'
      if (status === 'Đã xác nhận') return 'bg-primary text-white'
      if (status === 'Đang đóng gói') return 'bg-warning text-dark'
      if (status === 'Đang giao') return 'bg-info text-white'
      if (status === 'Hoàn thành') return 'bg-success text-white'
      return 'bg-danger text-white'
    }

    onMounted(() => {
      fetchOrders()
    })

    return {
      orders,
      loading,
      formatMoney,
      formatDate,
      getChannelIcon,
      getStatusBadge
    }
  }
}
</script>
