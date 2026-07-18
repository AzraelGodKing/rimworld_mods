# Homesteader translations

English text for **items, buildings, recipes, research, plants, incidents, and thoughts** is in `Homesteader/Defs/` (and versioned scenario defs under `1.5/` / `1.6/`).

C# strings (wash tub rejection, passive cooler inspect, favorite-food inspect, Kats Effect letter body) are in `Languages/English/Keyed/Homesteader.xml`.

## Add a language (example: Russian)

```
Homesteader/
  Languages/
    Russian/
      Keyed/
        Homesteader.xml
      DefInjected/
        ThingDef/
          Buildings_Homestead.xml
          Items_Food.xml
        RecipeDef/
          Recipes_Farmstead.xml
        ResearchProjectDef/
          …
        ThoughtDef/
          Thoughts_Statue27.xml
```

### Keyed

Copy `English/Keyed/Homesteader.xml`, translate values, keep tag names and `{0}` placeholders.

### DefInjected

Override def fields by `defName`:

```xml
<Homesteader_RootCellar.label>корневая погреб</Homesteader_RootCellar.label>
<Homesteader_RootCellar.description>…</Homesteader_RootCellar.description>
```

Thoughts, hediffs, and incidents use the same pattern (`Homesteader_KatsDirective.label`, etc.).

Package id: `AzraelGodKing.Homesteader`
