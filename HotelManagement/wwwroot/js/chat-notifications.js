(() => {
  const isCustomer = Boolean(document.querySelector("[data-customer-chat-widget]"));
  const receptionistBadges = document.querySelectorAll("[data-receptionist-chat-count]");
  const role = isCustomer ? "Customer" : (receptionistBadges.length > 0 ? "Receptionist" : "");

  if (!role) {
    return;
  }

  const baseTitle = document.title.replace(/^\(\d+\)\s*/, "");
  const summaryUrl = role === "Customer"
    ? "/Customer/Chat/Summary"
    : "/Receptionist/Chats/UnreadSummary";
  let unreadCount = 0;
  let startPromise = null;

  function formatCount(count) {
    return count > 99 ? "99+" : String(count);
  }

  function updateBadgeElements(selector, count) {
    document.querySelectorAll(selector).forEach((badge) => {
      badge.textContent = formatCount(count);
      badge.hidden = count <= 0;
    });
  }

  function setUnreadCount(value) {
    unreadCount = Math.max(0, Number(value) || 0);

    if (role === "Customer") {
      updateBadgeElements("[data-customer-chat-count]", unreadCount);
      const toggle = document.querySelector("[data-customer-chat-toggle]");
      if (toggle) {
        toggle.setAttribute(
          "aria-label",
          unreadCount > 0
            ? `Nhắn tin với lễ tân, ${unreadCount} tin chưa đọc`
            : "Nhắn tin với lễ tân");
      }
    } else {
      updateBadgeElements("[data-receptionist-chat-count]", unreadCount);
    }

    document.title = unreadCount > 0 ? `(${unreadCount}) ${baseTitle}` : baseTitle;
    document.dispatchEvent(new CustomEvent("hotel-chat:unread-changed", {
      detail: { role, unreadCount }
    }));
  }

  async function refreshUnreadCount() {
    try {
      const response = await fetch(summaryUrl, {
        cache: "no-store",
        credentials: "same-origin",
        headers: { "Accept": "application/json" }
      });

      if (!response.ok) {
        return;
      }

      const result = await response.json();
      setUnreadCount(result.unreadCount);
    } catch (error) {
      console.warn("Could not refresh chat unread count.", error);
    }
  }

  const connection = window.signalR
    ? new signalR.HubConnectionBuilder()
      .withUrl("/chatHub")
      .withAutomaticReconnect()
      .build()
    : null;

  async function ensureConnected() {
    if (!connection) {
      throw new Error("SignalR is not available.");
    }

    if (connection.state === signalR.HubConnectionState.Connected) {
      return connection;
    }

    if (!startPromise) {
      startPromise = connection.start().finally(() => {
        startPromise = null;
      });
    }

    await startPromise;
    return connection;
  }

  async function startWithRetry() {
    try {
      await ensureConnected();
      await refreshUnreadCount();
    } catch (error) {
      console.warn("Chat notification connection failed. Retrying...", error);
      window.setTimeout(startWithRetry, 5000);
    }
  }

  if (connection) {
    connection.on("CustomerMessageReceived", (message) => {
      document.dispatchEvent(new CustomEvent("hotel-chat:customer-message", {
        detail: message
      }));
    });

    connection.on("CustomerUnreadCountChanged", (count, conversationId) => {
      if (role === "Customer") {
        setUnreadCount(count);
      }

      document.dispatchEvent(new CustomEvent("hotel-chat:customer-unread", {
        detail: { unreadCount: Number(count) || 0, conversationId }
      }));
    });

    connection.on("ConversationUpdated", (conversationId) => {
      if (role === "Receptionist") {
        refreshUnreadCount();
      }

      document.dispatchEvent(new CustomEvent("hotel-chat:conversation-updated", {
        detail: { conversationId }
      }));
    });

    connection.onreconnected(() => {
      refreshUnreadCount();
    });
  }

  window.hotelChat = {
    connection,
    ensureConnected,
    refreshUnreadCount,
    setUnreadCount,
    getUnreadCount: () => unreadCount
  };

  refreshUnreadCount();
  startWithRetry();
  window.setInterval(refreshUnreadCount, 30000);
})();
