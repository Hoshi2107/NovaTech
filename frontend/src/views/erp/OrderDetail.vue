<template>
  <div class="row g-4" v-if="order">
    <!-- Workflow Progression bar -->
    <div class="col-12">
      <div class="card card-glass p-3 mb-2 text-dark">
        <div class="d-flex justify-content-between align-items-center flex-wrap gap-2">
          <span class="text-sm">Trình tự xử lý đơn hàng:</span>
          <div class="d-flex gap-2 align-items-center">
            <template v-for="(s, index) in steps" :key="s">
              <span class="btn btn-sm py-1 rounded-pill text-xs disabled" 
                    :class="order.status === s ? 'btn-info text-dark fw-bold' : 'btn-outline-secondary'">
                {{ s }}
              </span>
              <span v-if="index < steps.length - 1" class="text-muted text-xs mx-1">
                <i class="fa-solid fa-chevron-right"></i>
              </span>
            </template>
          </div>
        </div>
      </div>
    </div>

    <!-- Left Details Column -->
    <div class="col-md-8">
      <div class="card card-glass p-4 mb-4 text-dark">
        <h5 class="fw-bold mb-3 text-cyan">Danh sách sản phẩm mua</h5>
        <div class="table-responsive">
          <table class="table table-dark table-borderless text-sm align-middle">
            <thead>
              <tr class="border-bottom border-secondary border-opacity-25">
                <th>Sản phẩm</th>
                <th>Mã SKU</th>
                <th>Giá Bán</th>
                <th>Số Lượng</th>
                <th class="text-end">Tổng tiền</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in order.items" :key="item.productId">
                <td>
                  <div class="d-flex align-items-center gap-3">
                    <img :src="item.image" class="rounded-3" style="width: 40px; height: 40px; object-fit: cover;">
                    <span class="fw-bold text-dark">{{ item.productName }}</span>
                  </div>
                </td>
                <td><code>{{ item.sku }}</code></td>
                <td>{{ formatMoney(item.price) }} đ</td>
                <td>x {{ item.quantity }}</td>
                <td class="text-end text-info fw-bold">{{ formatMoney(item.total) }} đ</td>
              </tr>
            </tbody>
          </table>
        </div>

        <hr class="border-secondary my-4">
        <div class="d-flex justify-content-end">
          <div style="width: 300px;">
            <div class="d-flex justify-content-between mb-2 text-xs">
              <span class="text-muted-custom">Tạm tính:</span>
              <span>{{ formatMoney(order.subTotal) }} đ</span>
            </div>
            <div class="d-flex justify-content-between mb-2 text-xs">
              <span class="text-muted-custom">Chiết khấu / Giảm giá:</span>
              <span>- {{ formatMoney(order.discount) }} đ</span>
            </div>
            <div class="d-flex justify-content-between text-sm fw-bold">
              <span>Tổng tiền thanh toán:</span>
              <span class="text-info">{{ formatMoney(order.total) }} đ</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Right Operations Column -->
    <div class="col-md-4">
      <div class="card card-glass p-4 mb-4 text-dark">
        <h5 class="fw-bold mb-3 text-cyan">Thông tin giao nhận</h5>
        <table class="table table-dark table-borderless text-xs mb-0">
          <tbody>
            <tr><td class="text-muted w-35">Người nhận:</td><td class="fw-bold">{{ order.customerName }}</td></tr>
            <tr><td class="text-muted">Điện thoại:</td><td>{{ order.customerPhone }}</td></tr>
            <tr><td class="text-muted">Địa chỉ nhận:</td><td>{{ order.customerAddress }}</td></tr>
            <tr><td class="text-muted">Kênh mua:</td><td>{{ order.channel }}</td></tr>
            <tr><td class="text-muted">Thanh toán:</td><td>{{ order.paymentMethod }}</td></tr>
            <tr><td class="text-muted">Ghi chú:</td><td>{{ order.note || 'Không' }}</td></tr>
          </tbody>
        </table>
      </div>

      <div class="card card-glass p-4 text-dark" v-if="hasPerm('Approve_Order')">
        <h5 class="fw-bold mb-3 text-cyan">Cập nhật trạng thái đơn</h5>
        
        <div class="d-grid gap-2">
          <button v-if="order.status === 'Đơn mới'" @click="updateStatus('Đã xác nhận')" class="btn btn-primary rounded-pill py-2.5 text-xs">
            Xác nhận đơn hàng
          </button>
          
          <button v-else-if="order.status === 'Đã xác nhận'" @click="updateStatus('Đang đóng gói')" class="btn btn-warning text-dark fw-bold rounded-pill py-2.5 text-xs">
            Bắt đầu đóng gói
          </button>
          
          <button v-else-if="order.status === 'Đang đóng gói'" @click="updateStatus('Đang giao')" class="btn btn-info rounded-pill py-2.5 text-xs text-white">
            Giao cho shipper
          </button>
          
          <button v-else-if="order.status === 'Đang giao'" @click="updateStatus('Hoàn thành')" class="btn btn-success rounded-pill py-2.5 text-xs">
            Xác nhận hoàn thành
          </button>

          <button v-if="order.status !== 'Hoàn thành' && order.status !== 'Đã hủy'" @click="updateStatus('Đã hủy')" class="btn btn-outline-danger rounded-pill py-2 text-xs">
            Hủy đơn hàng
          </button>
          
          <div v-else class="alert alert-secondary text-center text-xs py-2 mb-0">
            Đơn hàng đã đóng khóa.
          </div>
        </div>
      </div>
    </div>
  </div>
  
  <div v-else class="text-center py-5">
    <div class="spinner-border text-info" role="status"></div>
  </div>
</template>

<script>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import axios from 'axios'
import { authService } from '../../services/auth'

export default {
  name: 'OrderDetail',
  setup() {
    const route = useRoute()
    const order = ref(null)
    const steps = ['Đơn mới', 'Đã xác nhận', 'Đang đóng gói', 'Đang giao', 'Hoàn thành']

    const hasPerm = (p) => authService.hasPermission(p)

    const fetchOrderDetail = async () => {
      try {
        const id = route.params.id
        const response = await axios.get(`/api/GetOrderDetail?id=${id}`)
        order.value = response.data
      } catch (err) {
        console.error('Error fetching order detail:', err)
      }
    }

    const updateStatus = async (status) => {
      if (status === 'Đã hủy' && !confirm('Bạn thực sự muốn hủy đơn này?')) return
      try {
        await axios.post('/api/UpdateOrderStatus', {
          id: order.value.id,
          status
        })
        fetchOrderDetail()
      } catch (err) {
        console.error('Error updating order status:', err)
      }
    }

    const formatMoney = (val) => {
      if (val === undefined || val === null) return 0
      return Number(val).toLocaleString()
    }

    onMounted(() => {
      fetchOrderDetail()
    })

    return {
      order,
      steps,
      hasPerm,
      updateStatus,
      formatMoney
    }
  }
}
</script>
