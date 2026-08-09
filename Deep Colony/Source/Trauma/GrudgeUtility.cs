using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>B17 — colonists remember which faction caused their trauma.</summary>
    public static class GrudgeUtility
    {
        public static void RememberFaction(Pawn pawn, Faction faction)
        {
            if (pawn == null || faction == null || faction.IsPlayer) return;
            if (!DeepColonySettings.Get.enableTrauma) return;
            if (!DeepColonySettings.Get.enableFactionRep) return;

            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;

            if (comp.grudgeFactionIds == null)
                comp.grudgeFactionIds = new List<int>();
            if (!comp.grudgeFactionIds.Contains(faction.loadID))
                comp.grudgeFactionIds.Add(faction.loadID);

            // Small lasting goodwill dent.
            GameComp_DeepColony.Instance?.AddFactionDrift(faction, -0.75f, FactionRepReason.Grudge);
        }

        public static bool HasGrudge(Pawn pawn, Faction faction)
        {
            if (pawn == null || faction == null) return false;
            var comp = pawn.TryGetComp<Comp_DeepColony>();
            return comp?.grudgeFactionIds != null && comp.grudgeFactionIds.Contains(faction.loadID);
        }

        public static void OnRaidFromFaction(Faction faction)
        {
            if (faction == null || !DeepColonySettings.Get.enableTrauma) return;

            foreach (Map map in Find.Maps)
            {
                foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
                {
                    if (!HasGrudge(p, faction)) continue;
                    FlashbackUtility.TryForceFlashback(p, "DC_FlashbackGrudge".Translate(faction.Name));
                }
            }
        }

        public static string InspectString(Pawn pawn)
        {
            var comp = pawn?.TryGetComp<Comp_DeepColony>();
            if (comp?.grudgeFactionIds == null || comp.grudgeFactionIds.Count == 0) return null;

            var names = new List<string>();
            foreach (int id in comp.grudgeFactionIds)
            {
                Faction f = Find.FactionManager?.AllFactionsListForReading?.Find(x => x.loadID == id);
                if (f != null) names.Add(f.Name);
            }
            if (names.Count == 0) return null;
            return "DC_InspectGrudge".Translate(string.Join(", ", names));
        }
    }
}
