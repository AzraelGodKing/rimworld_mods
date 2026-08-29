# Release & Publish (GitHub + Nexus)

Same shape as the Sunhaven **Release & Publish** workflow: you dispatch it, CI builds the selected mod(s), packs a Workshop-safe zip (`git archive` + DLL, `export-ignore` so `Source` and Homesteader refresh art stay out), then optionally:

1. **GitHub Release** — tag `{ZipName}-v{modVersion}` (example `Homesteader-v1.0.2`). Does **not** replace the rolling `latest` tag the docs site uses.
2. **Nexus Mods** — new file version on an existing Nexus file (retries with backoff, same as Sunhaven). Needs a Nexus page and IDs in the matrix. Uploads use `Nexus-Mods/upload-action@v1.0.0-beta.10` (there is no `v1` tag).

Version and Steam notes come from each mod’s `About.xml` `modVersion` and the matching block in `About/changelog.txt`. Do not bump versions in this workflow.

## Run it

Actions → **Release & Publish** → Run workflow:

| Input | Typical first run |
|---|---|
| `dry_run` | **true** until you have checked the zip artifact |
| `mod` | `all` or one key (`homesteader`, `datenight`, …). Living World and Azrael are not in this list. |
| `create_github_release` | **true** when you are ready to tag |
| `publish_nexus` | **false** until `nexus_file_id` is filled in |
| `release_anyway` | **false** (skips if that version is already tagged / already the latest Nexus file version) |

Repo secret: **`NEXUSMODS_API_KEY`** (same key as Sunhaven). GitHub Releases use `GITHUB_TOKEN`.

## Nexus IDs

Nexus cannot create a mod page from CI. Once a page exists and you have uploaded **one** file by hand:

1. Open the mod’s **Files** tab → **API Info** (or Manage Files).
2. Copy the **file id** into `nexus_file_id` and the numeric page id into `nexus_mod_id` on that row in [`scripts/matrix/mod-matrix.json`](../scripts/matrix/mod-matrix.json).
3. Set `nexus` to `https://www.nexusmods.com/rimworld/mods/<id>`.
4. Re-run **Release & Publish** with `publish_nexus=true`.

Empty `nexus_file_id` → GitHub Release still works; Nexus is skipped with a warning.

Living World and Azrael have `"publish": false` in the matrix. They stay in-repo; **Release & Publish** and the rolling `latest` zips omit them until that flag is flipped.

## Local pack check

```bash
dotnet build Homesteader/Source/Homesteader.csproj
bash scripts/pack_mod.sh Homesteader Homesteader Homesteader dist
python3 scripts/release_meta.py --mod-dir Homesteader
```
