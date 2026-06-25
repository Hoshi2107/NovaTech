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
        <button type="button" @click="isOpen = false">×</button>
      </div>

      <div ref="messagePanel" class="chat-messages">
        <div v-if="!thread" class="chat-empty">
          Chào bạn 👋 Hãy nhập tin nhắn để bắt đầu chat với NovaTech.
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

      <div class="chat-reply">
        <textarea
          v-model="messageText"
          placeholder="Nhập tin nhắn..."
          @keydown.enter.exact.prevent="sendMessage"
        ></textarea>

        <button type="button" :disabled="sending" @click="sendMessage">
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

const THREAD_KEY_PREFIX = 'novatech_customer_thread_id'
const GUEST_ID_KEY = 'novatech_customer_guest_id'
const LAST_THREAD_KEY = 'novatech_customer_last_thread_id'

const OLD_SHARED_KEY = 'novatech_customer_thread_id'
const OLD_GUEST_KEY = 'novatech_customer_thread_id_guest'
const OLD_PROFILE_KEY = 'novatech_customer_profile'

const isOpen = ref(false)
const sending = ref(false)
const statusMessage = ref('')
const statusOk = ref(false)
const thread = ref(null)
const threadId = ref(null)
const messagePanel = ref(null)
const messageText = ref('')
const lastStaffMessageCount = ref(0)
const activeStorageKey = ref(null)

let pollingTimer = null

const getSession = () => {
  try {
    const raw = localStorage.getItem('novatech_session')
    return raw ? JSON.parse(raw) : null
  } catch (err) {
    return null
  }
}

const getGuestId = () => {
  let guestId = localStorage.getItem(GUEST_ID_KEY)

  if (!guestId) {
    if (window.crypto && window.crypto.randomUUID) {
      guestId = window.crypto.randomUUID()
    } else {
      guestId = `${Date.now()}_${Math.random().toString(16).slice(2)}`
    }

    localStorage.setItem(GUEST_ID_KEY, guestId)
  }

  return guestId
}

const getThreadStorageKey = () => {
  const session = getSession()
  const roles = Array.isArray(session?.roles) ? session.roles : []

  if (session?.email && roles.includes('Khách hàng')) {
    return `${THREAD_KEY_PREFIX}_email_${session.email}`
  }

  return `${THREAD_KEY_PREFIX}_guest_${getGuestId()}`
}

const getCustomerName = () => {
  const session = getSession()

  if (session?.fullName) return session.fullName
  if (session?.name) return session.name
  if (session?.email) return session.email

  const guestId = getGuestId()
  return `Khách vãng lai #${guestId.slice(-6)}`
}

const cleanupOldStorage = () => {
  localStorage.removeItem(OLD_SHARED_KEY)
  localStorage.removeItem(OLD_GUEST_KEY)
  localStorage.removeItem(OLD_PROFILE_KEY)

  const keysToRemove = []

  for (let i = 0; i < localStorage.length; i += 1) {
    const key = localStorage.key(i)

    if (key && key.startsWith(`${THREAD_KEY_PREFIX}_anonymous_`)) {
      keysToRemove.push(key)
    }
  }

  keysToRemove.forEach(key => localStorage.removeItem(key))
}

const syncThreadIdFromStorage = () => {
  const storageKey = getThreadStorageKey()

  if (activeStorageKey.value !== storageKey) {
    activeStorageKey.value = storageKey

    const session = getSession()
    const storedId = Number(localStorage.getItem(storageKey)) || null

    if (storedId) {
      threadId.value = storedId
      thread.value = null
      lastStaffMessageCount.value = 0
      return
    }

    if (!session?.email) {
      const lastThreadId = Number(localStorage.getItem(LAST_THREAD_KEY)) || null

      if (lastThreadId) {
        threadId.value = lastThreadId
        thread.value = null
        lastStaffMessageCount.value = 0
        return
      }
    }

    threadId.value = null
    thread.value = null
    lastStaffMessageCount.value = 0
    return
  }

  const storedId = Number(localStorage.getItem(storageKey)) || null

  if (storedId && storedId !== threadId.value) {
    threadId.value = storedId
    thread.value = null
    lastStaffMessageCount.value = 0
  }
}

