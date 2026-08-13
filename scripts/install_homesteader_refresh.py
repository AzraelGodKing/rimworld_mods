#!/usr/bin/env python3
"""Copy Homesteader refresh sprites into Textures/HomesteaderRefresh and size them."""
from __future__ import annotations

import hashlib
import json
import shutil
import sys
from pathlib import Path

from PIL import Image

REPO = Path("/workspace")
ASSETS = Path("/opt/cursor/artifacts/assets")
TEX = REPO / "Homesteader/Textures"
CATALOG = Path("/tmp/hs_refresh_catalog.json")
MAP = Path("/tmp/hs_refresh_name_map.json")


def flood_transparent(im: Image.Image, thresh: int = 18) -> Image.Image:
    """Punch studio backdrops to alpha, but keep black outline pixels that touch content.

    Vanilla RimWorld sprites use thick near-black outlines. A naive flood of
    near-black from the corners would eat those outlines along with the backdrop.
    """
    im = im.convert("RGBA")
    px = im.load()
    w, h = im.size
    corners = [px[0, 0], px[w - 1, 0], px[0, h - 1], px[w - 1, h - 1]]

    def is_bg(c):
        r, g, b, a = c
        if a < 8:
            return True
        # near-black or near-white uniform studio backdrops
        if r <= thresh and g <= thresh and b <= thresh:
            return True
        if r >= 255 - thresh and g >= 255 - thresh and b >= 255 - thresh:
            return True
        return False

    def is_near_black(c):
        r, g, b, a = c
        return a >= 8 and r <= thresh and g <= thresh and b <= thresh

    def is_content(c):
        r, g, b, a = c
        if a < 8:
            return False
        if r <= thresh and g <= thresh and b <= thresh:
            return False
        return True

    if not any(is_bg(c) for c in corners):
        return im
    seen = [[False] * w for _ in range(h)]
    stack = []
    for x, y in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
        stack.append((x, y))
    while stack:
        x, y = stack.pop()
        if x < 0 or y < 0 or x >= w or y >= h or seen[y][x]:
            continue
        seen[y][x] = True
        if not is_bg(px[x, y]):
            continue
        if is_near_black(px[x, y]):
            keep_outline = False
            for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1), (-1, -1), (1, -1), (-1, 1), (1, 1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and is_content(px[nx, ny]):
                    keep_outline = True
                    break
            if keep_outline:
                continue
        px[x, y] = (0, 0, 0, 0)
        stack.extend(((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))
    return im


def process(src: Path, dest: Path, tw: int, th: int) -> None:
    im = Image.open(src)
    if dest.as_posix().find("/Terrain/") >= 0:
        im = im.convert("RGBA").resize((tw, th), Image.Resampling.LANCZOS)
    else:
        im = flood_transparent(im)
        im.thumbnail((tw, th), Image.Resampling.LANCZOS)
        canvas = Image.new("RGBA", (tw, th), (0, 0, 0, 0))
        x = (tw - im.width) // 2
        y = (th - im.height) // 2
        canvas.paste(im, (x, y), im)
        im = canvas
    dest.parent.mkdir(parents=True, exist_ok=True)
    im.save(dest, "PNG")


def main() -> int:
    catalog = {f["dest"]: f for f in json.loads(CATALOG.read_text())}
    name_map = json.loads(MAP.read_text()) if MAP.exists() else {}
    installed = 0
    missing = []
    for filename, dest_rel in name_map.items():
        src = ASSETS / filename
        if not src.exists():
            missing.append(filename)
            continue
        meta = catalog[dest_rel]
        process(src, TEX / dest_rel, meta["w"], meta["h"])
        installed += 1
    hashes = {}
    for p in (TEX / "HomesteaderRefresh").rglob("*.png"):
        hashes.setdefault(hashlib.md5(p.read_bytes()).hexdigest(), []).append(str(p.relative_to(TEX)))
    dups = {k: v for k, v in hashes.items() if len(v) > 1}
    print(f"installed={installed} missing={len(missing)} unique={len(hashes)} dup_groups={len(dups)}")
    if missing:
        print("MISSING", missing)
    if dups:
        print("DUPS", dups)
    return 0


if __name__ == "__main__":
    sys.exit(main())
