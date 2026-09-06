// Live Steam + Nexus stats.
// Steam/Nexus APIs block browser CORS. Each visit still tries them, then
// fills holes from the `stats` branch live.json (refreshed every 15 min,
// not committed on main). Static docs/data/stats-cache.json is last resort.
// Last snapshot is painted instantly; it never skips the live pull.
import { reactive, computed } from "vue";
import modsData from "../data/mods.json";

const STEAM_API =
  "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";
const NEXUS_GQL = "https://api.nexusmods.com/v2/graphql";
const NEXUS_GAME_ID = "424";
const LIVE_JSON =
  "https://raw.githubusercontent.com/AzraelGodKing/rimworld_mods/stats/live.json";
const CACHE_KEY = "azrael-workshop-stats-v2";
const BASE = import.meta.env.BASE_URL;

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
    total.subscriptions += num(m.subscriptions);
    total.favorited += num(m.favorited);
    total.views += num(m.views);
    total.lifetime_subscriptions += num(m.lifetime_subscriptions);
    total.lifetime_favorited += num(m.lifetime_favorited);
    total.nexus_downloads += num(m.nexus_downloads);
    total.nexus_endorsements += num(m.nexus_endorsements);
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

function parseCountNearLabel(text, labels) {
  for (const label of labels) {
    const before = text.match(
      new RegExp("([0-9][0-9,]*)\\s*</[^>]+>\\s*<[^>]+>\\s*" + label, "i")
    );
    if (before) {
      const n = Number(String(before[1]).replace(/,/g, ""));
      if (Number.isFinite(n)) return n;
    }
    const after = text.match(new RegExp(label + "[^0-9]{0,80}?([0-9][0-9,]*)", "i"));
    if (after) {
      const n = Number(String(after[1]).replace(/,/g, ""));
      if (Number.isFinite(n)) return n;
    }
  }
  return null;
}

function steamDetailUsable(detail) {
  return detail != null
    && Number(detail.result) === 1
    && (detail.subscriptions != null || detail.favorited != null);
}

async function fetchViaWorkshopPage(publishedFileId) {
  const pageUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=" + publishedFileId;
  const res = await fetch("https://r.jina.ai/" + pageUrl, {
    cache: "no-store",
    headers: { "X-Return-Format": "html" },
  });
  if (!res.ok) throw new Error("jina HTTP " + res.status);
  const text = await res.text();
  const subscriptions = parseCountNearLabel(text, ["Current Subscribers", "Subscribers"]);
  const favorited = parseCountNearLabel(text, ["Current Favorites", "Favorites"]);
  const views = parseCountNearLabel(text, ["Unique Visitors", "Unique visitors"]);
  if (subscriptions == null && favorited == null) throw new Error("could not parse workshop page");
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

async function fetchNexusGraphql(nexusModId) {
  const res = await fetch(NEXUS_GQL, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    mode: "cors",
    cache: "no-store",
    body: JSON.stringify({
      query: "query ($gameId: ID!, $modId: ID!) { mod(gameId: $gameId, modId: $modId) { downloads endorsements } }",
      variables: { gameId: NEXUS_GAME_ID, modId: String(nexusModId) },
    }),
  });
  if (!res.ok) throw new Error("Nexus HTTP " + res.status);
  const json = await res.json();
  const row = json?.data?.mod;
  if (!row) throw new Error("Nexus shape");
  return {
    nexus_downloads: num(row.downloads),
    nexus_endorsements: num(row.endorsements),
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
    nexus_downloads: detail.nexus_downloads != null ? num(detail.nexus_downloads) : null,
    nexus_endorsements: detail.nexus_endorsements != null ? num(detail.nexus_endorsements) : null,
  };
}

function mergeHole(target, fill, fields) {
  if (!fill) return;
  for (const f of fields) {
    if (target[f] == null) target[f] = num(fill[f]);
  }
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

async function fetchLiveStats() {
  let byId = null;
  try {
    byId = await fetchSteamApi(roster.map((m) => m.publishedFileId));
  } catch {
    byId = null;
  }

  const mods = {};
  for (const mod of roster) {
    try {
      let detail = byId?.get(mod.publishedFileId);
      if (!steamDetailUsable(detail)) {
        detail = await fetchViaWorkshopPage(mod.publishedFileId);
      }
      mods[mod.id] = detailToModStats(mod, detail);
    } catch {
      mods[mod.id] = detailToModStats(mod, {});
    }
    if (mod.nexusModId) {
      try {
        const nexus = await fetchNexusGraphql(mod.nexusModId);
        mods[mod.id].nexus_downloads = nexus.nexus_downloads;
        mods[mod.id].nexus_endorsements = nexus.nexus_endorsements;
      } catch { /* CORS — filled from live.json */ }
    }
  }

  let feed = null;
  try {
    feed = await fetchLiveJson();
  } catch { /* branch may not exist yet */ }

  const steamFields = [
    "subscriptions", "favorited", "views",
    "lifetime_subscriptions", "lifetime_favorited",
  ];
  const nexusFields = ["nexus_downloads", "nexus_endorsements"];
  for (const mod of roster) {
    const row = mods[mod.id];
    const fromFeed = feed?.mods?.[mod.id];
    mergeHole(row, fromFeed, steamFields);
    mergeHole(row, fromFeed, nexusFields);
    for (const f of [...steamFields, ...nexusFields]) {
      if (row[f] == null) row[f] = 0;
    }
  }

  const sources = [];
  if (byId) sources.push("steam-api");
  else sources.push("workshop-page");
  if (feed) sources.push("live-json");

  return {
    lastFetched: new Date().toISOString(),
    source: sources.join("+"),
    mods,
    site_total: buildSiteTotal(mods),
  };
}

let started = false;

export async function refreshStats({ force = false } = {}) {
  if (state.loading) return !state.error;
  if (force) {
    try { localStorage.removeItem(CACHE_KEY); } catch { /* ignore */ }
  } else {
    const snap = readLocalSnapshot();
    if (snap) apply(snap);
  }
  state.loading = true;
  state.error = false;
  try {
    const stats = await fetchLiveStats();
    writeLocalSnapshot(stats);
    apply(stats);
    return true;
  } catch {
    try {
      const feed = await fetchLiveJson();
      apply(feed);
      return true;
    } catch {
      try {
        apply(await fetchStaticFallback());
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
