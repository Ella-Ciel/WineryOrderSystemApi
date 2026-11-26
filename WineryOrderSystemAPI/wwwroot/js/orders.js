import { state, apiRequest, updateStoredUser, escapeHtml, formatPHP, formatDate, showToast } from "./api.js";

export function initOrdersAndProfile() {
  document.getElementById("refreshUserOrdersBtn").addEventListener("click", loadUserOrders);

  document.getElementById("profileForm").addEventListener("submit", async (e) => {
    e.preventDefault();
    const payload = {
      fullName: document.getElementById("profFullName").value.trim(),
      phone: document.getElementById("profPhone").value.trim() || null,
      address: document.getElementById("profAddress").value.trim() || null,
    };

    try {
      await apiRequest("PUT", `/api/Users/${state.user.userId}/details`, payload);
      updateStoredUser(payload);
      showToast("Profile updated successfully.");
    } catch (err) {
      showToast(err.message || "Could not update profile", true);
    }
  });
}

export function renderProfilePage() {
  document.getElementById("profEmail").value = state.user?.email || "";
  document.getElementById("profRole").value = state.user?.role || "Customer";
  document.getElementById("profFullName").value = state.user?.fullName || "";
  document.getElementById("profPhone").value = state.user?.phone || "";
  document.getElementById("profAddress").value = state.user?.address || "";
}

export async function loadUserOrders() {
  const tbody = document.getElementById("userOrdersTbody");
  if (!state.user?.userId) return;

  tbody.innerHTML = `<tr><td colspan="5" class="muted-text">Loading orders...</td></tr>`;
  try {
    const orders = await apiRequest("GET", `/api/v1/Orders/user/${state.user.userId}`);
    const sorted = (Array.isArray(orders) ? orders : []).sort(
      (a, b) => new Date(b.orderDate) - new Date(a.orderDate)
    );

    if (!sorted.length) {
      tbody.innerHTML = `<tr><td colspan="5" class="muted-text">You have not placed any orders yet.</td></tr>`;
      return;
    }

    tbody.innerHTML = "";
    sorted.forEach((order) => {
      const isPending = String(order.status || "").toLowerCase() === "pending";
      const statusCls = `status-${String(order.status || "pending").toLowerCase()}`;
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td><strong>#${order.orderId}</strong></td>
        <td>${formatDate(order.orderDate)}</td>
        <td><strong>${formatPHP(order.totalAmount)}</strong></td>
        <td><span class="status-badge ${statusCls}">${escapeHtml(order.status)}</span></td>
        <td class="action-btn-group">
          <button class="btn-outline-dark btn-xs view-ord-btn" type="button">View Items</button>
          ${isPending ? `<button class="btn-danger btn-xs cancel-ord-btn" type="button">Cancel Order</button>` : ""}
        </td>
      `;

      tr.querySelector(".view-ord-btn").addEventListener("click", () => openOrderDetailModal(order));
      const cancelBtn = tr.querySelector(".cancel-ord-btn");
      if (cancelBtn) {
        cancelBtn.addEventListener("click", async () => {
          if (!confirm(`Cancel Order #${order.orderId}? Stock will be restored.`)) return;
          try {
            await apiRequest("PUT", `/api/v1/Orders/${order.orderId}/status`, "Cancelled");
            showToast(`Order #${order.orderId} cancelled.`);
            loadUserOrders();
          } catch (err) {
            showToast(err.message || "Could not cancel order", true);
          }
        });
      }
      tbody.appendChild(tr);
    });
  } catch (err) {
    tbody.innerHTML = `<tr><td colspan="5" class="muted-text">Error: ${escapeHtml(err.message)}</td></tr>`;
  }
}

export function openOrderDetailModal(order, includeCustomerInfo = false) {
  const modal = document.getElementById("orderDetailModal");
  const body = document.getElementById("orderDetailBody");
  const items = order.items || order.orderItems || [];

  const customerBlock = includeCustomerInfo
    ? `<div class="channel-info-box mb-16">
         <strong>Customer:</strong> ${escapeHtml(order.customerName || "—")} (${escapeHtml(order.customerEmail || "—")})<br/>
         <strong>Delivery Address:</strong> ${escapeHtml(order.customerAddress || order.address || "On file")}
       </div>`
    : "";

  const rows = items
    .map((i) => {
      const sub = i.lineTotal ?? i.unitPrice * i.quantity;
      return `<tr>
        <td>${escapeHtml(i.wineName || `Wine #${i.productId}`)}</td>
        <td>${i.quantity}</td>
        <td>${formatPHP(i.unitPrice)}</td>
        <td><strong>${formatPHP(sub)}</strong></td>
      </tr>`;
    })
    .join("");

  body.innerHTML = `
    <h2 class="serif-heading mb-12">Order #${order.orderId}</h2>
    <p class="muted-text mb-12">Placed on ${formatDate(order.orderDate)} • Status: <strong>${escapeHtml(order.status)}</strong></p>
    ${customerBlock}
    <div class="table-responsive">
      <table class="data-table">
        <thead><tr><th>Wine</th><th>Qty</th><th>Unit Price</th><th>Subtotal</th></tr></thead>
        <tbody>${rows || `<tr><td colspan="4" class="muted-text">No line items available.</td></tr>`}</tbody>
      </table>
    </div>
    <div style="display:flex;justify-content:flex-end;margin-top:16px;font-size:1.1rem;">
      <strong>Grand Total: <span style="color:var(--crimson);">${formatPHP(order.totalAmount)}</span></strong>
    </div>
  `;

  modal.classList.remove("hidden");
}