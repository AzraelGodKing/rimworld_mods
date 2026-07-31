// Load with `defer`. Cache: docs/data/stats-cache.json → on Pages (publish /docs) public URL is …/data/stats-cache.json.

function resolveStatsCacheUrl() {
  const scripts = document.getElementsByTagName("script");
  for (let i = scripts.length - 1; i >= 0; i--) {
    const s = scripts[i];
    if (!s.src) continue;
    const u = new URL(s.src, window.location.href);
    if (!/\/scripts\/stats-display\.js(\?|$)/.test(u.pathname)) continue;
    u.pathname = u.pathname.replace(/\/scripts\/stats-display\.js$/, "/data/stats-cache.json");
    return u.href;
  }

  const m = window.location.pathname.match(/^(.*\/docs\/)/);
  if (m) {
    return new URL("data/stats-cache.json", window.location.origin + m[1]).href;
  }

  return new URL("data/stats-cache.json", window.location.href).href;
}

document.addEventListener("DOMContentLoaded", () => {
  const formatValue = (value) => {
    if (value == null) return "—";
    const num = Number(value);
    if (!Number.isFinite(num)) return "—";
    return num.toLocaleString();
  };

  const setText = (el, value) => {
    el.textContent = formatValue(value);
  };

  fetch(resolveStatsCacheUrl(), { cache: "no-cache" })
    .then((res) => {
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      return res.json();
    })
    .then((stats) => {
      const siteTotal = stats?.site_total || {};
      const mods = stats?.mods || {};

      document.querySelectorAll("[data-stats='total']").forEach((el) => {
        const field = el.getAttribute("data-field");
        switch (field) {
          case "subscriptions":
            setText(el, siteTotal.subscriptions);
            break;
          case "favorited":
            setText(el, siteTotal.favorited);
            break;
          case "views":
            setText(el, siteTotal.views);
            break;
          case "lifetime_subscriptions":
            setText(el, siteTotal.lifetime_subscriptions);
            break;
          case "lifetime_favorited":
            setText(el, siteTotal.lifetime_favorited);
            break;
          default:
            setText(el, null);
            break;
        }
      });

      document.querySelectorAll("[data-stats-mod]").forEach((el) => {
        const modId = el.getAttribute("data-stats-mod");
        const field = el.getAttribute("data-field");
        const mod = mods[modId] || {};
        switch (field) {
          case "subscriptions":
            setText(el, mod.subscriptions);
            break;
          case "favorited":
            setText(el, mod.favorited);
            break;
          case "views":
            setText(el, mod.views);
            break;
          case "lifetime_subscriptions":
            setText(el, mod.lifetime_subscriptions);
            break;
          case "lifetime_favorited":
            setText(el, mod.lifetime_favorited);
            break;
          default:
            setText(el, null);
            break;
        }
      });

      const fetched = stats?.lastFetched;
      document.querySelectorAll("[data-stats-fetched]").forEach((el) => {
        if (!fetched) {
          el.textContent = "—";
          return;
        }
        const d = new Date(fetched);
        el.textContent = Number.isNaN(d.valueOf()) ? "—" : d.toLocaleString();
      });
    })
    .catch(() => {
      // Enhancement-only: leave placeholder "—" values if fetch fails.
    });
});
