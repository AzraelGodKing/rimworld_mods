// Live Steam Workshop stats, ported from docs/scripts/stats-display.js.
// 1) Steam Web API from the browser (when CORS allows)
// 2) Workshop pages via r.jina.ai (CORS-friendly GET)
// 3) Static fallback data/stats-cache.json
// Cached in localStorage for one hour; auto-refreshes while the tab is open.
import { reactive, computed } from "vue";
import modsData from "../data/mods.json";

const STEAM_API =
  "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";
const CACHE_KEY = "azrael-workshop-stats-v1";
const CACHE_TTL_MS = 60 * 60 * 1000;
const BASE = import.meta.env.BASE_URL;

const roster = modsData.mods.map((m) => ({
  id: m.id,
  name: m.name,
  publishedFileId: String(m.publishedFileId),
}));

const state = reactive({
  mods: {},
  siteTotal: null,
  lastFetched: null,
  source: null,
  loading: false,
  error: false,
});

function num(v, fallback = 0) {
  const n = Number(v);
  return Number.isFinite(n) ? n : fallback;
}

function buildSiteTotal(modsMap) {
  const total = { subscriptions: 0, favorited: 0, views: 0, lifetime_subscriptions: 0, lifetime_favorited: 0 };
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
    const parsed = JSON.parse(localStorage.getItem(CACHE_KEY));
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
  } catch { /* private mode / quota */ }
}

function apply(stats) {
  state.mods = stats.mods || {};
  state.siteTotal = stats.site_total || buildSiteTotal(state.mods);
  state.lastFetched = stats.lastFetched || null;
  state.source = stats.source || null;
}

async function fetchSteamApi(ids) {
  const body = new URLSearchParams();
  body.set("itemcount", String(ids.length));
  ids.forEach((id, i) => body.set(`publishedfileids[${i}]`, id));
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
    const m = text.match(new RegExp(label + "[^0-9]{0,80}?([0-9][0-9,]*)", "i"));
    if (m) {
      const n = Number(String(m[1]).replace(/,/g, ""));
      if (Number.isFinite(n)) return n;
    }
  }
  return null;
}

async function fetchViaWorkshopPage(publishedFileId) {
  const pageUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=" + publishedFileId;
  const res = await fetch("https://r.jina.ai/" + pageUrl, { cache: "no-cache" });
  if (!res.ok) throw new Error("jina HTTP " + res.status);
  const text = await res.text();
  const subscriptions = parseCountNearLabel(text, ["Current Subscribers", "Subscribers"]);
  const favorited = parseCountNearLabel(text, ["Current Favorites", "Favorites"]);
  const views = parseCountNearLabel(text, ["Unique Visitors", "Unique visitors"]);
  if (subscriptions == null && favorited == null) throw new Error("could not parse workshop page");
  return {
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
    lifetime_subscriptions: num(detail.lifetime_subscriptions, num(detail.subscriptions)),
    lifetime_favorited: num(detail.lifetime_favorited, num(detail.favorited)),
  };
}

async function fetchLiveStats() {
  let byId = null;
  try {
    byId = await fetchSteamApi(roster.map((m) => m.publishedFileId));
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
  const res = await fetch(BASE + "data/stats-cache.json", { cache: "no-cache" });
  if (!res.ok) throw new Error("cache HTTP " + res.status);
  return res.json();
}

let started = false;

export async function refreshStats({ force = false } = {}) {
  if (state.loading) return !state.error;
  const cached = force ? null : readLocalCache();
  if (cached) {
    apply(cached);
    return true;
  }
  state.loading = true;
  state.error = false;
  try {
    const stats = await fetchLiveStats();
    writeLocalCache(stats);
    apply(stats);
    return true;
  } catch {
    try {
      apply(await fetchStaticFallback());
      return true;
    } catch {
      state.error = true;
      return false;
    }
  } finally {
    state.loading = false;
  }
}

export function useStats() {
  if (!started) {
    started = true;
    refreshStats();
    setInterval(() => refreshStats(), CACHE_TTL_MS);
  }

  const format = (v) => {
    const n = Number(v);
    return Number.isFinite(n) ? n.toLocaleString() : "—";
  };

  const forMod = (id) => computed(() => state.mods[id] || null);

  const updatedLabel = computed(() => {
    if (!state.lastFetched) return "—";
    const d = new Date(state.lastFetched);
    return Number.isNaN(d.valueOf()) ? "—" : d.toLocaleString();
  });

  return { state, forMod, format, updatedLabel, refreshStats };
}
