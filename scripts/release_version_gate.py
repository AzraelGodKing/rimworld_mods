#!/usr/bin/env python3
"""Compare About.xml modVersion to GitHub tags and Nexus page/file versions.

Used by Release & Publish to skip mods that are not new.

Stdlib only. Run from repo root.
"""
from __future__ import annotations

import argparse
import functools
import json
import os
import re
import sys
import urllib.error
import urllib.request
from pathlib import Path

from release_meta import parse_version, write_github_output

REPO = Path(__file__).resolve().parent.parent
MATRIX_PATH = REPO / "scripts" / "matrix" / "mod-matrix.json"
GAME = "rimworld"
SEMVER = re.compile(r"(\d+(?:\.\d+)*)")
UNFETCHED = "unfetched"
NONE = "none"


def normalize_version(value: str) -> str:
    text = (value or "").strip()
    if text in {UNFETCHED, NONE}:
        return ""
    text = re.sub(r"^[vV]", "", text)
    match = SEMVER.search(text)
    return match.group(1) if match else text


def version_key(value: str) -> tuple[int, ...]:
    parts: list[int] = []
    for piece in normalize_version(value).split("."):
        try:
            parts.append(int(piece))
        except ValueError:
            parts.append(0)
    return tuple(parts)


def already_published(about: str, remote: str) -> bool:
    """True when About is not newer than the remote version."""
    about_n = normalize_version(about)
    remote_n = normalize_version(remote)
    if not about_n or not remote_n:
        return False
    if about_n == remote_n:
        return True
    try:
        return version_key(about_n) <= version_key(remote_n)
    except (TypeError, ValueError):
        return False


def display_ver(value: str) -> str:
    return value if value else NONE


def load_matrix() -> list[dict]:
    return json.loads(MATRIX_PATH.read_text(encoding="utf-8"))


def row_for(mod_key: str) -> dict:
    for row in load_matrix():
        if row.get("modKey") == mod_key:
            return row
    raise SystemExit(f"Unknown mod key {mod_key}")


def about_version(mod_dir: str) -> str:
    about_path = REPO / mod_dir / "About" / "About.xml"
    if not about_path.is_file():
        raise SystemExit(f"Missing {about_path}")
    version = parse_version(about_path.read_text(encoding="utf-8"))
    if not version:
        raise SystemExit(f"No modVersion in {about_path}")
    return version


