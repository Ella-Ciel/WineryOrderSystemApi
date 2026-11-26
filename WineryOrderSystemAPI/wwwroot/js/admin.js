import {
  state,
  apiRequest,
  saveWineImage,
  escapeHtml,
  formatPHP,
  formatDate,
  showToast,
  renderWineVisual,
} from "./api.js";
import { openOrderDetailModal } from "./orders.js";

let adminOrdersCache = [];
let analyticsCache = null;
let editingWineId = null;
let activeChartMode = "stock";
let chartInstance = null;

export function initAdmin({ onNavigate }) {
  document.getElementById("adminSidebarToggle").addEventListener("click", () => {
    document.getElementById("adminSidebar").classList.toggle("open");
  });

  document.getElementById("kpiPendingCard").addEventListener("click", (e) => {
    if (e.target.closest("button")) return;
    document.getElementById("ordStatusFilter").value = "Pending";
    onNavigate("admin-orders");
  });

  // Switch between Stock Breakdown & OLAP Sales Trend inside crimson Inventory Summary
  document.querySelectorAll("[data-chart-mode]").forEach((btn) => {
    btn.addEventListener("click", () => {
      activeChartMode = btn.dataset.chartMode;
      document.querySelectorAll("[data-chart-mode]").forEach((b) => b.classList.toggle("active", b === btn));
      renderThemedAnalyticsChart();
    });
  });

  document.getElementById("invSearchInput").addEventListener("input", renderInventoryTable);
  document.getElementById("invCategoryFilter").addEventListener("change", renderInventoryTable);
  document.getElementById("openAddWineModalBtn").addEventListener("click", () => openWineFormModal(null));
  document.getElementById("adminWineForm").addEventListener("submit", handleSaveWine);

  document.getElementById("ordSearchInput").addEventListener("input", renderAdminOrdersTable);
  document.getElementById("ordStatusFilter").addEventListener("change", renderAdminOrdersTable);
  document.getElementById("adminRefreshOrdersBtn").addEventListener("click", () => loadAdminData("admin-orders"));
}

function formatStatusLabel(status) {
  const s = String(status || "Pending");
  return s.toLowerCase() === "pendingpayment" ? "Pending Payment" : s;
}

function animateCounter(el, target) {
  if (!el) return;
  const end = Number(target) || 0;
  const start = Number(el.textContent) || 0;
  if (start === end) { el.textContent = end; return; }
  const steps = 16;
  let cur = 0;
  const timer = setInterval(() => {
    cur++;
    el.textContent = Math.round(start + ((end - start) * cur) / steps);
    if (cur >= steps) { el.textContent = end; clearInterval(timer); }
  }, 18);
}

export async function loadAdminData(subRoute) {
  document.getElementById("adminSidebar").classList.remove("open");

  const tabs = {
    "admin-dashboard": "admin-tab-dashboard",
    "admin-inventory": "admin-tab-inventory",
    "admin-orders": "admin-tab-orders",
  };
  Object.values(tabs).forEach((id) => document.getElementById(id).classList.add("hidden"));
  document.getElementById(tabs[subRoute] || "admin-tab-dashboard").classList.remove("hidden");

  document.querySelectorAll(".admin-nav-btn").forEach((b) =>
    b.classList.toggle("active", b.dataset.route === subRoute)
  );

  // Match Figma sidebar tiles: Inventory shows Low-Stock Alerts; Dashboard/Orders shows Total Orders
  const isInventory = subRoute === "admin-inventory";
  document.getElementById("sideTileTotalOrders").classList.toggle("hidden", isInventory);
  document.getElementById("sideTileLowStock").classList.toggle("hidden", !isInventory);

  const [winesRes, ordersRes, analyticsRes] = await Promise.allSettled([
    apiRequest("GET", "/api/v1/Wines"),
    apiRequest("GET", "/api/Admin/orders"),
    apiRequest("GET", "/api/Analytics/sales-report"),
  ]);

  if (winesRes.status === "fulfilled" && Array.isArray(winesRes.value)) {
    state.wines = winesRes.value;
  }
  if (ordersRes.status === "fulfilled" && Array.isArray(ordersRes.value)) {
    adminOrdersCache = ordersRes.value.sort((a, b) => new Date(b.orderDate) - new Date(a.orderDate));
  }
  analyticsCache = analyticsRes.status === "fulfilled" ? analyticsRes.value : null;

  updateAdminKPIs();
  populateInventoryCategories();

  if (subRoute === "admin-dashboard") {
    renderPendingOrdersMini();
    renderRecentOrdersMini();
    renderLowStockPanel();
    renderThemedAnalyticsChart();
  } else if (subRoute === "admin-inventory") {
    renderInventoryTable();
  } else if (subRoute === "admin-orders") {
    renderAdminOrdersTable();
  }
}

