Strata art export
=================

StrataArt/ contains every texture PNG shipped as Strata-owned art:
  - Textures/Strata/Buildings/   (buildings, pipes, quest stairs, commissioned shaft art)
  - Textures/Strata/Items/
  - Textures/Things/Pawn/Animal/Canary/
  - Textures/UI/Strata/
  - About/Preview.png

Unused WIP preview folders (_vault_previews, _cargolift_previews, _sumppump_previews)
were dropped; live textures such as CargoLift.png remain.

To refresh this package after art edits, re-run the export from repo root or ask
the agent to repackage.

Restore into mod:
  copy StrataArt/ contents into Strata/ (merge Textures/ and About/)
