# Versioning and rollback

Player-facing version is `<modVersion>` in each mod's `About/About.xml`. RimWorld shows it in the mod list. Player.log prints it on load (`[Stormproof] v1.1.0 loaded from ...`).

## When to bump

Bump `modVersion` when you **ship** a Workshop, Nexus, or versioned GitHub release. Put a matching block at the top of that mod's `About/changelog.txt` — first line is `X.Y.Z`, then the Steam notes.

Do **not** bump on every PR. Do **not** bump inside CI. **Release & Publish** reads whatever is already in `About.xml`.

CI (`scripts/validate_mods.py`) fails if `modVersion` has no changelog block.

## Three GitHub artifacts

| What | Tag | Mutable? |
|---|---|---|
| Docs-site zips | `latest` | Yes — force-moved on each relevant push to `main` |
| CI snapshot | `downloads-YYYY-MM-DD-<sha7>` | No — prerelease, kept forever |
| Shipped version | `{ZipName}-v{modVersion}` (example `Homesteader-v1.0.2`) | No — **Release & Publish** |

`latest` stays so the site buttons keep a stable URL. Snapshots exist so a player can roll back after a bad `latest` rebuild. Versioned tags exist so a Workshop drop has a named zip that is never overwritten.

## Bug reports

Ask for the `modVersion` line from Player.log. If they installed a GitHub zip between Workshop updates, also ask which GitHub Release tag (`latest`, `downloads-…`, or `Stormproof-v1.1.0`).

## How to ship

See [RELEASE.md](RELEASE.md).
