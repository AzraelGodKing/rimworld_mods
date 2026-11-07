# Date Night languages

English keyed strings ship in `English/Keyed/DateNight.xml`. Chinese Simplified and Russian ship the same keys, plus DefInjected labels for TimeAssignment, Job and Thought defs.

As of 2.0.0 there are 111 keyed strings in each language. `scripts/validate_mods.py` fails the build if a key exists in one language but not in the others, so add every new key to all three files in the same change.

2.0.0 added keys for:

- settings sections, the gift list and picker, the column offset, romance buttons and date effects (`DateNight_Settings_*`, `DateNight_GiftPicker_*`)
- the Romance panel (`DateNight_Romance_*`)
- the Romance, Ask out tonight and Trying for a baby buttons (`DateNight_Gizmo_*`)
- planned-date messages (`DateNight_AskOut_*`)
- the fishing date report (`DateNight_Report_Fishing`)

Fleck defs have no visible label, so they need no DefInjected entries.

The Chinese and Russian 2.0.0 strings were machine-assisted. Native-speaker corrections are welcome. Russian names in `{0}` are not declined, so some lines use neutral phrasing.
