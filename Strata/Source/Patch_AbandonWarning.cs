using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Vanilla's settlement-abandon confirmation only looks at pawns on the
    // surface map. Levels below are pocket maps, and abandoning destroys the
    // whole column (destroyOnParentMapAbandoned), so anyone still underground
    // would be lost without a word of warning. Intercept the abandon flow: if
    // any player pawn or prisoner is on a descendant level, demand an explicit
    // confirmation first, then hand back to the vanilla flow.
    [HarmonyPatch(typeof(SettlementAbandonUtility), nameof(SettlementAbandonUtility.TryAbandonViaInterface))]
    public static class Patch_AbandonWarning
    {
        // Set while re-entering the vanilla flow from our dialog's confirm
        // button, so the prefix waves the second call through.
        private static bool warned;

        public static bool Prefix(MapParent settlement)
        {
            if (warned || settlement?.Map == null)
            {
                return true;
            }
            List<Pawn> below = PawnsBelow(settlement.Map);
            if (below.Count == 0)
            {
                return true;
            }

            var text = new StringBuilder();
            text.AppendLine("Abandoning this settlement will collapse every level beneath it, and these pawns are still underground:");
            text.AppendLine();
            foreach (Pawn pawn in below)
            {
                text.AppendLine("    " + pawn.LabelShortCap);
            }
            text.AppendLine();
            text.Append("They will be lost forever. Abandon anyway?");

            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                text.ToString(),
                delegate
                {
                    warned = true;
                    try
                    {
                        SettlementAbandonUtility.TryAbandonViaInterface(settlement);
                    }
                    finally
                    {
                        warned = false;
                    }
                },
                destructive: true));
            return false;
        }

        // Player pawns and colony prisoners on every level under 'root',
        // walking the full chain of stacked pocket maps.
        public static List<Pawn> PawnsBelow(Map root)
        {
            var result = new List<Pawn>();
            Collect(root, result, 0);
            return result;
        }

        private static void Collect(Map parent, List<Pawn> result, int depth)
        {
            if (depth > 32)
            {
                return;
            }
            foreach (Map map in Find.Maps)
            {
                if (!(map.Parent is PocketMapParent pocket) || pocket.sourceMap != parent)
                {
                    continue;
                }
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (pawn.Faction == Faction.OfPlayer || pawn.IsPrisonerOfColony)
                    {
                        result.Add(pawn);
                    }
                }
                Collect(map, result, depth + 1);
            }
        }
    }
}
