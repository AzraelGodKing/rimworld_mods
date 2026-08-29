#!/usr/bin/env python3
"""Read About.xml + About/changelog.txt for a mod folder. Prints GitHub Actions outputs."""
from __future__ import annotations

import argparse
import os
import re
import sys
from pathlib import Path

VERSION_LINE = re.compile(r"^\d+\.\d+\.\d+\s*$")
MODVERSION = re.compile(r"<modVersion[^>]*>([^<]+)</modVersion>", re.I)
MODNAME = re.compile(r"<name>([^<]+)</name>", re.I)


def parse_version(about: str) -> str:
    m = MODVERSION.search(about)
    return m.group(1).strip() if m else ""


def parse_name(about: str) -> str:
    m = MODNAME.search(about)
    return m.group(1).strip() if m else ""


def extract_changelog_block(text: str, version: str) -> str:
    lines = text.splitlines()
    start = None
    for i, line in enumerate(lines):
        stripped = line.strip()
        if stripped.startswith("#"):
            continue
        if stripped == version or stripped.startswith(version + " "):
            start = i
            break
    if start is None:
        return ""
    body = []
    for line in lines[start + 1 :]:
        stripped = line.strip()
        if VERSION_LINE.match(stripped):
            break
        if stripped.startswith("#"):
            continue
        body.append(line)
    return "\n".join(body).strip()


def bbcode_to_markdown(s: str) -> str:
    s = re.sub(r"\[h1\](.*?)\[/h1\]", r"# \1", s, flags=re.I | re.S)
    s = re.sub(r"\[b\](.*?)\[/b\]", r"**\1**", s, flags=re.I | re.S)
    s = re.sub(r"\[i\](.*?)\[/i\]", r"*\1*", s, flags=re.I | re.S)
    s = re.sub(r"\[/list\]", "", s, flags=re.I)
    s = re.sub(r"\[list\]", "", s, flags=re.I)
    s = re.sub(r"\[\*\]\s*", "- ", s)
    return s.strip()


def write_github_output(path: Path, values: dict[str, str]) -> None:
    with path.open("a", encoding="utf-8") as fh:
        for key, value in values.items():
            if "\n" in value:
                fh.write(f"{key}<<EOF\n{value}\nEOF\n")
            else:
                fh.write(f"{key}={value}\n")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--mod-dir", required=True)
    parser.add_argument("--github-output", action="store_true")
    args = parser.parse_args()

    root = Path(args.mod_dir)
    about_path = root / "About" / "About.xml"
    log_path = root / "About" / "changelog.txt"
    if not about_path.is_file():
        print(f"Missing {about_path}", file=sys.stderr)
        return 1

    about = about_path.read_text(encoding="utf-8")
    version = parse_version(about)
    name = parse_name(about)
    if not version:
        print(f"No modVersion in {about_path}", file=sys.stderr)
        return 1

    changelog_bb = ""
    if log_path.is_file():
        changelog_bb = extract_changelog_block(log_path.read_text(encoding="utf-8"), version)
    changelog_md = bbcode_to_markdown(changelog_bb) if changelog_bb else f"{name} {version}"

    values = {
        "version": version,
        "display_name": name,
        "changelog_bbcode": changelog_bb or f"{name} {version}",
        "changelog_md": changelog_md,
    }

    if args.github_output:
        out = os.environ.get("GITHUB_OUTPUT")
        if not out:
            print("GITHUB_OUTPUT is not set", file=sys.stderr)
            return 1
        write_github_output(Path(out), values)
    else:
        for k, v in values.items():
            print(f"{k}={v!r}" if "\n" in v else f"{k}={v}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
