using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Homesteader
{
    // Rare Misc incident keyed to finished 27 statues/monuments. Hybrid letter
    // (RimWorld anomalous broadcast + short Foundation addendum), heat dome,
    // short brain-rot hediff, silver Super Chat drain, and a personal Kats mood.
    // Unpaid Super Chat materializes hostile orange Wolfein "Kats" (soft-compat).
    public class IncidentWorker_KatsEffect : IncidentWorker
    {
        private static readonly string[] StatueDefNames =
        {
            "Homesteader_Monument27_Golden",
            "Homesteader_Monument27_Harvest",
            "Homesteader_StatueTwentySeven",
            "Homesteader_Statue27Grand",
        };

        // Low early-game Super Chat; thematic 27.
        private const int SuperChatCost = 27;

        private static readonly Color KatsOrangeHair = new Color(1.00f, 0.45f, 0.08f);

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map
                && map.IsPlayerHome
                && FindAnyStatue(map) != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            Building statue = FindAnyStatue(map);
            if (statue == null)
            {
                return false;
            }

            int durationTicks = Mathf.RoundToInt(def.durationDays.RandomInRange * GenDate.TicksPerDay);
            GameCondition condition = GameConditionMaker.MakeCondition(
                DefDatabase<GameConditionDef>.GetNamed("Homesteader_KatsHeatDome"),
                durationTicks);
            map.gameConditionManager.RegisterCondition(condition);

            int silverTaken = 0;
            Pawn kats = null;
            if (map.resourceCounter.GetCount(ThingDefOf.Silver) >= SuperChatCost)
            {
                TradeUtility.LaunchSilver(map, SuperChatCost);
                silverTaken = SuperChatCost;
            }
            else
            {
                kats = TrySpawnHostileKats(map);
            }

            HediffDef brainRot = DefDatabase<HediffDef>.GetNamedSilentFail("Homesteader_KatsBrainRot");
            ThoughtDef directive = DefDatabase<ThoughtDef>.GetNamedSilentFail("Homesteader_KatsDirective");
            foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
            {
                if (pawn.RaceProps.Humanlike && pawn.needs?.mood != null)
                {
                    if (brainRot != null && !pawn.health.hediffSet.HasHediff(brainRot))
                    {
                        pawn.health.AddHediff(brainRot);
                    }
                    if (directive != null)
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemory(directive);
                    }
                }
            }

            string letterText = BuildLetterText(silverTaken, kats);
            LookTargets look = kats != null
                ? new LookTargets(kats)
                : new LookTargets(statue);
            LetterDef letterDef = kats != null
                ? LetterDefOf.ThreatSmall
                : (def.letterDef ?? LetterDefOf.NeutralEvent);
            Find.LetterStack.ReceiveLetter(
                def.letterLabel,
                letterText,
                letterDef,
                look,
                parms.faction,
                parms.quest,
                parms.letterHyperlinkThingDefs);
            return true;
        }

        private static string BuildLetterText(int silverTaken, Pawn kats)
        {
            var sb = new StringBuilder();
            sb.Append("Homesteader_KatsLetter_Open".Translate());
            sb.AppendLine();
            sb.AppendLine();
            if (silverTaken > 0)
            {
                sb.Append("Homesteader_KatsLetter_SilverTaken".Translate(silverTaken));
            }
            else if (kats != null)
            {
                string raceLabel = kats.def?.label ?? "pawn";
                sb.Append("Homesteader_KatsLetter_KatsSpawned".Translate(SuperChatCost, raceLabel));
            }
            else
            {
                sb.Append("Homesteader_KatsLetter_NoSilver".Translate(SuperChatCost));
            }
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine("Homesteader_KatsLetter_FoundationHeader".Translate());
            sb.Append("Homesteader_KatsLetter_ContainmentNote".Translate());
            return sb.ToString();
        }

        private static Pawn TrySpawnHostileKats(Map map)
        {
            if (!RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 entry, map, CellFinder.EdgeRoadChance_Hostile))
            {
                if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map), map, CellFinder.EdgeRoadChance_Hostile, out entry))
                {
                    return null;
                }
            }

            Faction faction = Faction.OfAncientsHostile ?? Faction.OfPirates;
            if (faction == null)
            {
                return null;
            }

            // Prefer Wolfein when that race mod is loaded; any humanlike pawn kind is fine otherwise.
            PawnKindDef kind = ResolveKatsPawnKind(out bool wolfein);
            XenotypeDef xenotype = wolfein ? ResolveOrangeWolfeinXenotype() : null;

            var request = new PawnGenerationRequest(
                kind,
                faction,
                PawnGenerationContext.NonPlayer,
                -1,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                mustBeCapableOfViolence: true,
                allowFood: true,
                allowAddictions: false,
                forceNoIdeo: true,
                forcedXenotype: xenotype);

            Pawn kats = PawnGenerator.GeneratePawn(request);
            kats.Name = new NameTriple("Kats", "Kats", "Kats");
            ApplyOrangeLook(kats);

            GenSpawn.Spawn(kats, entry, map);

            // Keep her hostile and hunting the colony rather than milling at the edge.
            LordMaker.MakeNewLord(
                faction,
                new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: false),
                map,
                new[] { kats });

            return kats;
        }

        private static void ApplyOrangeLook(Pawn pawn)
        {
            if (pawn?.story == null)
            {
                return;
            }

            pawn.story.HairColor = KatsOrangeHair;
        }

        private static PawnKindDef ResolveKatsPawnKind(out bool wolfein)
        {
            wolfein = false;
            string[] preferred =
            {
                "Wolfein_Colonist",
                "Wolfein_Trader",
                "Wolfein_Pirate",
                "Wolfein_Soldier",
                "Alien_Wolfein",
                "Wolfein",
            };

            for (int i = 0; i < preferred.Length; i++)
            {
                PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(preferred[i]);
                if (IsUsableKatsKind(kind))
                {
                    wolfein = true;
                    return kind;
                }
            }

            List<PawnKindDef> all = DefDatabase<PawnKindDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                PawnKindDef kind = all[i];
                if (!IsUsableKatsKind(kind) || !IsWolfeinRace(kind.race))
                {
                    continue;
                }

                wolfein = true;
                return kind;
            }

            // No Wolfein mod — any combat-capable humanlike kind works.
            if (IsUsableKatsKind(PawnKindDefOf.Villager))
            {
                return PawnKindDefOf.Villager;
            }

            if (IsUsableKatsKind(PawnKindDefOf.Colonist))
            {
                return PawnKindDefOf.Colonist;
            }

            for (int i = 0; i < all.Count; i++)
            {
                PawnKindDef kind = all[i];
                if (IsUsableKatsKind(kind) && kind.RaceProps.Humanlike)
                {
                    return kind;
                }
            }

            return PawnKindDefOf.Villager;
        }

        private static bool IsUsableKatsKind(PawnKindDef kind)
        {
            return kind?.race != null && kind.RaceProps.Humanlike && kind.combatPower > 0f;
        }

        private static bool IsWolfeinRace(ThingDef race)
        {
            if (race == null)
            {
                return false;
            }

            string raceName = race.defName ?? string.Empty;
            string raceLabel = race.label ?? string.Empty;
            return raceName.IndexOf("Wolfein", System.StringComparison.OrdinalIgnoreCase) >= 0
                || raceLabel.IndexOf("Wolfein", System.StringComparison.OrdinalIgnoreCase) >= 0
                || raceName.IndexOf("Wolfin", System.StringComparison.OrdinalIgnoreCase) >= 0
                || raceLabel.IndexOf("Wolfin", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static XenotypeDef ResolveOrangeWolfeinXenotype()
        {
            if (!ModsConfig.BiotechActive)
            {
                return null;
            }

            XenotypeDef orangeWolfein = null;
            XenotypeDef anyWolfein = null;
            List<XenotypeDef> all = DefDatabase<XenotypeDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                XenotypeDef xeno = all[i];
                string name = xeno.defName ?? string.Empty;
                string label = xeno.label ?? string.Empty;
                bool wolfein = name.IndexOf("Wolfein", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || label.IndexOf("Wolfein", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Wolfin", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (!wolfein)
                {
                    continue;
                }

                anyWolfein ??= xeno;
                bool orange = name.IndexOf("Orange", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || label.IndexOf("Orange", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (orange)
                {
                    orangeWolfein = xeno;
                    break;
                }
            }

            return orangeWolfein ?? anyWolfein;
        }

        private static Building FindAnyStatue(Map map)
        {
            for (int i = 0; i < StatueDefNames.Length; i++)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(StatueDefNames[i]);
                if (def == null)
                {
                    continue;
                }
                List<Building> buildings = map.listerBuildings.AllBuildingsColonistOfDef(def);
                if (buildings.Count > 0)
                {
                    return buildings.RandomElement();
                }
            }
            return null;
        }
    }

    // ~+10°C map temperature offset with a light warm-orange sky tint.
    public class GameCondition_KatsHeatDome : GameCondition
    {
        private const float TempOffsetCelsius = 10f;

        private static readonly SkyColorSet HeatDomeSkyColors = new SkyColorSet(
            new Color(1.00f, 0.72f, 0.48f),
            new Color(1.00f, 0.88f, 0.70f),
            new Color(0.90f, 0.58f, 0.38f),
            1.0f);

        public override float TemperatureOffset()
        {
            return TempOffsetCelsius;
        }

        public override float SkyTargetLerpFactor(Map map)
        {
            return GameConditionUtility.LerpInOutValue(this, 2000f, 0.35f);
        }

        public override SkyTarget? SkyTarget(Map map)
        {
            return new SkyTarget(0.9f, HeatDomeSkyColors, 1f, 1f);
        }
    }
}
