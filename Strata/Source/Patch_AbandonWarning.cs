using System.Collections.Generic;
using System.Reflection;
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
    // would be lost without a word of warning.
    //
    // Two styles, chosen in mod options:
    //  - merged (default): one combined dialog listing surface AND underground
    //    pawns, then straight to vanilla's colony check (skipping vanilla's
    //    own surface-pawn dialog, which the merged one supersedes).
    //  - chained: Strata's underground warning first, then the untouched
    //    vanilla flow with its own surface warning.
    // The merged path re-enters vanilla through the private
    // TryAbandonWithColonyCheck; if a game update renames it, the patch
    // falls back to the chained style rather than breaking.
    [HarmonyPatch(typeof(SettlementAbandonUtility), nameof(SettlementAbandonUtility.TryAbandonViaInterface))]
    public static class Patch_AbandonWarning
    {
        private static readonly MethodInfo ColonyCheck =
            AccessTools.Method(typeof(SettlementAbandonUtility), "TryAbandonWithColonyCheck");

        // Set while re-entering the vanilla flow from the chained dialog's
        // confirm button, so the prefix waves the second call through.
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
            bool merged = (StrataMod.Settings?.mergedAbandonWarning ?? true) && ColonyCheck != null;
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                merged ? MergedText(settlement.Map, below) : ChainedText(below),
                delegate
                {
                    if (merged)
                    {
                        // Vanilla's surface-pawn dialog is superseded by the
                        // merged listing; go straight to the last-colony check.
                        ColonyCheck.Invoke(null, new object[] { settlement });
                        return;
                    }
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

        private static string ChainedText(List<Pawn> below)
        {
            var text = new StringBuilder();
            text.AppendLine("Abandoning this settlement will collapse every linked level (above and below), and these pawns are still on those floors:");
            text.AppendLine();
            AppendPawns(text, below);
            text.AppendLine();
            text.Append("They will be lost forever. Abandon anyway?");
            return text.ToString();
        }

        private static string MergedText(Map surface, List<Pawn> below)
        {
            var text = new StringBuilder();
            text.AppendLine("Abandoning this settlement will collapse every linked level (above and below).");
            List<Pawn> onSurface = LeftBehindOn(surface);
            if (onSurface.Count > 0)
            {
                text.AppendLine();
                text.AppendLine("Left behind on the surface:");
                AppendPawns(text, onSurface);
            }
            text.AppendLine();
            text.AppendLine("Still on linked levels:");
            AppendPawns(text, below);
            text.AppendLine();
            text.Append("They will all be lost forever. Abandon anyway?");
            return text.ToString();
        }

        private static void AppendPawns(StringBuilder text, List<Pawn> pawns)
        {
            foreach (Pawn pawn in pawns)
            {
                text.AppendLine("    " + pawn.LabelShortCap);
            }
        }

        // Player pawns and colony prisoners on every level under 'root',
        // walking the full chain of stacked pocket maps.
        public static List<Pawn> PawnsBelow(Map root)
        {
            var result = new List<Pawn>();
            Collect(root, result, 0);
            return result;
        }

        private static List<Pawn> LeftBehindOn(Map map)
        {
            var result = new List<Pawn>();
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.Faction == Faction.OfPlayer || pawn.IsPrisonerOfColony)
                {
                    result.Add(pawn);
                }
            }
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
