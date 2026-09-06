// Live Steam + Nexus stats.
// Steam/Nexus APIs block browser CORS. Do not wait on jina or GraphQL —
// those hang or write zeros that hide the real counts. Each visit pulls
// stats/live.json (Actions, every 15 min) and overlays Steam if CORS
// happens to work. Static docs/data/stats-cache.json is last resort.
// Last snapshot is painted instantly; it never skips the live pull.
import { reactive, computed } from "vue";
import modsData from "../data/mods.json";

const STEAM_API =
  "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";
const LIVE_JSON =
  "https://raw.githubusercontent.com/AzraelGodKing/rimworld_mods/stats/live.json";
const CACHE_KEY = "azrael-workshop-stats-v3";
const BASE = import.meta.env.BASE_URL;

const STEAM_FIELDS = [
  "subscriptions",
  "favorited",
  "views",
  "lifetime_subscriptions",
  "lifetime_favorited",
];
const NEXUS_FIELDS = ["nexus_downloads", "nexus_endorsements"];
const ALL_FIELDS = [...STEAM_FIELDS, ...NEXUS_FIELDS];

const roster = modsData.mods.map((m) => ({
  id: m.id,
  name: m.name,
  publishedFileId: String(m.publishedFileId),
  nexusModId: m.nexusModId ? String(m.nexusModId) : "",
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

function maybeNum(v) {
  if (v == null || v === "") return null;
  const n = Number(v);
  return Number.isFinite(n) ? n : null;
}

function emptyTotal() {
  return {
    subscriptions: 0,
    favorited: 0,
    views: 0,
    lifetime_subscriptions: 0,
    lifetime_favorited: 0,
    nexus_downloads: 0,
    nexus_endorsements: 0,
  };
}

function buildSiteTotal(modsMap) {
  const total = emptyTotal();
  for (const m of Object.values(modsMap)) {
    for (const f of ALL_FIELDS) total[f] += num(m[f]);
  }
  return total;
}

function readLocalSnapshot() {
  try {
    const parsed = JSON.parse(localStorage.getItem(CACHE_KEY));
    if (!parsed?.mods) return null;
    return parsed;
  } catch {
    return null;
  }
}

function writeLocalSnapshot(stats) {
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

function steamDetailUsable(detail) {
  return detail != null
    && Number(detail.result) === 1
    && (detail.subscriptions != null || detail.favorited != null);
}

function emptyRow(mod) {
  return {
    name: mod.name,
    publishedFileId: mod.publishedFileId,
    title: mod.name,
    subscriptions: null,
    favorited: null,
    views: null,
    lifetime_subscriptions: null,
    lifetime_favorited: null,
    nexus_downloads: null,
    nexus_endorsements: null,
  };
}

function detailToModStats(mod, detail) {
  const row = emptyRow(mod);
  if (!detail) return row;
  row.title = detail.title || mod.name;
  row.subscriptions = maybeNum(detail.subscriptions);
  row.favorited = maybeNum(detail.favorited);
  row.views = maybeNum(detail.views);
  row.lifetime_subscriptions = maybeNum(detail.lifetime_subscriptions) ?? row.subscriptions;
  row.lifetime_favorited = maybeNum(detail.lifetime_favorited) ?? row.favorited;
  row.nexus_downloads = maybeNum(detail.nexus_downloads);
  row.nexus_endorsements = maybeNum(detail.nexus_endorsements);
  return row;
}

function mergeHole(target, fill, fields) {
  if (!fill) return;
  for (const f of fields) {
    if (target[f] == null && fill[f] != null) target[f] = maybeNum(fill[f]);
  }
}

function settleRow(row) {
  for (const f of ALL_FIELDS) {
    if (row[f] == null) row[f] = 0;
  }
  return row;
}

function hasUsefulCounts(stats) {
  return Object.values(stats?.mods || {}).some(
    (m) => num(m.subscriptions) > 0 || num(m.nexus_downloads) > 0
  );
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
    cache: "no-store",
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

async function fetchLiveJson() {
  const res = await fetch(LIVE_JSON + "?t=" + Date.now(), { cache: "no-store" });
  if (!res.ok) throw new Error("live.json HTTP " + res.status);
  return res.json();
}

async function fetchStaticFallback() {
  const res = await fetch(BASE + "data/stats-cache.json", { cache: "no-store" });
  if (!res.ok) throw new Error("cache HTTP " + res.status);
  return res.json();
}

function assemble(byId, feed, cache) {
  const mods = {};
  for (const mod of roster) {
    const detail = byId?.get(mod.publishedFileId);
    const row = steamDetailUsable(detail)
      ? detailToModStats(mod, detail)
      : emptyRow(mod);
    mergeHole(row, feed?.mods?.[mod.id], ALL_FIELDS);
    mergeHole(row, cache?.mods?.[mod.id], ALL_FIELDS);
    mods[mod.id] = settleRow(row);
  }
  const sources = [];
  if (byId) sources.push("steam-api");
  if (feed) sources.push("live-json");
  if (cache) sources.push("stats-cache");
  return {
    lastFetched: feed?.lastFetched || cache?.lastFetched || new Date().toISOString(),
    source: sources.join("+") || "empty",
    mods,
    site_total: buildSiteTotal(mods),
  };
}

async function fetchLiveStats() {
  let feed = null;
  let cache = null;
  let byId = null;

  const feedP = fetchLiveJson()
    .then((json) => {
      feed = json;
      if (hasUsefulCounts(json)) apply(assemble(null, json, null));
      return json;
    })
    .catch(() => null);

  const cacheP = fetchStaticFallback()
    .then((json) => {
      cache = json;
      return json;
    })
    .catch(() => null);

  const steamP = fetchSteamApi(roster.map((m) => m.publishedFileId))
    .then((map) => {
      byId = map;
      return map;
    })
    .catch(() => null);

  await Promise.all([feedP, cacheP, steamP]);
  return assemble(byId, feed, cache);
}

let started = false;

export async function refreshStats({ force = false } = {}) {
  if (state.loading) return !state.error;
  if (force) {
    try { localStorage.removeItem(CACHE_KEY); } catch { /* ignore */ }
  } else {
    const snap = readLocalSnapshot();
    if (snap && hasUsefulCounts(snap)) apply(snap);
  }
  state.loading = true;
  state.error = false;
  try {
    const stats = await fetchLiveStats();
    if (hasUsefulCounts(stats)) writeLocalSnapshot(stats);
    apply(stats);
    return hasUsefulCounts(stats);
  } catch {
    try {
      const feed = await fetchLiveJson();
      apply(assemble(null, feed, null));
      return true;
    } catch {
      try {
        const cache = await fetchStaticFallback();
        apply(assemble(null, null, cache));
        return true;
      } catch {
        state.error = true;
        return false;
      }
    }
  } finally {
    state.loading = false;
  }
}

export function useStats() {
  if (!started) {
    started = true;
    refreshStats();
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
