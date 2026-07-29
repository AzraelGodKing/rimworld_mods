# Stormproof translations

English player-facing text for **buildings, research, incidents, and defs** lives in `Stormproof/Defs/` (labels and descriptions there are the English source).

Strings from C# (messages, gizmo labels, inspect panels, mod settings) are listed in `Languages/English/Keyed/Stormproof.xml`.

**Shipped:** `ChineseSimplified` and `Russian` include Keyed + DefInjected packs.

Translators should copy the English Keyed file into a new language folder and translate the **values**, keeping the **tag names** unchanged.

## Add a language (example: Russian)

```
Stormproof/
  Languages/
    Russian/
      Keyed/
        Stormproof.xml          ← copy from English/Keyed, translate text inside tags
      DefInjected/
        ThingDef/
          Buildings_Stormproof.xml
        ResearchProjectDef/
          Research_Stormproof.xml
        IncidentDef/
          Incidents_Stormproof.xml
```

### Keyed (code strings)

```xml
<Stormproof_OfflineNeedsPower>Offline: needs power.</Stormproof_OfflineNeedsPower>
```

Russian override:

```xml
<Stormproof_OfflineNeedsPower>Не в сети: нужно питание.</Stormproof_OfflineNeedsPower>
```

Placeholders `{0}`, `{1}` must stay in the translated string.

### DefInjected (XML defs)

English labels/descriptions are **not** duplicated under `Languages/English/`. For other languages, add files under `DefInjected/` mirroring def types from `Defs/`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>
  <Stormproof_StormSpire.label>штормовой шпиль</Stormproof_StormSpire.label>
  <Stormproof_StormSpire.description>…</Stormproof_StormSpire.description>
</LanguageData>
```

Use the def **`defName`** as the XML tag prefix (`Stormproof_StormSpire`, not the filename).

Package id: `AzraelGodKing.Stormproof`
