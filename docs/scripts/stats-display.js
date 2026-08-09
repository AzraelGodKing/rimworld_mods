// Live Steam Workshop stats for the docs site (no GitHub Action).
// 1) Try Steam Web API from the browser (works when CORS allows)
// 2) Else read Workshop pages via r.jina.ai (CORS-friendly GET)
// 3) Else fall back to docs/data/stats-cache.json
// Results cached in localStorage for one hour.

const STEAM_API =
  "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";
const CACHE_KEY = "azrael-workshop-stats-v1";
const CACHE_TTL_MS = 60 * 60 * 1000;

function resolveDocsDataUrl(fileName) {
  const scripts = document.getElementsByTagName("script");
  for (let i = scripts.length - 1; i >= 0; i--) {
    const s = scripts[i];
    if (!s.src) continue;
    const u = new URL(s.src, window.location.href);
    if (!/\/scripts\/stats-display\.js(\?|$)/.test(u.pathname)) continue;
    u.pathname = u.pathname.replace(
      /\/scripts\/stats-display\.js$/,
      "/data/" + fileName
    );
    return u.href;
  }

  const m = window.location.pathname.match(/^(.*\/docs\/)/);
  if (m) {
    return new URL("data/" + fileName, window.location.origin + m[1]).href;
  }

  return new URL("data/" + fileName, window.location.href).href;
}

function formatValue(value) {
  if (value == null) return "—";
  const num = Number(value);
  if (!Number.isFinite(num)) return "—";
  return num.toLocaleString();
}

function setText(el, value) {
  el.textContent = formatValue(value);
}

function emptySiteTotal() {
  return {
    subscriptions: 0,
    favorited: 0,
    views: 0,
    lifetime_subscriptions: 0,
    lifetime_favorited: 0,
  };
}

function num(v, fallback = 0) {
  const n = Number(v);
  return Number.isFinite(n) ? n : fallback;
}

function buildSiteTotal(modsMap) {
  const total = emptySiteTotal();
  for (const m of Object.values(modsMap)) {
    total.subscriptions += num(m.subscriptions);
    total.favorited += num(m.favorited);
    total.views += num(m.views);
    total.lifetime_subscriptions += num(m.lifetime_subscriptions);
    total.lifetime_favorited += num(m.lifetime_favorited);
  }
  return total;
}

function readLocalCache() {
  try {
    const raw = localStorage.getItem(CACHE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw);
    if (!parsed?.lastFetched || !parsed?.mods) return null;
    const age = Date.now() - new Date(parsed.lastFetched).valueOf();
    if (!Number.isFinite(age) || age < 0 || age > CACHE_TTL_MS) return null;
    return parsed;
  } catch {
    return null;
  }
}

function writeLocalCache(stats) {
  try {
    localStorage.setItem(CACHE_KEY, JSON.stringify(stats));
  } catch {
    /* private mode / quota — ignore */
  }
}

