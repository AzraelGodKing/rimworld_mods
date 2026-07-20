# Brought sprites

User-supplied or third-party art dropped here for later use in mods.

## Drop new assets here

Put brought sprites (and related stills) under this folder, optionally in a subfolder named by mod or purpose, e.g.:

- `art/brought/Homesteader/`
- `art/brought/Stormproof/`
- `art/brought/Strata/`
- `art/brought/Nemesis/`

Do **not** scatter new brought files in temp folders or loose mod roots; use this tree.

## Agent rules

**Do not** use anything in this folder as a generation reference:

- No `GenerateImage` `reference_image_paths`
- No “derive / restyle / match this style” prompts based on these files
- No workshop preview, building art, or other new art generated from these assets

**Allowed:** when the user explicitly asks, **copy or install the exact file unchanged** into a mod `Textures/` (or similar) path. That is placement of the brought asset as-is, not generation.

Existing brought art already installed under a mod’s Texture paths stays where it is; this folder is the convention for **new** drops going forward.
