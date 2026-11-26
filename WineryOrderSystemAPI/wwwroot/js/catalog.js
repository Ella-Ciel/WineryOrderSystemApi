import { state, apiRequest, escapeHtml, formatPHP, bottleSvg, renderWineVisual } from "./api.js";
import { addToBasket } from "./basket.js";

export function initCatalog({ onNavigate }) {
  document.getElementById("heroBottleArt").innerHTML = bottleSvg("Red Wine", 300);

  const rerender = () => renderCatalogGrid();
  ["catSearchInput", "catMinPrice", "catMaxPrice"].forEach((id) =>
    document.getElementById(id).addEventListener("input", rerender)
  );
  ["catCategorySelect", "catAvailSelect", "catSortSelect"].forEach((id) =>
    document.getElementById(id).addEventListener("change", rerender)
  );

  document.getElementById("catResetBtn").addEventListener("click", () => {
    document.getElementById("catSearchInput").value = "";
    document.getElementById("catCategorySelect").value = "All";
    document.getElementById("catMinPrice").value = "";
    document.getElementById("catMaxPrice").value = "";
    document.getElementById("catAvailSelect").value = "all";
    document.getElementById("catSortSelect").value = "default";
    renderCatalogGrid();
  });

  document.getElementById("navQuickSearch").addEventListener("keydown", (e) => {
    if (e.key === "Enter") {
      document.getElementById("catSearchInput").value = e.target.value;
      onNavigate("wines");
    }
  });
}

export async function loadWinesData() {
  try {
    const wines = await apiRequest("GET", "/api/v1/Wines");
    state.wines = Array.isArray(wines) ? wines : [];
  } catch {
    state.wines = [];
  }
  populateCategoryDropdowns();
  renderHomeSections();
  renderCatalogGrid();
}

function populateCategoryDropdowns() {
  const categories = ["All", ...new Set(state.wines.map((w) => w.category).filter(Boolean))];
  const select = document.getElementById("catCategorySelect");
  const current = select.value;
  select.innerHTML = categories
    .map((c) => `<option value="${escapeHtml(c)}">${c === "All" ? "All Categories" : escapeHtml(c)}</option>`)
    .join("");
  if (categories.includes(current)) select.value = current;
}

export function renderHomeSections() {
  const highlights = document.getElementById("heroHighlights");
  highlights.innerHTML = state.wines
    .slice(0, 3)
    .map((w) => `<span class="highlight-chip"><strong>${escapeHtml(w.wineName)}</strong> &bull; ${formatPHP(w.price)}</span>`)
    .join("");

  const grid = document.getElementById("bestSellersGrid");
  const topFour = [...state.wines].sort((a, b) => b.price - a.price).slice(0, 4);

  if (!topFour.length) {
    grid.innerHTML = `<div class="empty-box" style="grid-column:1/-1;">No wines available in the cellar yet.</div>`;
    return;
  }

  grid.innerHTML = "";
  topFour.forEach((wine, idx) => grid.appendChild(createScrapbookCard(wine, "Order Now", idx)));
}

export function renderCatalogGrid() {
  const q = document.getElementById("catSearchInput").value.trim().toLowerCase();
  const cat = document.getElementById("catCategorySelect").value;
  const minP = parseFloat(document.getElementById("catMinPrice").value);
  const maxP = parseFloat(document.getElementById("catMaxPrice").value);
  const avail = document.getElementById("catAvailSelect").value;
  const sort = document.getElementById("catSortSelect").value;

  let list = state.wines.filter((w) => {
    if (cat !== "All" && w.category !== cat) return false;
    if (q && !`${w.wineName} ${w.description} ${w.category}`.toLowerCase().includes(q)) return false;
    if (!Number.isNaN(minP) && w.price < minP) return false;
    if (!Number.isNaN(maxP) && w.price > maxP) return false;
    if (avail === "in" && w.stockQuantity <= 0) return false;
    if (avail === "low" && (w.stockQuantity <= 0 || w.stockQuantity >= 10)) return false;
    if (avail === "out" && w.stockQuantity > 0) return false;
    return true;
  });

  if (sort === "price-asc") list.sort((a, b) => a.price - b.price);
  else if (sort === "price-desc") list.sort((a, b) => b.price - a.price);
  else if (sort === "name-asc") list.sort((a, b) => a.wineName.localeCompare(b.wineName));

  document.getElementById("catalogResultCount").textContent = `Showing ${list.length} of ${state.wines.length} bottles`;

  const grid = document.getElementById("catalogGrid");
  if (!list.length) {
    grid.innerHTML = `<div class="empty-box" style="grid-column:1/-1;">No wines match your current filters.</div>`;
    return;
  }

  grid.innerHTML = "";
  list.forEach((wine, idx) => grid.appendChild(createScrapbookCard(wine, "Inspect", idx)));
}

