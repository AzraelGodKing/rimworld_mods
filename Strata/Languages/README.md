# Strata translations

English text for **buildings, research, incidents, jobs, thoughts, hediffs, and other defs** is in `Strata/Defs/` (plus `Strata/Odyssey/Defs/` for gravship content).

C# strings (mod settings, alerts, messages, gizmos, inspect text, letters) are listed in `Languages/English/Keyed/Strata.xml`. Copy that file for each new language and translate the values inside the tags.

## Add a language (example: Russian)

```
Strata/
  Languages/
    Russian/
      Keyed/
        Strata.xml
      DefInjected/
        ThingDef/
          Buildings_Strata.xml
          Buildings_LivingBelow.xml
        ResearchProjectDef/
          Research_Strata.xml
        IncidentDef/
          Incidents_Strata.xml
        JobDef/
          Jobs_Strata.xml
        MainButtonDef/
          MainButtons_Strata.xml
        ThoughtDef/
          Thoughts_Strata.xml
        HediffDef/
          Hediffs_Strata.xml
```

### Keyed (code strings)

Keys use the `Strata_` prefix. Placeholders `{0}`, `{1}` must remain in translated strings.

```xml
<Strata_StairsConnectedBelow>Broke through to the existing level below.</Strata_StairsConnectedBelow>
```

### DefInjected (XML defs)

English does **not** need DefInjected — def labels and descriptions in `Defs/` are the English source. Other languages override by `defName`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>
  <Strata_StairsDown.label>лестница вниз</Strata_StairsDown.label>
  <Strata_StairsDown.description>…</Strata_StairsDown.description>
  <Strata_Research_LivingBelow.label>жизнь под землёй</Strata_Research_LivingBelow.label>
</LanguageData>
```

Nested fields use dot notation (`defName.field`). For `description` with multiple lines, paste the full translated text.

Package id: `AzraelGodKing.Strata`

## Why an empty English folder did not work

RimWorld loads `Languages/&lt;LanguageName&gt;/` for the **active game language**. Translators need:

1. **Keyed** entries for every string emitted from C# via `"KeyName".Translate()`.
2. **DefInjected** overrides for def labels/descriptions when the game language is not English.

An empty `Languages/English/.gitkeep` folder does not break English (defs + hardcoded C# still show), but it gave no keys to copy and C# strings were not wired through the translation system — so non-English packs could not override them.
