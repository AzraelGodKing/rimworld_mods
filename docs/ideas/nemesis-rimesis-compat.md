# Nemesis ↔ Rimesis / Deep Colony soft-compat

**Status:** coexistence shipped; Availability / Missing handshake designed (await Font API names); deeper combat handoff is a later idea (Font).  
**API:** `Nemesis.NemesisCompatApi` — `HasActiveHunt`, `ActiveNemesisPawn`, `IsNemesisPawn`, `WouldClaim`, `ShouldReportMissingToRimesis`.  
**Nemesis packageId:** `AzraelGodKing.Nemesis`  
**Rimesis packageIds tried:** `Font.Rimesis`, `font.rimesis`, `Rimesis` (confirm live id with Font).

---

## Shipped: exclusive pawn claim

Nemesis fails open when alone. With Rimesis / Back for Vengeance loaded:

- Skip hunt **create** if the candidate already has foreign antagonist hediff markers (`Rimesis` / `BFV` / `BackForVengeance` / `Vengeance` without `Nemesis` in the defName).
- Skip **raid inject** of that pawn under the same markers.
- Rimesis (or others) can call `NemesisCompatApi` before claiming a captain so both mods don’t own the same vendetta.

Intent: both mods loadable together without double-captains. Not a full combat merge.

**Coexistence bar (shipped today)** vs **later leader-raid inject:** exclusive claim / skip / Missing report is the coexistence bar. Handing Nemesis vengeance / army-return raids into Rimesis’s raid-injection pipeline is a separate, unscheduled pass (see [Later (Font)](#later-font-leader-raid--rimesis-injection)).

---

## Rimesis Availability (Font-owned) vs Nemesis Missing (our obligation)

Font is adding a system that hooks off **Availability** of the Rimesis (pawn busy / free for events). Nemesis does **not** own that enum; we only need to (1) respect busy states when we soft-read them, and (2) **report Missing** when a Rimesis can’t be called to action or hunted down because Nemesis has exclusive claim.

### Font Availability states (Rimesis-owned)

**In-use by Rimesis** (pawn busy — don’t steal / don’t treat as free):

| State | Meaning (paraphrase) |
|-------|----------------------|
| `AwaitingInvestigation` | Rimesis pipeline holding the pawn |
| `LocatedCampsite` | Campsite beat in progress |
| `LocatedSettlement` | Settlement beat in progress |
| `IncomingRaid` | Raid inbound |
| `DispatchingRaid` | Raid being dispatched |
| `EncounterActive` | Live encounter |

**Free for events:**

| State | Meaning |
|-------|---------|
| `Available` | Free — fair game for Rimesis events / (with care) other soft-compat claims |

### Nemesis Missing report (our side)

When Rimesis asks (or we push, once Font’s call shape is known): report **Missing** if that pawn cannot be called to action / hunted down by Rimesis because Nemesis owns the claim.

**When to report Missing** (design; see stub below):

| Condition | Missing? | Notes |
|-----------|----------|-------|
| `IsNemesisPawn(pawn)` | **Yes** | Hunt owns this exact pawn |
| Active hunt + this pawn is the nemesis | **Yes** | Same as above |
| `WouldClaim(pawn)` | **≈ Yes, but broader** | Today: `IsNemesisPawn \|\| HasActiveHunt` — true for *any* pawn while a hunt is engaged. Prefer `ShouldReportMissingToRimesis` for per-pawn Missing |
| Foreign claim (`SoftCompat.IsForeignAntagonistPawn`) | **No** (Rimesis/BFV already owns) | Nemesis skips create/inject instead |
| No hunt / not the nemesis pawn | **No** | Fail-open: Rimesis may treat as free |

Stable helper (shipped stub):

```csharp
NemesisCompatApi.ShouldReportMissingToRimesis(pawn)  // → IsNemesisPawn(pawn)
```

Rimesis can also keep using `IsNemesisPawn` / `WouldClaim` directly; `ShouldReportMissingToRimesis` exists so Font doesn’t have to guess which API maps to Missing.

### How Nemesis might *read* Rimesis Availability (fail-open, design only)

Do **not** hard-require Rimesis. Pattern matches existing `SoftCompat` (packageId + reflection):

1. Gate on `SoftCompat.RimesisActive` (`Font.Rimesis` / `font.rimesis` / `Rimesis`).
2. Resolve Font’s public type/method via `GenTypes.GetTypeInAnyAssembly` + `MethodInfo` (exact names **TBD with Font**).
3. On null type, missing method, or exception → treat as “unknown / allow” (fail-open), same as foreign-antagonist hediff scan.
4. If state ∈ busy set above → skip Nemesis hunt create / raid inject / steal for that pawn (don’t yank a busy Rimesis).
5. If `Available` (or Rimesis absent) → current exclusive-claim rules apply.

No reflection reader implemented yet — waiting on Font’s type/method/enum names so we don’t invent a brittle surface.

### Who calls whom (open)

| Direction | Purpose | Status |
|-----------|---------|--------|
| Rimesis → `NemesisCompatApi.ShouldReportMissingToRimesis` / `IsNemesisPawn` | Mark pawn Missing in Font’s Availability system | Stub ready; Font wires call |
| Nemesis → Font Availability API (reflection) | Don’t steal busy Rimesis pawns | Design only until API names land |
| Nemesis → Font raid inject | Leader-raid combat handoff | Later; not coexistence |

---

## Deep Colony: capture / truce goodwill (reviewed — no gap fix)

Nemesis resolution (`Dialog_NemesisResolution`):

| Outcome | Vanilla goodwill | Notes |
|---------|------------------|-------|
| Execute | `TryAffectGoodwillWith` **−30** | Immediate faction hit |
| Release | `TryAffectGoodwillWith` **+20** | Immediate faction bump |
| Keep prisoner | none | Hunt ends; pawn stays |
| Truce | **none** | Timer only (`truceUntilTick`); hunt dormant until expiry |

Deep Colony FactionRep does **not** patch `TryAffectGoodwillWith`. It only adds fractional ledger drift via its own hooks (raids, trades, gifts, shared kills, envoy, Living World signals). Attitude reads live goodwill + ledger.

**Verdict:** fine together. Nemesis execute/release move vanilla goodwill; DC ledger won’t get a dedicated “nemesis resolved” row (by design — not double-buffered). Truce doesn’t touch goodwill, so DC idle drift still applies to that faction during the truce window. No code change required unless playtests want a ledger reason for Execute/Release later.

---

## Later (Font): leader-raid → Rimesis injection

From Font (Rimesis author), paraphrased:

> When you pull a nemesis into a “leader raid,” it would just have to call Rimesis raid injection — that way the normal Rimesis style/combat stuff all functions. But admittedly that is more work than just the two being capable of coexisting.

**Meaning:** today’s soft-compat stops at exclusive claim / skip / Missing. A deeper pass would hand Nemesis vengeance / army-return raids into Rimesis’s raid-injection pipeline so captains fight with Rimesis combat focus, tactics, and presentation — instead of Nemesis’s own `NemesisRaidInject` path.

**Not scheduled** until both sides agree on a public inject hook and packageId confirmation. Coexistence (claim + Availability/Missing) remains the shipped / next-shipped bar.
