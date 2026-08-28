#!/usr/bin/env python3
"""Fail CI on malformed XML, Keyed translation drift, and missing mod-owned texPaths.

Stdlib only. Run from repo root: python3 scripts/validate_mods.py
"""
from __future__ import annotations

import sys
import xml.etree.ElementTree as ET
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent

SKIP_DIR_NAMES = {
    ".git",
    "node_modules",
    "obj",
    "bin",
    "ArtPackage",
    ".tmp",
    ".local",
    "site",
    "docs",
    "_decompile_tmp",
    "_reflect_tmp",
}

TEXTURE_SUFFIXES = (".png", ".jpg", ".jpeg")
DIRECTIONAL = ("_north", "_south", "_east", "_west")


def skip_dir(path: Path) -> bool:
    return any(part in SKIP_DIR_NAMES for part in path.parts)


def mod_dirs() -> list[Path]:
    found = []
    for child in sorted(REPO.iterdir()):
        if child.is_dir() and (child / "About" / "About.xml").is_file():
            found.append(child)
    return found


def iter_xml_files() -> list[Path]:
    files = []
    for path in REPO.rglob("*.xml"):
        if skip_dir(path):
            continue
        files.append(path)
    return files


def check_well_formed(files: list[Path]) -> list[str]:
    errors = []
    for path in files:
        try:
            ET.parse(path)
        except ET.ParseError as exc:
            rel = path.relative_to(REPO).as_posix()
            errors.append(f"malformed XML: {rel}: {exc}")
    return errors


def keyed_tags(lang_dir: Path) -> set[str]:
    tags: set[str] = set()
    keyed = lang_dir / "Keyed"
    if not keyed.is_dir():
        return tags
    for xml in keyed.rglob("*.xml"):
        try:
            root = ET.parse(xml).getroot()
        except ET.ParseError:
            continue
        for child in list(root):
            if child.tag and not child.tag.startswith("{"):
                tags.add(child.tag)
    return tags


def check_keyed_parity(mods: list[Path]) -> list[str]:
    errors = []
    for mod in mods:
        english = mod / "Languages" / "English"
        if not english.is_dir():
            continue
        en_tags = keyed_tags(english)
        if not en_tags:
            continue
        langs = mod / "Languages"
        for lang_dir in sorted(p for p in langs.iterdir() if p.is_dir()):
            if lang_dir.name == "English":
                continue
            if not (lang_dir / "Keyed").is_dir():
                continue
            other = keyed_tags(lang_dir)
            missing = sorted(en_tags - other)
            extra = sorted(other - en_tags)
            rel = f"{mod.name}/Languages/{lang_dir.name}"
            if missing:
                preview = ", ".join(missing[:12])
                more = f" (+{len(missing) - 12} more)" if len(missing) > 12 else ""
                errors.append(
                    f"Keyed parity: {rel} missing {len(missing)} EN key(s): {preview}{more}"
                )
            if extra:
                preview = ", ".join(extra[:8])
                more = f" (+{len(extra) - 8} more)" if len(extra) > 8 else ""
                print(
                    f"  note: {rel} has {len(extra)} extra key(s) not in EN: {preview}{more}"
                )
    return errors


def texpath_exists(textures: Path, tex: str) -> bool:
    rel = tex.replace("\\", "/").strip().lstrip("/")
    if not rel:
        return False
    base = textures / Path(*rel.split("/"))
    if base.is_file():
        return True
    for suffix in TEXTURE_SUFFIXES:
        if Path(str(base) + suffix).is_file():
            return True
        for facing in DIRECTIONAL:
            if Path(str(base) + facing + suffix).is_file():
                return True
    if base.is_dir():
        return True
    return False


def check_texpaths(mods: list[Path]) -> list[str]:
    errors = []
    for mod in mods:
        textures = mod / "Textures"
        if not textures.is_dir():
            continue
        for xml in mod.rglob("*.xml"):
            if skip_dir(xml):
                continue
            try:
                root = ET.parse(xml).getroot()
            except ET.ParseError:
                continue
            for node in root.iter("texPath"):
                tex = (node.text or "").strip()
                if not tex:
                    continue
                rel = tex.replace("\\", "/").strip().lstrip("/")
                parent = textures / Path(*rel.split("/")[:-1]) if "/" in rel else textures
                # Vanilla / other-mod paths are not shipped here. Only fail when this
                # mod already has the folder that should contain the sprite.
                if not parent.is_dir():
                    continue
                if not texpath_exists(textures, tex):
                    rel_xml = xml.relative_to(REPO).as_posix()
                    errors.append(f"missing texPath: {tex} (from {rel_xml})")
    return errors


def main() -> int:
    xml_files = iter_xml_files()
    mods = mod_dirs()
    errors: list[str] = []
    errors.extend(check_well_formed(xml_files))
    errors.extend(check_keyed_parity(mods))
    errors.extend(check_texpaths(mods))

    print(f"Checked {len(xml_files)} XML files across {len(mods)} mods.")
    if errors:
        print(f"FAILED ({len(errors)}):")
        for line in errors:
            print(f"  {line}")
        return 1
    print("OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
