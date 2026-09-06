/**
 * Refresh docs/data/stats-cache.json (Steam + Nexus).
 *
 * Used by .github/workflows/live-stats.yml, which copies this file to
 * live.json and force-pushes the `stats` branch only. Do not commit the
 * cache onto main from a runner — that caused hourly rebase noise.
 *
 * Roster: docs/data/workshop-mods.json
 *
 * Usage:
 *   node scripts/fetch-workshop-stats.js
 *   node scripts/fetch-workshop-stats.js --force
 */
const fs = require("fs");
const path = require("path");

const ROOT = path.resolve(__dirname, "..");
const ROSTER_PATH = path.join(ROOT, "docs", "data", "workshop-mods.json");
const CACHE_PATH = path.join(ROOT, "docs", "data", "stats-cache.json");
const TMP_PATH = `${CACHE_PATH}.tmp`;
const STEAM_URL =
  "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";
const NEXUS_GQL = "https://api.nexusmods.com/v2/graphql";
const NEXUS_GAME_ID = "424";

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
    nexus_downloads: 0,
    nexus_endorsements: 0,
  };
}

function loadRoster() {
  if (!fs.existsSync(ROSTER_PATH)) {
    throw new Error(`Missing roster: ${ROSTER_PATH}`);
  }
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
      nexusModId: row.nexusModId ? String(row.nexusModId) : "",
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

async function fetchNexusDetails(ids) {
  const unique = [...new Set(ids.filter(Boolean))];
  const byId = new Map();
  if (unique.length === 0) return byId;
  const fields = unique
    .map((id, i) => `m${i}: mod(gameId: "${NEXUS_GAME_ID}", modId: "${id}") { downloads endorsements }`)
    .join("\n");
  const res = await fetch(NEXUS_GQL, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ query: `query {\n${fields}\n}` }),
  });
  if (!res.ok) {
    throw new Error(`HTTP ${res.status} from Nexus GraphQL`);
  }
  const json = await res.json();
  if (json.errors) {
    throw new Error(json.errors.map((e) => e.message).join("; "));
  }
  unique.forEach((id, i) => {
    const row = json?.data?.[`m${i}`];
    if (row) byId.set(id, row);
  });
  return byId;
}

function num(v, fallback = 0) {
  const n = Number(v);
  return Number.isFinite(n) ? n : fallback;
}

function buildModStats(mod, detail, previous) {
  const prev = previous?.mods?.[mod.id] || {};
  if (!detail || Number(detail.result) !== 1) {
    console.warn(
      `[stats] No details for ${mod.id} (${mod.publishedFileId}); keeping prior cache`
    );
    return {
      name: mod.name,
      publishedFileId: mod.publishedFileId,
      subscriptions: num(prev.subscriptions),
      favorited: num(prev.favorited),
      views: num(prev.views),
      lifetime_subscriptions: num(prev.lifetime_subscriptions),
      lifetime_favorited: num(prev.lifetime_favorited),
      nexus_downloads: num(prev.nexus_downloads),
      nexus_endorsements: num(prev.nexus_endorsements),
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
    nexus_downloads: num(detail.nexus_downloads, num(prev.nexus_downloads)),
    nexus_endorsements: num(detail.nexus_endorsements, num(prev.nexus_endorsements)),
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
    total.nexus_downloads += num(m.nexus_downloads);
    total.nexus_endorsements += num(m.nexus_endorsements);
  }
  return total;
}

function writeCacheAtomic(data) {
  ensureParentDir(CACHE_PATH);
  fs.writeFileSync(TMP_PATH, `${JSON.stringify(data, null, 2)}\n`, "utf8");
  fs.renameSync(TMP_PATH, CACHE_PATH);
}

async function main() {
  console.log(
    "[stats] Writing Steam + Nexus snapshot (site last-resort cache / stats-branch live.json)."
  );
  const force = isForceRefresh();
  const cache = readCache();
  const now = new Date();
  if (!force && cache?.lastFetched) {
    const last = new Date(cache.lastFetched);
    if (!Number.isNaN(last.valueOf()) && utcHourBucket(last) === utcHourBucket(now)) {
      console.log(
        "Stats already up to date (same UTC hour). Use --force or STATS_FORCE=1 to refresh anyway."
      );
      return;
    }
  }
  if (force) {
    console.log("[stats] Force refresh: bypassing same-hour short-circuit");
  }

  const roster = loadRoster();
  const ids = roster.map((m) => m.publishedFileId);
  const byId = await fetchPublishedFileDetails(ids);
  let nexusById = new Map();
  try {
    nexusById = await fetchNexusDetails(roster.map((m) => m.nexusModId));
  } catch (err) {
    console.warn(`[stats] Nexus GraphQL failed: ${err.message}`);
  }

  const mods = {};
  for (const mod of roster) {
    mods[mod.id] = buildModStats(mod, byId.get(mod.publishedFileId), cache);
    const nexus = nexusById.get(mod.nexusModId);
    if (nexus) {
      mods[mod.id].nexus_downloads = num(nexus.downloads);
      mods[mod.id].nexus_endorsements = num(nexus.endorsements);
    }
    const s = mods[mod.id];
    console.log(
      `[stats] ${mod.id}: ${s.subscriptions} subs, ${s.favorited} favs; nexus ${s.nexus_downloads} dls, ${s.nexus_endorsements} endo${s.stale ? " (stale)" : ""}`
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
    `[stats] Site total: ${next.site_total.subscriptions} subs, ${next.site_total.favorited} favs; nexus ${next.site_total.nexus_downloads} dls, ${next.site_total.nexus_endorsements} endo`
  );
}

main().catch((err) => {
  console.error("[stats] Fatal error:", err);
  process.exitCode = 1;
});
