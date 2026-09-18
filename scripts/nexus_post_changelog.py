#!/usr/bin/env python3
"""POST a changelog entry to Nexus v3 after a file version already exists.

Does not upload a zip. Exit 0 on HTTP 2xx, 1 otherwise.
"""
from __future__ import annotations

import argparse
import json
import os
import sys
import urllib.error
import urllib.request


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--mod-id", required=True, help="Nexus v3 global mod id")
    parser.add_argument("--version", required=True)
    parser.add_argument(
        "--changelog",
        default="",
        help="Changelog text. Default: NEXUS_CHANGELOG env (supports multiline).",
    )
    args = parser.parse_args()

    api_key = os.environ.get("NEXUS_API_KEY") or os.environ.get("NEXUSMODS_API_KEY") or ""
    if not api_key:
        print("NEXUS_API_KEY is missing", file=sys.stderr)
        return 1

    changelog = args.changelog or os.environ.get("NEXUS_CHANGELOG") or ""
    if not changelog.strip():
        print("Changelog text is empty", file=sys.stderr)
        return 1

    url = f"https://api.nexusmods.com/v3/mods/{args.mod_id}/changelogs"
    body = json.dumps({"version": args.version, "changelog": changelog}).encode("utf-8")
    req = urllib.request.Request(
        url,
        data=body,
        method="POST",
        headers={
            "accept": "application/json",
            "content-type": "application/json",
            "apikey": api_key,
            "User-Agent": "rimworld_mods/nexus_post_changelog",
        },
    )
    try:
        with urllib.request.urlopen(req, timeout=60) as resp:
            print(f"Nexus changelog posted ({resp.status}) for v{args.version}")
            return 0
    except urllib.error.HTTPError as exc:
        raw = exc.read().decode("utf-8", errors="replace")
        print(f"Nexus changelog POST failed: {exc.code} {raw[:800]}", file=sys.stderr)
        return 1
    except (urllib.error.URLError, TimeoutError) as exc:
        print(f"Nexus changelog POST failed: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
