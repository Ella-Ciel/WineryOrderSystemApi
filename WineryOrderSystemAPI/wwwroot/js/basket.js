import {
  state,
  apiRequest,
  saveLocalCart,
  loadLocalCart,
  escapeHtml,
  formatPHP,
  showToast,
  renderWineVisual,
} from "./api.js";

const PAYMENT_CHANNELS = {
  GCASH: "You will be redirected to Xendit to authorize your GCash mobile number and MPIN.",
  MAYA: "Pay securely via Maya Wallet using your registered mobile number and OTP on Xendit.",
  CARD: "Supports Visa and Mastercard credit/debit cards with 3D Secure verification via Xendit.",
  BPI: "Direct online bank transfer via BPI Online login on the Xendit checkout page.",
  UNIONBANK: "Instant fund transfer using your UnionBank Online credentials via Xendit.",
};

export function initBasket({ onNavigate }) {
  document.getElementById("proceedCheckoutBtn").addEventListener("click", () => {
    if (!state.cart.items.length) return;
    onNavigate("checkout");
  });

  document.getElementById("clearBasketBtn").addEventListener("click", clearBasket);

  const radios = document.querySelectorAll('input[name="paymentMethod"]');
  const infoBox = document.getElementById("paymentChannelDetails");
  const syncChannel = () => {
    radios.forEach((r) => r.closest(".payment-option").classList.toggle("selected", r.checked));
    const active = document.querySelector('input[name="paymentMethod"]:checked')?.value || "GCASH";
    infoBox.textContent = PAYMENT_CHANNELS[active] || "";
  };
  radios.forEach((r) => r.addEventListener("change", syncChannel));
  syncChannel();

  document.getElementById("checkoutForm").addEventListener("submit", (e) => handleCheckoutSubmit(e, onNavigate));
}

export async function loadBasket() {
  if (!state.user?.userId) {
    state.cart = { items: [], subTotal: 0 };
    updateBasketBadges();
    return;
  }

  try {
    const data = await apiRequest("GET", `/api/v1/Cart/${state.user.userId}`);
    state.cart = data?.items ? data : { items: [], subTotal: 0 };
    state.useLocalCartFallback = false;
  } catch {
    state.useLocalCartFallback = true;
    state.cart = loadLocalCart();
  }
  updateBasketBadges();
  renderBasketPage();
}

export async function addToBasket(wine, quantity) {
  const maxStock = wine.stockQuantity ?? 99;
  const existing = (state.cart.items || []).find((i) => i.productId === wine.productId);
  if ((existing?.quantity || 0) + quantity > maxStock) {
    return showToast(`Cannot exceed available stock (${maxStock} bottles).`, true);
  }

  if (!state.useLocalCartFallback && state.user?.userId) {
    try {
      const updated = await apiRequest("POST", `/api/v1/Cart/${state.user.userId}/items`, {
        productId: wine.productId,
        quantity,
      });
      if (updated?.items) state.cart = updated;
      else await loadBasket();
      updateBasketBadges();
      renderBasketPage();
      return showToast(`Added ${quantity}× ${wine.wineName} to basket`);
    } catch {
      state.useLocalCartFallback = true;
    }
  }

  // Fallback if Cart table is not yet created in DB
  if (existing) existing.quantity += quantity;
  else {
    state.cart.items.push({
      cartItemId: Date.now(),
      productId: wine.productId,
      wineName: wine.wineName,
      unitPrice: wine.price,
      quantity,
    });
  }
  recalcLocalCart();
  showToast(`Added ${quantity}× ${wine.wineName} to basket`);
}

function recalcLocalCart() {
  state.cart.subTotal = state.cart.items.reduce((s, i) => s + i.unitPrice * i.quantity, 0);
  saveLocalCart();
  updateBasketBadges();
  renderBasketPage();
}

export function updateBasketBadges() {
  const total = (state.cart.items || []).reduce((s, i) => s + (i.quantity || 0), 0);
  const badge = document.getElementById("navBasketBadge");
  if (badge) badge.textContent = total;
  const mobileBadge = document.getElementById("navBasketBadgeMobile");
  if (mobileBadge) mobileBadge.textContent = total;
}

