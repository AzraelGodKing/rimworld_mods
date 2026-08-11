# Living World languages

English keyed strings ship in `English/Keyed/LivingWorld.xml` (letters, debug, diplomacy UI copy).

DefInjected packs can follow the same layout as Nemesis / Homesteader when translating settlement inspect text and related defs.

## Add a language (example: Russian)

```
LivingWorld/
  Languages/
    Russian/
      Keyed/
        LivingWorld.xml
```

Copy `English/Keyed/LivingWorld.xml`, translate values, keep tag names and `{0}` placeholders.

Package id: `azraelgodking.livingworld`