function applyStats(stats) {
  const siteTotal = stats?.site_total || {};
  const mods = stats?.mods || {};

  document.querySelectorAll("[data-stats='total']").forEach((el) => {
    const field = el.getAttribute("data-field");
    setText(el, siteTotal[field]);
  });

  document.querySelectorAll("[data-stats-mod]").forEach((el) => {
    const modId = el.getAttribute("data-stats-mod");
    const field = el.getAttribute("data-field");
    const mod = mods[modId] || {};
    setText(el, mod[field]);
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
}

async function loadRoster() {
  const res = await fetch(resolveDocsDataUrl("workshop-mods.json"), {
    cache: "no-cache",
  });
  if (!res.ok) throw new Error("roster HTTP " + res.status);
  const rows = await res.json();
  if (!Array.isArray(rows) || rows.length === 0) {
    throw new Error("empty roster");
  }
  return rows.map((row) => ({
    id: String(row.id),
    name: String(row.name || row.id),
    publishedFileId: String(row.publishedFileId),
  }));
}

async function fetchSteamApi(ids) {
  const body = new URLSearchParams();
  body.set("itemcount", String(ids.length));
  ids.forEach((id, i) => body.set("publishedfileids[" + i + "]", id));

  const res = await fetch(STEAM_API, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body,
    mode: "cors",
  });
  if (!res.ok) throw new Error("Steam API HTTP " + res.status);
  const json = await res.json();
  const details = json?.response?.publishedfiledetails;
  if (!Array.isArray(details)) throw new Error("Steam API shape");
  const byId = new Map();
  for (const d of details) {
    if (d?.publishedfileid != null) byId.set(String(d.publishedfileid), d);
  }
  return byId;
}

function parseCountNearLabel(text, labels) {
  for (const label of labels) {
    // Markdown / HTML-ish: **Label** … **1,565**  or Label\n1,565
    const re = new RegExp(
      label + "[^0-9]{0,80}?([0-9][0-9,]*)",
      "i"
    );
    const m = text.match(re);
    if (m) {
      const n = Number(String(m[1]).replace(/,/g, ""));
      if (Number.isFinite(n)) return n;
    }
  }
  return null;
}

async function fetchViaWorkshopPage(publishedFileId) {
  const pageUrl =
    "https://steamcommunity.com/sharedfiles/filedetails/?id=" + publishedFileId;
  // r.jina.ai returns a CORS-friendly text/markdown render of the page.
  const res = await fetch("https://r.jina.ai/" + pageUrl, {
    cache: "no-cache",
  });
  if (!res.ok) throw new Error("jina HTTP " + res.status);
  const text = await res.text();
  const subscriptions = parseCountNearLabel(text, [
    "Current Subscribers",
    "Subscribers",
  ]);
  const favorited = parseCountNearLabel(text, [
    "Current Favorites",
    "Favorites",
  ]);
  const views = parseCountNearLabel(text, [
    "Unique Visitors",
    "Unique visitors",
  ]);
  if (subscriptions == null && favorited == null) {
    throw new Error("could not parse workshop page");
  }
  return {
    result: 1,
    publishedfileid: publishedFileId,
    subscriptions: subscriptions ?? 0,
    favorited: favorited ?? 0,
    views: views ?? 0,
    lifetime_subscriptions: subscriptions ?? 0,
    lifetime_favorited: favorited ?? 0,
  };
}

function detailToModStats(mod, detail) {
  return {
    name: mod.name,
    publishedFileId: mod.publishedFileId,
    title: detail.title || mod.name,
    subscriptions: num(detail.subscriptions),
    favorited: num(detail.favorited),
    views: num(detail.views),
    lifetime_subscriptions: num(
      detail.lifetime_subscriptions,
      num(detail.subscriptions)
    ),
    lifetime_favorited: num(detail.lifetime_favorited, num(detail.favorited)),
  };
}

async function fetchLiveStats(roster) {
  const ids = roster.map((m) => m.publishedFileId);
  let byId = null;

  try {
    byId = await fetchSteamApi(ids);
  } catch {
    byId = null;
  }

  const mods = {};
  for (const mod of roster) {
    let detail = byId?.get(mod.publishedFileId);
    if (!detail || Number(detail.result) !== 1) {
      detail = await fetchViaWorkshopPage(mod.publishedFileId);
    }
    mods[mod.id] = detailToModStats(mod, detail);
  }

  return {
    lastFetched: new Date().toISOString(),
    source: byId ? "steam-api" : "workshop-page",
    mods,
    site_total: buildSiteTotal(mods),
  };
}

async function fetchStaticFallback() {
  const res = await fetch(resolveDocsDataUrl("stats-cache.json"), {
    cache: "no-cache",
  });
  if (!res.ok) throw new Error("cache HTTP " + res.status);
  return res.json();
}

document.addEventListener("DOMContentLoaded", () => {
  const cached = readLocalCache();
  if (cached) {
    applyStats(cached);
  }

  loadRoster()
    .then((roster) => fetchLiveStats(roster))
    .then((stats) => {
      writeLocalCache(stats);
      applyStats(stats);
    })
    .catch(() => {
      if (cached) return;
      return fetchStaticFallback()
        .then((stats) => applyStats(stats))
        .catch(() => {
          /* leave placeholders */
        });
    });
});
