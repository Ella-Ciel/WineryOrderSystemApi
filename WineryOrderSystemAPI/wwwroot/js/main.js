import { state, clearAuth, isAdmin, showToast } from "./api.js";
import { initAuth } from "./auth.js";
import { initCatalog, loadWinesData } from "./catalog.js";
import { initBasket, loadBasket, renderBasketPage, renderCheckoutPage } from "./basket.js";
import { initOrdersAndProfile, loadUserOrders, renderProfilePage } from "./orders.js";
import { initChatbot } from "./chatbot.js";
import { initAdmin, loadAdminData } from "./admin.js";

function closeProfileDropdown() {
  document.getElementById("profileDropdown")?.classList.add("hidden");
  document.getElementById("profileMenuBtn")?.setAttribute("aria-expanded", "false");
}

function navigate(route) {
  // Auth guard
  if (!state.token && route !== "login" && route !== "signup") {
    route = "login";
  }
  if (route.startsWith("admin-") && !isAdmin()) {
    showToast("Admin access required.", true);
    route = "home";
  }

  window.location.hash = route;
  document.getElementById("mobileNavDrawer").classList.add("hidden");
  closeProfileDropdown();

  // Hide all top-level views
  ["view-login", "view-signup", "view-home", "view-wines", "view-basket", "view-checkout", "view-orders", "view-profile", "view-admin"]
    .forEach((id) => document.getElementById(id).classList.add("hidden"));

  const isAuthPage = route === "login" || route === "signup";
  const isAdminPage = route.startsWith("admin-");

  document.getElementById("storeNavbar").classList.toggle("hidden", isAuthPage || isAdminPage);
  document.getElementById("chatbotRoot").classList.toggle("hidden", isAuthPage || isAdminPage);
  document.getElementById("profileAdminSwitch")?.classList.toggle("hidden", !isAdmin());
  document.getElementById("mobileAdminSwitch")?.classList.toggle("hidden", !isAdmin());

  document.querySelectorAll(".nav-item, .nav-outside-icon, .mobile-nav-item, .profile-dropdown-item").forEach((el) => {
    const target = el.dataset.route || el.dataset.activeRoute;
    const isMatch =
      target === route ||
      (route === "checkout" && target === "basket");
    el.classList.toggle("active", isMatch);
  });

  if (isAdminPage) {
    document.getElementById("view-admin").classList.remove("hidden");
    loadAdminData(route);
    return;
  }

  document.getElementById(`view-${route}`)?.classList.remove("hidden");
  window.scrollTo({ top: 0, behavior: "instant" });

  if (route === "home" || route === "wines") loadWinesData();
  if (route === "basket") renderBasketPage();
  if (route === "checkout") renderCheckoutPage();
  if (route === "orders") loadUserOrders();
  if (route === "profile") renderProfilePage();
}

function handleLogout() {
  clearAuth();
  showToast("Signed out.");
  navigate("login");
}

function checkPaymentReturnParams() {
  const params = new URLSearchParams(window.location.search);
  const payment = params.get("payment");
  const banner = document.getElementById("paymentStatusBanner");
  if (payment === "success") {
    banner.textContent = "Payment successful! Thank you for ordering from Oak Vine Cellar.";
    banner.className = "payment-banner success";
  } else if (payment === "failed") {
    banner.textContent = "Payment was not completed. You can retry payment from your Orders page.";
    banner.className = "payment-banner failed";
  }
}

function boot() {
  initAuth({
    onAuthSuccess: async (targetRoute) => {
      await loadWinesData();
      await loadBasket();
      navigate(targetRoute);
    },
  });
  initCatalog({ onNavigate: navigate });
  initBasket({ onNavigate: navigate });
  initOrdersAndProfile();
  initChatbot();
  initAdmin({ onNavigate: navigate });

  // Global route & modal triggers
  document.addEventListener("click", (e) => {
    const routeBtn = e.target.closest("[data-route]");
    if (routeBtn) navigate(routeBtn.dataset.route);

    const closeBtn = e.target.closest("[data-close-modal]");
    if (closeBtn) document.getElementById(closeBtn.dataset.closeModal)?.classList.add("hidden");

    if (!e.target.closest("#profileMenuWrap")) {
      closeProfileDropdown();
    }
  });

  // Profile dropdown toggle
  const profileMenuBtn = document.getElementById("profileMenuBtn");
  const profileDropdown = document.getElementById("profileDropdown");

  profileMenuBtn.addEventListener("click", (e) => {
    e.stopPropagation();
    const isHidden = profileDropdown.classList.toggle("hidden");
    profileMenuBtn.setAttribute("aria-expanded", String(!isHidden));
  });

  // Mobile menu & logout triggers
  document.getElementById("mobileMenuBtn").addEventListener("click", () => {
    document.getElementById("mobileNavDrawer").classList.toggle("hidden");
  });
  ["profileDropdownLogoutBtn", "mobileLogoutBtn", "adminLogoutBtn", "adminMobileLogout"].forEach((id) =>
    document.getElementById(id)?.addEventListener("click", handleLogout)
  );

  checkPaymentReturnParams();

  const initialHash = window.location.hash.replace("#", "");
  if (state.token) {
    loadWinesData();
    loadBasket();
    navigate(initialHash || (isAdmin() ? "admin-dashboard" : "home"));
  } else {
    navigate(initialHash === "signup" ? "signup" : "login");
  }
}

boot();