const saveThreadIdToStorage = (id) => {
  if (!id) return

  const storageKey = getThreadStorageKey()

  localStorage.setItem(storageKey, String(id))
  localStorage.setItem(LAST_THREAD_KEY, String(id))
  threadId.value = id
}

const removeThreadIdFromStorage = () => {
  const storageKey = getThreadStorageKey()

  localStorage.removeItem(storageKey)

  if (String(localStorage.getItem(LAST_THREAD_KEY)) === String(threadId.value)) {
    localStorage.removeItem(LAST_THREAD_KEY)
  }
}

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

const loadThread = async () => {
  syncThreadIdFromStorage()

  if (!threadId.value) return

  try {
    const response = await axios.get(`/api/GetCustomerThread?id=${threadId.value}`)
    const oldMessageCount = thread.value?.messages?.length || 0

    thread.value = response.data
    thread.value.messages = Array.isArray(thread.value.messages) ? thread.value.messages : []

    const newMessageCount = thread.value.messages.length

    if (newMessageCount !== oldMessageCount) {
      scrollToBottom()
    }

    if (isOpen.value) {
      markStaffMessagesSeen()
    }
  } catch (err) {
    thread.value = null
    removeThreadIdFromStorage()
    threadId.value = null
  }
}

const toggleChat = async () => {
  isOpen.value = !isOpen.value

  if (isOpen.value) {
    await loadThread()
    markStaffMessagesSeen()
    scrollToBottom()
  }
}

const createNewThread = async () => {
  const response = await axios.post('/api/CreateCustomerInquiry', {
    customerName: getCustomerName(),
    customerPhone: '',
    subject: 'Chat hỗ trợ NovaTech',
    message: messageText.value
  })

  const newThread = response.data?.thread

  if (newThread?.id) {
    thread.value = newThread
    thread.value.messages = Array.isArray(thread.value.messages) ? thread.value.messages : []

    saveThreadIdToStorage(newThread.id)

    messageText.value = ''
    statusOk.value = true
    statusMessage.value = 'Đã gửi tin nhắn. NovaTech sẽ trả lời tại đây.'
    scrollToBottom()
  }
}

const sendMessageToExistingThread = async () => {
  await axios.post('/api/AddCustomerInquiryMessage', {
    threadId: threadId.value,
    message: messageText.value
  })

  messageText.value = ''
  statusOk.value = true
  statusMessage.value = 'Đã gửi tin nhắn.'

  await loadThread()
  scrollToBottom()
}

const sendMessage = async () => {
  statusMessage.value = ''

  if (!messageText.value.trim()) {
    statusOk.value = false
    statusMessage.value = 'Vui lòng nhập nội dung tin nhắn.'
    return
  }

  try {
    sending.value = true

    await loadThread()

    if (threadId.value) {
      await sendMessageToExistingThread()
    } else {
      await createNewThread()
    }
  } catch (err) {
    statusOk.value = false
    statusMessage.value = err.response?.data?.message || 'Gửi tin nhắn thất bại.'
  } finally {
    sending.value = false
  }
}

onMounted(() => {
  cleanupOldStorage()
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
  height: 440px;
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

.chat-reply {
  padding: 12px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  border-top: 1px solid #e5e7eb;
}

.chat-reply textarea {
  width: 100%;
  border: 1px solid #cbd5e1;
  border-radius: 10px;
  padding: 10px;
  outline: none;
  font-size: 14px;
  box-sizing: border-box;
  resize: none;
  height: 70px;
}

.chat-reply button {
  border: none;
  border-radius: 10px;
  background: #2563eb;
  color: white;
  padding: 10px;
  cursor: pointer;
  font-weight: 600;
}

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