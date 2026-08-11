// Unlisted admin console for Workshop stats.
// Passphrase gate is client-side obscurity (SHA-256 hash in docs/data/admin-gate.json).
// Never schedules commits — refresh is on-demand only.

const UNLOCK_KEY = "azrael-admin-stats-unlocked-v1";
const TOKEN_KEY = "azrael-admin-gh-token-v1";
const REPO = {
  owner: "AzraelGodKing",
  repo: "rimworld_mods",
  path: "docs/data/stats-cache.json",
  branch: "main",
};

let lastStats = null;

function $(id) {
  return document.getElementById(id);
}

function setStatus(el, message, tone) {
  if (!el) return;
  el.textContent = message || "";
  if (tone) el.setAttribute("data-tone", tone);
  else el.removeAttribute("data-tone");
}

async function sha256Hex(text) {
  const data = new TextEncoder().encode(text);
  const digest = await crypto.subtle.digest("SHA-256", data);
  return Array.from(new Uint8Array(digest))
    .map((b) => b.toString(16).padStart(2, "0"))
    .join("");
}

async function loadExpectedHash() {
  const api = window.AzraelWorkshopStats;
  const url = api.resolveDocsDataUrl("admin-gate.json");
  const res = await fetch(url, { cache: "no-cache" });
  if (!res.ok) throw new Error("Could not load admin-gate.json (" + res.status + ")");
  const json = await res.json();
  const hash = String(json.passphraseSha256 || "").trim().toLowerCase();
  if (!/^[a-f0-9]{64}$/.test(hash)) throw new Error("admin-gate.json has no valid passphraseSha256");
  return hash;
}

function isUnlocked() {
  try {
    return sessionStorage.getItem(UNLOCK_KEY) === "1";
  } catch {
    return false;
  }
}

function setUnlocked(on) {
  try {
    if (on) sessionStorage.setItem(UNLOCK_KEY, "1");
    else sessionStorage.removeItem(UNLOCK_KEY);
  } catch {
    /* ignore */
  }
}

function showConsole(show) {
  $("gate").classList.toggle("admin-hidden", show);
  $("console").classList.toggle("admin-hidden", !show);
}

function renderStats(stats) {
  lastStats = stats;
  const tbody = $("stats-rows");
  const api = window.AzraelWorkshopStats;
  const mods = stats?.mods || {};
  const rows = Object.keys(mods).sort();
  if (!rows.length) {
    tbody.innerHTML = "<tr><td colspan=\"4\">No mods in response.</td></tr>";
  } else {
    tbody.innerHTML = rows
      .map((id) => {
        const m = mods[id];
        return (
          "<tr>" +
          "<td><b>" +
          escapeHtml(m.name || id) +
          "</b></td>" +
          "<td>" +
          api.formatValue(m.subscriptions) +
          "</td>" +
          "<td>" +
          api.formatValue(m.favorited) +
          "</td>" +
          "<td>" +
          api.formatValue(m.views) +
          "</td>" +
          "</tr>"
        );
      })
      .join("");
    const total = stats.site_total || {};
    tbody.innerHTML +=
      "<tr>" +
      "<td><b>Total</b></td>" +
      "<td>" +
      api.formatValue(total.subscriptions) +
      "</td>" +
      "<td>" +
      api.formatValue(total.favorited) +
      "</td>" +
      "<td>" +
      api.formatValue(total.views) +
      "</td>" +
      "</tr>";
  }

  $("stats-json").value = JSON.stringify(stats, null, 2) + "\n";
}

