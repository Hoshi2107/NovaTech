(function() {
    const root = document.getElementById('customerMessenger');
    if (!root) return;

    const THREAD_KEY_PREFIX = 'novatech_customer_thread_id';


    const oldKeys = [
   'novatech_customer_thread_id',
   'novatech_customer_thread_id_guest',
   'novatech_customer_profile'
    ];

    let isOpen = false;
    let sending = false;
    let thread = null;
    let threadId = null;
    let lastStaffMessageCount = 0;
    let activeStorageKey = null;
    let pollingTimer = null;

    const toggleButton = document.getElementById('customerChatToggle');
    const closeButton = document.getElementById('customerChatClose');
    const box = document.getElementById('customerChatBox');
    const messagesBox = document.getElementById('customerChatMessages');
    const input = document.getElementById('customerChatInput');
    const sendButton = document.getElementById('customerChatSend');
    const statusBox = document.getElementById('customerChatStatus');
    const badge = document.getElementById('customerChatBadge');

    const userEmail = root.dataset.userEmail || '';
    const userName = root.dataset.userName || '';
    const isCustomer = root.dataset.isCustomer === 'true';

    const escapeHtml = (value) => {
        return String(value || '')
   .replaceAll('&', '&amp;')
   .replaceAll('<', '&lt;')
   .replaceAll('>', '&gt;')
   .replaceAll('"', '&quot;')
   .replaceAll("'", '&#039;');
    };

    const formatTime = (value) => {
        if (!value) return '';

        return new Date(value).toLocaleTimeString('vi-VN', {
       hour: '2-digit',
       minute: '2-digit'
   });
    };

    const showStatus = (message, ok) => {
        if (!message) {
            statusBox.style.display = 'none';
            statusBox.textContent = '';
            statusBox.classList.remove('error');
            return;
        }

        statusBox.style.display = 'block';
        statusBox.textContent = message;
        statusBox.classList.toggle('error', !ok);
    };

    const getThreadStorageKey = () => {
        if (userEmail && isCustomer) {
            return `${THREAD_KEY_PREFIX}_email_${userEmail}`;
        }

        return `${THREAD_KEY_PREFIX}_guest_${getGuestId()}`;
    };

    const getCustomerName = () => {
    if (userName) return userName;
    if (userEmail) return userEmail;

    return 'Khách hàng NovaTech';
};
    const cleanupOldStorage = () => {
        oldKeys.forEach(key => localStorage.removeItem(key));

        const keysToRemove = [];

        for (let i = 0; i < localStorage.length; i += 1) {
            const key = localStorage.key(i);

            if (key && key.startsWith(`${THREAD_KEY_PREFIX}_anonymous_`)) {
                keysToRemove.push(key);
            }
        }

        keysToRemove.forEach(key => localStorage.removeItem(key));
    };

    const syncThreadIdFromStorage = () => {
    const getThreadStorageKey = () => {
    return `${THREAD_KEY_PREFIX}_email_${userEmail}`;
};

        if (activeStorageKey !== storageKey) {
            activeStorageKey = storageKey;

            const storedId = Number(localStorage.getItem(storageKey)) || null;

            if (storedId) {
                threadId = storedId;
                thread = null;
                lastStaffMessageCount = 0;
                return;
            }

         

            threadId = null;
            thread = null;
            lastStaffMessageCount = 0;
            return;
        }

        const storedId = Number(localStorage.getItem(storageKey)) || null;

        if (storedId && storedId !== threadId) {
            threadId = storedId;
            thread = null;
            lastStaffMessageCount = 0;
        }
    };

    const saveThreadIdToStorage = (id) => {
        if (!id) return;

        const storageKey = getThreadStorageKey();

        localStorage.setItem(storageKey, String(id));
        
        threadId = id;
    };

    const removeThreadIdFromStorage = () => {
        const storageKey = getThreadStorageKey();

        localStorage.removeItem(storageKey);
    };

    const scrollToBottom = () => {
        requestAnimationFrame(() => {
       messagesBox.scrollTop = messagesBox.scrollHeight;
   });
    };

    const markStaffMessagesSeen = () => {
        if (!thread || !Array.isArray(thread.messages)) return;

        lastStaffMessageCount = thread.messages.filter(item => item.sender === 'staff').length;
        updateBadge();
    };

    const updateBadge = () => {
        if (!thread || !Array.isArray(thread.messages)) {
            badge.style.display = 'none';
            return;
        }

        const staffMessages = thread.messages.filter(item => item.sender === 'staff');
        const diff = staffMessages.length - lastStaffMessageCount;

        if (diff > 0 && !isOpen) {
            badge.textContent = String(diff);
            badge.style.display = 'inline-flex';
        } else {
            badge.style.display = 'none';
        }
    };

    const renderMessages = () => {
        if (!thread || !Array.isArray(thread.messages) || thread.messages.length === 0) {
            messagesBox.innerHTML = `
                <div class="chat-empty">
                    Chào bạn 👋 Hãy nhập tin nhắn để bắt đầu chat với NovaTech.
                </div>
            `;
            return;
        }

        messagesBox.innerHTML = thread.messages.map(message => `
            <div class="chat-message ${message.sender === 'customer' ? 'from-customer' : 'from-staff'}">
                <div class="bubble">
                    <div class="sender">${message.sender === 'customer' ? 'Bạn' : 'NovaTech'}</div>
                    <div>${escapeHtml(message.text)}</div>
                    <small>${formatTime(message.timestamp)}</small>
                </div>
            </div>
        `).join('');

        scrollToBottom();
    };

    const loadThread = async () => {
        syncThreadIdFromStorage();

        if (!threadId) return;

        try {
            const oldMessageCount = thread && Array.isArray(thread.messages)
   ? thread.messages.length
   : 0;

            const response = await fetch(`/SalesCSKH/GetCustomerThread?id=${threadId}`);

            if (!response.ok) {
                throw new Error('Không tìm thấy hội thoại.');
            }

            thread = await response.json();
            thread.messages = Array.isArray(thread.messages) ? thread.messages : [];

            const newMessageCount = thread.messages.length;

            renderMessages();

            if (isOpen) {
                markStaffMessagesSeen();
            } else if (newMessageCount !== oldMessageCount) {
                updateBadge();
            }
        } catch (err) {
            thread = null;
            removeThreadIdFromStorage();
            threadId = null;
            renderMessages();
        }
    };

    const postJson = async (url, body) => {
        const response = await fetch(url, {
       method: 'POST',
       headers: {
           'Content-Type': 'application/json'
       },
       body: JSON.stringify(body)
   });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.message || 'Có lỗi xảy ra.');
        }

        return data;
    };

    const createNewThread = async (messageText) => {
        const data = await postJson('/SalesCSKH/CreateCustomerInquiry', {
       customerName: getCustomerName(),
       customerPhone: '',
       subject: 'Chat hỗ trợ NovaTech',
       message: messageText
   });

        if (data.thread && data.thread.id) {
            thread = data.thread;
            thread.messages = Array.isArray(thread.messages) ? thread.messages : [];

            saveThreadIdToStorage(thread.id);
            renderMessages();
            markStaffMessagesSeen();
            showStatus('Đã gửi tin nhắn. NovaTech sẽ trả lời tại đây.', true);
        }
    };

    const sendMessageToExistingThread = async (messageText) => {
        await postJson('/SalesCSKH/AddCustomerInquiryMessage', {
       threadId: threadId,
       message: messageText
   });

        showStatus('Đã gửi tin nhắn.', true);
        await loadThread();
        scrollToBottom();
    };

    const sendMessage = async () => {
        const messageText = input.value.trim();

        showStatus('', true);

        if (!messageText) {
            showStatus('Vui lòng nhập nội dung tin nhắn.', false);
            return;
        }

        try {
            sending = true;
            sendButton.disabled = true;
            sendButton.textContent = 'Đang gửi...';

            await loadThread();

            if (threadId) {
                await sendMessageToExistingThread(messageText);
            } else {
                await createNewThread(messageText);
            }

            input.value = '';
        } catch (err) {
            showStatus(err.message || 'Gửi tin nhắn thất bại.', false);
        } finally {
            sending = false;
            sendButton.disabled = false;
            sendButton.textContent = 'Gửi';
        }
    };

    const openChat = async () => {
        isOpen = true;
        box.style.display = 'flex';

        await loadThread();
        renderMessages();
        markStaffMessagesSeen();
        scrollToBottom();
    };

    const closeChat = () => {
        isOpen = false;
        box.style.display = 'none';
        updateBadge();
    };

    const toggleChat = () => {
        if (isOpen) {
            closeChat();
        } else {
            openChat();
        }
    };

    toggleButton.addEventListener('click', toggleChat);
    closeButton.addEventListener('click', closeChat);
    sendButton.addEventListener('click', sendMessage);

    input.addEventListener('keydown', function(event) {
       if (event.key === 'Enter' && !event.shiftKey) {
           event.preventDefault();
           sendMessage();
       }
   });

    cleanupOldStorage();
    loadThread();

    pollingTimer = setInterval(() => {
       loadThread();
   }, 1500);

    window.addEventListener('beforeunload', function() {
       if (pollingTimer) {
           clearInterval(pollingTimer);
       }
   });
})();