using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// Fail-open Nemesis bridge. When the named nemesis kills a colonist, the people who
    /// cared about the victim gain a "hunted" trauma that names the nemesis, and a grudge
    /// against the nemesis's faction. Uses Nemesis.NemesisCompatApi.IsNemesisPawn by reflection.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class NemesisSoftCompat
    {
        private const int MinOpinion = 20;

        private static readonly Func<Pawn, bool> isNemesisPawn;

        static NemesisSoftCompat()
        {
            if (!SoftCompat.NemesisLoaded) return;
            try
            {
                var m = AccessTools.Method("Nemesis.NemesisCompatApi:IsNemesisPawn", new[] { typeof(Pawn) });
                if (m == null || m.ReturnType != typeof(bool)) return;
                isNemesisPawn = (Func<Pawn, bool>)Delegate.CreateDelegate(typeof(Func<Pawn, bool>), m);
                Log.Message("[DeepColony] Nemesis hook ready (fail-open).");
            }
            catch (Exception e)
            {
                Log.Warning("[DeepColony] Nemesis hook failed: " + e.Message);
            }
        }

        public static bool IsNemesis(Pawn pawn)
        {
            if (isNemesisPawn == null || pawn == null) return false;
            try
            {
                return isNemesisPawn(pawn);
            }
            catch (Exception e)
            {
                Log.WarningOnce("[DeepColony] Nemesis IsNemesisPawn threw: " + e.Message, 0x5DC0DE30);
                return false;
            }
        }

        /// <summary>Called from the Pawn.Kill postfix for colonist victims.</summary>
        public static void NotifyColonistKilled(Pawn victim, DamageInfo? dinfo)
        {
            if (!DeepColonySettings.Get.enableTrauma) return;
            if (!(dinfo?.Instigator is Pawn killer) || !IsNemesis(killer)) return;
            TraumaDef hunted = DC_DefOf.DC_Trauma_Hunted;
            Map map = victim.MapHeld;
            if (hunted == null || map == null) return;

            foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
            {
                if (colonist.Dead || colonist == victim || colonist.relations == null) continue;
                if (colonist.relations.OpinionOf(victim) < MinOpinion
                    && !colonist.GetRelations(victim).Any())
                    continue;
                TraumaUtility.ApplyTrauma(colonist, hunted, killer, killer.Faction);
            }
        }
    }
}
