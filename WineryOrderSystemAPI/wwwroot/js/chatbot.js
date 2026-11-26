import { state, apiRequest, escapeHtml } from "./api.js";

let sessions = JSON.parse(localStorage.getItem("ovc_chat_sessions") || "[]");
let activeSessionIndex = 0;
let pendingReplyPrompt = null;

export function initChatbot() {
  const fab = document.getElementById("chatFab");
  const widget = document.getElementById("chatWidget");
  const sessionSelect = document.getElementById("chatSessionSelect");
  const promptsBar = document.getElementById("chatQuickPrompts");

  // Allow mouse-wheel horizontal scrolling on quick prompt chips even without a visible scrollbar
  promptsBar.addEventListener(
    "wheel",
    (e) => {
      if (Math.abs(e.deltaY) > Math.abs(e.deltaX)) {
        e.preventDefault();
        promptsBar.scrollLeft += e.deltaY;
      }
    },
    { passive: false }
  );

  const openChat = () => {
    widget.classList.remove("hidden");
    if (!sessions.length) createLocalSession();
    renderSessionSelector();
    renderMessages();
    loadQuickPrompts();
  };

  fab.addEventListener("click", () => {
    if (widget.classList.contains("hidden")) openChat();
    else widget.classList.add("hidden");
  });

  document.getElementById("heroOpenChatBtn").addEventListener("click", openChat);
  document.getElementById("chatCloseBtn").addEventListener("click", () => widget.classList.add("hidden"));

  document.getElementById("chatNewBtn").addEventListener("click", () => {
    if (pendingReplyPrompt) return;
    createLocalSession();
    renderSessionSelector();
    renderMessages();
  });

  sessionSelect.addEventListener("change", () => {
    activeSessionIndex = Number(sessionSelect.value);
    renderMessages();
  });

  document.getElementById("chatForm").addEventListener("submit", async (e) => {
    e.preventDefault();
    if (pendingReplyPrompt) return;
    const input = document.getElementById("chatInput");
    const text = input.value.trim();
    if (!text) return;
    input.value = "";
    await sendPrompt(text);
  });
}

function createLocalSession() {
  sessions.unshift({
    serverSessionId: null,
    label: `Chat ${new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}`,
    messages: [
      {
        role: "bot",
        text: "Welcome to Oak Vine Cellar. Ask me about our wines, pricing, stock, or delivery!",
      },
    ],
  });
  activeSessionIndex = 0;
  saveSessions();
}

function saveSessions() {
  localStorage.setItem("ovc_chat_sessions", JSON.stringify(sessions.slice(0, 10)));
}

function renderSessionSelector() {
  const select = document.getElementById("chatSessionSelect");
  select.innerHTML = sessions
    .map(
      (s, idx) =>
        `<option value="${idx}" ${idx === activeSessionIndex ? "selected" : ""}>${escapeHtml(s.label)}</option>`
    )
    .join("");
}

function truncateQuote(text, maxLen = 46) {
  const clean = String(text || "").trim();
  return clean.length > maxLen ? `${clean.slice(0, maxLen)}…` : clean;
}

function renderMessages() {
  const feed = document.getElementById("chatMessages");
  const current = sessions[activeSessionIndex];
  feed.innerHTML = "";

  (current?.messages || []).forEach((m) => {
    const div = document.createElement("div");
    div.className = `chat-bubble ${m.role}`;

    const refHtml =
      m.role === "bot" && m.replyTo
        ? `<div class="chat-reply-ref"><strong>Replying to</strong><span>"${escapeHtml(truncateQuote(m.replyTo))}"</span></div>`
        : "";

    div.innerHTML = `${refHtml}<div>${escapeHtml(m.text)}</div>`;
    feed.appendChild(div);
  });

  // Live "Replying to..." loading indicator while awaiting bot response
  if (pendingReplyPrompt) {
    const loadingDiv = document.createElement("div");
    loadingDiv.className = "chat-bubble bot loading";
    loadingDiv.innerHTML = `
      <div class="chat-reply-ref">
        <strong>Replying to</strong>
        <span>"${escapeHtml(truncateQuote(pendingReplyPrompt))}"</span>
      </div>
      <div class="chat-typing-row">
        <span>Sommelier is replying</span>
        <span class="typing-dots" aria-hidden="true"><span></span><span></span><span></span></span>
      </div>
    `;
    feed.appendChild(loadingDiv);
  }

  feed.scrollTop = feed.scrollHeight;
}

async function loadQuickPrompts() {
  const bar = document.getElementById("chatQuickPrompts");
  try {
    const faqs = await apiRequest("GET", "/api/Chatbot/faqs");
    if (!Array.isArray(faqs) || !faqs.length) return;
    bar.innerHTML = "";
    faqs.slice(0, 6).forEach((f) => {
      const q = f.question ?? f.Question;
      if (!q) return;
      const btn = document.createElement("button");
      btn.type = "button";
      btn.className = "prompt-chip";
      btn.textContent = q;
      btn.addEventListener("click", () => {
        if (!pendingReplyPrompt) sendPrompt(q);
      });
      bar.appendChild(btn);
    });
  } catch { /* ignore */ }
}

async function sendPrompt(prompt) {
  const current = sessions[activeSessionIndex];
  current.messages.push({ role: "user", text: prompt });
  pendingReplyPrompt = prompt;
  renderMessages();

  const btn = document.getElementById("chatSendBtn");
  const input = document.getElementById("chatInput");
  btn.disabled = true;
  input.disabled = true;

  try {
    const res = await apiRequest("POST", "/api/Chatbot/message", {
      userId: state.user?.userId || 0,
      sessionId: current.serverSessionId,
      prompt,
    });
    if (res?.sessionId) current.serverSessionId = res.sessionId;
    const reply =
      typeof res === "string"
        ? res
        : res?.response ?? res?.reply ?? res?.answer ?? res?.message ?? "No response received.";
    current.messages.push({ role: "bot", text: reply, replyTo: prompt });
  } catch (err) {
    current.messages.push({
      role: "bot",
      text: `Sommelier unavailable: ${err.message}`,
      replyTo: prompt,
    });
  } finally {
    pendingReplyPrompt = null;
    btn.disabled = false;
    input.disabled = false;
    input.focus();
    saveSessions();
    renderMessages();
  }
}