(() => {
  const widget = document.querySelector("[data-customer-chat-widget]");
  const chat = window.hotelChat;

  if (!widget || !chat) {
    return;
  }

  const toggle = widget.querySelector("[data-customer-chat-toggle]");
  const closeButton = widget.querySelector("[data-customer-chat-close]");
  const panel = widget.querySelector("[data-customer-chat-panel]");
  const messagesBox = widget.querySelector("[data-customer-chat-messages]");
  const status = widget.querySelector("[data-customer-chat-status]");
  const errorBox = widget.querySelector("[data-customer-chat-error]");
  const messageForm = widget.querySelector("[data-customer-chat-message-form]");
  const messageInput = widget.querySelector("[data-customer-chat-message]");
  const imageForm = widget.querySelector("[data-customer-chat-image-form]");
  const imageInput = widget.querySelector("[data-customer-chat-image]");
  const conversationInput = widget.querySelector("[data-customer-chat-conversation-id]");
  const currentUserId = Number(widget.dataset.currentUserId);
  let conversationId = 0;
  let isOpen = false;
  let isLoading = false;
  let pendingMessages = [];

  function setStatus(message) {
    status.textContent = message;
  }

  function scrollToBottom() {
    messagesBox.scrollTop = messagesBox.scrollHeight;
  }

  function appendMessage(message) {
    if (!message || (conversationId > 0 && Number(message.conversationId) !== conversationId)) {
      return;
    }

    if (message.id && messagesBox.querySelector(`[data-message-id="${message.id}"]`)) {
      return;
    }

    const wrapper = document.createElement("div");
    const isMine = message.isMine === true || Number(message.senderId) === currentUserId;
    wrapper.className = `chat-message ${isMine ? "mine" : "theirs"}`;

    if (message.id) {
      wrapper.dataset.messageId = message.id;
    }

    const bubble = document.createElement("div");
    bubble.className = "chat-bubble";

    const meta = document.createElement("div");
    meta.className = "chat-meta";

    const sender = document.createElement("strong");
    sender.textContent = message.senderName || "";
    const time = document.createElement("span");
    time.textContent = message.createdAtText || "";
    meta.append(sender, time);
    bubble.appendChild(meta);

    if (message.messageType === "Image") {
      const link = document.createElement("a");
      link.href = message.imageUrl;
      link.target = "_blank";
      link.rel = "noopener noreferrer";

      const image = document.createElement("img");
      image.src = message.imageUrl;
      image.alt = message.originalFileName || "Ảnh chat";
      image.className = "chat-image";
      link.appendChild(image);
      bubble.appendChild(link);
    } else {
      const text = document.createElement("div");
      text.className = "chat-text";
      text.textContent = message.content || "";
      bubble.appendChild(text);
    }

    wrapper.appendChild(bubble);
    messagesBox.appendChild(wrapper);
    scrollToBottom();
  }

  function renderMessages(messages) {
    messagesBox.innerHTML = "";

    if (!messages || messages.length === 0) {
      const empty = document.createElement("div");
      empty.className = "customer-chat-widget-empty";
      empty.textContent = "Hãy gửi lời nhắn, lễ tân sẽ phản hồi bạn sớm nhất.";
      messagesBox.appendChild(empty);
      return;
    }

    messages.forEach(appendMessage);
    scrollToBottom();
  }

  async function markAsRead() {
    if (conversationId <= 0) {
      return;
    }

    chat.setUnreadCount(0);

    try {
      const connection = await chat.ensureConnected();
      await connection.invoke("MarkConversationAsRead", conversationId);
    } catch (error) {
      console.warn("Could not mark chat messages as read.", error);
    }
  }

  async function loadConversation() {
    if (isLoading) {
      return;
    }

    isLoading = true;
    errorBox.textContent = "";
    setStatus("Đang tải cuộc trò chuyện...");

    try {
      const response = await fetch("/Customer/Chat/Messages", {
        cache: "no-store",
        credentials: "same-origin",
        headers: { "Accept": "application/json" }
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const result = await response.json();
      conversationId = Number(result.conversationId);
      conversationInput.value = String(conversationId);
      renderMessages(result.messages);
      pendingMessages.forEach(appendMessage);
      pendingMessages = [];
      setStatus("Đang kết nối với lễ tân");
      await markAsRead();
    } catch (error) {
      console.error(error);
      errorBox.textContent = "Không tải được cuộc trò chuyện. Vui lòng thử lại.";
      setStatus("Tạm thời mất kết nối");
    } finally {
      isLoading = false;
    }
  }

  async function openWidget() {
    isOpen = true;
    panel.hidden = false;
    toggle.setAttribute("aria-expanded", "true");
    await loadConversation();
    messageInput.focus();
  }

  function closeWidget() {
    isOpen = false;
    panel.hidden = true;
    toggle.setAttribute("aria-expanded", "false");
    toggle.focus();
  }

  toggle.addEventListener("click", () => {
    if (isOpen) {
      closeWidget();
    } else {
      openWidget();
    }
  });

  closeButton.addEventListener("click", closeWidget);

  messageForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    const content = messageInput.value.trim();

    if (!content || conversationId <= 0) {
      return;
    }

    try {
      errorBox.textContent = "";
      const connection = await chat.ensureConnected();
      const message = await connection.invoke("SendTextMessage", conversationId, content);
      appendMessage(message);
      messageInput.value = "";
    } catch (error) {
      console.error(error);
      errorBox.textContent = "Không gửi được tin nhắn.";
    }
  });

  imageForm.addEventListener("submit", async (event) => {
    event.preventDefault();

    if (!imageInput.files || imageInput.files.length === 0 || conversationId <= 0) {
      errorBox.textContent = "Vui lòng chọn ảnh.";
      return;
    }

    const formData = new FormData();
    const token = imageForm.querySelector("input[name='__RequestVerificationToken']");
    formData.append("__RequestVerificationToken", token.value);
    formData.append("conversationId", String(conversationId));
    formData.append("image", imageInput.files[0]);

    try {
      errorBox.textContent = "";
      const response = await fetch("/Chat/UploadImage", {
        method: "POST",
        credentials: "same-origin",
        body: formData
      });
      const result = await response.json();

      if (!response.ok) {
        throw new Error(result.message || "Không gửi được ảnh.");
      }

      appendMessage(result.message);
      imageInput.value = "";
    } catch (error) {
      console.error(error);
      errorBox.textContent = error.message || "Không gửi được ảnh.";
    }
  });

  document.addEventListener("hotel-chat:customer-message", (event) => {
    const message = event.detail;

    if (isLoading) {
      pendingMessages.push(message);
      return;
    }

    if (conversationId > 0 && Number(message.conversationId) === conversationId) {
      appendMessage(message);

      if (isOpen && !document.hidden && Number(message.senderId) !== currentUserId) {
        markAsRead();
      }
    }
  });

  document.addEventListener("visibilitychange", () => {
    if (isOpen && !document.hidden) {
      markAsRead();
    }
  });

  const pageUrl = new URL(window.location.href);
  if (pageUrl.searchParams.get("openChat")?.toLowerCase() === "true") {
    pageUrl.searchParams.delete("openChat");
    window.history.replaceState(null, "", `${pageUrl.pathname}${pageUrl.search}${pageUrl.hash}`);
    openWidget();
  }
})();