function updateAdminKPIs() {
  const lowCount = state.wines.filter((w) => w.stockQuantity < 10).length;
  const pendingList = adminOrdersCache.filter((o) =>
    ["pending", "pendingpayment"].includes(String(o.status || "").toLowerCase())
  );
  const completedList = adminOrdersCache.filter((o) =>
    ["completed", "delivered", "paid"].includes(String(o.status || "").toLowerCase())
  );
  const completedRevenue = completedList.reduce((sum, o) => sum + Number(o.totalAmount || 0), 0);

  animateCounter(document.getElementById("sideTotalWines"), state.wines.length);
  animateCounter(document.getElementById("sideTotalOrders"), adminOrdersCache.length);
  animateCounter(document.getElementById("sideLowStock"), lowCount);
  animateCounter(document.getElementById("kpiPendingOrders"), pendingList.length);
  animateCounter(document.getElementById("kpiCompletedOrders"), completedList.length);

  document.getElementById("kpiCompletedRevenue").textContent = formatPHP(completedRevenue);
  document.getElementById("kpiLowStockBadge").textContent = `${lowCount} alerts`;
}

function renderPendingOrdersMini() {
  const box = document.getElementById("pendingOrdersMiniList");
  const pending = adminOrdersCache.filter((o) =>
    ["pending", "pendingpayment"].includes(String(o.status || "").toLowerCase())
  );

  if (!pending.length) {
    box.innerHTML = `<div class="mini-feed-row"><small>No pending orders right now.</small></div>`;
    return;
  }

  box.innerHTML = "";
  pending.slice(0, 8).forEach((o) => {
    const row = document.createElement("div");
    row.className = "mini-feed-row";
    row.innerHTML = `
      <div>
        <strong>#${o.orderId} &bull; ${escapeHtml(o.customerName || "Customer")}</strong>
        <small>${formatDate(o.orderDate)} &bull; ${formatStatusLabel(o.status)}</small>
      </div>
      <strong>${formatPHP(o.totalAmount)}</strong>
    `;
    box.appendChild(row);
  });
}

function renderRecentOrdersMini() {
  const box = document.getElementById("adminRecentOrdersFeed");
  const top10 = adminOrdersCache.slice(0, 10);
  if (!top10.length) {
    box.innerHTML = `<div class="mini-feed-row"><small>No orders yet.</small></div>`;
    return;
  }

  box.innerHTML = "";
  top10.forEach((o) => {
    const cls = `status-${String(o.status || "pending").toLowerCase()}`;
    const row = document.createElement("div");
    row.className = "mini-feed-row";
    row.style.cursor = "pointer";
    row.innerHTML = `
      <div>
        <strong>#${o.orderId} ${escapeHtml(o.customerName || "Customer")}</strong>
        <small>${formatDate(o.orderDate)} &bull; ${formatPHP(o.totalAmount)}</small>
      </div>
      <span class="status-badge ${cls}">${escapeHtml(formatStatusLabel(o.status))}</span>
    `;
    row.addEventListener("click", () => openOrderDetailModal(o, true));
    box.appendChild(row);
  });
}

