# Idea sweep — September 2026

A fresh pass over all nine in-repo mods plus the Linear board (AZR-1 … AZR-201), looking for
**10 new features** and **10 simple fixes** per mod, and **5 new mods** that fill the seams
between the ones that already ship.

**Provenance.** Everything here comes from reading this repo and the Linear board — not from
playtesting. Items marked **[verified]** were confirmed against the tree in this sweep. Everything
else is a proposal: check it before you ticket it. Nothing here duplicates an item already sitting
in a per-mod `ROADMAP.md` or `docs/ideas/` pool, except where a pooled item is called out by ID
because it is still the highest-value thing on the list.

**Fixes** are scoped deliberately small — an afternoon each, most of them less. They are not a bug
list; they are the papercuts, parity gaps, and hygiene chores that a mod this size accumulates.

---

## Homesteader (1.0.4)

### Features

1. **Cellar humidity** — root cellars track humidity alongside temperature. Too dry ruins pickles, too damp molds jerky; a damper building tunes it. Gives the cellar a second dial and makes preservation a skill instead of a temperature check.
2. **Sourdough starter** — a tended Thing that must be fed flour on a cadence. Feeds better-quality bread at the hearth, dies if neglected through a siege. Cheap to build, and it ties mill → hearth with a thing the player can lose.
3. **Vintage and provenance** — every preserve stack records year, maker, and quality. A five-year-old jam trades at a premium and carries a "taste of home" thought. This is the data layer that makes pooled **HS-A03** (aging) worth having.
4. **Crop rotation** — planting the same crop in a cell season after season drops fertility; rotating or fallowing restores it. Optional toggle. Makes irrigated soil and the planter a real decision rather than a strict upgrade.
5. **Vermin in the stores** — rats and squirrels nibble unsealed stockpiles; sealed crates or a barn cat stop it. A small recurring incident that gives the nesting box a sibling and the storage ladder a reason.
6. **Winter stores planner** — extend the pantry tab: "at current population and burn rate, stores last until <date>," with an autumn warning when you are short. The natural third act after AZR-68 and AZR-147.
7. **Charcuterie chain** — hang whole carcasses to age, then break them into cuts; long-cure smokehouse items (prosciutto, bacon) at the top. Gives the smokehouse a late game.
8. **Well water quality** — shallow wells near corpses or toxic ground draw tainted water with a food-poisoning risk; deep wells run clean, and the solar still purifies. Turns the well ladder into a genuine choice instead of a straight upgrade.
9. **Barn raising** — a group work ritual where several colonists raise a large farm structure in one session, faster and with a mood payoff. Ideology-aware, works without it.
10. **Seasonal homestead ledger** — an end-of-season letter: harvested, preserved, lost to rot, best crop, worst. The farm's annual report, and a deliberate sibling to the Stormproof almanac.

### Fixes

1. **Pantry tab has no text filter** — no `TextField` or search state anywhere in `Homesteader/Source`. A long pantry is a scroll wall. **[verified]**
2. **Favorite food can be unobtainable** — if a pawn rolls a favorite whose ingredients aren't in the load order, they carry a permanent quiet mood gap. Reroll on join when the food's ingredients don't resolve.
3. **Cellar breach has no alert** — a root cellar that loses its cool (roof breached, door propped) fails silently. One-shot alert when it crosses 5 °C.
4. **Wells and cisterns don't show fill rate** — add L/day and time-to-full to the inspect string so the upgrade ladder is legible.
5. **Hearth bill list needs grouping** — the hearth carries a lot of recipes with no categories; group them by course or input.
6. **No-research recipes aren't flagged** — pemmican on the drying rack without research is a selling point the bill UI never states.
7. **Irrigated soil doesn't visibly revert** — when the water source is deconstructed, confirm fertility and the terrain graphic both fall back.
8. **Storage descriptions vs. real capacity** — the general-fixes doc lists "storage descriptions match `maxItemsInCell` × footprint" as landed; add a CI assertion so it can't drift again.
9. **`Legacy/` and `1.6/` def trees both ship** — two `Scenario_Homesteaders.xml` copies exist. Confirm only one loads, and that the Workshop zip isn't carrying both. **[verified: both present]**
10. **Storyteller double-inject is untested** — Azrael is canonical in Homesteader and standalone-injects only when Homesteader is absent. That guard has no test; it's a one-assertion addition to `Tests/ModChecks`.

