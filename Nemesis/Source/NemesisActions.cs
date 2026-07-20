using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Nemesis
{
    public static class NemesisActions
    {
        public static void Execute(NemesisAction action, NemesisData data, Map map)
        {
            // Consecutive ignored actions (no arrest / damage on the nemesis since last fire).
            if (data.harassmentCount > 0 && !data.engagedSinceLastAction)
            {
                data.ignoredActionsCount++;
                int thresh = NemesisMod.Settings?.ignoredActionsThreshold ?? 3;
                if (data.ignoredActionsCount == thresh)
                {
                    data.aggressionLevel = Mathf.Min(data.aggressionLevel + 0.35f, 10f);
                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_IgnoredTitle".Translate(data.nemesisName),
                        "Nemesis_Letter_IgnoredBody".Translate(
                            data.nemesisName, NemesisTaunts.TargetPhrase(data)),
                        LetterDefOf.ThreatSmall);
                }
            }

            data.lastActionKind = (int)action;
            data.harassmentCount++;
            data.engagedSinceLastAction = false;

            switch (action)
            {
                case NemesisAction.CommsTaunt:
                    CommsTaunt(data, map);
                    break;
                case NemesisAction.DirectRaid:
                    DirectRaid(data, map);
                    ScheduleSilenceAfter(data, map);
                    break;
                case NemesisAction.NemesisAssault:
                    NemesisAssault(data, map);
                    ScheduleSilenceAfter(data, map);
                    break;
                case NemesisAction.WastePackDrop:
                    WastePackDrop(data, map);
                    break;
                case NemesisAction.FakeSignalAmbush:
                    FakeSignalAmbush(data, map);
                    break;
                case NemesisAction.CaravanHarass:
                    CaravanHarass(data, map);
                    break;
                case NemesisAction.PowerSabotage:
                    PowerSabotage(data, map);
                    break;
                case NemesisAction.FoodStoreRaid:
                    FoodStoreRaid(data, map);
                    break;
                case NemesisAction.AnomalyBait:
                    AnomalyBait(data, map);
                    break;
                case NemesisAction.KidnapAttempt:
                    KidnapAttempt(data, map);
                    ScheduleSilenceAfter(data, map);
                    break;
                case NemesisAction.SniperTerror:
                    SniperTerror(data, map);
                    break;
                case NemesisAction.GraveDesecration:
                    GraveDesecration(data, map);
                    break;
                case NemesisAction.FoodTampering:
                    FoodTampering(data, map);
                    break;
                case NemesisAction.InformantReveal:
                    InformantReveal(data, map);
                    break;
                default:
                    CommsTaunt(data, map);
                    break;
            }

            NemesisMood.NotifyActionStruck(data);
        }

        public static void CommsTaunt(NemesisData data, Map map)
        {
            string taunt = NemesisTaunts.PickCommsLine(data);
            string title;
            string body;
            if (HasCommsConsole(map))
            {
                title = "Nemesis_Letter_CommsTitle".Translate();
                body = "Nemesis_Letter_CommsBody".Translate(taunt);
            }
            else
            {
                title = "Nemesis_Letter_NoteTitle".Translate(data.nemesisName);
                body = "Nemesis_Letter_NoteBody".Translate(taunt);
            }

            Find.LetterStack.ReceiveLetter(
                title,
                body + "\n\n" + "Nemesis_Letter_PhaseFooter".Translate(data.PhaseLabelKeyed()),
                LetterDefOf.NeutralEvent);
        }

        private static bool HasCommsConsole(Map map)
        {
            if (map == null) return false;
            List<Building> buildings = map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < buildings.Count; i++)
            {
                Building b = buildings[i];
                if (!(b is Building_CommsConsole)) continue;
                CompPowerTrader power = b.TryGetComp<CompPowerTrader>();
                if (power == null || power.PowerOn) return true;
            }
            return false;
        }

        private static void DirectRaid(NemesisData data, Map map)
        {
            Faction faction = FindFaction(data);
            if (faction == null || faction.IsPlayer)
            {
                CommsTaunt(data, map);
                return;
            }

            IncidentDef raidDef = DefDatabase<IncidentDef>.GetNamedSilentFail("RaidEnemy");
            if (raidDef == null)
            {
                CommsTaunt(data, map);
                return;
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
            parms.faction = faction;
            parms.points = Mathf.Max(250f, parms.points * AggressionRaidFactor(data.EffectiveAggression));
            parms.customLetterLabel = "Nemesis_Letter_RaidTitle".Translate(data.nemesisName);
            parms.customLetterText = "Nemesis_Letter_RaidBody".Translate(data.nemesisName, NemesisTaunts.TargetPhrase(data));

            if (!raidDef.Worker.TryExecute(parms))
                CommsTaunt(data, map);
        }

        private static void NemesisAssault(NemesisData data, Map map)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            Pawn nemesis = comp?.FindNemesisPawn();

            if (nemesis == null || nemesis.Spawned)
            {
                DirectRaid(data, map);
                return;
            }

            if (nemesis.IsWorldPawn())
                Find.WorldPawns.RemovePawn(nemesis);

            if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map) && !c.Fogged(map), map, 0f, out IntVec3 spawnCell))
                spawnCell = CellFinder.RandomEdgeCell(map);

            GenSpawn.Spawn(nemesis, spawnCell, map);
            NemesisRegistry.CachedNemesis = nemesis;
            NemesisRegistry.CachedNemesisId = nemesis.thingIDNumber;

            // canTimeoutOrFlee: true so they pull back when losing; Kill intercept still handles "death".
            if (nemesis.Faction != null && !nemesis.Faction.IsPlayer)
            {
                LordMaker.MakeNewLord(
                    nemesis.Faction,
                    new LordJob_AssaultColony(nemesis.Faction, canKidnap: false, canTimeoutOrFlee: true),
                    map,
                    new[] { nemesis });
            }

            Faction faction = FindFaction(data);
            if (faction != null)
            {
                IncidentDef raidDef = DefDatabase<IncidentDef>.GetNamedSilentFail("RaidEnemy");
                if (raidDef != null)
                {
                    IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
                    parms.faction = faction;
                    parms.points = Mathf.Max(150f, parms.points * AggressionRaidFactor(data.EffectiveAggression) * 0.55f);
                    raidDef.Worker.TryExecute(parms);
                }
            }

            string body = data.targetMode == NemesisTargetMode.Pawn && data.targetPawnName != null
                ? "Nemesis_Letter_AssaultPawn".Translate(data.nemesisName, data.targetPawnName)
                : "Nemesis_Letter_AssaultColony".Translate(data.nemesisName);

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_AssaultTitle".Translate(data.nemesisName),
                body,
                LetterDefOf.ThreatBig,
                nemesis);
        }

        private static void WastePackDrop(NemesisData data, Map map)
        {
            ThingDef wasteDef = DefDatabase<ThingDef>.GetNamedSilentFail("Wastepack");
            if (wasteDef == null || !ModsConfig.BiotechActive)
            {
                CommsTaunt(data, map);
                return;
            }

            IntVec3 center = map.Center;
            int count = Rand.RangeInclusive(3, 6);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell = CellFinder.RandomClosewalkCellNear(center, map, 18);
                if (cell.IsValid)
                    GenSpawn.Spawn(ThingMaker.MakeThing(wasteDef), cell, map);
            }

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_WasteTitle".Translate(data.nemesisName),
                "Nemesis_Letter_WasteBody".Translate(data.nemesisName),
                LetterDefOf.NegativeEvent,
                new GlobalTargetInfo(center, map));
        }

        private static void FakeSignalAmbush(NemesisData data, Map map)
        {
            // Stage 1: believable offer. Stage 2 fires from GameComponent after delay.
            data.pendingFakeAmbush = true;
            data.fakeAmbushTick = Find.TickManager.TicksGame + Rand.RangeInclusive(2500, 10000);

            string kind = Rand.Bool
                ? "Nemesis_Fake_Trader".Translate()
                : "Nemesis_Fake_Distress".Translate();

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_FakeSignalTitle".Translate(),
                "Nemesis_Letter_FakeSignalBody".Translate(kind, data.nemesisName),
                LetterDefOf.PositiveEvent);
        }

        public static void ResolvePendingFakeAmbush(NemesisData data, Map map)
        {
            data.pendingFakeAmbush = false;
            data.fakeAmbushTick = -1;

            Faction faction = FindFaction(data);
            IncidentDef raidDef = DefDatabase<IncidentDef>.GetNamedSilentFail("RaidEnemy");
            if (faction == null || raidDef == null)
            {
                CommsTaunt(data, map);
                return;
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
            parms.faction = faction;
            parms.points = Mathf.Max(300f, parms.points * AggressionRaidFactor(data.EffectiveAggression) * 0.85f);
            parms.customLetterLabel = "Nemesis_Letter_AmbushTitle".Translate(data.nemesisName);
            parms.customLetterText = "Nemesis_Letter_AmbushBody".Translate(data.nemesisName);

            if (!raidDef.Worker.TryExecute(parms))
                CommsTaunt(data, map);
            else
                ScheduleSilenceAfter(data, map);
        }

        private static void CaravanHarass(NemesisData data, Map map)
        {
            List<Caravan> caravans = Find.WorldObjects.Caravans;
            Caravan target = null;
            for (int i = 0; i < caravans.Count; i++)
            {
                Caravan c = caravans[i];
                if (c != null && c.Faction == Faction.OfPlayer && c.PawnsListForReading.Count > 0)
                {
                    target = c;
                    break;
                }
            }

            if (target == null)
            {
                // No caravan out — diversion raid / taunt about roads.
                if (Rand.Chance(0.45f))
                    DirectRaid(data, map);
                else
                {
                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_CaravanWatchTitle".Translate(data.nemesisName),
                        "Nemesis_Letter_CaravanWatchBody".Translate(data.nemesisName),
                        LetterDefOf.ThreatSmall);
                }
                return;
            }

            // Soft hit: minor inventory loss if the caravan carries haulables.
            List<Thing> inventory = new List<Thing>();
            foreach (Thing t in target.AllThings)
            {
                if (t == null || t is Pawn || t.def.IsCorpse || !t.def.EverHaulable) continue;
                inventory.Add(t);
            }
            int ruined = 0;
            while (inventory.Count > 0 && ruined < 2)
            {
                int idx = Rand.Range(0, inventory.Count);
                Thing t = inventory[idx];
                inventory.RemoveAt(idx);
                if (t == null || t.Destroyed) continue;
                int lose = Mathf.Max(1, t.stackCount / 4);
                t.SplitOff(lose).Destroy(DestroyMode.Vanish);
                ruined++;
            }

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_CaravanHitTitle".Translate(data.nemesisName),
                "Nemesis_Letter_CaravanHitBody".Translate(data.nemesisName, target.LabelCap),
                LetterDefOf.NegativeEvent,
                target);
        }

        private static void PowerSabotage(NemesisData data, Map map)
        {
            List<Building> candidates = new List<Building>();
            List<Building> all = map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < all.Count; i++)
            {
                Building b = all[i];
                if (b == null || b.Destroyed) continue;
                CompPowerTrader power = b.TryGetComp<CompPowerTrader>();
                if (power == null || !power.PowerOn) continue;
                if (SoftCompat.IsBuildingEmpProtected(b)) continue;
                candidates.Add(b);
            }

            if (candidates.Count == 0)
            {
                string dampenNote = SoftCompat.StormproofActive
                    ? "Nemesis_Letter_SabotageBlocked".Translate(data.nemesisName)
                    : null;
                if (dampenNote != null)
                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_SabotageTitle".Translate(data.nemesisName),
                        dampenNote,
                        LetterDefOf.NeutralEvent);
                else
                    CommsTaunt(data, map);
                return;
            }

            int hits = Mathf.Clamp(1 + (int)(data.EffectiveAggression / 4f), 1, 4);
            int applied = 0;
            for (int i = 0; i < hits && candidates.Count > 0; i++)
            {
                Building b = candidates[Rand.Range(0, candidates.Count)];
                candidates.Remove(b);
                DamageInfo dinfo = new DamageInfo(DamageDefOf.EMP, 50f);
                b.TakeDamage(dinfo);
                applied++;
            }

            bool surgeBlocked = SoftCompat.MapHasReadySurgeProtector(map);
            if (!surgeBlocked && Rand.Chance(0.35f))
            {
                // Extra "Zzzt!" flavor when unprotected.
                IncidentDef zzzt = DefDatabase<IncidentDef>.GetNamedSilentFail("ShortCircuit");
                zzzt?.Worker.TryExecute(StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, map));
            }

            string body = surgeBlocked
                ? "Nemesis_Letter_SabotageSurgeBlocked".Translate(data.nemesisName, applied)
                : "Nemesis_Letter_SabotageBody".Translate(data.nemesisName, applied);

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_SabotageTitle".Translate(data.nemesisName),
                body,
                LetterDefOf.ThreatSmall);
        }

        private static void FoodStoreRaid(NemesisData data, Map map)
        {
            List<Thing> foods = new List<Thing>();
            List<Thing> candidates = map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree);
            if (candidates != null)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    Thing t = candidates[i];
                    if (t == null || t.Destroyed || t.Faction != Faction.OfPlayer) continue;
                    if (!t.def.IsNutritionGivingIngestible) continue;
                    if (t.stackCount < 5) continue;
                    foods.Add(t);
                }
            }

            // Prefer cellar-adjacent stacks when Homesteader is present.
            bool cellar = SoftCompat.MapHasRootCellar(map);
            int ruined = 0;
            int targetCount = Rand.RangeInclusive(1, 3);
            for (int i = 0; i < foods.Count && ruined < targetCount; i++)
            {
                Thing t = foods[Rand.Range(0, foods.Count)];
                if (t == null || t.Destroyed) continue;
                int lose = Mathf.Clamp(t.stackCount / 3, 1, 25);
                t.SplitOff(lose).Destroy(DestroyMode.Vanish);
                ruined++;
            }

            if (ruined == 0)
            {
                CommsTaunt(data, map);
                return;
            }

            string body = cellar
                ? "Nemesis_Letter_FoodCellar".Translate(data.nemesisName, ruined)
                : "Nemesis_Letter_FoodBody".Translate(data.nemesisName, ruined);

            Pawn victim = GameComponent_Nemesis.Instance?.FindTargetPawn();
            string fav = SoftCompat.TryFavoriteFoodLabel(victim);
            if (fav != null)
                body += "\n\n" + "Nemesis_Letter_FoodFavoriteNote".Translate(data.nemesisName, victim.LabelShort, fav);

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_FoodTitle".Translate(data.nemesisName),
                body,
                LetterDefOf.NegativeEvent);
        }

        private static void AnomalyBait(NemesisData data, Map map)
        {
            if (!ModsConfig.AnomalyActive)
            {
                CommsTaunt(data, map);
                return;
            }

            string[] tryDefs =
            {
                "FleshbeastAttack",
                "HateChanters",
                "FleshmassHeart",
                "PsychicRitualSiege",
                "PitGate",
            };

            for (int i = 0; i < tryDefs.Length; i++)
            {
                IncidentDef def = DefDatabase<IncidentDef>.GetNamedSilentFail(tryDefs[i]);
                if (def?.Worker == null) continue;
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(
                    def.category ?? IncidentCategoryDefOf.ThreatBig, map);
                parms.points = Mathf.Max(200f, parms.points * 0.55f);
                if (def.Worker.CanFireNow(parms) && def.Worker.TryExecute(parms))
                {
                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_AnomalyTitle".Translate(data.nemesisName),
                        "Nemesis_Letter_AnomalyBody".Translate(data.nemesisName),
                        LetterDefOf.ThreatBig);
                    return;
                }
            }

            CommsTaunt(data, map);
        }

        private static void KidnapAttempt(NemesisData data, Map map)
        {
            if (data.Phase < NemesisHuntPhase.Obsessed)
            {
                CommsTaunt(data, map);
                return;
            }

            Faction faction = FindFaction(data);
            if (faction == null || faction.IsPlayer)
            {
                CommsTaunt(data, map);
                return;
            }

            PawnKindDef kind = faction.RandomPawnKind();
            if (kind == null || !kind.RaceProps.Humanlike)
                kind = PawnKindDefOf.SpaceRefugee;

            int count = Rand.RangeInclusive(2, 3);
            List<Pawn> squad = new List<Pawn>();
            for (int i = 0; i < count; i++)
            {
                PawnGenerationRequest req = new PawnGenerationRequest(
                    kind, faction, PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true, canGeneratePawnRelations: false, allowDead: false);
                Pawn p = PawnGenerator.GeneratePawn(req);
                if (p == null) continue;
                squad.Add(p);
            }

            if (squad.Count == 0)
            {
                CommsTaunt(data, map);
                return;
            }

            if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map) && !c.Fogged(map), map, 0f, out IntVec3 edge))
                edge = CellFinder.RandomEdgeCell(map);

            for (int i = 0; i < squad.Count; i++)
                GenSpawn.Spawn(squad[i], CellFinder.RandomClosewalkCellNear(edge, map, 4), map);

            // Kidnap-capable assault — vanilla downed-kidnap flow creates the rescue arc on success.
            LordMaker.MakeNewLord(
                faction,
                new LordJob_AssaultColony(faction, canKidnap: true, canTimeoutOrFlee: true),
                map,
                squad);

            string mark = NemesisTaunts.TargetPhrase(data);
            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_KidnapTitle".Translate(data.nemesisName),
                "Nemesis_Letter_KidnapBody".Translate(data.nemesisName, mark),
                LetterDefOf.ThreatBig,
                squad[0]);
        }

        private static void SniperTerror(NemesisData data, Map map)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            Pawn nemesis = comp?.FindNemesisPawn();
            if (nemesis == null || nemesis.Spawned || nemesis.Dead || nemesis.IsPrisonerOfColony)
            {
                CommsTaunt(data, map);
                return;
            }

            if (nemesis.IsWorldPawn())
                Find.WorldPawns.RemovePawn(nemesis);

            // Far edge — prefer cell distant from colony center.
            IntVec3 spawnCell = IntVec3.Invalid;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map) && !c.Fogged(map), map, 0f, out IntVec3 c))
                    continue;
                if (c.DistanceTo(map.Center) < 30f) continue;
                spawnCell = c;
                break;
            }
            if (!spawnCell.IsValid)
                spawnCell = CellFinder.RandomEdgeCell(map);

            GenSpawn.Spawn(nemesis, spawnCell, map);
            NemesisRegistry.CachedNemesis = nemesis;
            NemesisRegistry.CachedNemesisId = nemesis.thingIDNumber;

            data.sniperActive = true;
            data.sniperUntilTick = Find.TickManager.TicksGame + 2500;
            data.sniperShotsLeft = Rand.RangeInclusive(2, 4);

            // Stand and fire toward the target's area — lord assaults briefly; GameComponent despawns.
            if (nemesis.Faction != null && !nemesis.Faction.IsPlayer)
            {
                LordMaker.MakeNewLord(
                    nemesis.Faction,
                    new LordJob_AssaultColony(nemesis.Faction, canKidnap: false, canTimeoutOrFlee: true),
                    map,
                    new[] { nemesis });
            }

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_SniperTitle".Translate(data.nemesisName),
                "Nemesis_Letter_SniperBody".Translate(data.nemesisName, NemesisTaunts.TargetPhrase(data)),
                LetterDefOf.ThreatSmall,
                nemesis);
        }

        /// <summary>Despawn sniper terror nemesis back to world pawns (FireEscape pattern).</summary>
        public static void EndSniperTerror(NemesisData data, Pawn nemesis, bool approached)
        {
            if (data == null) return;
            data.sniperActive = false;
            data.sniperUntilTick = -1;
            data.sniperShotsLeft = 0;

            if (nemesis == null || nemesis.Destroyed || nemesis.Dead) return;
            if (nemesis.IsPrisonerOfColony) return;

            Map map = nemesis.Map;
            IntVec3 pos = nemesis.Spawned ? nemesis.Position : IntVec3.Invalid;

            if (nemesis.Spawned)
                nemesis.DeSpawn(DestroyMode.WillReplace);
            if (!nemesis.IsWorldPawn())
                Find.WorldPawns.PassToWorld(nemesis, PawnDiscardDecideMode.KeepForever);

            NemesisRegistry.CachedNemesis = nemesis;
            NemesisRegistry.CachedNemesisId = nemesis.thingIDNumber;

            if (approached && map != null && pos.IsValid)
            {
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_SniperFleeTitle".Translate(data.nemesisName),
                    "Nemesis_Letter_SniperFleeBody".Translate(data.nemesisName),
                    LetterDefOf.NeutralEvent,
                    new GlobalTargetInfo(pos, map));
            }
        }

        private static void GraveDesecration(NemesisData data, Map map)
        {
            List<Thing> targets = new List<Thing>();
            List<Thing> graves = map.listerThings.ThingsOfDef(ThingDefOf.Grave);
            if (graves != null)
            {
                for (int i = 0; i < graves.Count; i++)
                {
                    Thing t = graves[i];
                    if (t != null && !t.Destroyed && t.Faction == Faction.OfPlayer)
                        targets.Add(t);
                }
            }

            ThingDef sarco = DefDatabase<ThingDef>.GetNamedSilentFail("Sarcophagus");
            if (sarco != null)
            {
                List<Thing> list = map.listerThings.ThingsOfDef(sarco);
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        Thing t = list[i];
                        if (t != null && !t.Destroyed && t.Faction == Faction.OfPlayer)
                            targets.Add(t);
                    }
                }
            }

            // Art installations — prefer ones with a tale referencing a colonist.
            List<Thing> artPrefer = new List<Thing>();
            List<Thing> artOther = new List<Thing>();
            List<Thing> all = map.listerThings.ThingsInGroup(ThingRequestGroup.Art);
            if (all != null)
            {
                for (int i = 0; i < all.Count; i++)
                {
                    Thing t = all[i];
                    if (t == null || t.Destroyed || t.Faction != Faction.OfPlayer) continue;
                    CompArt art = t.TryGetComp<CompArt>();
                    if (art == null || !art.Active) continue;
                    if (ArtReferencesColonist(art))
                        artPrefer.Add(t);
                    else
                        artOther.Add(t);
                }
            }

            for (int i = 0; i < artPrefer.Count; i++) targets.Add(artPrefer[i]);
            for (int i = 0; i < artOther.Count; i++) targets.Add(artOther[i]);

            if (targets.Count == 0)
            {
                CommsTaunt(data, map);
                return;
            }

            int hits = Rand.RangeInclusive(1, 2);
            int ruined = 0;
            Thing look = null;
            for (int i = 0; i < hits && targets.Count > 0; i++)
            {
                int idx = Rand.Range(0, Mathf.Min(targets.Count, artPrefer.Count > 0 ? artPrefer.Count + 2 : targets.Count));
                if (idx >= targets.Count) idx = targets.Count - 1;
                Thing t = targets[idx];
                targets.RemoveAt(idx);
                if (t == null || t.Destroyed) continue;
                float dmg = t.MaxHitPoints * Rand.Range(0.55f, 1.1f);
                t.TakeDamage(new DamageInfo(DamageDefOf.Blunt, dmg));
                look = t;
                ruined++;
            }

            if (ruined == 0)
            {
                CommsTaunt(data, map);
                return;
            }

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_GraveTitle".Translate(data.nemesisName),
                "Nemesis_Letter_GraveBody".Translate(data.nemesisName, ruined),
                LetterDefOf.NegativeEvent,
                look);
        }

        private static bool ArtReferencesColonist(CompArt art)
        {
            try
            {
                string desc = art.GenerateImageDescription();
                if (string.IsNullOrEmpty(desc)) return false;
                List<Pawn> colonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists;
                if (colonists == null) return false;
                for (int i = 0; i < colonists.Count; i++)
                {
                    Pawn p = colonists[i];
                    if (p == null) continue;
                    string shortName = p.LabelShort;
                    if (!string.IsNullOrEmpty(shortName) && desc.IndexOf(shortName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            catch
            {
                // Fail-open: treat as non-preferred art.
            }
            return false;
        }

        private static void FoodTampering(NemesisData data, Map map)
        {
            List<Thing> foods = new List<Thing>();
            List<Thing> candidates = map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree);
            if (candidates != null)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    Thing t = candidates[i];
                    if (t == null || t.Destroyed || t.Faction != Faction.OfPlayer) continue;
                    if (!t.def.IsNutritionGivingIngestible) continue;
                    if (t.stackCount < 15) continue;
                    foods.Add(t);
                }
            }

            if (foods.Count == 0)
            {
                CommsTaunt(data, map);
                return;
            }

            // Prefer largest stack.
            Thing best = foods[0];
            for (int i = 1; i < foods.Count; i++)
            {
                if (foods[i].stackCount > best.stackCount)
                    best = foods[i];
            }

            data.taintedFoodThingId = best.thingIDNumber;
            data.taintedUntilTick = Find.TickManager.TicksGame + 60000 * Rand.RangeInclusive(2, 4);
            data.taintedRevealTick = Find.TickManager.TicksGame + Rand.RangeInclusive(15000, 45000);
            data.taintedRevealed = false;

            // Silent — delayed reveal letter fires from GameComponent.
        }

        private static void InformantReveal(NemesisData data, Map map)
        {
            int leave = data.lastVisitorLeaveTick;
            if (leave < 0)
            {
                CommsTaunt(data, map);
                return;
            }

            int age = Find.TickManager.TicksGame - leave;
            // Within a few days of a trade/visitor group leaving.
            if (age < 0 || age > 60000 * 4)
            {
                CommsTaunt(data, map);
                return;
            }

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_InformantTitle".Translate(data.nemesisName),
                "Nemesis_Letter_InformantBody".Translate(data.nemesisName, NemesisTaunts.TargetPhrase(data)),
                LetterDefOf.ThreatSmall);
        }

        /// <summary>Reckoning refuse path — heaviest personal + faction assault.</summary>
        public static void ExecuteFinaleAssault(NemesisData data, Map map)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            Pawn nemesis = comp?.FindNemesisPawn();

            if (nemesis != null && !nemesis.Dead && !nemesis.Spawned && !nemesis.IsPrisonerOfColony)
            {
                if (nemesis.IsWorldPawn())
                    Find.WorldPawns.RemovePawn(nemesis);
                if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map) && !c.Fogged(map), map, 0f, out IntVec3 spawnCell))
                    spawnCell = CellFinder.RandomEdgeCell(map);
                GenSpawn.Spawn(nemesis, spawnCell, map);
                NemesisRegistry.CachedNemesis = nemesis;
                NemesisRegistry.CachedNemesisId = nemesis.thingIDNumber;
                if (nemesis.Faction != null && !nemesis.Faction.IsPlayer)
                {
                    LordMaker.MakeNewLord(
                        nemesis.Faction,
                        new LordJob_AssaultColony(nemesis.Faction, canKidnap: false, canTimeoutOrFlee: false),
                        map,
                        new[] { nemesis });
                }
            }

            Faction faction = FindFaction(data);
            IncidentDef raidDef = DefDatabase<IncidentDef>.GetNamedSilentFail("RaidEnemy");
            if (faction != null && raidDef != null)
            {
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
                parms.faction = faction;
                parms.points = Mathf.Max(500f, parms.points * AggressionRaidFactor(10f) * 1.35f);
                parms.customLetterLabel = "Nemesis_Letter_FinaleAssaultTitle".Translate(data.nemesisName);
                parms.customLetterText = "Nemesis_Letter_FinaleAssaultBody".Translate(
                    data.nemesisName, NemesisTaunts.TargetPhrase(data));
                raidDef.Worker.TryExecute(parms);
            }

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_FinaleAssaultTitle".Translate(data.nemesisName),
                "Nemesis_Letter_FinaleAssaultBody".Translate(data.nemesisName, NemesisTaunts.TargetPhrase(data)),
                LetterDefOf.ThreatBig,
                nemesis);
        }

        public static void ScheduleSilenceAfter(NemesisData data, Map map)
        {
            if (data == null) return;
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            int interval = Mathf.Max(
                NemesisMod.Settings?.minActionCooldownTicks ?? 90000,
                (int)(300000f / Mathf.Max(1f, data.EffectiveAggression)));
            data.ScheduleSilence(interval);
        }

        private static float AggressionRaidFactor(float aggressionLevel) =>
            Mathf.Lerp(0.55f, 2.2f, (aggressionLevel - 1f) / 9f);

        public static Faction FindFaction(NemesisData data)
        {
            if (data == null) return null;

            if (data.faction != null && !data.faction.IsPlayer && !data.faction.defeated)
                return data.faction;

            if (data.factionName == null) return null;

            foreach (Faction f in Find.FactionManager.AllFactions)
            {
                if (f != null && f.Name == data.factionName && !f.IsPlayer && !f.defeated)
                    return f;
            }
            return null;
        }
    }
}