function escapeHtml(s) {
  return String(s)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function downloadJson(stats) {
  const blob = new Blob([JSON.stringify(stats, null, 2) + "\n"], {
    type: "application/json",
  });
  const a = document.createElement("a");
  a.href = URL.createObjectURL(blob);
  a.download = "stats-cache.json";
  a.click();
  URL.revokeObjectURL(a.href);
}

function toBase64Utf8(text) {
  // GitHub Contents API expects base64 of UTF-8 bytes.
  const bytes = new TextEncoder().encode(text);
  let binary = "";
  bytes.forEach((b) => {
    binary += String.fromCharCode(b);
  });
  return btoa(binary);
}

async function publishToGitHub(stats, token) {
  const apiBase =
    "https://api.github.com/repos/" +
    REPO.owner +
    "/" +
    REPO.repo +
    "/contents/" +
    REPO.path;
  const headers = {
    Accept: "application/vnd.github+json",
    Authorization: "Bearer " + token,
    "X-GitHub-Api-Version": "2022-11-28",
  };

  const getRes = await fetch(apiBase + "?ref=" + encodeURIComponent(REPO.branch), {
    headers,
  });
  if (!getRes.ok) {
    throw new Error("GitHub GET failed HTTP " + getRes.status);
  }
  const current = await getRes.json();
  const body = {
    message: "chore(docs): refresh Steam Workshop stats cache (admin)",
    content: toBase64Utf8(JSON.stringify(stats, null, 2) + "\n"),
    branch: REPO.branch,
    sha: current.sha,
  };
  const putRes = await fetch(apiBase, {
    method: "PUT",
    headers: { ...headers, "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!putRes.ok) {
    const errText = await putRes.text();
    throw new Error("GitHub PUT failed HTTP " + putRes.status + ": " + errText.slice(0, 200));
  }
  return putRes.json();
}

async function onUnlock(event) {
  event.preventDefault();
  const status = $("gate-status");
  setStatus(status, "Checking…");
  try {
    const expected = await loadExpectedHash();
    const given = await sha256Hex($("passphrase").value);
    if (given !== expected) {
      setStatus(status, "Passphrase did not match.", "error");
      return;
    }
    setUnlocked(true);
    $("passphrase").value = "";
    showConsole(true);
    setStatus($("console-status"), "Unlocked. Hit Force refresh when ready.", "ok");
  } catch (err) {
    setStatus(status, String(err.message || err), "error");
  }
}

async function onRefresh() {
  const status = $("console-status");
  const btn = $("btn-refresh");
  btn.disabled = true;
  setStatus(status, "Fetching live Steam Workshop stats…");
  try {
    const stats = await window.AzraelWorkshopStats.refreshLiveStats({ force: true });
    renderStats(stats);
    setStatus(
      status,
      "Refreshed " +
        new Date(stats.lastFetched).toLocaleString() +
        " via " +
        (stats.source || "steam") +
        ".",
      "ok"
    );
  } catch (err) {
    setStatus(status, "Refresh failed: " + (err.message || err), "error");
  } finally {
    btn.disabled = false;
  }
}

function onDownload() {
  if (!lastStats) {
    setStatus($("console-status"), "Refresh first, then download.", "error");
    return;
  }
  downloadJson(lastStats);
  setStatus($("console-status"), "Downloaded stats-cache.json.", "ok");
}

function onClear() {
  window.AzraelWorkshopStats.clearLocalCache();
  setStatus($("console-status"), "Cleared this browser’s stats cache.", "ok");
}

function onLock() {
  setUnlocked(false);
  try {
    sessionStorage.removeItem(TOKEN_KEY);
  } catch {
    /* ignore */
  }
  $("gh-token").value = "";
  showConsole(false);
  setStatus($("gate-status"), "Locked.", "ok");
}

async function onPublish() {
  const status = $("console-status");
  if (!lastStats) {
    setStatus(status, "Refresh first, then publish.", "error");
    return;
  }
  const token = ($("gh-token").value || "").trim();
  if (!token) {
    setStatus(status, "Paste a GitHub token first.", "error");
    return;
  }
  try {
    sessionStorage.setItem(TOKEN_KEY, token);
  } catch {
    /* ignore */
  }
  const btn = $("btn-publish");
  btn.disabled = true;
  setStatus(status, "Publishing docs/data/stats-cache.json to main…");
  try {
    await publishToGitHub(lastStats, token);
    setStatus(
      status,
      "Published. Pages will redeploy from main. This was a manual commit — not a scheduled runner.",
      "ok"
    );
  } catch (err) {
    setStatus(status, String(err.message || err), "error");
  } finally {
    btn.disabled = false;
  }
}

document.addEventListener("DOMContentLoaded", () => {
  $("unlock-form").addEventListener("submit", onUnlock);
  $("btn-refresh").addEventListener("click", onRefresh);
  $("btn-download").addEventListener("click", onDownload);
  $("btn-clear").addEventListener("click", onClear);
  $("btn-lock").addEventListener("click", onLock);
  $("btn-publish").addEventListener("click", onPublish);

  try {
    const saved = sessionStorage.getItem(TOKEN_KEY);
    if (saved) $("gh-token").value = saved;
  } catch {
    /* ignore */
  }

  if (isUnlocked()) {
    showConsole(true);
    setStatus($("console-status"), "Session still unlocked. Force refresh when ready.", "ok");
  }
});
