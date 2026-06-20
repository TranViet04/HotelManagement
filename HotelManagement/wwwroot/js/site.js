// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

(() => {
  const dayInMilliseconds = 24 * 60 * 60 * 1000;
  const dateFormatter = new Intl.DateTimeFormat("vi-VN", {
    weekday: "short",
    day: "2-digit",
    month: "2-digit"
  });
  const currencyFormatter = new Intl.NumberFormat("vi-VN");

  function parseDate(value) {
    if (!value) {
      return null;
    }

    const parts = value.split("-").map(Number);
    if (parts.length !== 3 || parts.some(Number.isNaN)) {
      return null;
    }

    return new Date(parts[0], parts[1] - 1, parts[2]);
  }

  function formatInputDate(date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    return `${year}-${month}-${day}`;
  }

  function addDays(date, days) {
    const result = new Date(date);
    result.setDate(result.getDate() + days);
    return result;
  }

  document.querySelectorAll("[data-stay-date-picker]").forEach((picker) => {
    const checkInInput = picker.querySelector("[data-stay-check-in]");
    const checkOutInput = picker.querySelector("[data-stay-check-out]");
    const summary = picker.querySelector("[data-stay-summary]");
    const presetButtons = picker.querySelectorAll("[data-stay-nights]");
    const nightsOutput = picker.querySelector("[data-booking-nights]");
    const totalOutput = picker.querySelector("[data-booking-total]");
    const pricePerNight = Number(picker.dataset.pricePerNight);

    if (!checkInInput || !checkOutInput) {
      return;
    }

    function updateDisplay() {
      const checkIn = parseDate(checkInInput.value);
      const checkOut = parseDate(checkOutInput.value);
      const nights = checkIn && checkOut
        ? Math.round((checkOut.getTime() - checkIn.getTime()) / dayInMilliseconds)
        : 0;

      presetButtons.forEach((button) => {
        button.classList.toggle("active", Number(button.dataset.stayNights) === nights);
      });

      if (summary) {
        summary.textContent = nights > 0
          ? `${nights} đêm: ${dateFormatter.format(checkIn)} - ${dateFormatter.format(checkOut)}`
          : "Chọn ngày nhận và trả phòng";
      }

      if (nightsOutput && nights > 0) {
        nightsOutput.textContent = String(nights);
      }

      if (totalOutput && nights > 0 && Number.isFinite(pricePerNight)) {
        totalOutput.textContent = `${currencyFormatter.format(pricePerNight * nights)} VND`;
      }
    }

    function constrainCheckOut(adjustInvalidValue) {
      const checkIn = parseDate(checkInInput.value);
      if (!checkIn) {
        return;
      }

      const minimumCheckOut = addDays(checkIn, 1);
      checkOutInput.min = formatInputDate(minimumCheckOut);

      const checkOut = parseDate(checkOutInput.value);
      if (adjustInvalidValue && (!checkOut || checkOut <= checkIn)) {
        checkOutInput.value = formatInputDate(minimumCheckOut);
      }
    }

    checkInInput.addEventListener("change", () => {
      constrainCheckOut(true);
      updateDisplay();
    });

    checkOutInput.addEventListener("change", updateDisplay);

    presetButtons.forEach((button) => {
      button.addEventListener("click", () => {
        let checkIn = parseDate(checkInInput.value);
        const today = new Date();
        today.setHours(0, 0, 0, 0);

        if (!checkIn || checkIn < today) {
          checkIn = today;
          checkInInput.value = formatInputDate(checkIn);
        }

        const nights = Number(button.dataset.stayNights);
        checkOutInput.value = formatInputDate(addDays(checkIn, nights));
        constrainCheckOut(false);
        updateDisplay();
      });
    });

    constrainCheckOut(false);
    updateDisplay();
  });
})();

(() => {
  const form = document.querySelector("[data-room-availability-form]");
  const results = document.querySelector("[data-room-availability-results]");

  if (!form || !results || !window.fetch) {
    return;
  }

  const submitButton = form.querySelector("[data-room-availability-submit]");

  form.addEventListener("submit", async (event) => {
    if (!form.reportValidity()) {
      return;
    }

    event.preventDefault();

    const requestUrl = new URL(form.action, window.location.origin);
    requestUrl.search = new URLSearchParams(new FormData(form)).toString();
    const originalButtonText = submitButton?.textContent;

    results.classList.remove("is-updated");
    results.classList.add("is-loading");
    results.setAttribute("aria-busy", "true");

    if (submitButton) {
      submitButton.disabled = true;
      submitButton.textContent = "Đang kiểm tra...";
    }

    try {
      const response = await fetch(requestUrl, {
        headers: {
          "X-Requested-With": "XMLHttpRequest"
        }
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      results.innerHTML = await response.text();
      results.classList.add("is-updated");
      window.history.replaceState(null, "", `${requestUrl.pathname}${requestUrl.search}`);
    } catch {
      results.innerHTML = `
        <div class="alert alert-danger" role="alert">
          Không thể kiểm tra phòng lúc này. Vui lòng thử lại.
        </div>`;
    } finally {
      results.classList.remove("is-loading");
      results.removeAttribute("aria-busy");

      if (submitButton) {
        submitButton.disabled = false;
        submitButton.textContent = originalButtonText || "Kiểm tra";
      }
    }
  });
})();
