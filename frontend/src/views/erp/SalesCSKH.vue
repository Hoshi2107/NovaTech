<template>
  <div class="row g-4">
    <div class="col-12">
      <div class="card card-glass p-4 text-dark">
        <div class="d-flex flex-wrap justify-content-between align-items-center gap-3">
          <div>
            <div class="d-flex flex-wrap gap-2 mb-2">
              <span class="badge bg-info text-dark rounded-pill px-3 py-2">Bán hàng và chăm sóc khách hàng</span>
              <span class="badge bg-success rounded-pill px-3 py-2">{{ unreadCount }} tin chưa đọc</span>
            </div>
            <h4 class="fw-bold mb-1">Trả lời khách hàng trong một nơi</h4>
            <div class="text-sm text-muted-custom">Khách nhắn vào sẽ hiện ở đây để nhân viên phản hồi nhanh, gắn trạng thái và theo dõi cuộc hội thoại.</div>
          </div>
          <div class="d-flex flex-wrap gap-2">
            <button class="btn btn-outline-info rounded-pill px-3" @click="reloadInbox">
              <i class="fa-solid fa-rotate me-2"></i>Làm mới
            </button>
            <button class="btn btn-info text-white rounded-pill px-3" @click="markSelectedRead" :disabled="!selectedThread">
              Đánh dấu đã đọc
            </button>
          </div>
        </div>
      </div>
    </div>

    <div class="col-lg-4">
      <div class="card card-glass p-4 h-100">
        <div class="d-flex justify-content-between align-items-center mb-3">
          <h5 class="fw-bold m-0 text-cyan"><i class="fa-solid fa-inbox me-2"></i>Hội thoại</h5>
          <span class="badge bg-light text-dark rounded-pill">{{ threads.length }} khách</span>
        </div>

        <div class="mb-3">
          <input v-model="searchText" type="text" class="form-control form-control-sm" placeholder="Tìm khách, số điện thoại, tiêu đề..." />
        </div>

        <div class="overflow-y-auto" style="max-height: 70vh;">
          <button
            v-for="thread in filteredThreads"
            :key="thread.id"
            class="w-100 text-start border-0 bg-transparent p-0 mb-2"
            @click="openThread(thread)"
          >
            <div class="p-3 rounded-4 border" :class="selectedThread?.id === thread.id ? 'border-info bg-info bg-opacity-10' : 'border-light bg-white bg-opacity-50'">
              <div class="d-flex justify-content-between gap-2">
                <div>
                  <div class="fw-semibold text-dark">{{ thread.customerName }}</div>
                  <div class="text-xs text-muted">{{ thread.subject }}</div>
                </div>
                <div class="text-end">
                  <span v-if="thread.unreadCount > 0" class="badge bg-danger rounded-pill">{{ thread.unreadCount }}</span>
                  <div class="text-xxs text-muted mt-1">{{ formatShortDate(thread.updatedAt) }}</div>
                </div>
              </div>
              <div class="d-flex flex-wrap gap-2 mt-2">
                <span class="badge bg-light text-dark rounded-pill">{{ thread.channel }}</span>
                <span class="badge rounded-pill" :class="getStatusClass(thread.status)">{{ thread.status }}</span>
                <span class="badge bg-warning text-dark rounded-pill">{{ thread.priority }}</span>
              </div>
              <div class="text-xs text-muted-custom mt-2 text-truncate">{{ thread.lastMessage }}</div>
            </div>
          </button>
        </div>
      </div>
    </div>

    <div class="col-lg-8">
      <div class="card card-glass p-4 h-100 text-dark">
        <template v-if="selectedThread">
          <div class="d-flex flex-wrap justify-content-between align-items-start gap-3 mb-3 border-bottom border-light pb-3">
            <div>
              <h5 class="fw-bold mb-1 text-cyan">{{ selectedThread.customerName }}</h5>
              <div class="text-sm text-muted-custom">
                {{ selectedThread.customerPhone }} | {{ selectedThread.channel }} | {{ selectedThread.subject }}
              </div>
            </div>
            <div class="text-end">
              <span class="badge rounded-pill px-3 py-2" :class="getStatusClass(selectedThread.status)">{{ selectedThread.status }}</span>
              <div class="text-xxs text-muted mt-2">Cập nhật: {{ formatDate(selectedThread.updatedAt) }}</div>
            </div>
          </div>

          <div class="message-panel overflow-y-auto pe-2 mb-3" ref="messagePanel">
            <div v-for="message in selectedThread.messages" :key="message.id" class="mb-3">
              <div v-if="message.sender === 'customer'" class="d-flex justify-content-start">
                <div class="bubble bubble-customer">
                  <div class="text-xxs fw-semibold mb-1">Khách hàng</div>
                  <div>{{ message.text }}</div>
                  <div class="text-xxs text-muted mt-2">{{ formatTime(message.timestamp) }}</div>
                </div>
              </div>
              <div v-else class="d-flex justify-content-end">
                <div class="bubble bubble-staff">
                  <div class="text-xxs fw-semibold mb-1">Nhân viên CSKH</div>
                  <div>{{ message.text }}</div>
                  <div class="text-xxs text-muted mt-2">{{ formatTime(message.timestamp) }}</div>
                </div>
              </div>
            </div>
          </div>

          <div class="mb-3">
            <label class="form-label text-xs fw-semibold">Trạng thái xử lý</label>
            <select v-model="replyForm.status" class="form-select form-select-sm">
              <option>Chưa trả lời</option>
              <option>Đang xử lý</option>
              <option>Đã trả lời</option>
            </select>
          </div>

          <div class="mb-3">
            <label class="form-label text-xs fw-semibold">Mẫu trả lời nhanh</label>
            <div class="d-flex flex-wrap gap-2">
              <button class="btn btn-sm btn-outline-info rounded-pill px-3" @click="fillQuickReply('greeting')">Chào khách</button>
              <button class="btn btn-sm btn-outline-success rounded-pill px-3" @click="fillQuickReply('policy')">Chính sách</button>
              <button class="btn btn-sm btn-outline-warning rounded-pill px-3" @click="fillQuickReply('handover')">Chuyển quản lý</button>
            </div>
          </div>

          <div class="mb-3">
            <label class="form-label text-xs fw-semibold">Nội dung phản hồi</label>
            <textarea
              v-model="replyForm.message"
              class="form-control"
              rows="4"
              placeholder="Nhập nội dung trả lời khách hàng..."
            ></textarea>
          </div>

          <div class="d-flex flex-wrap justify-content-between align-items-center gap-3">
            <div class="text-xs text-muted-custom">Gợi ý: giữ câu trả lời ngắn, rõ và chốt thời gian phản hồi tiếp theo nếu cần.</div>
            <button class="btn btn-info text-white rounded-pill px-4" :disabled="sending || !replyForm.message.trim()" @click="sendReply">
              {{ sending ? 'Đang gửi...' : 'Gửi phản hồi' }}
            </button>
          </div>
        </template>

        <div v-else class="h-100 d-flex align-items-center justify-content-center text-center text-muted">
          <div>
            <i class="fa-solid fa-comments fs-1 mb-3 text-cyan"></i>
            <div class="fw-semibold">Chọn một hội thoại để bắt đầu trả lời</div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { computed, nextTick, onMounted, ref } from 'vue'
