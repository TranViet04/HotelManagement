(() => {
  const region = document.querySelector("[data-management-live-region]");
  const status = document.querySelector("[data-management-live-status]");
  const label = status?.querySelector("[data-management-live-label]");
  const refreshButton = status?.querySelector("[data-management-live-refresh]");

  if (!region || !window.fetch || !window.DOMParser) {
    return;
  }

  const configuredInterval = Number(region.dataset.managementLiveInterval);
  const interval = Number.isFinite(configuredInterval) && configuredInterval >= 3000
    ? configuredInterval
    : 5000;

  let timerId;
  let requestInProgress = false;
  let pausedForEditing = false;

  function setStatus(state, message) {
    if (!status || !label) {
      return;
    }

    status.dataset.state = state;
    label.textContent = message;
  }

  function getComparableHtml(element) {
    const copy = element.cloneNode(true);

    copy.querySelectorAll("input[name='__RequestVerificationToken']").forEach((input) => {
      input.value = "";
    });

    return copy.innerHTML.trim();
  }

  function isUserInteracting() {
    const activeElement = document.activeElement;
    const isEditing = activeElement
      && region.contains(activeElement)
      && activeElement.matches("input, select, textarea, [contenteditable='true']");

    return pausedForEditing
      || isEditing
      || Boolean(document.querySelector(".modal.show"));
  }

  function scheduleNext(delay = interval) {
    window.clearTimeout(timerId);
    timerId = window.setTimeout(refresh, delay);
  }

  async function refresh({ force = false } = {}) {
    if (requestInProgress || document.hidden) {
      scheduleNext();
      return;
    }

    if (!force && isUserInteracting()) {
      setStatus("paused", "Tạm dừng khi đang thao tác");
      scheduleNext();
      return;
    }

    requestInProgress = true;
    refreshButton?.setAttribute("disabled", "disabled");
    setStatus("syncing", "Đang kiểm tra dữ liệu mới...");

    try {
      const response = await fetch(window.location.href, {
        cache: "no-store",
        credentials: "same-origin",
        headers: {
          "X-Requested-With": "XMLHttpRequest"
        }
      });

      if (!response.ok || response.redirected) {
        throw new Error(`HTTP ${response.status}`);
      }

      const html = await response.text();
      const nextDocument = new DOMParser().parseFromString(html, "text/html");
      const nextRegion = nextDocument.querySelector("[data-management-live-region]");

      if (!nextRegion) {
        throw new Error("Live region was not found in the response.");
      }

      if (getComparableHtml(region) !== getComparableHtml(nextRegion)) {
        region.innerHTML = nextRegion.innerHTML;
        region.classList.remove("management-live-sync-updated");
        void region.offsetWidth;
        region.classList.add("management-live-sync-updated");
        document.dispatchEvent(new CustomEvent("management:content-updated", {
          detail: { region }
        }));
        setStatus("updated", `Đã cập nhật lúc ${new Date().toLocaleTimeString("vi-VN")}`);
      } else {
        setStatus("connected", "Dữ liệu đang được đồng bộ");
      }

      scheduleNext();
    } catch (error) {
      console.warn("Management live sync failed.", error);
      setStatus("error", "Mất kết nối, sẽ thử lại");
      scheduleNext(Math.max(interval * 2, 10000));
    } finally {
      requestInProgress = false;
      refreshButton?.removeAttribute("disabled");
    }
  }

  region.addEventListener("input", (event) => {
    if (event.target.matches("input:not([type='hidden']), select, textarea, [contenteditable='true']")) {
      pausedForEditing = true;
      setStatus("paused", "Tạm dừng khi đang thao tác");
    }
  });

  region.addEventListener("change", (event) => {
    if (event.target.matches("input:not([type='hidden']), select, textarea")) {
      pausedForEditing = true;
      setStatus("paused", "Tạm dừng khi đang thao tác");
    }
  });

  refreshButton?.addEventListener("click", () => {
    pausedForEditing = false;
    refresh({ force: true });
  });

  document.addEventListener("visibilitychange", () => {
    if (!document.hidden) {
      refresh();
    }
  });

  scheduleNext();
})();