---

## Strata (3.5.0)

The biggest mod in the repo by an order of magnitude — 310 source files, 61k lines.

### Features

1. **Shoring and pit props** — timber or steel supports that raise a level's collapse resistance locally. A cheap early counter to cave-ins, and a natural extension of AZR-63's span and support.
2. **Level names and colours** — name a level "The Freezer" and tint it; alerts and letters use the name. Pure UX, and the payoff scales with every floor you add.
3. **Ventilation as a visible graph** — ducts form a named network with a fan as its source, showing airflow direction and a room's air age. Turns the smoke system from a hazard into something you design against.
4. **Cutting charges** — a placeable blast that mines a marked block in one go. Loud, which feeds the pooled "noise attracts the dark" rule, and risks opening a gas pocket.
5. **Deep reservoir** — cap the water table (AZR-139) with a sump → reservoir that turns seepage from a hazard into a water source Homesteader can read. Soft-compat in both directions, fail-open.
6. **Purpose tags on the stairwell** — a stairwell shows which AZR-140 tags the level below carries, so you can read the base's layout from the stairs instead of from a menu.
7. **Emergency lighting** — low-draw lights on a local cell that stay lit when the shaft conduit drops. An underground blackout is currently total, and total darkness is not tension, it's a camera problem.
8. **Rock types with character** — sandstone crumbles (faster to mine, more cave-ins), granite resists, limestone hides caves and water. This is what gives the core sampling drill (AZR-138) something worth reporting.
9. **Freight lift** — a powered, items-only elevator variant with real throughput, so pawns stop riding the passenger car with every stack. Sits above the pooled dumbwaiter, not instead of it.
10. **Murder holes** — a designated floor hatch lets pawns above fire down through a stairwell landing. Makes vertical space tactical rather than purely logistical, and gives sealed shafts a third option between open and closed.

### Fixes

1. **~45 hardcoded English phrases in `Source`** — the highest literal count in the repo, against 322 `Translate()` calls. Sweep them into Keyed. **[verified]**
2. **Purpose tags have no clear-all** — AZR-140 gives no reset on a level you've retagged.
3. **No level-switch hotkey** — PgUp/PgDn (rebindable) to move up and down the stack.
4. **Smoke overlay has no keybind** — it lives in Play Settings only.
5. **Sealed stairwell gives no arrival alert** — nothing tells you something is waiting on the far side.
6. **The elevator's fail-safe isn't documented in-game** — "always rides up on power loss" is a real design promise and it's only in the README.
7. **Deep vein and gas pocket letters don't jump to the level** — every cross-level letter should carry a jump target.
8. **Cave-ins have no tell** — dust motes or a creak a few seconds ahead turns an ambush into a warning you can act on.
9. **`incompatibleWith` is packageId-only** — AASB and MultiFloors are listed, but a rename or fork slips through. Add a defName probe alongside AZR-56's load-time detection.
10. **No architecture map for 310 files** — a `Source/ARCHITECTURE.md` naming the atmosphere / portal / relay / gravship clusters. At 61k lines this pays for itself the first time you come back cold.

---

## Stormproof (1.3.0)

### Features

1. **Per-spire strike ledger** — spires log their strikes and a well-struck spire becomes a named landmark. Extends fulgurite (AZR-80) from an item into a history.
2. **Microgrid islanding** — designate a critical sub-grid (hospital, freezer) that automatically islands onto its own batteries during a flare, instead of the solar shield's all-or-nothing 2,500W bet.
3. **Storm shift** — a work schedule that pulls colonists indoors automatically when the forecaster calls a break.
4. **Generator wear and maintenance** — apply AZR-78's storm wear to generators, with scheduled maintenance bills. Gives the Homesteader wood generator a lifecycle.
5. **Hail** — damages crops and unroofed solar panels; countered by a hail screen. A genuine gap in vanilla weather, and squarely this mod's business.
6. **Inrush current** — starting a large machine draws a brief spike that capacitor banks smooth. Makes the capacitor bank matter on a clear day.
7. **Rolling grid history** — the monitor keeps a three-day production and consumption chart, not just an instantaneous readout.
8. **Storm shelter room role** — a designated shelter gives a mood buff during severe weather and colonists path there on alarm.
9. **Turbine wake** — wind turbines placed too close lose output, with a placement overlay. This mod already does coverage overlays well (AZR-143); this is the same trick applied to a real trade-off.
10. **Seasonal grid plan** — the almanac projects "you will be 1,200W short in winter at this build" and names the gap. The natural sequel to AZR-142 and AZR-144.

