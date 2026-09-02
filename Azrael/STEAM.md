# Azrael — Steam / Workshop notes

**Package ID:** `azraelgodking.Azrael`  
**Requires:** Harmony, RimWorld 1.6  
**Hard deps:** none beyond Harmony  
**Recommended (soft):** Homesteader (canonical Azrael teller), Strata, Stormproof, Living World, Nemesis, Deep Colony

## What this is

Thin series showcase:

1. **The Deep Homestead** — scenario that locks Azrael and starts a homestead-flavored trio. Pick a mountainous / cave-friendly tile.
2. **Azrael storyteller** — **owned by Homesteader**. This package only injects the teller when Homesteader is absent (`PatchOperationFindMod` nomatch), so Homesteader + Azrael together never double-define `Azrael`.
3. **Series hub** — Mod Options → Azrael. Loaded series mods + versions, live bridges, named conflicts. Copy report when asking for help.

## Playtest checklist

- [ ] Homesteader alone → Azrael appears in storyteller list.
- [ ] Azrael alone (no Homesteader) → Azrael appears; Deep Homestead scenario works; no red errors.
- [ ] Homesteader + Azrael → one Azrael teller (no duplicate def); Deep Homestead forces Azrael.
- [ ] Mod Options → Azrael hub lists Homesteader as loaded with a version; Copy report pastes text.
- [ ] + Strata: `Strata` research unlocked at Deep Homestead start.
- [ ] Mid-save add does not brick the save.

## Load order

Harmony → Core → (DLC) → Homesteader / series content → **Azrael** last among them.
