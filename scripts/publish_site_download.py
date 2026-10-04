#!/usr/bin/env python3
"""Upload one zip to the mods website bucket and record it in downloads.json.

Same layout the site links: <game>/<slug>/<version>/<slug>-<version>.zip
plus a .sha256 file. downloads.json at the bucket root is the hosted-version record.

Requires CLOUDFLARE_API_TOKEN and CLOUDFLARE_ACCOUNT_ID unless --dry-run.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
from datetime import datetime, timezone
from pathlib import Path

# Website slugs follow the mod folder name, except this Sun Haven package.
SLUG_OVERRIDES = {"SunHavenMuseumUtilityTracker": "smut"}


def slug_from_mod_dir(mod_dir: str) -> str:
    if mod_dir in SLUG_OVERRIDES:
        return SLUG_OVERRIDES[mod_dir]
    slug = re.sub(r"([a-z0-9])([A-Z])", r"\1-\2", mod_dir)
    slug = re.sub(r"[^a-zA-Z0-9]+", "-", slug).strip("-").lower()
    return slug

BUCKET = "azraels-mods"
MANIFEST_KEY = "downloads.json"


def file_name(slug: str, version: str) -> str:
    return f"{slug}-{version}.zip"


def object_key(game: str, slug: str, version: str) -> str:
    return f"{game}/{slug}/{version}/{file_name(slug, version)}"


def record_upload(manifest: dict | None, entry: dict) -> dict:
    files = dict((manifest or {}).get("files") or {})
    files[f"{entry['game']}/{entry['slug']}"] = {
        "version": entry["version"],
        "file": file_name(entry["slug"], entry["version"]),
        "sha256": entry["sha256"],
        "bytes": entry["bytes"],
        "uploadedAt": entry["uploadedAt"],
    }
    return {"updatedAt": entry["uploadedAt"], "files": files}


def wrangler(args: list[str]) -> subprocess.CompletedProcess[str]:
    command = ["npx", "--yes", "wrangler@4", "r2", "object", *args]
    return subprocess.run(command, check=False, text=True, capture_output=True)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--game", required=True)
    parser.add_argument("--slug", default="")
    parser.add_argument("--mod-dir", default="", help="Mod folder name. Used when --slug is omitted.")
    parser.add_argument("--version", required=True)
    parser.add_argument("--zip", required=True, dest="zip_path")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    slug = args.slug or slug_from_mod_dir(args.mod_dir)
    if not slug:
        print("Pass --slug or --mod-dir", file=sys.stderr)
        return 1

    zip_path = Path(args.zip_path)
    if not zip_path.is_file():
        print(f"Missing zip {zip_path}", file=sys.stderr)
        return 1

    data = zip_path.read_bytes()
    sha256 = hashlib.sha256(data).hexdigest()
    key = object_key(args.game, slug, args.version)
    uploaded_at = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
    print(f"{'would upload' if args.dry_run else 'uploading'} {BUCKET}/{key}")
    print(f"sha256 {sha256} ({len(data)} bytes)")
    if args.dry_run:
        return 0

    token = os.environ.get("CLOUDFLARE_API_TOKEN") or ""
    account = os.environ.get("CLOUDFLARE_ACCOUNT_ID") or ""
    if not token or not account:
        print(
            "::warning::CLOUDFLARE_API_TOKEN or CLOUDFLARE_ACCOUNT_ID is missing. "
            "The website download was not updated.",
            file=sys.stderr,
        )
        return 0
    if shutil.which("npx") is None:
        print("npx is required to upload with wrangler", file=sys.stderr)
        return 1

    with tempfile.TemporaryDirectory() as tmp:
        root = Path(tmp)
        manifest_path = root / "downloads.json"
        got = wrangler(["get", f"{BUCKET}/{MANIFEST_KEY}", "--file", str(manifest_path), "--remote"])
        manifest: dict = {"updatedAt": None, "files": {}}
        if got.returncode == 0 and manifest_path.is_file():
            try:
                manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
            except json.JSONDecodeError:
                manifest = {"updatedAt": None, "files": {}}
        nxt = record_upload(
            manifest,
            {
                "game": args.game,
                "slug": slug,
                "version": args.version,
                "sha256": sha256,
                "bytes": len(data),
                "uploadedAt": uploaded_at,
            },
        )
        manifest_path.write_text(json.dumps(nxt, indent=2) + "\n", encoding="utf-8")
        checksum_path = root / "file.sha256"
        checksum_path.write_text(f"{sha256}  {file_name(slug, args.version)}\n", encoding="utf-8")

        uploads = [
            ["put", f"{BUCKET}/{key}", "--file", str(zip_path), "--remote", "--content-type", "application/zip"],
            [
                "put",
                f"{BUCKET}/{key}.sha256",
                "--file",
                str(checksum_path),
                "--remote",
                "--content-type",
                "text/plain; charset=utf-8",
            ],
            [
                "put",
                f"{BUCKET}/{MANIFEST_KEY}",
                "--file",
                str(manifest_path),
                "--remote",
                "--content-type",
                "application/json; charset=utf-8",
                "--cache-control",
                "public, max-age=60",
            ],
        ]
        for item in uploads:
            result = wrangler(item)
            if result.returncode != 0:
                detail = (result.stdout + "\n" + result.stderr).strip()
                print(detail or f"wrangler failed ({result.returncode})", file=sys.stderr)
                return result.returncode or 1
    print(f"recorded {args.game}/{slug} {args.version}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
