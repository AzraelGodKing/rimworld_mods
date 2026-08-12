// Shared docs site helpers (nav, a11y, color mode). Independent of Steam stats.

(function initColorMode() {
  const KEY = "azrael-color-mode";
  const root = document.documentElement;

  function preferred() {
    try {
      const saved = localStorage.getItem(KEY);
      if (saved === "light" || saved === "dark") return saved;
    } catch (_) { /* private mode */ }
    return "dark";
  }

  function labelFor(mode) {
    // Button shows the mode you can switch *to*.
    return mode === "dark" ? "Light" : "Dark";
  }

  function apply(mode) {
    const next = mode === "light" ? "light" : "dark";
    root.dataset.colorMode = next;
    root.style.colorScheme = next;
    document.querySelectorAll(".theme-toggle").forEach((btn) => {
      const other = next === "dark" ? "light" : "dark";
      btn.setAttribute("aria-pressed", next === "dark" ? "true" : "false");
      btn.textContent = labelFor(next);
      btn.title = `Switch to ${other} mode`;
      btn.setAttribute("aria-label", `Switch to ${other} mode`);
    });
  }

  // Apply before paint when this file is sync; with defer, still before DOMContentLoaded paint of body widgets.
  apply(preferred());

  function ensureToggle() {
    if (document.querySelector(".theme-toggle")) return;
    const wrap = document.querySelector(".site-nav .wrap");
    if (!wrap) return;

    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = "theme-toggle";
    btn.textContent = labelFor(preferred());

    let actions = wrap.querySelector(".nav-actions");
    if (!actions) {
      actions = document.createElement("div");
      actions.className = "nav-actions";
      const menu = wrap.querySelector(".nav-toggle");
      if (menu && menu.parentElement === wrap) {
        // Keep menu where it is; park the theme control beside it on small screens.
        wrap.insertBefore(actions, menu);
      } else {
        wrap.appendChild(actions);
      }
    }
    actions.appendChild(btn);
  }

  document.addEventListener("DOMContentLoaded", () => {
    ensureToggle();
    apply(preferred());
    document.querySelectorAll(".theme-toggle").forEach((btn) => {
      btn.addEventListener("click", () => {
        const next = root.dataset.colorMode === "dark" ? "light" : "dark";
        try { localStorage.setItem(KEY, next); } catch (_) { /* ignore */ }
        apply(next);
      });
    });
  });
})();

document.addEventListener("DOMContentLoaded", () => {
  const nav = document.querySelector(".site-nav");
  const toggle = document.querySelector(".nav-toggle");
  const links = document.getElementById("primary-nav");
  if (!nav || !toggle || !links) return;

  const setOpen = (open) => {
    nav.classList.toggle("is-open", open);
    toggle.setAttribute("aria-expanded", open ? "true" : "false");
    toggle.textContent = open ? "Close" : "Menu";
  };

  toggle.addEventListener("click", () => {
    setOpen(!nav.classList.contains("is-open"));
  });

  links.querySelectorAll("a").forEach((a) => {
    a.addEventListener("click", () => setOpen(false));
  });

  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape") setOpen(false);
  });
});