### Fixes

1. **Almanac has no export** — Deep Colony has chronicle export (AZR-71) and Azrael has a copy-report; the almanac has neither. **[verified: no `Export`/`Clipboard` in `Stormproof/Source`]**
2. **Forecaster warning lead time isn't configurable** — one hour is hardcoded in the design.
3. **Storm caller recharge has no countdown** — five days, invisible on the inspect line.
4. **Load shedder has no dry run** — no way to preview which sub-grid the cutoff will drop.
5. **Surge protector recharging state isn't readable at a glance** — needs a distinct map graphic or overlay, not just inspect text.
6. **Solar shield doesn't project total flare cost** — show Wd needed for the flare's remaining duration, which is the number that decides whether you survive it.
7. **Fallout scrubber gives no failure reason** — when the room isn't enclosed, say which cell breaks it.
8. **Static discharge pylon has no friendly-fire guard** — at minimum a warning; ideally a toggle.
9. **943-line single buildings file for 18 buildings** — split per building or per family. Diffs and merges are painful at this size. **[verified]**
10. **Almanac seasons vs. save age** — confirm the almanac reads correctly on a save older than one full year, and on a map changed mid-save.

---

## Nemesis (1.1.1)

### Features

1. **Their colony** — the nemesis eventually plants a visible world-map base that grows between assaults. Razing it is the hunt's finale rather than a lucky kill.
2. **Trophies** — take their weapon or apparel after a win; it carries the epitaph text (AZR-76) and a mood buff for the fixation pawn.
3. **Informant relationships** — bought leads (AZR-75) build standing with informants. A trusted one eventually volunteers intel free — or sells you out.
4. **They recruit your castoffs** — a colonist you banished or a prisoner you released turns up in the next assault. Makes your own decisions the raid generator.
5. **Counter-intelligence** — a comms action to feed *them* a false lead, drawing an assault onto a killbox or a decoy. The inversion of a mechanic the mod already owns.
6. **Scars both ways** — the nemesis accumulates permanent injuries across fights, visible in the dossier. A one-eyed, peg-legged nemesis is a record of your survival.
7. **Succession** — on their death, a lieutenant inherits the grudge at lower aggression (opt-in). Lets the hunt be a dynasty instead of a single arc.
8. **Colony dread** — colonists who know a hunt is active carry a low-grade unease; resolving it gives colony-wide relief. Fail-open soft-compat with Deep Colony trauma.
9. **Readable sabotage** — sabotage currently just lands. Leave an evidence trail a high-Intellectual colonist can read to predict the next target.
10. **The truce** — buy a season of peace with tribute. It holds, breaking it costs you — and they break it too.

### Fixes

1. **13 hardcoded English literals** — sweep into Keyed. **[verified]**
2. **Dossier doesn't show what you *don't* know** — naming the empty slots is what makes a bought lead feel like a purchase.
3. **Hunt letters don't link to the dossier.**
4. **No maximum hunt duration setting** — some players want an exit.
5. **Epitaph isn't in the Deep Colony chronicle export** — obvious cross-mod parity, both features already exist.
6. **Taunt strings need a no-repeat window** — a long hunt will cycle them.
7. **AZR-59 is a 15-minute check still sitting in Todo** — confirming live BFV / Rimesis packageIds unblocks AZR-60.
8. **Aggression has no player-visible tier** — the dossier should name it, especially once content is gated behind a threshold.
9. **Mech retinue despawn on flee** — verify the Mechanitor focus doesn't orphan mechs when the nemesis escapes. Classic leak shape.
10. **Only 4 def files** — taunt and flavor text is likely in C#. Move it to Defs or Keyed so translators can reach it.

---

## Deep Colony (2.0)

### Features

