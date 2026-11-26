import { apiRequest, setAuth, isAdmin, showToast } from "./api.js";

export function initAuth({ onAuthSuccess }) {
  const loginForm = document.getElementById("loginForm");
  const signupForm = document.getElementById("signupForm");

  loginForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    const btn = document.getElementById("loginSubmitBtn");
    btn.disabled = true;
    btn.textContent = "SIGNING IN...";

    try {
      const res = await apiRequest("POST", "/api/v1/Auth/login", {
        email: document.getElementById("loginEmail").value.trim(),
        password: document.getElementById("loginPassword").value,
      });
      setAuth(res);
      showToast(`Welcome back, ${res.fullName || res.email}`);
      onAuthSuccess(isAdmin() ? "admin-dashboard" : "home");
    } catch (err) {
      showToast(err.message || "Invalid email or password", true);
    } finally {
      btn.disabled = false;
      btn.textContent = "LOGIN";
    }
  });

  signupForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    const pass = document.getElementById("regPassword").value;
    const confirmPass = document.getElementById("regConfirmPassword").value;
    if (pass !== confirmPass) {
      return showToast("Passwords do not match.", true);
    }

    const btn = document.getElementById("signupSubmitBtn");
    btn.disabled = true;
    btn.textContent = "CREATING ACCOUNT...";

    const payload = {
      fullName: document.getElementById("regFullName").value.trim(),
      email: document.getElementById("regEmail").value.trim(),
      password: pass,
      phone: document.getElementById("regPhone").value.trim() || null,
      address: document.getElementById("regAddress").value.trim() || null,
    };

    try {
      const res = await apiRequest("POST", "/api/v1/Auth/register", payload);
      setAuth({ ...res, phone: payload.phone, address: payload.address });
      showToast("Account created! Welcome to Oak Vine Cellar.");
      onAuthSuccess("home");
    } catch (err) {
      showToast(err.message || "Registration failed", true);
    } finally {
      btn.disabled = false;
      btn.textContent = "SIGN UP";
    }
  });
}