// Post-build for GitHub Pages:
// 1. Merge legacy docs/ content (images, data, extra pages) into dist/,
//    excluding pages the Vue app replaces.
// 2. Write meta-refresh stubs so old .html deep links land on the new routes.
// 3. Copy index.html to 404.html so history-mode routing works on Pages.
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const siteDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const repoRoot = path.resolve(siteDir, "..");
const docsDir = path.join(repoRoot, "docs");
const distDir = path.join(siteDir, "dist");

// pages the Vue app replaces -> new route
const replaced = {
  "index.html": null,
  "strata.html": "strata",
  "homesteader.html": "homesteader",
  "stormproof.html": "stormproof",
  "nemesis.html": "nemesis",
  "deep-colony.html": "deep-colony",
  "datenight.html": "date-night",
};

function copyRecursive(src, dest, skip = () => false) {
  for (const entry of fs.readdirSync(src, { withFileTypes: true })) {
    const from = path.join(src, entry.name);
    const to = path.join(dest, entry.name);
    const rel = path.relative(docsDir, from).replaceAll("\\", "/");
    if (skip(rel, entry)) continue;
    if (entry.isDirectory()) {
      fs.mkdirSync(to, { recursive: true });
      copyRecursive(from, to, skip);
    } else {
      fs.mkdirSync(path.dirname(to), { recursive: true });
      fs.copyFileSync(from, to);
    }
  }
}

copyRecursive(docsDir, distDir, (rel) => rel in replaced);

for (const [page, route] of Object.entries(replaced)) {
  if (!route) continue;
  const stub = `<!doctype html>
<html lang="en"><head>
<meta charset="utf-8">
<meta http-equiv="refresh" content="0; url=./${route}">
<link rel="canonical" href="https://azraelgodking.github.io/rimworld_mods/${route}">
<title>Redirecting…</title>
</head><body><p>Moved: <a href="./${route}">continue to the new page</a>.</p></body></html>
`;
  fs.writeFileSync(path.join(distDir, page), stub);
}

fs.copyFileSync(path.join(distDir, "index.html"), path.join(distDir, "404.html"));
fs.writeFileSync(path.join(distDir, ".nojekyll"), "");

console.log("postbuild: legacy docs merged, redirect stubs + 404 shim written");