1. **Colonist journals** — a readable per-pawn diary generated from real history: first kill, mentor, trauma, marriage. The Legacy tab already holds the data; this is the presentation.
2. **Vocations** — after enough years in a role, a pawn earns a colony title ("Master Smith") that gives a small aura to apprentices in the room.
3. **Memorials and grief** — a grave or memorial specific survivors visit, where visiting resolves grief trauma faster. Ties Estate (AZR-70) to the trauma system.
4. **Colony culture drift** — the colony accumulates traits from its members (industrious, violent, scholarly) that nudge new-joiner generation and visitor impressions.
5. **Mentor's notes** — extend graduation's perk inheritance into a physical item that can teach that perk to a third pawn later. Makes a dead mentor's knowledge survivable.
6. **Burnout** — sustained single-skill work builds a fatigue hediff perks can't offset; rotation counters it. Natural partner to Quiet Hours (AZR-65).
7. **Named envoys** — the reputation system gets recurring NPC envoys whose opinion of *specific colonists* drives goodwill drift.
8. **Reunion** — a long-lost relative arrives as a quest seeded from the Legacy tab's founder lines.
9. **Therapy styles** — counselors develop an approach (empathic, clinical, blunt) that suits some patients and not others, so who counsels whom matters.
10. **Formal retirement** — elders (B20) can retire: no labor, a large mentoring aura, and a real mood cost to the colony when they die.

### Fixes

1. **27 hardcoded English literals** — sweep into Keyed. **[verified]**
2. **No colonist search in the three tabs** — Perks, Legacy, and Reputation all become scroll walls in a large colony.
3. **Perks tab has no "next unlock" line** — say what this pawn earns at the next threshold.
4. **No "untreated only" filter on the trauma list** — that is the list you actually act on.
5. **Chronicle export is text-only** — add markdown or CSV.
6. **Mentoring work type has no tooltip guidance** — where it belongs in a priority column isn't obvious.
7. **Pedigree view needs zoom and pan** — confirm it survives a colony with four generations.
8. **Touch-averse tiers have no in-game explanation** — 637 lines of behavior that players will read as a bug when a romance doesn't start.
9. **`DebugActions_DeepColony.cs` is 806 lines** — verify it's gated out of the release build. **[verified: present in `Source/Debug`]**
10. **No tick-budget readout** — AZR-123 staggered fifteen subsystems; a dev panel showing each one's cost makes the next regression measurable instead of anecdotal.

---

## Date Night (1.1.1)

### Features

1. **Courtship stages** — pre-relationship dates that escalate, rather than the binary lover / not-lover the vanilla system hands you.
2. **Breakups have a tail** — a cooldown where exes avoid each other's venues and react badly to meeting there.
3. **Gifts that land** — remembering a partner's favorite food (Homesteader hook) or bringing a crafted item raises date quality a tier.
4. **Friend outings** — extend double dates to platonic groups that build non-romantic bonds. The activity system already supports it.
5. **Buildable date spots** — a designation (fireplace, chairs, art) with a quality score you can improve, so venue memory (AZR-82) has something to prefer.
6. **Weather-aware dates** — stargazing is better on a clear night, picnics fail in rain. Fail-open soft-compat with the Stormproof forecaster.
7. **Jealousy** — a partner who witnesses a date with someone else reacts.
8. **Weddings you plan** — schedule the date, invite guests, roll quality tiers. The anniversary system already models the aftermath.
9. **Long distance** — a partner away on caravan builds a "missing you" mood; the reunion date pays extra.
10. **Date memories in the bio** — one line for their best date, which feeds straight into Deep Colony journals.

### Fixes

1. **6,105 lines for a schedule mod** — `DateNightUtility.cs` at 926 lines and `DateNightActivities.cs` at 727 are the split candidates. **[verified]**
2. **4 hardcoded English literals** — sweep. **[verified]**
3. **No "copy schedule to partner"** — the mismatch alert already detects the problem; add the one-click fix.
4. **No date frequency / MTB setting.**
5. **Favourite venue isn't visible on the venue** — the table should say which couples favor it.
6. **Anniversary letter has no jump-to-couple.**
7. **Private time settings need restating in the tooltip** — the "children never qualify" rule is a trust point; say it where the toggle is.
8. **Ruined-date thought doesn't say why** — the reason exists internally; surface it.
9. **Only 3 def files** — activities are likely hardcoded. Move them to Defs so other mods can add their own.
10. **Better Pawn Control smoke tests** — listed as "Later" in the roadmap and never done. It's the most-installed schedule mod and the likeliest conflict surface you have.

