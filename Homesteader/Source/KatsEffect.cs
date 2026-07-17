using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Homesteader
{
    // Rare Misc incident keyed to finished 27 statues/monuments. Hybrid letter
    // (RimWorld anomalous broadcast + short Foundation addendum), heat dome,
    // short brain-rot hediff, silver Super Chat drain, and a personal Kats mood.
    public class IncidentWorker_KatsEffect : IncidentWorker
    {
        private static readonly string[] StatueDefNames =
        {
            "Homesteader_Monument27_Golden",
            "Homesteader_Monument27_Harvest",
            "Homesteader_StatueTwentySeven",
            "Homesteader_Statue27Grand",
        };

        private const int SuperChatMin = 27;
        private const int SuperChatMax = 81;

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

            int silverTaken = DrainSuperChatSilver(map);

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

            string letterText = BuildLetterText(silverTaken);
            Find.LetterStack.ReceiveLetter(
                def.letterLabel,
                letterText,
                def.letterDef ?? LetterDefOf.NeutralEvent,
                new TargetInfo(statue),
                parms.faction,
                parms.quest,
                parms.letterHyperlinkThingDefs);
            return true;
        }

        private static string BuildLetterText(int silverTaken)
        {
            var sb = new StringBuilder();
            sb.Append("An anomalous broadcast has locked onto your colony's 27 monument frequency. ");
            sb.Append("A brief heat dome is forming over the region, and several colonists report a short-lived fogginess — \"brain rot,\" they call it.");
            sb.AppendLine();
            sb.AppendLine();
            if (silverTaken > 0)
            {
                sb.Append("Colony accounts show ").Append(silverTaken).Append(" silver diverted as a \"Super Chat.\"");
            }
            else
            {
                sb.Append("The broadcast asked for a Super Chat, but the colony had no silver to spare.");
            }
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine("Foundation addendum // Item # SCP-27272727 // Keter (Adorable)");
            sb.Append("Containment note: hydrate / medicate / masticate. Do not look away from the number.");
            return sb.ToString();
        }

        private static int DrainSuperChatSilver(Map map)
        {
            int want = Rand.RangeInclusive(SuperChatMin, SuperChatMax);
            int available = map.resourceCounter.GetCount(ThingDefOf.Silver);
            int take = Mathf.Min(want, available);
            if (take > 0)
            {
                TradeUtility.LaunchSilver(map, take);
            }
            return take;
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
