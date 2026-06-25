<template>
  <div class="customer-chat">
    <button class="chat-toggle" @click="toggleChat">
      <span>💬</span>
      <span>Chat với NovaTech</span>
      <b v-if="unreadStaffCount > 0">{{ unreadStaffCount }}</b>
    </button>

    <div v-if="isOpen" class="chat-box">
      <div class="chat-header">
        <div>
          <h3>NovaTech Support</h3>
          <p>Thường phản hồi trong vài phút</p>
        </div>
        <button @click="isOpen = false">×</button>
      </div>

      <div ref="messagePanel" class="chat-messages">
        <div v-if="!thread" class="chat-empty">
          Chào bạn 👋 Hãy gửi câu hỏi, nhân viên NovaTech sẽ trả lời tại đây.
        </div>

        <template v-else>
          <div
            v-for="message in thread.messages"
            :key="message.id"
            class="chat-message"
            :class="message.sender === 'customer' ? 'from-customer' : 'from-staff'"
          >
            <div class="bubble">
              <div class="sender">
                {{ message.sender === 'customer' ? 'Bạn' : 'NovaTech' }}
              </div>
              <div>{{ message.text }}</div>
              <small>{{ formatTime(message.timestamp) }}</small>
            </div>
          </div>
        </template>
      </div>

      <div v-if="!thread" class="chat-form">
        <input
          v-model="form.customerName"
          placeholder="Tên của bạn"
        />

        <input
          v-model="form.customerPhone"
          placeholder="Số điện thoại"
        />

        <input
          v-model="form.subject"
          placeholder="Tiêu đề"
        />

        <textarea
          v-model="form.message"
          placeholder="Nhập câu hỏi của bạn..."
          @keydown.enter.exact.prevent="sendMessage"
        ></textarea>

        <button :disabled="sending" @click="sendMessage">
          {{ sending ? 'Đang gửi...' : 'Gửi câu hỏi' }}
        </button>
      </div>

      <div v-else class="chat-reply">
        <textarea
          v-model="replyText"
          placeholder="Nhập tin nhắn..."
          @keydown.enter.exact.prevent="sendMessage"
        ></textarea>

        <button :disabled="sending" @click="sendMessage">
          {{ sending ? 'Đang gửi...' : 'Gửi' }}
        </button>
      </div>

      <div v-if="statusMessage" class="chat-status" :class="{ error: !statusOk }">
        {{ statusMessage }}
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import axios from 'axios'

const STORAGE_KEY = 'novatech_customer_thread_id'

const isOpen = ref(false)
const sending = ref(false)
const statusMessage = ref('')
const statusOk = ref(false)
const thread = ref(null)
const messagePanel = ref(null)
const replyText = ref('')
const lastStaffMessageCount = ref(0)

const threadId = ref(Number(localStorage.getItem(STORAGE_KEY)) || null)

const form = ref({
  customerName: '',
  customerPhone: '',
  subject: 'Hỏi về sản phẩm',
  message: ''
})

let pollingTimer = null

const unreadStaffCount = computed(() => {
  if (!thread.value?.messages) return 0

  const staffMessages = thread.value.messages.filter(item => item.sender === 'staff')
  const diff = staffMessages.length - lastStaffMessageCount.value

  return diff > 0 && !isOpen.value ? diff : 0
})

const formatTime = (value) => {
  if (!value) return ''

  return new Date(value).toLocaleTimeString('vi-VN', {
    hour: '2-digit',
    minute: '2-digit'
  })
}

const scrollToBottom = () => {
  nextTick(() => {
    if (messagePanel.value) {
      messagePanel.value.scrollTop = messagePanel.value.scrollHeight
    }
  })
}

const markStaffMessagesSeen = () => {
  if (!thread.value?.messages) return

  lastStaffMessageCount.value = thread.value.messages.filter(
    item => item.sender === 'staff'
  ).length
}

const toggleChat = () => {
  isOpen.value = !isOpen.value

  if (isOpen.value) {
    markStaffMessagesSeen()
    scrollToBottom()
  }
}

const loadThread = async () => {
  if (!threadId.value) return

  try {
    const response = await axios.get(`/api/GetCustomerThread?id=${threadId.value}`)
    const oldMessageCount = thread.value?.messages?.length || 0

    thread.value = response.data

    const newMessageCount = thread.value?.messages?.length || 0
    if (newMessageCount !== oldMessageCount) {
      scrollToBottom()
    }

    if (isOpen.value) {
      markStaffMessagesSeen()
    }
  } catch (err) {
    thread.value = null
    threadId.value = null
    localStorage.removeItem(STORAGE_KEY)
  }
}

