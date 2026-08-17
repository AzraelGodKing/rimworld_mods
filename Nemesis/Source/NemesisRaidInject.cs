using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Nemesis
{
    /// <summary>
    /// When a DirectRaid fires after the nemesis has fled, inject that same pawn
    /// into the raid group (BFV-style: return with an army, not alone).
    /// </summary>
    public static class NemesisRaidInject
    {
        public static Pawn Pending;

        public static void Arm(Pawn nemesis)
        {
            Pending = nemesis;
        }

        public static void Clear()
        {
            Pending = null;
        }
    }

    [HarmonyPatch(typeof(PawnGroupMakerUtility), nameof(PawnGroupMakerUtility.GeneratePawns))]
    public static class Patch_GeneratePawns_InjectNemesis
    {
        public static IEnumerable<Pawn> Postfix(IEnumerable<Pawn> __result, PawnGroupMakerParms parms)
        {
            bool yielded = false;
            foreach (Pawn p in __result)
            {
                yielded = true;
                yield return p;
            }

            Pawn inject = NemesisRaidInject.Pending;
            if (!yielded || inject == null || parms == null) yield break;
            if (parms.groupKind != PawnGroupKindDefOf.Combat) yield break;
            if (parms.faction == null) yield break;
            if (inject.Faction != parms.faction)
                NemesisPawnUtil.EnsureHuntFaction(inject, parms.faction);
            if (inject.Faction != parms.faction) yield break;
            if (inject.Destroyed || inject.Dead || inject.IsPrisonerOfColony) yield break;
            // Soft-compat: don't double-inject if BFV/Rimesis already owns them.
            if (SoftCompat.IsForeignAntagonistPawn(inject))
            {
                NemesisRaidInject.Clear();
                yield break;
            }

            NemesisRaidInject.Clear();
            NemesisPawnUtil.DetachFromLord(inject);
            NemesisPawnUtil.EnsureNotWorldPawn(inject);
            NemesisPawnUtil.RecoverForReturn(inject);
            NemesisData data = GameComponent_Nemesis.Instance?.Data;
            if (data != null)
                NemesisProgression.Apply(inject, data);
            if (inject.Spawned)
                inject.DeSpawn(DestroyMode.Vanish);

            yield return inject;
        }
    }
}