---

## Niceties (1.2.0)

The leanest mod in the repo (1,490 lines, no defs), and the design discipline in its ROADMAP is
worth protecting: every nicety independently toggleable, nested knobs no-op when the master is off.

### Features

1. **Don't haul into a fire** — skip hauling to a burning or deconstructing building.
2. **Bill settings memory** — copying a bill to a new bench remembers ingredient filters and counts.
3. **Colony default medicine tier** — set once, applied to new pawns, overridable per pawn.
4. **Surgery draft guard** — confirm before drafting a surgeon mid-operation.
5. **Floor stack merge** — nearby identical stacks merge when a hauler passes, instead of littering.
6. **Per-alert mute list** — stop the top-right nagging about the one thing you've decided to live with.
7. **Useful "no path" feedback** — name the blocking cell instead of shrugging.
8. **Guest and prisoner food policy** — respect the pantry rather than eating your preserves. Fail-open Homesteader hook.
9. **Auto-named rooms** — label rooms by function so the room list is readable at a glance.
10. **Don't forbid what you just made** — an opt-in for the dropped-apparel and butchery cases that catch people out.

### Fixes

1. **`UpdateNews.cs` exists — confirm it's surfaced** — a "what changed" panel is only worth having if players see it. **[verified: file present]**
2. **No CI test for the master-toggle rule** — the ROADMAP states nested knobs must no-op when the master is off. That's exactly the kind of invariant `Tests/ModChecks` can assert.
3. **`incompatibleWith` fails silently** — five mods are named; tell the player *which* one is loaded and what it collides with.
4. **Well-kept apparel doesn't show its multiplier** — the inspect line should say the current deterioration scaling.
5. **Melee hunting body-size cap is unexplained** — the setting needs a number and an example.
6. **Shared bedrooms gizmo is undiscoverable** — hint the first time a room gets a second bed.
7. **No temporary reveal for hidden cryptosleep pawns.**
8. **"Leave a way out" doesn't show what it deferred** — AZR-201 works, but silently; mark the frame it's holding.
9. **Presets have no diff view** — Soft / Default / Hard should show what they actually change before you commit.
10. **Series-wide "reset all settings"** — nine mods with settings, no single reset. Niceties is the right home for it.

---

## Living World (1.0.0, not release-ready)

Phases 1 and 2 are marked done, but the mod is not in the release zips and not on the docs hub.
That status gap is the real issue; most of what follows is downstream of resolving it.

### Features

1. **AZR-84 — the chronicle tab.** Still Todo, and it is the mod's entire shopfront. A world sim nobody can read is a world sim nobody buys.
2. **AZR-85 — rumours arrive wrong and get corrected.** Still Todo, and it's the mechanic that makes the chronicle feel alive rather than omniscient.
3. **Trade route prosperity** — settlements that trade with you visibly prosper, so your commerce shows up in the world.
4. **World map overlay** — tint the map by war, prosperity, or tension.
5. **Named leaders with ambitions** — give war declarations a face and a motive.
6. **Migration** — a crushed settlement's survivors found a new one elsewhere, so the map doesn't only lose pins.
7. **Your deeds as news** — your raids and gifts enter the chronicle and other factions react to what they heard.
8. **Regional years** — a plague year, a good harvest, a festival that shifts trader stock for a season.
9. **Envoys you send** — spend goods and a colonist's time to move a faction pair from War toward Tension.
10. **LW9 inter-settlement traffic** — pooled already; it's the thing that makes the world look inhabited rather than simulated.

### Fixes