export function renderBasketPage() {
  const list = document.getElementById("basketItemsList");
  const items = state.cart.items || [];
  const totalCount = items.reduce((s, i) => s + i.quantity, 0);
  const grandTotal = items.reduce((s, i) => s + (i.lineTotal ?? i.unitPrice * i.quantity), 0);

  document.getElementById("basketTotalItems").textContent = totalCount;
  document.getElementById("basketGrandTotal").textContent = formatPHP(grandTotal);
  document.getElementById("proceedCheckoutBtn").disabled = items.length === 0;

  if (!items.length) {
    list.innerHTML = `<div class="empty-box">Your basket is currently empty. Browse the cellar to add fine wines.</div>`;
    return;
  }

  list.innerHTML = "";
  items.forEach((item) => {
    const wineRef = state.wines.find((w) => w.productId === item.productId) || {
      productId: item.productId,
      wineName: item.wineName,
      stockQuantity: 99,
    };
    const maxStock = wineRef.stockQuantity ?? 99;
    const lineSub = item.lineTotal ?? item.unitPrice * item.quantity;

    const card = document.createElement("div");
    card.className = "card-light basket-item-card";
    card.innerHTML = `
      <div class="basket-thumb">${renderWineVisual(wineRef, 72)}</div>
      <div>
        <h3 class="serif-heading">${escapeHtml(item.wineName || wineRef.wineName)}</h3>
        <p class="muted-text">${formatPHP(item.unitPrice)} each • Max stock: ${maxStock}</p>
        <div class="qty-stepper mt-8">
          <button class="step-dec" type="button">−</button>
          <span>${item.quantity}</span>
          <button class="step-inc" type="button" ${item.quantity >= maxStock ? "disabled" : ""}>+</button>
        </div>
      </div>
      <div class="basket-item-right">
        <strong class="wine-card-price">${formatPHP(lineSub)}</strong>
        <button class="btn-outline-dark btn-xs remove-item" type="button">Remove</button>
      </div>
    `;

    card.querySelector(".step-dec").addEventListener("click", () => updateItemQty(item, item.quantity - 1));
    card.querySelector(".step-inc").addEventListener("click", () => {
      if (item.quantity < maxStock) updateItemQty(item, item.quantity + 1);
    });
    card.querySelector(".remove-item").addEventListener("click", () => removeItem(item));
    list.appendChild(card);
  });
}

async function updateItemQty(item, nextQty) {
  if (nextQty <= 0) return removeItem(item);
  if (!state.useLocalCartFallback) {
    try {
      const updated = await apiRequest("PUT", `/api/v1/Cart/items/${item.cartItemId}`, { quantity: nextQty });
      if (updated?.items) state.cart = updated;
      else await loadBasket();
      updateBasketBadges();
      return renderBasketPage();
    } catch {
      state.useLocalCartFallback = true;
    }
  }
  item.quantity = nextQty;
  recalcLocalCart();
}

async function removeItem(item) {
  if (!state.useLocalCartFallback) {
    try {
      await apiRequest("DELETE", `/api/v1/Cart/items/${item.cartItemId}`);
      return await loadBasket();
    } catch {
      state.useLocalCartFallback = true;
    }
  }
  state.cart.items = state.cart.items.filter((i) => i.cartItemId !== item.cartItemId);
  recalcLocalCart();
}

async function clearBasket() {
  if (!state.useLocalCartFallback && state.user?.userId) {
    try {
      await apiRequest("DELETE", `/api/v1/Cart/${state.user.userId}/clear`);
    } catch { /* ignore */ }
  }
  state.cart = { items: [], subTotal: 0 };
  recalcLocalCart();
  showToast("Basket cleared");
}

export function renderCheckoutPage() {
  document.getElementById("chkFullName").value = state.user?.fullName || "";
  document.getElementById("chkEmail").value = state.user?.email || "";
  document.getElementById("chkPhone").value = state.user?.phone || "";
  document.getElementById("chkAddress").value = state.user?.address || "";

  const items = state.cart.items || [];
  const grandTotal = items.reduce((s, i) => s + (i.lineTotal ?? i.unitPrice * i.quantity), 0);
  document.getElementById("checkoutGrandTotal").textContent = formatPHP(grandTotal);

  const box = document.getElementById("checkoutSummaryItems");
  box.innerHTML = items
    .map((i) => {
      const sub = i.lineTotal ?? i.unitPrice * i.quantity;
      return `<div class="checkout-item-row"><span>${escapeHtml(i.wineName)} × ${i.quantity}</span><strong>${formatPHP(sub)}</strong></div>`;
    })
    .join("");
}

async function handleCheckoutSubmit(e, onNavigate) {
  e.preventDefault();
  const items = state.cart.items || [];
  if (!items.length) return showToast("Your basket is empty.", true);

  const btn = document.getElementById("placeOrderPayBtn");
  btn.disabled = true;
  btn.textContent = "Creating Order & Xendit Invoice...";

  try {
    // 1. Place order via checkout endpoint
    const order = await apiRequest("POST", "/api/v1/Orders/checkout", {
      userId: state.user.userId,
      items: items.map((i) => ({ productId: i.productId, quantity: i.quantity })),
    });

    // Clear cart locally & remotely
    state.cart = { items: [], subTotal: 0 };
    saveLocalCart();
    updateBasketBadges();

    // 2. Generate Xendit Invoice
    try {
      const invRes = await apiRequest("POST", `/api/v1/Payments/create-invoice/${order.orderId}`);
      const urlMatch = JSON.stringify(invRes || "").match(/https?:\/\/[^\s"\\]+/);
      if (urlMatch) {
        showToast(`Order #${order.orderId} created! Redirecting to Xendit...`);
        window.open(urlMatch[0], "_blank", "noopener");
      } else {
        showToast(`Order #${order.orderId} placed! Check Orders for payment status.`);
      }
    } catch {
      showToast(`Order #${order.orderId} placed. Xendit invoice can be retried from Orders.`);
    }

    onNavigate("orders");
  } catch (err) {
    showToast(err.message || "Checkout failed", true);
  } finally {
    btn.disabled = false;
    btn.textContent = "Proceed to Payment (Xendit)";
  }
}