import axios from 'axios'

export default {
  name: 'SalesCSKH',
  setup() {
    const threads = ref([])
    const selectedThread = ref(null)
    const searchText = ref('')
    const sending = ref(false)
    const messagePanel = ref(null)

    const replyForm = ref({
      message: '',
      status: 'Đang xử lý'
    })

    const loadThreads = async () => {
      const response = await axios.get('/api/GetCustomerInbox')
      threads.value = response.data || []
      if (!selectedThread.value && threads.value.length > 0) {
        selectedThread.value = threads.value[0]
      }
    }

    const reloadInbox = async () => {
      await loadThreads()
    }

    const filteredThreads = computed(() => {
      const keyword = searchText.value.trim().toLowerCase()
      if (!keyword) return threads.value
      return threads.value.filter(thread => {
        return [thread.customerName, thread.customerPhone, thread.subject, thread.channel]
          .join(' ')
          .toLowerCase()
          .includes(keyword)
      })
    })

    const unreadCount = computed(() => threads.value.reduce((sum, thread) => sum + (thread.unreadCount || 0), 0))

    const openThread = async (thread) => {
      selectedThread.value = thread
      replyForm.value.status = thread.status === 'Chưa trả lời' ? 'Đang xử lý' : thread.status
      replyForm.value.message = ''
      await nextTick()
      scrollMessagesToBottom()
    }

    const markSelectedRead = async () => {
      if (!selectedThread.value) return
      await axios.post('/api/MarkCustomerThreadRead', { threadId: selectedThread.value.id })
      await reloadInbox()
      const refreshed = threads.value.find(thread => thread.id === selectedThread.value.id)
      if (refreshed) selectedThread.value = refreshed
    }

    const sendReply = async () => {
      if (!selectedThread.value || !replyForm.value.message.trim()) return
      sending.value = true
      try {
        await axios.post('/api/ReplyCustomerMessage', {
          threadId: selectedThread.value.id,
          message: replyForm.value.message,
          status: replyForm.value.status
        })
        await reloadInbox()
        const refreshed = threads.value.find(thread => thread.id === selectedThread.value.id)
        if (refreshed) {
          selectedThread.value = refreshed
        }
        replyForm.value.message = ''
        replyForm.value.status = 'Đã trả lời'
        await nextTick()
        scrollMessagesToBottom()
      } finally {
        sending.value = false
      }
    }

    const fillQuickReply = (type) => {
      if (type === 'greeting') {
        replyForm.value.message = 'Chào anh/chị, em đã nhận được tin nhắn. Em kiểm tra và phản hồi ngay cho mình ạ.'
        replyForm.value.status = 'Đang xử lý'
      } else if (type === 'policy') {
        replyForm.value.message = 'Dạ chính sách bên em là... Em xin phép xác nhận thêm thông tin để tư vấn đúng nhất cho mình ạ.'
        replyForm.value.status = 'Đang xử lý'
      } else if (type === 'handover') {
        replyForm.value.message = 'Dạ em đã chuyển nội dung này cho quản lý phụ trách. Anh/chị chờ em phản hồi trong thời gian sớm nhất ạ.'
        replyForm.value.status = 'Đang xử lý'
      }
    }

    const getStatusClass = (status) => {
      if (status === 'Đã trả lời') return 'bg-success text-white'
      if (status === 'Đang xử lý') return 'bg-warning text-dark'
      return 'bg-secondary text-white'
    }

    const formatDate = (dateStr) => {
      if (!dateStr) return ''
      const date = new Date(dateStr)
      return `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')} - ${String(date.getDate()).padStart(2, '0')}/${String(date.getMonth() + 1).padStart(2, '0')}/${date.getFullYear()}`
    }

    const formatShortDate = (dateStr) => {
      if (!dateStr) return ''
      const date = new Date(dateStr)
      return `${String(date.getDate()).padStart(2, '0')}/${String(date.getMonth() + 1).padStart(2, '0')}`
    }

    const formatTime = (dateStr) => {
      if (!dateStr) return ''
      const date = new Date(dateStr)
      return `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`
    }

    const scrollMessagesToBottom = () => {
      nextTick(() => {
        if (messagePanel.value) {
          messagePanel.value.scrollTop = messagePanel.value.scrollHeight
        }
      })
    }

    onMounted(async () => {
      await loadThreads()
      await nextTick()
      scrollMessagesToBottom()
    })

    return {
      threads,
      selectedThread,
      searchText,
      sending,
      replyForm,
      messagePanel,
      unreadCount,
      filteredThreads,
      reloadInbox,
      openThread,
      markSelectedRead,
      sendReply,
      fillQuickReply,
      getStatusClass,
      formatDate,
      formatShortDate,
      formatTime
    }
  }
}
</script>

<style scoped>
.message-panel {
  max-height: 52vh;
}

.bubble {
  max-width: 85%;
  padding: 14px 16px;
  border-radius: 18px;
  font-size: 0.9rem;
  line-height: 1.5;
}

.bubble-customer {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  color: #0f172a;
}

.bubble-staff {
  background: rgba(14, 165, 233, 0.1);
  border: 1px solid rgba(14, 165, 233, 0.2);
  color: #0f172a;
}
</style>