1. **Ship it or archive it** — not in the release zips, and `living-world.html` is on the AZR-135 legacy keep-list "until the mod is on the hub." Decide. **[verified]**
2. **7 hardcoded English literals** — sweep. **[verified]**
3. **About.xml still reads 1.0.0 after two completed phases** — version and changelog hygiene.
4. **Chronicle ring buffer size should be a setting.**
5. **Letter spam caps need a player-facing slider** — they're internal-only today.
6. **`Scenario_ListeningPost` ships translations for an unreleased mod** — verify it loads standalone without errors. **[verified: def and both translation files present]**
7. **No dev fast-forward** — "advance N years" makes the world sim testable in minutes instead of hours.
8. **No test for DC1 fail-open** — Deep Colony's consumer is done; assert it degrades cleanly when Living World is absent.
9. **Not in the README install list as release-ready** — the README says so, but the ROADMAP reads as shipped. Make them agree.
10. **`Languages/README.md` parity** — present in every other mod.

---

## Azrael (1.0.0)

1,032 lines, and it's the front door to the whole series. It deserves the highest polish-per-line
in the repo.

### Features

1. **AZR-137 — safe removal wizard.** Ticketed, still Todo, and the highest-value unshipped item in the series: the README currently asks players to manually deconstruct and sell before removing a mod.
2. **Azrael portraits** — stop reusing Cassandra art. Roadmap item; do it once, in Homesteader.
3. **Load-order advisor** — name the correct order for the nine mods and offer to apply it.
4. **Weekly digest** — one series letter instead of nine mods each sending their own.
5. **Bridge toggles** — turn individual soft-compat bridges on and off from the hub, which makes "fail-open" testable by the player.
6. **Save health scan** — find orphaned defs from a series mod that was removed without the wizard.
7. **Series settings profiles** — one Soft / Default / Hard that sets all nine mods coherently.
8. **Storyteller tuning panel** — expose Azrael's incident weights.
9. **"The Long Year"** — a full-series showcase scenario requiring four or more mods.
10. **Version drift warning** — flag when one series mod is much older than the rest, which is the real-world cause of most "it broke" reports.

### Fixes

1. **`SeriesHub.cs` references `AzraelGodKing.Wellspring` and `Wellspring_HandDugWell`** — a mod that does not exist in this repo. Folded into Homesteader at some point; the bridge check is dead code. Remove it or document why it stays. **[verified, lines 91 and 162]**
2. **`Azrael/Languages` has no `README.md`** — every other mod has one. **[verified]**
3. **3 hardcoded English literals** — sweep. **[verified]**
4. **Copy-report should include RimWorld and DLC versions** — AZR-136 built the panel; this is the field that makes a pasted report actionable.
5. **Hub should list what is *not* loaded** — absence is the diagnostic, and right now it's invisible.
6. **No settings class of its own** — confirm the hub is reachable when Homesteader is absent, since that's the standalone path.
7. **`Scenario_DeepHomestead` MayRequires two mods** — give a friendly message when neither is present instead of an empty scenario.
8. **No test for the storyteller inject guard** — canonical-in-Homesteader vs. standalone is exactly a `Tests/ModChecks` assertion.
9. **Azrael's release status is ambiguous** — the README groups it with Living World as "not release-ready," but it ships the series hub that the other mods point at.
10. **Hub roster is hand-maintained** — AZR-125 was "the hub doesn't list Niceties." The test added for it should be extended to assert the roster matches the repo's mod folders.

---

# Five new mods that fill the gaps

Each one is scoped the way your existing ROADMAPs scope things: what it owns, and — more
importantly — what it must not own, so the series keeps its clean seams.

## 1. Teamster — on-map bulk logistics

**The gap.** Strata relays hauling *vertically*. Homesteader gives you places to put things.
Nothing owns moving mass *horizontally*. A colonist carries 75kg in their arms across a 250-cell
map, forever, and that's the whole logistics game.

**The hook.** Handcarts and sledges a pawn pushes (slow, high capacity), packed roads that speed
hauling along them, loading docks that batch a stockpile into one trip, and a hand-cranked ropeway
for crossing terrain that carts can't. Late game: a powered conveyor short-run.

