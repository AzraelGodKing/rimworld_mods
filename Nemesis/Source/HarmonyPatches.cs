using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Nemesis
{
    /// <summary>
    /// AZR-385 — one shared lookup for the private-signature targets several patches depend on,
    /// so a RimWorld update that changes them yields one Nemesis-specific diagnostic instead of
    /// five silently skipped classes behind SafePatchAll's generic failure letter.
    /// </summary>
    internal static class NemesisPatchTargets
    {
        private static bool _killResolved;
        private static MethodInfo _pawnKill;
        private static bool _guestPawnResolved;
        private static bool _guestPawnFieldExists;

        internal static MethodInfo PawnKill
        {
            get
            {
                if (_killResolved) return _pawnKill;
                _killResolved = true;
                _pawnKill = AccessTools.Method(typeof(Pawn), "Kill", new[] { typeof(DamageInfo?), typeof(Hediff) });
                if (_pawnKill == null)
                {
                    Log.Error("[Nemesis] Pawn.Kill(DamageInfo?, Hediff) not found (RimWorld update?). "
                        + "Disabled: nemesis cheat-death/escape intercept, killed-ally / fixation / wounded-escape triggers, "
                        + "and instant kill end-checks. Prison-break and slave triggers still run; hunt end falls back to the periodic check.");
                }
                return _pawnKill;
            }
        }

        internal static bool GuestTrackerHasPawnField
        {
            get
            {
                if (_guestPawnResolved) return _guestPawnFieldExists;
                _guestPawnResolved = true;
                _guestPawnFieldExists = AccessTools.Field(typeof(Pawn_GuestTracker), "pawn") != null;
                if (!_guestPawnFieldExists)
                {
                    Log.Warning("[Nemesis] Pawn_GuestTracker.pawn field not found (RimWorld update?). "
                        + "Capture is still detected by the periodic resolution check, just less promptly.");
                }
                return _guestPawnFieldExists;
            }
        }
    }

    // --- Nemesis cannot die until the hunt is over (Dredd foundation) ---

    [HarmonyPatch]
    public static class Patch_Pawn_Kill_Nemesis
    {
        static bool Prepare() => NemesisPatchTargets.PawnKill != null;

        [HarmonyTargetMethod]
        static MethodBase TargetMethod() => NemesisPatchTargets.PawnKill;

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        static bool Prefix(Pawn __instance)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || !comp.IsNemesisPawn(__instance)) return true;
            comp.HandleLethalDamage(__instance);
            return false;
        }
    }

    // --- Triggers ---

    [HarmonyPatch]
    public static class Patch_Pawn_Kill_TriggerNemesis
    {
        static bool Prepare() => NemesisPatchTargets.PawnKill != null;

        [HarmonyTargetMethod]
        static MethodBase TargetMethod() => NemesisPatchTargets.PawnKill;

        [HarmonyPostfix]
        static void Postfix(Pawn __instance, DamageInfo? dinfo)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || comp.IsEngaged) return;
            if (__instance.Faction == null || __instance.Faction.IsPlayer || __instance.Faction.def.hidden) return;
            if (!__instance.RaceProps.Humanlike) return;

            if (NemesisTriggers.IsColonyInternedOrExecution(__instance, dinfo)) return;

            Pawn attacker = dinfo?.Instigator as Pawn;
            if (attacker == null || !attacker.IsColonist) return;

            if (!Rand.Chance(NemesisMod.Settings?.killedAllyChance ?? 0.15f)) return;

            comp.CreateNemesis(__instance, NemesisTargetMode.Pawn, NemesisTrigger.KilledAlly, attacker);
        }
    }

    [HarmonyPatch]
    public static class Patch_Pawn_Kill_FixationTrigger
    {
        static bool Prepare() => NemesisPatchTargets.PawnKill != null;

        [HarmonyTargetMethod]
        static MethodBase TargetMethod() => NemesisPatchTargets.PawnKill;

        [HarmonyPostfix]
        static void Postfix(Pawn __instance, DamageInfo? dinfo)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || comp.IsEngaged) return;
            if (!__instance.IsColonist) return;

            Pawn killer = dinfo?.Instigator as Pawn;
            if (killer?.Faction == null || killer.Faction.IsPlayer || killer.Faction.def.hidden) return;
            if (!killer.RaceProps.Humanlike) return;

            if (!Rand.Chance(NemesisMod.Settings?.fixationChance ?? 0.10f)) return;

            Map map = __instance.MapHeld ?? killer.MapHeld ?? killer.Map;
            if (map?.mapPawns?.FreeColonistsSpawned == null) return;

            List<Pawn> candidates = new List<Pawn>();
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn p = colonists[i];
                if (p != null && p != __instance && !p.Dead)
                    candidates.Add(p);
            }
            if (candidates.Count == 0) return;

            comp.CreateNemesis(killer, NemesisTargetMode.Pawn, NemesisTrigger.Fixation,
                candidates[Rand.Range(0, candidates.Count)], useAsNemesis: killer);
        }
    }

    /// <summary>
    /// Wounded-and-escaped: a lethal blow on a hostile humanlike sometimes becomes a cinematic escape
    /// and starts a personal hunt against the colonist who struck them.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_Pawn_Kill_WoundedEscape
    {
        static bool Prepare() => NemesisPatchTargets.PawnKill != null;

        [HarmonyTargetMethod]
        static MethodBase TargetMethod() => NemesisPatchTargets.PawnKill;

        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        static bool Prefix(Pawn __instance, DamageInfo? dinfo)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || comp.IsEngaged) return true;
            if (__instance.Faction == null || __instance.Faction.IsPlayer || __instance.Faction.def.hidden) return true;
            if (!__instance.RaceProps.Humanlike) return true;
            if (comp.IsNemesisPawn(__instance)) return true;
            // Executions, prisoners, and slaves are already a death — don't park them
            // as a world-map hunt (Steam: executing a prisoner started a nemesis).
            if (NemesisTriggers.IsColonyInternedOrExecution(__instance, dinfo)) return true;
            if (!__instance.HostileTo(Faction.OfPlayer)) return true;

            Pawn attacker = dinfo?.Instigator as Pawn;
            if (attacker == null || !attacker.IsColonist) return true;

            if (!Rand.Chance(NemesisMod.Settings?.woundedEscapeChance ?? 0.12f)) return true;

            // Do not AddHediff(Anesthetic) here: a ShouldBeDead pawn can re-enter
            // Kill, leave a corpse, and CreateNemesis then generates a lookalike.
            comp.CreateNemesis(__instance, NemesisTargetMode.Pawn, NemesisTrigger.WoundedAndEscaped,
                attacker, useAsNemesis: __instance);
            // Foreign-antagonist / failed create must not cancel vanilla death.
            return !comp.IsEngaged;
        }
    }

    [HarmonyPatch]
    public static class Patch_PrisonBreak_TriggerNemesis
    {
        [HarmonyTargetMethod]
        static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(PrisonBreakUtility), nameof(PrisonBreakUtility.StartPrisonBreak), new[]
            {
                typeof(Pawn),
                typeof(string).MakeByRefType(),
                typeof(string).MakeByRefType(),
                typeof(LetterDef).MakeByRefType(),
                typeof(List<Pawn>).MakeByRefType(),
            });

        [HarmonyPostfix]
        static void Postfix(Pawn initiator, List<Pawn> escapingPrisoners)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || comp.IsEngaged) return;
            if (escapingPrisoners == null || escapingPrisoners.Count == 0) return;
            if (initiator?.Faction == null || initiator.Faction.IsPlayer || initiator.Faction.def.hidden) return;

            if (!Rand.Chance(NemesisMod.Settings?.prisonerEscapedChance ?? 0.10f)) return;

            comp.CreateNemesis(initiator, NemesisTargetMode.Colony, NemesisTrigger.PrisonerEscaped);
        }
    }

    /// <summary>Ideology slave rebellion — patched only when the method exists.</summary>
    [HarmonyPatch]
    public static class Patch_SlaveRebellion_TriggerNemesis
    {
        static bool Prepare()
        {
            return AccessTools.Method(
                typeof(SlaveRebellionUtility),
                "StartSlaveRebellion",
                new[] { typeof(Pawn), typeof(bool) }) != null;
        }

        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(SlaveRebellionUtility),
                "StartSlaveRebellion",
                new[] { typeof(Pawn), typeof(bool) });
        }

        [HarmonyPostfix]
        static void Postfix(Pawn initiator)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || comp.IsEngaged) return;
            if (initiator?.Faction == null || initiator.Faction.IsPlayer || initiator.Faction.def.hidden) return;
            if (!Rand.Chance(NemesisMod.Settings?.slaveEscapedChance ?? 0.12f)) return;

            comp.CreateNemesis(initiator, NemesisTargetMode.Colony, NemesisTrigger.SlaveEscaped);
        }
    }

    // --- End-condition dirty flags (cheap; real check is staggered in GameComponent) ---

    [HarmonyPatch]
    public static class Patch_Pawn_Kill_EndConditionDirty
    {
        static bool Prepare() => NemesisPatchTargets.PawnKill != null;

        [HarmonyTargetMethod]
        static MethodBase TargetMethod() => NemesisPatchTargets.PawnKill;

        [HarmonyPostfix]
        static void Postfix(Pawn __instance)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || !comp.IsEngaged) return;
            if (comp.IsTargetPawn(__instance) || comp.IsNemesisPawn(__instance))
                NemesisRegistry.ResolutionDirty = true;
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction))]
    public static class Patch_Pawn_SetFaction_HandOver
    {
        [HarmonyPostfix]
        static void Postfix(Pawn __instance, Faction newFaction)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || !comp.IsEngaged) return;
            if (!comp.IsTargetPawn(__instance)) return;
            if (newFaction == Faction.OfPlayer) return;
            NemesisRegistry.ResolutionDirty = true;
        }
    }

    [HarmonyPatch(typeof(Pawn_GuestTracker), nameof(Pawn_GuestTracker.SetGuestStatus))]
    public static class Patch_GuestStatus_ResolutionDirty
    {
        static bool Prepare() => NemesisPatchTargets.GuestTrackerHasPawnField;

        [HarmonyPostfix]
        static void Postfix(Pawn ___pawn)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || !comp.IsEngaged) return;
            if (___pawn != null && comp.IsNemesisPawn(___pawn))
                NemesisRegistry.ResolutionDirty = true;
        }
    }

    /// <summary>Boost social-fight chance between nemesis and fixation target.</summary>
    [HarmonyPatch]
    public static class Patch_SocialFightChance
    {
        static bool Prepare() =>
            AccessTools.Method(typeof(Pawn_InteractionsTracker), "SocialFightChance",
                new[] { typeof(InteractionDef), typeof(Pawn) }) != null;

        [HarmonyTargetMethod]
        static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(Pawn_InteractionsTracker), "SocialFightChance",
                new[] { typeof(InteractionDef), typeof(Pawn) });

        // RimWorld 1.6 renamed the second arg from otherPawn → initiator.
        [HarmonyPostfix]
        static void Postfix(Pawn ___pawn, Pawn initiator, ref float __result)
        {
            if (__result <= 0f || ___pawn == null || initiator == null) return;
            __result *= NemesisSocial.SocialFightMultiplier(___pawn, initiator);
        }
    }

    [HarmonyPatch(typeof(Building_CommsConsole), nameof(Building_CommsConsole.GetFloatMenuOptions))]
    public static class Patch_Comms_Informants
    {
        static void Postfix(Building_CommsConsole __instance, Pawn myPawn, ref IEnumerable<FloatMenuOption> __result)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp?.Data == null || !comp.Data.active)
                return;

            var extras = new List<FloatMenuOption>();

            // AZR-301 — reply options (taunt / truce / demand surrender).
            extras.Add(new FloatMenuOption(
                "Nemesis_Comms_TauntBack".Translate(comp.Data.nemesisName),
                () => NemesisPlayerCommand.EnqueueReplyTaunt()));
            extras.Add(new FloatMenuOption(
                "Nemesis_Comms_OfferTruce".Translate(comp.Data.nemesisName),
                () => NemesisPlayerCommand.EnqueueReplyTruce()));
            extras.Add(new FloatMenuOption(
                "Nemesis_Comms_DemandSurrender".Translate(comp.Data.nemesisName),
                () => NemesisPlayerCommand.EnqueueReplySurrender()));

            // AZR-300 — informant purchases go through the tick-drain queue.
            if (NemesisMod.Settings?.enableInformants ?? true)
            {
                int cost = NemesisInformants.LeadCost(comp.Data);
                Map map = __instance.Map;
                extras.Add(new FloatMenuOption(
                    "Nemesis_Comms_BuyLead".Translate(cost),
                    () => NemesisPlayerCommand.EnqueueBuyLead(map)));
                extras.Add(new FloatMenuOption(
                    "Nemesis_Comms_PostBounty".Translate(cost),
                    () => NemesisPlayerCommand.EnqueuePostBounty(map, cost)));
            }

            __result = __result == null ? extras : __result.Concat(extras);
        }
    }
}
