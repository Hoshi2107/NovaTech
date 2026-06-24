<template>
  <div class="row justify-content-center">
    <div class="col-md-9">
      <div class="card card-glass p-4 text-dark animate-fade-in" style="height: 78vh; display: flex; flex-direction: column;">
        <div class="d-flex justify-content-between align-items-center mb-3 border-bottom border-secondary border-opacity-25 pb-2">
          <h5 class="fw-bold m-0 text-cyan"><i class="fa-solid fa-robot me-2"></i>AI Assistant - Quản trị thông minh</h5>
          <span class="badge bg-success-subtle text-success px-3 py-1 text-xs">AI Online</span>
        </div>

        <!-- Chat screen box -->
        <div class="flex-grow-1 overflow-y-auto px-2 py-3 mb-3 d-flex flex-column gap-3" ref="chatContainer" style="max-height: calc(100vh - 350px);">
          <div v-for="(msg, index) in history" :key="index">
            <!-- AI Reply -->
            <div v-if="msg.sender === 'AI'" class="d-flex align-items-start gap-2.5 max-w-75">
              <div class="bg-gradient-cyan text-white d-flex align-items-center justify-content-center rounded-circle shadow" style="width: 32px; height: 32px; flex-shrink: 0;">
                <i class="fa-solid fa-robot text-xs text-dark"></i>
              </div>
              <div class="bg-light p-3 rounded-3 border border-secondary border-opacity-10 text-dark">
                <p class="text-xs mb-1 text-info fw-bold">NovaTech AI</p>
                <p class="text-xs mb-0" style="line-height: 1.5; white-space: pre-line;">{{ msg.message }}</p>
              </div>
            </div>
            
            <!-- User Message -->
            <div v-else class="d-flex align-items-start gap-2.5 justify-content-end max-w-75 ms-auto">
              <div class="bg-info bg-opacity-10 p-3 rounded-3 border border-info border-opacity-20 text-end text-dark">
                <p class="text-xs mb-1 text-warning fw-bold">Quản trị viên</p>
                <p class="text-xs mb-0" style="line-height: 1.5;">{{ msg.message }}</p>
              </div>
              <div class="bg-gradient-purple text-white d-flex align-items-center justify-content-center rounded-circle shadow" style="width: 32px; height: 32px; flex-shrink: 0;">
                <i class="fa-solid fa-user-tie text-xs"></i>
              </div>
            </div>
          </div>
        </div>

        <!-- Question input area -->
        <form @submit.prevent="handleAsk" class="border-top border-secondary border-opacity-25 pt-3 mt-auto">
          <div class="input-group">
            <input type="text" v-model="question" required class="form-control bg-light text-dark rounded-start-pill py-2.5 px-4 text-xs" placeholder="Hỏi AI về doanh thu, tồn kho hoặc gợi ý chương trình bán hàng...">
            <button type="submit" class="btn btn-info rounded-end-pill px-4" :disabled="sending">
              <i class="fa-solid fa-paper-plane text-dark"></i>
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, onMounted, nextTick } from 'vue'
import axios from 'axios'

export default {
  name: 'AiAssistant',
  setup() {
    const history = ref([])
    const question = ref('')
    const sending = ref(false)
    const chatContainer = ref(null)

    const fetchHistory = async () => {
      try {
        const response = await axios.get('/api/GetChatHistory')
        history.value = response.data
        scrollToBottom()
      } catch (err) {
        console.error('Error fetching chat history:', err)
      }
    }

    const handleAsk = async () => {
      if (!question.value.trim()) return
      
      const userQ = question.value
      question.value = ''
      sending.value = true

      // Optimistically append user message
      history.value.push({
        sender: 'User',
        message: userQ,
        timestamp: new Date()
      })
      scrollToBottom()

      try {
        const response = await axios.post('/api/AskAi', { question: userQ })
        history.value.push(response.data)
        scrollToBottom()
      } catch (err) {
        console.error('Error asking AI:', err)
      } finally {
        sending.value = false
      }
    }

    const scrollToBottom = () => {
      nextTick(() => {
        if (chatContainer.value) {
          chatContainer.value.scrollTop = chatContainer.value.scrollHeight
        }
      })
    }

    onMounted(() => {
      fetchHistory()
    })

    return {
      history,
      question,
      sending,
      chatContainer,
      handleAsk
    }
  }
}
</script>
