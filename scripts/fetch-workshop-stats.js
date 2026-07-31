/**
 * Fetch Steam Workshop subscribers / favorites for mods in workshop-mods.json
 * and write docs/data/stats-cache.json (same pattern as azrael-sunhaven-website).
 *
 * Usage:
 *   node scripts/fetch-workshop-stats.js
 *   node scripts/fetch-workshop-stats.js --force
 *   STATS_FORCE=1 node scripts/fetch-workshop-stats.js
 */
const fs = require("fs");
const path = require("path");

const ROOT = path.resolve(__dirname, "..");
const ROSTER_PATH = path.join(ROOT, "scripts", "workshop-mods.json");
const CACHE_PATH = path.join(ROOT, "docs", "data", "stats-cache.json");
const TMP_PATH = `${CACHE_PATH}.tmp`;
const STEAM_URL = "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";

function utcHourBucket(date) {
  return `${date.getUTCFullYear()}-${String(date.getUTCMonth() + 1).padStart(2, "0")}-${String(date.getUTCDate()).padStart(2, "0")}T${String(date.getUTCHours()).padStart(2, "0")}`;
}

function isForceRefresh() {
  const argv = process.argv.slice(2);
  if (argv.includes("--force") || argv.includes("-f")) return true;
  const v = String(process.env.STATS_FORCE || "").trim().toLowerCase();
  return v === "1" || v === "true" || v === "yes";
}

function ensureParentDir(filePath) {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
}

function readCache() {
  if (!fs.existsSync(CACHE_PATH)) {
    return { lastFetched: null, mods: {}, site_total: emptySiteTotal() };
  }
  try {
    return JSON.parse(fs.readFileSync(CACHE_PATH, "utf8"));
  } catch (err) {
    console.warn(`[stats] Failed to parse cache, starting fresh: ${err.message}`);
    return { lastFetched: null, mods: {}, site_total: emptySiteTotal() };
  }
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

function loadRoster() {
  const text = fs.readFileSync(ROSTER_PATH, "utf8").replace(/^\uFEFF/, "");
  const rows = JSON.parse(text);
  if (!Array.isArray(rows) || rows.length === 0) {
    throw new Error("workshop-mods.json must be a non-empty array");
  }
  return rows.map((row) => {
    if (!row?.id || !row?.publishedFileId) {
      throw new Error(`Invalid roster row: ${JSON.stringify(row)}`);
    }
    return {
      id: String(row.id),
      name: String(row.name || row.id),
      publishedFileId: String(row.publishedFileId),
      page: row.page || null,
    };
  });
}

async function fetchPublishedFileDetails(ids) {
  const body = new URLSearchParams();
  body.set("itemcount", String(ids.length));
  ids.forEach((id, i) => body.set(`publishedfileids[${i}]`, id));

  const res = await fetch(STEAM_URL, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body,
  });
  if (!res.ok) {
    throw new Error(`HTTP ${res.status} from Steam GetPublishedFileDetails`);
  }
  const json = await res.json();
  const details = json?.response?.publishedfiledetails;
  if (!Array.isArray(details)) {
    throw new Error("Steam response missing publishedfiledetails");
  }
  const byId = new Map();
  for (const d of details) {
    if (d?.publishedfileid != null) byId.set(String(d.publishedfileid), d);
  }
  return byId;
}

function num(v, fallback = 0) {
  const n = Number(v);
  return Number.isFinite(n) ? n : fallback;
}

function buildModStats(mod, detail, previous) {
  const prev = previous?.mods?.[mod.id] || {};
  if (!detail || Number(detail.result) !== 1) {
    console.warn(`[stats] No details for ${mod.id} (${mod.publishedFileId}); keeping prior cache`);
    return {
      name: mod.name,
      publishedFileId: mod.publishedFileId,
      subscriptions: num(prev.subscriptions),
      favorited: num(prev.favorited),
      views: num(prev.views),
      lifetime_subscriptions: num(prev.lifetime_subscriptions),
      lifetime_favorited: num(prev.lifetime_favorited),
      title: prev.title || mod.name,
      stale: true,
    };
  }
  return {
    name: mod.name,
    publishedFileId: mod.publishedFileId,
    title: detail.title || mod.name,
    subscriptions: num(detail.subscriptions),
    favorited: num(detail.favorited),
    views: num(detail.views),
    lifetime_subscriptions: num(detail.lifetime_subscriptions),
    lifetime_favorited: num(detail.lifetime_favorited),
  };
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

function writeCacheAtomic(data) {
  ensureParentDir(CACHE_PATH);
  fs.writeFileSync(TMP_PATH, `${JSON.stringify(data, null, 2)}\n`, "utf8");
  fs.renameSync(TMP_PATH, CACHE_PATH);
}

async function main() {
  const force = isForceRefresh();
  const cache = readCache();
  const now = new Date();
  if (!force && cache?.lastFetched) {
    const last = new Date(cache.lastFetched);
    if (!Number.isNaN(last.valueOf()) && utcHourBucket(last) === utcHourBucket(now)) {
      console.log("Stats already up to date (same UTC hour). Use --force or STATS_FORCE=1 to refresh anyway.");
      return;
    }
  }
  if (force) {
    console.log("[stats] Force refresh: bypassing same-hour short-circuit");
  }

  const roster = loadRoster();
  const ids = roster.map((m) => m.publishedFileId);
  const byId = await fetchPublishedFileDetails(ids);

  const mods = {};
  for (const mod of roster) {
    mods[mod.id] = buildModStats(mod, byId.get(mod.publishedFileId), cache);
    const s = mods[mod.id];
    console.log(
      `[stats] ${mod.id}: ${s.subscriptions} subs, ${s.favorited} favs${s.stale ? " (stale)" : ""}`
    );
  }

  const next = {
    lastFetched: now.toISOString(),
    mods,
    site_total: buildSiteTotal(mods),
  };
  writeCacheAtomic(next);
  console.log(`[stats] Updated ${CACHE_PATH}`);
  console.log(
    `[stats] Site total: ${next.site_total.subscriptions} subs, ${next.site_total.favorited} favs`
  );
}

main().catch((err) => {
  console.error("[stats] Fatal error:", err);
  process.exitCode = 1;
});
