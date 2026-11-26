const STORAGE = {
  token: "ovc_jwt",
  user: "ovc_user",
  target: "ovc_api_target",
  localCart: "ovc_local_cart",
  wineImages: "ovc_wine_images",
};

export const state = {
  token: localStorage.getItem(STORAGE.token) || "",
  user: JSON.parse(localStorage.getItem(STORAGE.user) || "null"),
  apiTarget: localStorage.getItem(STORAGE.target) || "http://localhost:5183",
  wines: [],
  cart: { items: [], subTotal: 0 },
  useLocalCartFallback: false,
  wineImages: JSON.parse(localStorage.getItem(STORAGE.wineImages) || "{}"),
};

export function setAuth(data) {
  if (data?.token) {
    state.token = data.token;
    localStorage.setItem(STORAGE.token, data.token);
  }
  state.user = {
    userId: data.userId ?? state.user?.userId ?? null,
    fullName: data.fullName ?? state.user?.fullName ?? "",
    email: data.email ?? state.user?.email ?? "",
    role: data.role ?? state.user?.role ?? "Customer",
    phone: data.phone ?? state.user?.phone ?? "",
    address: data.address ?? state.user?.address ?? "",
  };
  localStorage.setItem(STORAGE.user, JSON.stringify(state.user));
}

export function updateStoredUser(partial) {
  state.user = { ...(state.user || {}), ...partial };
  localStorage.setItem(STORAGE.user, JSON.stringify(state.user));
}

export function clearAuth() {
  state.token = "";
  state.user = null;
  state.cart = { items: [], subTotal: 0 };
  localStorage.removeItem(STORAGE.token);
  localStorage.removeItem(STORAGE.user);
}

export function isAdmin() {
  return String(state.user?.role || "").toLowerCase() === "admin";
}

export function saveWineImage(productId, url) {
  if (!url) delete state.wineImages[productId];
  else state.wineImages[productId] = url;
  localStorage.setItem(STORAGE.wineImages, JSON.stringify(state.wineImages));
}

export function saveLocalCart() {
  localStorage.setItem(STORAGE.localCart, JSON.stringify(state.cart));
}

export function loadLocalCart() {
  try {
    return JSON.parse(localStorage.getItem(STORAGE.localCart) || '{"items":[],"subTotal":0}');
  } catch {
    return { items: [], subTotal: 0 };
  }
}

export function escapeHtml(str) {
  return String(str ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

export function formatPHP(value) {
  return `₱${Number(value || 0).toLocaleString("en-PH", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`;
}

export function formatDate(iso) {
  if (!iso) return "—";
  return new Date(iso).toLocaleDateString("en-PH", {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

export function showToast(message, isError = false) {
  const root = document.getElementById("toastRoot");
  const el = document.createElement("div");
  el.className = `toast ${isError ? "error" : ""}`;
  el.textContent = message;
  root.appendChild(el);
  setTimeout(() => el.remove(), 3500);
}

export async function apiRequest(method, path, body) {
  const headers = { "X-Api-Target": state.apiTarget };
  if (state.token) headers["Authorization"] = `Bearer ${state.token}`;

  let payload;
  if (body !== undefined && method !== "GET" && method !== "DELETE") {
    headers["Content-Type"] = "application/json";
    payload = JSON.stringify(body);
  }

  const res = await fetch(path, { method, headers, body: payload });
  const text = await res.text();
  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch { data = text; }

  if (!res.ok) {
    const msg =
      (typeof data === "object" && (data?.message || data?.title || data?.error)) ||
      (typeof data === "string" && data.split("\n")[0].slice(0, 140)) ||
      `Request failed (${res.status})`;
    const err = new Error(msg);
    err.status = res.status;
    throw err;
  }
  return data;
}

export function bottleSvg(category = "", size = 140) {
  const lower = String(category).toLowerCase();
  let glass = "#360c14";
  let foil = "#8c1d2e";
  if (lower.includes("white")) { glass = "#4a5332"; foil = "#c8a96e"; }
  else if (lower.includes("ros")) { glass = "#8e4050"; foil = "#d99b9b"; }
  else if (lower.includes("spark") || lower.includes("champ")) { glass = "#243b2c"; foil = "#d4af37"; }

  return `
    <svg viewBox="0 0 80 190" height="${size}" role="img" aria-label="Wine bottle">
      <rect x="31" y="6" width="18" height="24" rx="2" fill="${foil}" />
      <rect x="31" y="28" width="18" height="28" fill="${glass}" />
      <path d="M31 56 C18 66, 16 78, 16 96 L16 178 C16 183, 20 186, 25 186 L55 186 C60 186, 64 183, 64 178 L64 96 C64 78, 62 66, 49 56 Z" fill="${glass}" />
      <rect x="21" y="96" width="38" height="54" rx="3" fill="#FFFCF2" stroke="#A9927D" stroke-width="1.2" />
      <text x="40" y="117" text-anchor="middle" font-family="Georgia, serif" font-size="8" fill="#49111C" font-weight="bold">OAK VINE</text>
      <line x1="27" y1="124" x2="53" y2="124" stroke="#A9927D" stroke-width="0.9" />
      <text x="40" y="136" text-anchor="middle" font-family="Georgia, serif" font-size="6.5" fill="#5E503F">CELLAR</text>
      <path d="M23 76 L23 176" stroke="rgba(255,255,255,0.22)" stroke-width="3" stroke-linecap="round" />
    </svg>
  `;
}

export function renderWineVisual(wine, size = 145) {
  const customUrl = wine.imageUrl || state.wineImages[wine.productId];
  if (customUrl) {
    return `<img src="${escapeHtml(customUrl)}" alt="${escapeHtml(wine.wineName)}" onerror="this.remove()" />`;
  }
  return bottleSvg(wine.category, size);
}