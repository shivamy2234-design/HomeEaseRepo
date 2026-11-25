document.addEventListener("DOMContentLoaded", () => {
  const prices = {
    "Home Cleaning": 799, "Kitchen & Bathroom": 599, "Electrical Services": 299,
    "Plumbing": 349, "Laundry & Ironing": 199, "AC & Appliance Care": 499,
    "Pest Control": 499, "Painting & Repairs": 999, "Gardening": 399,
    "Carpentry": 399, "Cook & Maid Services": 499
  };
  const form = document.getElementById("bookingForm");
  const serviceSelect = document.getElementById("service");
  const dateInput = document.getElementById("bookingDate");
  const timeSelect = document.getElementById("timeSlot");
  const today = new Date();
  const localToday = new Date(today.getTime() - today.getTimezoneOffset() * 60000).toISOString().slice(0, 10);
  dateInput.min = localToday;

  const params = new URLSearchParams(window.location.search);
  const requested = params.get("service");
  if (requested && [...serviceSelect.options].some(option => option.value === requested || option.textContent === requested)) {
    serviceSelect.value = requested;
  }

  function updateSummary() {
    const service = serviceSelect.value;
    document.getElementById("summaryService").textContent = service;
    document.getElementById("summaryPrice").textContent = `₹${prices[service] || "To be confirmed"}`;
    const date = dateInput.value ? new Date(`${dateInput.value}T00:00:00`).toLocaleDateString("en-IN", { day: "numeric", month: "short", year: "numeric" }) : "";
    document.getElementById("summaryWhen").textContent = [date, timeSelect.value].filter(Boolean).join(" · ") || "Select your preferred date and time.";
  }
  serviceSelect.addEventListener("change", updateSummary);
  dateInput.addEventListener("change", updateSummary);
  timeSelect.addEventListener("change", updateSummary);
  document.getElementById("currentYear").textContent = new Date().getFullYear();
  updateSummary();

  form.addEventListener("submit", (event) => {
    event.preventDefault();
    if (!form.checkValidity()) {
      form.reportValidity();
      return;
    }
    if (dateInput.value < localToday) {
      dateInput.setCustomValidity("Please select today or a future date.");
      dateInput.reportValidity();
      dateInput.setCustomValidity("");
      return;
    }
    const data = Object.fromEntries(new FormData(form).entries());
    data.createdAt = new Date().toISOString();
    // Demo-only local storage. This is not a real database or secure booking system.
    try {
      const existing = JSON.parse(localStorage.getItem("homeEaseDemoBookings") || "[]");
      existing.push({ ...data, demoReference: `HE-${Date.now().toString().slice(-8)}`, status: "Pending (demo)" });
      localStorage.setItem("homeEaseDemoBookings", JSON.stringify(existing));
      const message = document.getElementById("bookingMessage");
      message.className = "alert alert-success mt-3";
      message.innerHTML = `<strong>Demo booking saved in this browser.</strong><br>Service: ${escapeHtml(data.service)}<br>Date: ${escapeHtml(data.bookingDate)} · ${escapeHtml(data.timeSlot)}<br><br>This is not a confirmed service request yet. ASP.NET MVC + SQL Server integration is still required.`;
      message.scrollIntoView({ behavior: "smooth", block: "nearest" });
    } catch (error) {
      const message = document.getElementById("bookingMessage");
      message.className = "alert alert-warning mt-3";
      message.textContent = "Your browser could not save this demo. Please check browser storage settings.";
    }
  });

  function escapeHtml(value) {
    return String(value ?? "").replace(/[&<>"']/g, character => ({
      "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
    })[character]);
  }
});