import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import { fileURLToPath } from "node:url";
import path from "node:path";
import fs from "node:fs";

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const docsDir = path.join(repoRoot, "docs");

const MIME = {
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".gif": "image/gif",
  ".webp": "image/webp",
  ".svg": "image/svg+xml",
  ".css": "text/css",
  ".js": "text/javascript",
  ".json": "application/json",
  ".html": "text/html",
  ".md": "text/markdown",
};

/** Dev-only: serve legacy docs/ assets (img, data, assets, legacy pages) so the
 *  app sees the same URLs it will have in production, where postbuild copies
 *  them into dist. */
function serveDocsAssets() {
  return {
    name: "serve-docs-assets",
    apply: "serve",
    configureServer(server) {
      server.middlewares.use((req, res, next) => {
        let url = (req.url || "").split("?")[0];
        if (url.startsWith("/rimworld_mods/")) url = url.slice("/rimworld_mods".length);
        const m = url.match(/^\/(img|assets|data|scripts|ideas)\/(.+)$|^\/([\w-]+\.(?:css|html|md))$/);
        if (!m) return next();
        const rel = m[3] ? m[3] : `${m[1]}/${m[2]}`;
        const file = path.join(docsDir, rel);
        if (!file.startsWith(docsDir) || !fs.existsSync(file) || !fs.statSync(file).isFile()) {
          return next();
        }
        res.setHeader("Content-Type", MIME[path.extname(file).toLowerCase()] || "application/octet-stream");
        fs.createReadStream(file).pipe(res);
      });
    },
  };
}

export default defineConfig({
  base: "/rimworld_mods/",
  build: {
    // keep hashed app bundles out of the legacy docs assets/ folder
    assetsDir: "app",
  },
  plugins: [vue(), serveDocsAssets()],
  server: {
    fs: {
      // allow ?raw imports of mod CHANGELOG.md files at the repo root
      allow: [repoRoot],
    },
  },
});