const sendMessage = async () => {
  statusMessage.value = ''

  try {
    sending.value = true

    if (!threadId.value) {
      if (!form.value.customerName.trim() || !form.value.message.trim()) {
        statusOk.value = false
        statusMessage.value = 'Vui lòng nhập tên và nội dung câu hỏi.'
        return
      }

      const response = await axios.post('/api/CreateCustomerInquiry', form.value)
      const newThread = response.data?.thread

      if (newThread?.id) {
        threadId.value = newThread.id
        thread.value = newThread
        localStorage.setItem(STORAGE_KEY, String(newThread.id))

        form.value.message = ''
        statusOk.value = true
        statusMessage.value = 'Đã gửi câu hỏi. NovaTech sẽ trả lời tại đây.'
        scrollToBottom()
      }
    } else {
      if (!replyText.value.trim()) {
        statusOk.value = false
        statusMessage.value = 'Vui lòng nhập nội dung tin nhắn.'
        return
      }

      await axios.post('/api/AddCustomerInquiryMessage', {
        threadId: threadId.value,
        message: replyText.value
      })

      replyText.value = ''
      statusOk.value = true
      statusMessage.value = 'Đã gửi tin nhắn.'

      await loadThread()
      scrollToBottom()
    }
  } catch (err) {
    statusOk.value = false
    statusMessage.value = err.response?.data?.message || 'Gửi tin nhắn thất bại.'
  } finally {
    sending.value = false
  }
}

onMounted(() => {
  loadThread()

  pollingTimer = setInterval(() => {
    loadThread()
  }, 1500)
})

onBeforeUnmount(() => {
  if (pollingTimer) {
    clearInterval(pollingTimer)
  }
})
</script>

<style scoped>
.customer-chat {
  position: fixed;
  right: 24px;
  bottom: 24px;
  z-index: 9999;
  font-family: Arial, sans-serif;
}

.chat-toggle {
  display: flex;
  align-items: center;
  gap: 8px;
  border: none;
  border-radius: 999px;
  background: #2563eb;
  color: white;
  padding: 12px 18px;
  cursor: pointer;
  box-shadow: 0 10px 25px rgba(37, 99, 235, 0.35);
  font-weight: 600;
}

.chat-toggle b {
  min-width: 22px;
  height: 22px;
  border-radius: 999px;
  background: #ef4444;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
}

.chat-box {
  width: 360px;
  height: 540px;
  background: white;
  border-radius: 18px;
  box-shadow: 0 20px 60px rgba(15, 23, 42, 0.25);
  overflow: hidden;
  display: flex;
  flex-direction: column;
  border: 1px solid #e5e7eb;
}

.chat-header {
  background: #2563eb;
  color: white;
  padding: 16px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.chat-header h3 {
  margin: 0;
  font-size: 16px;
}

.chat-header p {
  margin: 4px 0 0;
  font-size: 12px;
  opacity: 0.9;
}

.chat-header button {
  border: none;
  background: transparent;
  color: white;
  font-size: 28px;
  cursor: pointer;
}

.chat-messages {
  flex: 1;
  padding: 14px;
  overflow-y: auto;
  background: #f8fafc;
}

.chat-empty {
  color: #64748b;
  text-align: center;
  padding: 40px 20px;
  font-size: 14px;
}

.chat-message {
  display: flex;
  margin-bottom: 10px;
}

.from-customer {
  justify-content: flex-end;
}

.from-staff {
  justify-content: flex-start;
}

.bubble {
  max-width: 78%;
  padding: 10px 12px;
  border-radius: 14px;
  font-size: 14px;
  line-height: 1.4;
}

.from-customer .bubble {
  background: #2563eb;
  color: white;
  border-bottom-right-radius: 4px;
}

.from-staff .bubble {
  background: white;
  color: #0f172a;
  border: 1px solid #e2e8f0;
  border-bottom-left-radius: 4px;
}

.sender {
  font-size: 11px;
  font-weight: 700;
  opacity: 0.75;
  margin-bottom: 3px;
}

.bubble small {
  display: block;
  margin-top: 4px;
  font-size: 10px;
  opacity: 0.7;
}

.chat-form,
.chat-reply {
  padding: 12px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  border-top: 1px solid #e5e7eb;
}

.chat-form input,
.chat-form textarea,
.chat-reply textarea {
  width: 100%;
  border: 1px solid #cbd5e1;
  border-radius: 10px;
  padding: 10px;
  outline: none;
  font-size: 14px;
  box-sizing: border-box;
}

.chat-form textarea,
.chat-reply textarea {
  resize: none;
  height: 70px;
}

.chat-form button,
.chat-reply button {
  border: none;
  border-radius: 10px;
  background: #2563eb;
  color: white;
  padding: 10px;
  cursor: pointer;
  font-weight: 600;
}

.chat-form button:disabled,
.chat-reply button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.chat-status {
  padding: 8px 12px;
  font-size: 13px;
  color: #15803d;
  background: #dcfce7;
}

.chat-status.error {
  color: #b91c1c;
  background: #fee2e2;
}
</style>