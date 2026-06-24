import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import './services/mockApi.js'
import './assets/admin_custom.css'

const app = createApp(App)
app.use(router)
app.mount('#app')