function stockStampHtml(qty) {
  if (qty <= 0) return `<span class="ink-stamp out">Sold Out</span>`;
  if (qty < 10) return `<span class="ink-stamp low">Only ${qty} Left</span>`;
  return `<span class="ink-stamp ok">${qty} In Cellar</span>`;
}

function createScrapbookCard(wine, buttonLabel, idx) {
  const card = document.createElement("article");
  card.className = "scrapbook-card";
  card.style.animationDelay = `${Math.min(idx * 65, 360)}ms`;
  const entryNo = String(wine.productId ?? idx + 1).padStart(3, "0");

  card.innerHTML = `
    <span class="washi-tape" aria-hidden="true"></span>
    <div class="scrap-photo">
      <div class="scrap-photo-inner">
        ${renderWineVisual(wine, 146)}
        ${stockStampHtml(wine.stockQuantity)}
      </div>
    </div>
    <div class="scrap-meta-row">
      <span class="scrap-cat-tag">${escapeHtml(wine.category || "Cellar")}</span>
      <span>No. ${entryNo}</span>
    </div>
    <h3 class="scrap-title">${escapeHtml(wine.wineName)}</h3>
    <p class="scrap-note">"${escapeHtml(wine.description || "Aged vintage from Oak Vine Cellar.")}"</p>
    <div class="scrap-footer">
      <span class="price-ticket">${formatPHP(wine.price)}</span>
      <button class="btn-outline-dark btn-xs view-wine-btn" type="button">${buttonLabel}</button>
    </div>
  `;

  card.querySelector(".view-wine-btn").addEventListener("click", () => openWineDetailModal(wine));
  card.querySelector(".scrap-photo").addEventListener("click", () => openWineDetailModal(wine));
  return card;
}

export function openWineDetailModal(wine) {
  const modal = document.getElementById("wineDetailModal");
  const body = document.getElementById("wineDetailBody");
  const out = wine.stockQuantity <= 0;
  let qty = 1;

  body.innerHTML = `
    <div class="wine-modal-grid">
      <div class="wine-modal-art">${renderWineVisual(wine, 195)}</div>
      <div class="stack-12">
        <div>
          <span class="cat-pill">${escapeHtml(wine.category || "Estate")}</span>
          <h2 class="serif-heading mt-8" style="font-size:1.75rem;">${escapeHtml(wine.wineName)}</h2>
        </div>
        <p class="wine-card-price" style="font-size:1.35rem;">${formatPHP(wine.price)}</p>
        <p class="muted-text" style="font-style:italic;">${escapeHtml(wine.description || "Carefully aged vintage from Oak Vine Cellar.")}</p>
        <p class="muted-text"><strong>Stock Available:</strong> ${out ? "Out of Stock" : `${wine.stockQuantity} bottles`}</p>
        <div style="display:flex;align-items:center;gap:12px;flex-wrap:wrap;margin-top:6px;">
          <div class="qty-stepper">
            <button id="mdDec" type="button">−</button>
            <span id="mdQty">1</span>
            <button id="mdInc" type="button">+</button>
          </div>
          <button id="mdAddBasket" class="btn-crimson" type="button" ${out ? "disabled" : ""}>
            ${out ? "Out of Stock" : "Add to Basket"}
          </button>
        </div>
      </div>
    </div>
  `;

  const qtySpan = body.querySelector("#mdQty");
  body.querySelector("#mdDec").addEventListener("click", () => {
    if (qty > 1) qtySpan.textContent = --qty;
  });
  body.querySelector("#mdInc").addEventListener("click", () => {
    if (qty < wine.stockQuantity) qtySpan.textContent = ++qty;
  });
  body.querySelector("#mdAddBasket").addEventListener("click", async () => {
    await addToBasket(wine, qty);
    modal.classList.add("hidden");
  });

  modal.classList.remove("hidden");
}