def http_json(url: str, headers: dict[str, str]) -> tuple[int, object]:
    req = urllib.request.Request(url, headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            body = resp.read().decode("utf-8")
            return resp.status, json.loads(body) if body else {}
    except urllib.error.HTTPError as exc:
        raw = exc.read().decode("utf-8", errors="replace")
        try:
            parsed: object = json.loads(raw) if raw else {}
        except json.JSONDecodeError:
            parsed = {}
        return exc.code, parsed
    except (urllib.error.URLError, TimeoutError, json.JSONDecodeError):
        return 0, {}


@functools.lru_cache(maxsize=4)
def github_release_tags(api_url: str, repo: str, token: str) -> tuple[str, ...] | None:
    if not token or not repo:
        return None
    status, body = http_json(
        f"{api_url.rstrip('/')}/repos/{repo}/releases?per_page=100",
        {
            "Accept": "application/vnd.github+json",
            "Authorization": f"Bearer {token}",
        },
    )
    if status != 200 or not isinstance(body, list):
        return None
    return tuple(str(item.get("tag_name") or "") for item in body if item.get("tag_name"))


def github_tag_exists(api_url: str, repo: str, tag: str, token: str) -> bool:
    if not tag:
        return False
    tags = github_release_tags(api_url, repo, token)
    return bool(tags) and tag in tags


def github_latest_version(api_url: str, repo: str, zip_name: str, token: str) -> str:
    tags = github_release_tags(api_url, repo, token)
    if tags is None:
        return UNFETCHED
    prefix = f"{zip_name}-v"
    best_ver = ""
    best_key: tuple[int, ...] | None = None
    for tag in tags:
        if not tag.startswith(prefix):
            continue
        ver = tag[len(prefix) :]
        if not normalize_version(ver):
            continue
        key = version_key(ver)
        if best_key is None or key > best_key:
            best_key = key
            best_ver = normalize_version(ver)
    return best_ver or NONE


def nexus_page_version(mod_id: str, api_key: str) -> str:
    if not mod_id or not api_key:
        return UNFETCHED
    status, body = http_json(
        f"https://api.nexusmods.com/v1/games/{GAME}/mods/{mod_id}.json",
        {"accept": "application/json", "apikey": api_key},
    )
    if status != 200 or not isinstance(body, dict):
        return UNFETCHED
    return display_ver(str(body.get("version") or ""))


def nexus_file_version(mod_id: str, file_id: str, api_key: str) -> str:
    if not mod_id or not api_key:
        return UNFETCHED
    status, body = http_json(
        f"https://api.nexusmods.com/v1/games/{GAME}/mods/{mod_id}/files.json",
        {"accept": "application/json", "apikey": api_key},
    )
    if status != 200 or not isinstance(body, dict):
        return UNFETCHED
    files = body.get("files") or []
    matched = []
    for item in files:
        if not isinstance(item, dict):
            continue
        version = str(item.get("version") or "")
        if not version:
            continue
        raw_id = item.get("file_id", item.get("fileid"))
        if raw_id is None and isinstance(item.get("id"), list) and item["id"]:
            raw_id = item["id"][0]
        if file_id and str(raw_id) != str(file_id):
            continue
        matched.append((int(item.get("uploaded_timestamp") or 0), version))
    if not matched:
        return NONE
    matched.sort()
    return display_ver(matched[-1][1])


def evaluate_mod(
    mod_key: str,
    *,
    repo: str,
    api_url: str,
    github_token: str,
    nexus_key: str,
    create_github: bool,
    publish_nexus: bool,
) -> dict[str, str]:
    row = row_for(mod_key)
    about = about_version(str(row["modDir"]))
    zip_name = str(row.get("zipName") or "")
    tag = f"{zip_name}-v{about}" if zip_name else ""
    github_ver = github_latest_version(api_url, repo, zip_name, github_token)
    github_hit = github_tag_exists(api_url, repo, tag, github_token)

    nexus_publish = row.get("nexus_publish") is not False
    mod_id = str(row.get("nexus_mod_id") or "")
    file_id = str(row.get("nexus_file_id") or "")
    page_v = nexus_page_version(mod_id, nexus_key) if mod_id else NONE
    file_v = nexus_file_version(mod_id, file_id, nexus_key) if mod_id else NONE
    nexus_ver = page_v if page_v not in {NONE, UNFETCHED} else file_v
    nexus_hit = False
    if nexus_publish and mod_id:
        nexus_hit = already_published(about, page_v) or already_published(about, file_v)

    github_work = create_github and not github_hit
    nexus_work = publish_nexus and nexus_publish and bool(file_id) and not nexus_hit
    sources = []
    if github_hit:
        sources.append("github")
    if nexus_hit:
        sources.append("nexus")
    return {
        "mod_key": mod_key,
        "about_version": about,
        "github_version": github_ver,
        "github_tag": tag,
        "github_unchanged": "true" if github_hit else "false",
        "nexus_version": nexus_ver,
        "nexus_page_version": page_v,
        "nexus_file_version": file_v,
        "nexus_unchanged": "true" if nexus_hit else "false",
        "upstream_nexus_version": nexus_ver,
        "version_match_sources": ",".join(sources),
        "has_work": "true" if github_work or nexus_work else "false",
        "display_name": str(row.get("zipName") or mod_key),
    }


def truthy(value: str) -> bool:
    return value.strip().lower() in {"1", "true", "yes"}


def write_compare_table(rows: list[dict[str, str]], keep: set[str], bypass: bool) -> str:
    lines = [
        "## Version gate",
        "",
        "| Mod | About.xml | GitHub | Nexus | Action |",
        "| --- | --- | --- | --- | --- |",
    ]
    for info in rows:
        action = "pack (dry-run / anyway)" if bypass else ("ship" if info["mod_key"] in keep else "skip")
        lines.append(
            f"| `{info['mod_key']}` | `{info['about_version']}` | `{info['github_version']}` "
            f"| `{info['nexus_version']}` (page `{info['nexus_page_version']}`, file `{info['nexus_file_version']}`) "
            f"| {action} |"
        )
    lines.append("")
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--mod-key")
    parser.add_argument("--keys", default="", help="Comma-separated mod keys for --select-new")
    parser.add_argument("--select-new", action="store_true")
    parser.add_argument("--repo", default=os.environ.get("GITHUB_REPOSITORY", ""))
    parser.add_argument("--api-url", default=os.environ.get("GITHUB_API_URL", "https://api.github.com"))
    parser.add_argument("--create-github-release", default="true")
    parser.add_argument("--publish-nexus", default="false")
    parser.add_argument("--dry-run", default="false")
    parser.add_argument("--release-anyway", default="false")
    parser.add_argument("--github-output", action="store_true")
    args = parser.parse_args()

    github_token = os.environ.get("GITHUB_TOKEN") or os.environ.get("GH_TOKEN") or ""
    nexus_key = os.environ.get("NEXUS_API_KEY") or os.environ.get("NEXUSMODS_API_KEY") or ""
    create_github = truthy(args.create_github_release)
    publish_nexus = truthy(args.publish_nexus)
    bypass = truthy(args.dry_run) or truthy(args.release_anyway)

    def dump(values: dict[str, str]) -> None:
        if args.github_output:
            out = os.environ.get("GITHUB_OUTPUT")
            if not out:
                print("GITHUB_OUTPUT is not set", file=sys.stderr)
                raise SystemExit(1)
            write_github_output(Path(out), values)
        else:
            for key, value in values.items():
                print(f"{key}={value}")

    if args.select_new:
        keys = [k.strip() for k in args.keys.split(",") if k.strip()]
        if not keys:
            print("--keys is required with --select-new", file=sys.stderr)
            return 1
        rows: list[dict[str, str]] = []
        keep: list[str] = []
        skipped: list[str] = []
        for key in keys:
            info = evaluate_mod(
                key,
                repo=args.repo,
                api_url=args.api_url,
                github_token=github_token,
                nexus_key=nexus_key,
                create_github=create_github,
                publish_nexus=publish_nexus,
            )
            rows.append(info)
            if bypass or info["has_work"] == "true":
                keep.append(key)
                continue
            skipped.append(
                f"{info['display_name']} About `{info['about_version']}` GitHub `{info['github_version']}` "
                f"Nexus `{info['nexus_version']}`"
            )
        values = {
            "matrix": json.dumps(keep, separators=(",", ":")),
            "has_mods": "true" if keep else "false",
            "skipped": "; ".join(skipped),
        }
        dump(values)
        table = write_compare_table(rows, set(keep), bypass)
        print(table, file=sys.stderr)
        summary = os.environ.get("GITHUB_STEP_SUMMARY")
        if summary:
            with Path(summary).open("a", encoding="utf-8") as fh:
                fh.write(table)
        return 0

    if not args.mod_key:
        print("--mod-key or --select-new is required", file=sys.stderr)
        return 1

    info = evaluate_mod(
        args.mod_key,
        repo=args.repo,
        api_url=args.api_url,
        github_token=github_token,
        nexus_key=nexus_key,
        create_github=create_github,
        publish_nexus=publish_nexus,
    )
    if bypass:
        info["github_unchanged"] = "false"
        info["nexus_unchanged"] = "false"
        info["has_work"] = "true"
    dump(info)
    print(
        f"About.xml={info['about_version']} GitHub={info['github_version']} "
        f"Nexus={info['nexus_version']} (page {info['nexus_page_version']}, file {info['nexus_file_version']})",
        file=sys.stderr,
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