function renderLowStockPanel() {
  const box = document.getElementById("lowStockList");
  const lowItems = state.wines.filter((w) => w.stockQuantity < 10);
  if (!lowItems.length) {
    box.innerHTML = `<div class="mini-feed-row"><small>All wines have 10+ bottles in stock.</small></div>`;
    return;
  }

  box.innerHTML = "";
  lowItems.forEach((w) => {
    const row = document.createElement("div");
    row.className = "mini-feed-row";
    row.innerHTML = `
      <div>
        <strong>${escapeHtml(w.wineName)}</strong>
        <small style="color:#fca5a5;">${w.stockQuantity === 0 ? "Out of stock" : `Only ${w.stockQuantity} left`}</small>
      </div>
      <button class="btn-sand btn-xs restock-btn" type="button">Restock</button>
    `;
    row.querySelector(".restock-btn").addEventListener("click", () => openWineFormModal(w));
    box.appendChild(row);
  });
}

/* WINERY-THEMED ANALYTICS CHART (Embedded inside Crimson Inventory Summary) */
function renderThemedAnalyticsChart() {
  if (typeof window.Chart === "undefined") return;
  const canvas = document.getElementById("wineryAnalyticsChart");
  const ctx = canvas.getContext("2d");
  const subtitle = document.getElementById("chartActiveSubtitle");

  let labels = [];
  let values = [];
  let datasetLabel = "";

  if (activeChartMode === "stock") {
    subtitle.textContent = "Cellar Stock Breakdown by Category";
    datasetLabel = "Bottles in Cellar";
    const catMap = {};
    state.wines.forEach((w) => {
      const k = w.category || "Other";
      catMap[k] = (catMap[k] || 0) + (w.stockQuantity || 0);
    });
    labels = Object.keys(catMap);
    values = Object.values(catMap);
  } else {
    subtitle.textContent = "OLAP Warehouse Revenue Trend (₱)";
    datasetLabel = "Revenue (₱)";
    const rows = Array.isArray(analyticsCache)
      ? analyticsCache
      : analyticsCache?.salesByWine || analyticsCache?.monthlySales || analyticsCache?.data || [];

    if (Array.isArray(rows) && rows.length) {
      labels = rows.map((r) => r.wineName || r.period || r.month || r.category || "Item");
      values = rows.map((r) => Number(r.totalRevenue ?? r.revenue ?? r.totalSales ?? 0));
    } else {
      const byDate = {};
      adminOrdersCache.forEach((o) => {
        const d = formatDate(o.orderDate);
        byDate[d] = (byDate[d] || 0) + Number(o.totalAmount || 0);
      });
      labels = Object.keys(byDate).reverse();
      values = Object.values(byDate).reverse();
    }
  }

  const grad = ctx.createLinearGradient(0, 0, 0, 240);
  grad.addColorStop(0, "#E3CEB4");
  grad.addColorStop(1, "#8F745B");

  if (chartInstance) chartInstance.destroy();
  chartInstance = new window.Chart(ctx, {
    type: "bar",
    data: {
      labels: labels.length ? labels : ["No Data"],
      datasets: [{
        label: datasetLabel,
        data: values.length ? values : [0],
        backgroundColor: grad,
        hoverBackgroundColor: "#FFFCF2",
        borderColor: "#F2F4F3",
        borderWidth: 1,
        borderRadius: 4,
        maxBarThickness: 48,
      }],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      animation: { duration: 650, easing: "easeOutQuart" },
      plugins: {
        legend: { display: false },
        tooltip: {
          backgroundColor: "#252422",
          titleColor: "#A9927D",
          bodyColor: "#FFFCF2",
          borderColor: "#A9927D",
          borderWidth: 1,
          titleFont: { family: "Cormorant Garamond", size: 14, weight: "bold" },
          bodyFont: { family: "DM Sans", size: 12 },
          padding: 10,
        },
      },
      scales: {
        x: {
          grid: { display: false },
          ticks: { color: "#F2F4F3", font: { family: "Cormorant Garamond", size: 13 } },
          border: { color: "rgba(169,146,125,0.35)" },
        },
        y: {
          beginAtZero: true,
          grid: { color: "rgba(242,244,243,0.12)", tickLength: 0 },
          ticks: { color: "#D6C5B3", font: { family: "DM Sans", size: 11 } },
          border: { display: false },
        },
      },
    },
  });
}