**Owns:** on-map hauling throughput, cart/dock/road/ropeway buildings, haul batching.
**Does not own:** vertical transport (Strata's shafts and lifts), storage buildings
(Homesteader), world-map travel (Trailblazer, below).
**Bridges:** carts ride Strata elevators; Homesteader crates load onto docks directly.

## 2. Ward — the body after the fight

**The gap.** Deep Colony owns the mind — trauma, therapy, resilience. Strata gives you smoke
inhalation. Stormproof gives you toxic surges and burns. Three mods generate lasting *physical*
harm and nothing follows up on it. Vanilla treats recovery as a progress bar in a bed.

**The hook.** Contagion that spreads between pawns and needs quarantine, infirmary quality that
actually changes outcomes, convalescence as a real phase (light duty, relapse risk if you draft
them), and long-tail conditions that need managing rather than curing.

**Owns:** illness, contagion, quarantine, recovery arcs, infirmary rooms and medical furniture.
**Does not own:** psychology and trauma (Deep Colony), the hazards that cause injury
(Strata, Stormproof), surgery itself (vanilla).
**Bridges:** Deep Colony trauma reads physical recovery; Strata smoke inhalation and Stormproof
toxic buildup become Ward conditions rather than dead-end hediffs. This is the strongest
soft-compat story of the five — it's the missing half of three mods you already ship.

## 3. Tradecraft — tools and the workshop

**The gap.** Deep Colony's apprenticeship teaches skills brilliantly and there is no craft economy
to spend them in. Homesteader owns farm-to-table; nothing owns bench-to-market. A Crafting 20
master and a Crafting 8 apprentice differ by a quality roll and nothing else.

**The hook.** Tools as equipment that wear out and get replaced, workshop quality as a room role,
maker's marks on crafted goods, and commissioned work — a trader asks for a specific item at a
specific quality and pays for the risk.

**Owns:** tools, tool wear, workshop rooms, maker's marks, commissions.
**Does not own:** skills and teaching (Deep Colony), food production (Homesteader),
trade caravans (Trailblazer).
**Bridges:** an apprentice's first tool comes from their mentor; Homesteader preserves get maker's
marks and vintage; a master's mark raises Living World prosperity where their goods end up.

## 4. Bulwark — fortification and siege

**The gap.** Stormproof defends the grid. Nemesis sends one person who hates you. Living World
starts wars you watch from a distance. Strata lets you seal a stairwell. Nobody owns the wall.
Vanilla sieges are a mortar and a nap.

**The hook.** Wall integrity that degrades under sustained fire, gates that open for your caravans
and not for theirs, watchtowers with real sightline bonuses, patrol routes colonists walk on
schedule, and siege attrition that makes a long investment a supply problem for the besieger too.

**Owns:** fortification buildings, wall integrity, gates, patrols, siege mechanics.
**Does not own:** the antagonist driving the siege (Nemesis), the wars that generate hostility
(Living World), weather damage to structures (Stormproof), sealing underground (Strata).
**Bridges:** a Nemesis assault tests your walls specifically; Living World wars produce siege
warbands; Stormproof storm wear applies to fortifications.

## 5. Trailblazer — the road

**The gap.** Three mods point at the road and none of them own it. Homesteader ships preserve
crates to "pack a mixed lot for trade in one click." Nemesis wants a caravan-route ambush.
Living World wants inter-settlement traffic. Meanwhile vanilla caravanning is a spreadsheet
and a loading screen.

**The hook.** Expedition prep as a real phase (provisioning, pack animals, route planning),
way-camps you build and return to, route hazards that depend on terrain and season, and returning
caravans that bring back stories — a veteran hediff, world knowledge, and trade contacts.

**Owns:** caravan preparation, way-camps, route hazards, expedition outcomes.
**Does not own:** the world map itself and faction movement (Living World), on-map hauling
(Teamster), the ambushes a nemesis stages (Nemesis owns those, Trailblazer just provides the road).
**Bridges:** Homesteader preserve crates are the ideal expedition provision; Nemesis route ambushes
land on Trailblazer routes; Living World's LW9 traffic and your caravans share the same road model.

---

## If you only do five things from this document

1. **AZR-137 safe removal wizard** (Azrael) — the README currently asks players to do it by hand.
2. **Resolve Living World's status** — shipped per its ROADMAP, absent from every release path.
3. **The three literal sweeps** — Strata 45, Deep Colony 27, Nemesis 13. Mechanical, and you ship
   in three languages.
4. **Remove the dead `Wellspring` bridge** in `SeriesHub.cs` — the series hub is the thing that
   tells players what's healthy; it shouldn't be checking for a mod that doesn't exist.
5. **Ward**, of the five new mods — it's the only one that completes systems in three mods you
   have already built, rather than opening a sixth front.
