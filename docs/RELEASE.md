# Release & Publish (GitHub + Nexus)

Same shape as the Sunhaven **Release & Publish** workflow: you dispatch it, CI validates the repo, builds the selected mod(s), packs a Workshop-safe zip (`git archive` + DLL, `export-ignore` so `Source` and Homesteader refresh art stay out), then optionally:

1. **GitHub Release** — tag `{ZipName}-v{modVersion}` (example `Homesteader-v1.0.2`). Does **not** replace the rolling `latest` tag the docs site uses.
2. **Nexus Mods** — new file version on an existing Nexus file (retries with backoff, same as Sunhaven). Needs a Nexus page and IDs in the matrix. Uploads use `Nexus-Mods/upload-action@v1.0.0-beta.10` (there is no `v1` tag).

A Nexus upload from this workflow:

- Sets the **page** version from `About.xml` `modVersion` (`update_mod_version`)
- Archives the previous Main file (`archive_existing_version`)
- Marks the new zip as the Vortex / NMM default (`primary_mod_manager_download`)
- Posts `About/changelog.txt` to the Nexus **Changelog** tab (looks up the v3 mod id from the URL number at upload time)

Version and Steam notes come from each mod’s `About.xml` `modVersion` and `About/changelog.txt` (current version only). Do not bump versions in this workflow. When you *are* ready to ship, bump `modVersion` and replace the changelog first — see [VERSIONING.md](VERSIONING.md).

GitHub and Nexus gates are independent. A tag that already exists does not skip Nexus, and a Nexus file that already has this version does not skip the GitHub Release. `release_anyway` ignores both gates.

## Rollback

The **Build mod DLLs** workflow still force-moves `latest` so the docs site buttons stay stable. Each of those runs also cuts an immutable prerelease `downloads-YYYY-MM-DD-<sha7>` with the same zips. Old snapshots stay on the [Releases](https://github.com/AzraelGodKing/rimworld_mods/releases) page. Shipped Workshop builds are the versioned `{ZipName}-v{modVersion}` tags from this workflow.

## Run it

Actions → **Release & Publish** → Run workflow:

| Input | Typical ship |
|---|---|
| `dry_run` | **true** until you have checked the zip artifact |
| `mod` | `all` or one key (`homesteader`, `datenight`, `niceties`, …). Living World and Azrael are not in this list. |
| `create_github_release` | **true** when you are ready to tag |
| `publish_nexus` | **true** to upload (every published mod already has `nexus_file_id`) |
| `release_anyway` | **false** unless you are re-publishing a version that is already tagged or already on that Nexus file |

Repo secret: **`NEXUSMODS_API_KEY`** (same key as Sunhaven). GitHub Releases use `GITHUB_TOKEN`.

Setup runs `python3 scripts/validate_mods.py` before any pack, same checks as **Build mod DLLs**.

## Nexus IDs

Nexus cannot create a mod page from CI. Once a page exists and you have uploaded **one** file by hand:

1. Open the mod’s **Files** tab → **API Info** (or Manage Files).
2. Copy the **file id** into `nexus_file_id` and the numeric page id into `nexus_mod_id` on that row in [`scripts/matrix/mod-matrix.json`](../scripts/matrix/mod-matrix.json).
3. Set `nexus` to `https://www.nexusmods.com/rimworld/mods/<id>`.
4. Re-run **Release & Publish** with `publish_nexus=true`.

Empty `nexus_file_id` → GitHub Release still works; Nexus is skipped with a warning.

`"nexus_publish": false` keeps the IDs on the row but skips Nexus upload and the Nexus version gate (use after a manual Nexus drop). Flip it to `true` (or omit the key) when CI should upload again.

Living World and Azrael have `"publish": false` in the matrix. They stay in-repo; **Release & Publish** and the rolling `latest` zips omit them until that flag is flipped.

## Local pack check

```bash
python3 scripts/validate_mods.py
dotnet build Homesteader/Source/Homesteader.csproj
bash scripts/pack_mod.sh Homesteader Homesteader Homesteader dist
python3 scripts/release_meta.py --mod-dir Homesteader
```