/* INVENTORY MANAGEMENT */
function populateInventoryCategories() {
  const select = document.getElementById("invCategoryFilter");
  const cur = select.value;
  const cats = ["All", ...new Set(state.wines.map((w) => w.category).filter(Boolean))];
  select.innerHTML = cats.map((c) => `<option value="${escapeHtml(c)}">${c === "All" ? "All Categories" : escapeHtml(c)}</option>`).join("");
  if (cats.includes(cur)) select.value = cur;
}

function renderInventoryTable() {
  const q = document.getElementById("invSearchInput").value.trim().toLowerCase();
  const cat = document.getElementById("invCategoryFilter").value;
  const tbody = document.getElementById("adminInventoryTbody");

  const list = state.wines.filter((w) => {
    if (cat !== "All" && w.category !== cat) return false;
    if (q && !w.wineName.toLowerCase().includes(q)) return false;
    return true;
  });

  if (!list.length) {
    tbody.innerHTML = `<tr><td colspan="6" style="opacity:0.75;">No wines match your filter.</td></tr>`;
    return;
  }

  tbody.innerHTML = "";
  list.forEach((w) => {
    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td><div class="inv-thumb">${renderWineVisual(w, 44)}</div></td>
      <td>
        <strong>${escapeHtml(w.wineName)}</strong>
        <div style="font-size:0.76rem;color:#D8C7B8;max-width:280px;">${escapeHtml(w.description || "—")}</div>
      </td>
      <td>${escapeHtml(w.category || "—")}</td>
      <td><strong>${formatPHP(w.price)}</strong></td>
      <td><span class="stock-badge ${w.stockQuantity === 0 ? "stock-out" : w.stockQuantity < 10 ? "stock-low" : "stock-ok"}">${w.stockQuantity}</span></td>
      <td class="action-btn-group">
        <button class="btn-sand btn-xs edit-w-btn" type="button">Edit / Restock</button>
        <button class="btn-danger btn-xs del-w-btn" type="button">Delete</button>
      </td>
    `;

    tr.querySelector(".edit-w-btn").addEventListener("click", () => openWineFormModal(w));
    tr.querySelector(".del-w-btn").addEventListener("click", async () => {
      if (!confirm(`Delete "${w.wineName}" permanently?`)) return;
      try {
        await apiRequest("DELETE", `/api/Admin/wines/${w.productId}`);
        showToast("Wine deleted");
        loadAdminData("admin-inventory");
      } catch (err) {
        showToast(err.message, true);
      }
    });
    tbody.appendChild(tr);
  });
}

function openWineFormModal(wine) {
  editingWineId = wine ? wine.productId : null;
  document.getElementById("adminWineModalTitle").textContent = wine ? `Edit / Restock: ${wine.wineName}` : "Add New Wine";
  document.getElementById("awName").value = wine?.wineName || "";
  document.getElementById("awCategory").value = wine?.category || "";
  document.getElementById("awPrice").value = wine?.price ?? "";
  document.getElementById("awStock").value = wine?.stockQuantity ?? "";
  document.getElementById("awDesc").value = wine?.description || "";
  document.getElementById("awImageUrl").value = wine ? (state.wineImages[wine.productId] || "") : "";
  document.getElementById("adminWineModal").classList.remove("hidden");
}

async function handleSaveWine(e) {
  e.preventDefault();
  const dto = {
    wineName: document.getElementById("awName").value.trim(),
    category: document.getElementById("awCategory").value.trim(),
    price: parseFloat(document.getElementById("awPrice").value) || 0,
    stockQuantity: parseInt(document.getElementById("awStock").value, 10) || 0,
    description: document.getElementById("awDesc").value.trim() || null,
  };
  const imgUrl = document.getElementById("awImageUrl").value.trim();

  try {
    let saved;
    if (editingWineId) {
      saved = await apiRequest("PUT", `/api/Admin/wines/${editingWineId}`, dto);
      saveWineImage(editingWineId, imgUrl);
      showToast("Wine updated successfully.");
    } else {
      saved = await apiRequest("POST", "/api/Admin/wines", dto);
      if (saved?.productId && imgUrl) saveWineImage(saved.productId, imgUrl);
      showToast("New wine added to cellar.");
    }
    document.getElementById("adminWineModal").classList.add("hidden");
    const activeTab = document.querySelector(".admin-nav-btn.active")?.dataset.route || "admin-inventory";
    loadAdminData(activeTab);
  } catch (err) {
    showToast(err.message || "Could not save wine", true);
  }
}

/* ORDER MANAGEMENT */
function renderAdminOrdersTable() {
  const q = document.getElementById("ordSearchInput").value.trim().toLowerCase();
  const st = document.getElementById("ordStatusFilter").value;
  const tbody = document.getElementById("adminOrdersTbody");

  const list = adminOrdersCache.filter((o) => {
    if (st !== "All" && String(o.status).toLowerCase() !== st.toLowerCase()) return false;
    if (q && !`${o.orderId} ${o.customerName} ${o.customerEmail}`.toLowerCase().includes(q)) return false;
    return true;
  });

  if (!list.length) {
    tbody.innerHTML = `<tr><td colspan="7" style="opacity:0.75;">No orders match your filter.</td></tr>`;
    return;
  }

  tbody.innerHTML = "";
  list.forEach((o) => {
    const items = o.items || o.orderItems || [];
    const summary = items.length
      ? items.slice(0, 2).map((i) => `${i.quantity}x ${i.wineName || `Wine #${i.productId}`}`).join(", ") +
        (items.length > 2 ? ` +${items.length - 2} more` : "")
      : "—";

    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td><strong>#${o.orderId}</strong></td>
      <td>
        <strong>${escapeHtml(o.customerName || "Customer")}</strong>
        <div style="font-size:0.74rem;color:#D8C7B8;">${escapeHtml(o.customerEmail || "")}</div>
      </td>
      <td>${formatDate(o.orderDate)}</td>
      <td style="font-size:0.8rem;">${escapeHtml(summary)}</td>
      <td><strong>${formatPHP(o.totalAmount)}</strong></td>
      <td>
        <select class="inline-status-select">
          ${["Pending", "PendingPayment", "Processing", "Completed", "Cancelled"]
            .map((s) => `<option value="${s}" ${String(o.status).toLowerCase() === s.toLowerCase() ? "selected" : ""}>${formatStatusLabel(s)}</option>`)
            .join("")}
        </select>
      </td>
      <td class="action-btn-group">
        <button class="btn-sand btn-xs view-d-btn" type="button">Details</button>
        <button class="btn-danger btn-xs cancel-d-btn" type="button">Cancel</button>
      </td>
    `;

    tr.querySelector(".inline-status-select").addEventListener("change", async (e) => {
      const next = e.target.value;
      try {
        await apiRequest("PUT", `/api/v1/Orders/${o.orderId}/status`, next);
        o.status = next;
        updateAdminKPIs();
        showToast(`Order #${o.orderId} updated to ${formatStatusLabel(next)}`);
      } catch (err) {
        showToast(err.message, true);
        renderAdminOrdersTable();
      }
    });

    tr.querySelector(".view-d-btn").addEventListener("click", () => openOrderDetailModal(o, true));
    tr.querySelector(".cancel-d-btn").addEventListener("click", async () => {
      if (!confirm(`Cancel Order #${o.orderId}?`)) return;
      try {
        await apiRequest("PUT", `/api/v1/Orders/${o.orderId}/status`, "Cancelled");
        showToast(`Order #${o.orderId} cancelled.`);
        loadAdminData("admin-orders");
      } catch (err) {
        showToast(err.message, true);
      }
    });

    tbody.appendChild(tr);
  });
}