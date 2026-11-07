#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Nemesis
{
    /// <summary>
    /// One shared lookup for the private-signature targets several patches depend on,
    /// so a RimWorld update that changes them yields one Nemesis-specific diagnostic instead of
    /// five silently skipped classes behind SafePatchAll's generic failure letter.
    /// </summary>
    internal static class NemesisPatchTargets
    {
        private static bool _killResolved;
        private static MethodInfo? _pawnKill;
        private static bool _guestPawnResolved;
        private static bool _guestPawnFieldExists;

        internal static MethodInfo? PawnKill
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

    // --- Pawn.Kill: one patch class for every Nemesis hook (AZR-535) ---

    /// <summary>
    /// Every Nemesis hook on Pawn.Kill, in one prefix and one postfix, so each death costs one
    /// component lookup instead of five patch calls. Arguments bind by position (__0 = dinfo),
    /// so a 1.7 parameter rename cannot break the patch.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_Pawn_Kill
    {
        static bool Prepare() => NemesisPatchTargets.PawnKill != null;

        [HarmonyTargetMethod]
        static MethodBase TargetMethod() => NemesisPatchTargets.PawnKill!;

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        static bool Prefix(Pawn __instance, DamageInfo? __0)
        {
            GameComponent_Nemesis? comp = GameComponent_Nemesis.Instance;
            if (comp == null) return true;
            // A nemesis cannot die until the hunt is over (Dredd foundation).
            if (comp.IsNemesisPawn(__instance))
                return !comp.CancelLethalHit(__instance);
            return !TryWoundedEscape(comp, __instance, __0);
        }

        [HarmonyPostfix]
        static void Postfix(Pawn __instance, DamageInfo? __0)
        {
            GameComponent_Nemesis? comp = GameComponent_Nemesis.Instance;
            if (comp == null) return;
            // End-condition dirty flag (cheap; the real check is staggered in the component).
            if (comp.IsEngaged && (comp.IsTargetPawn(__instance) || comp.IsNemesisPawn(__instance)))
                NemesisRegistry.ResolutionDirty = true;
            if (__instance.IsColonist)
                TryFixation(comp, __instance, __0);
            else
                TryKilledAlly(comp, __instance, __0);
        }

        /// <summary>
        /// Wounded-and-escaped: a lethal blow on a hostile humanlike sometimes becomes a cinematic
        /// escape and starts a personal hunt against the colonist who struck them. True cancels the death.
        /// </summary>
        private static bool TryWoundedEscape(GameComponent_Nemesis comp, Pawn pawn, DamageInfo? dinfo)
        {
            if (pawn.Faction == null || pawn.Faction.IsPlayer || pawn.Faction.def.hidden) return false;
            if (!pawn.RaceProps.Humanlike) return false;
            if (comp.EngagedHuntForNemesis(pawn) != null) return false;
            if (!comp.CanStartHunt(pawn.Faction)) return false;
            // Executions, prisoners, and slaves are already a death: don't park them
            // as a world-map hunt (Steam: executing a prisoner started a nemesis).
            if (NemesisTriggers.IsColonyInternedOrExecution(pawn, dinfo)) return false;
            if (!pawn.HostileTo(Faction.OfPlayer)) return false;

            if (!(dinfo?.Instigator is Pawn attacker) || !attacker.IsColonist) return false;
            if (!Rand.Chance(NemesisMod.Settings?.woundedEscapeChance ?? 0.12f)) return false;

            // Do not AddHediff(Anesthetic) here: a ShouldBeDead pawn can re-enter
            // Kill, leave a corpse, and CreateNemesis then generates a lookalike.
            comp.CreateNemesis(pawn, NemesisTargetMode.Pawn, NemesisTrigger.WoundedAndEscaped,
                attacker, useAsNemesis: pawn);
            // Foreign-antagonist / failed create must not cancel vanilla death.
            return comp.HuntForNemesis(pawn) != null;
        }

        /// <summary>A colonist killed a hostile humanlike: its faction may send a nemesis after them.</summary>
        private static void TryKilledAlly(GameComponent_Nemesis comp, Pawn dead, DamageInfo? dinfo)
        {
            if (dead.Faction == null || dead.Faction.IsPlayer || dead.Faction.def.hidden) return;
            if (!dead.RaceProps.Humanlike) return;
            if (!comp.CanStartHunt(dead.Faction)) return;
            if (NemesisTriggers.IsColonyInternedOrExecution(dead, dinfo)) return;
            if (!(dinfo?.Instigator is Pawn attacker) || !attacker.IsColonist) return;
            if (!Rand.Chance(NemesisMod.Settings?.killedAllyChance ?? 0.15f)) return;

            comp.CreateNemesis(dead, NemesisTargetMode.Pawn, NemesisTrigger.KilledAlly, attacker);
        }

        /// <summary>A hostile killed a colonist: the killer may fixate on one of the survivors.</summary>
        private static void TryFixation(GameComponent_Nemesis comp, Pawn dead, DamageInfo? dinfo)
        {
            if (!(dinfo?.Instigator is Pawn killer)) return;
            if (killer.Faction == null || killer.Faction.IsPlayer || killer.Faction.def.hidden) return;
            if (!killer.RaceProps.Humanlike) return;
            if (!comp.CanStartHunt(killer.Faction)) return;
            if (!Rand.Chance(NemesisMod.Settings?.fixationChance ?? 0.10f)) return;

            Map? map = dead.MapHeld ?? killer.MapHeld ?? killer.Map;
            List<Pawn>? colonists = map?.mapPawns?.FreeColonistsSpawned;
            if (colonists == null) return;

            List<Pawn> candidates = new List<Pawn>();
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn p = colonists[i];
                if (p != null && p != dead && !p.Dead)
                    candidates.Add(p);
            }
            if (candidates.Count == 0) return;

            comp.CreateNemesis(killer, NemesisTargetMode.Pawn, NemesisTrigger.Fixation,
                candidates.RandomElement(), useAsNemesis: killer);
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

        // __0 = initiator, __4 = out escapingPrisoners.
        [HarmonyPostfix]
        static void Postfix(Pawn __0, List<Pawn> __4)
        {
            Pawn initiator = __0;
            GameComponent_Nemesis? comp = GameComponent_Nemesis.Instance;
            if (comp == null) return;
            if (__4 == null || __4.Count == 0) return;
            if (initiator?.Faction == null || initiator.Faction.IsPlayer || initiator.Faction.def.hidden) return;
            if (!comp.CanStartHunt(initiator.Faction)) return;

            if (!Rand.Chance(NemesisMod.Settings?.prisonerEscapedChance ?? 0.10f)) return;

            comp.CreateNemesis(initiator, NemesisTargetMode.Colony, NemesisTrigger.PrisonerEscaped);
        }
    }

    /// <summary>
    /// Ideology slave rebellion. Patches the overload that builds the letter, which the
    /// two-argument one calls, and only counts a rebellion that started.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_SlaveRebellion_TriggerNemesis
    {
        static MethodBase Target() => AccessTools.Method(
            typeof(SlaveRebellionUtility),
            "StartSlaveRebellion",
            new[]
            {
                typeof(Pawn),
                typeof(string).MakeByRefType(),
                typeof(string).MakeByRefType(),
                typeof(LetterDef).MakeByRefType(),
                typeof(LookTargets).MakeByRefType(),
                typeof(bool),
            });

        static bool Prepare() => Target() != null;

        [HarmonyTargetMethod]
        static MethodBase TargetMethod() => Target();

        [HarmonyPostfix]
        static void Postfix(Pawn __0, bool __result)
        {
            Pawn initiator = __0;
            if (!__result) return;
            GameComponent_Nemesis? comp = GameComponent_Nemesis.Instance;
            if (comp == null) return;
            if (initiator?.Faction == null || initiator.Faction.IsPlayer || initiator.Faction.def.hidden) return;
            if (!comp.CanStartHunt(initiator.Faction)) return;
            if (!Rand.Chance(NemesisMod.Settings?.slaveEscapedChance ?? 0.12f)) return;

            comp.CreateNemesis(initiator, NemesisTargetMode.Colony, NemesisTrigger.SlaveEscaped);
        }
    }

    // --- End-condition dirty flags (cheap; real check is staggered in GameComponent) ---

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction))]
    public static class Patch_Pawn_SetFaction_HandOver
    {
        [HarmonyPostfix]
        static void Postfix(Pawn __instance, Faction __0)
        {
            Faction newFaction = __0;
            GameComponent_Nemesis? comp = GameComponent_Nemesis.Instance;
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
            GameComponent_Nemesis? comp = GameComponent_Nemesis.Instance;
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
                new[] { typeof(InteractionDef), typeof(Pawn) }) != null
            && AccessTools.Field(typeof(Pawn_InteractionsTracker), "pawn") != null;

        [HarmonyTargetMethod]
        static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(Pawn_InteractionsTracker), "SocialFightChance",
                new[] { typeof(InteractionDef), typeof(Pawn) });

        // __1 = the other pawn; 1.6 renamed it from otherPawn to initiator, so bind by position.
        [HarmonyPostfix]
        static void Postfix(Pawn ___pawn, Pawn __1, ref float __result)
        {
            Pawn initiator = __1;
            if (__result <= 0f || ___pawn == null || initiator == null) return;
            __result *= NemesisSocial.SocialFightMultiplier(___pawn, initiator);
        }
    }

    /// <summary>
    /// Nemesis replies and informant purchases on a colony comms console. Shown only where
    /// vanilla's own options are usable (power, path, talking); picking one sends the pawn to
    /// the console first (<see cref="JobDriver_NemesisComms"/>), like a vanilla call.
    /// </summary>
    [HarmonyPatch(typeof(Building_CommsConsole), nameof(Building_CommsConsole.GetFloatMenuOptions))]
    public static class Patch_Comms_Informants
    {
        static void Postfix(Building_CommsConsole __instance, Pawn __0, ref IEnumerable<FloatMenuOption> __result)
        {
            Pawn myPawn = __0;
            if (__instance == null || myPawn == null || !__instance.CanUseCommsNow)
                return;
            if (!myPawn.CanReach(__instance, PathEndMode.InteractionCell, Danger.Some))
                return;
            if (!myPawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking))
                return;

            GameComponent_Nemesis? comp = GameComponent_Nemesis.Instance;
            if (comp == null || comp.Hunts.Count == 0)
                return;

            var extras = new List<FloatMenuOption>();
            bool informants = NemesisMod.Settings?.enableInformants ?? true;
            IReadOnlyList<NemesisData> hunts = comp.Hunts;
            for (int i = 0; i < hunts.Count; i++)
            {
                NemesisData h = hunts[i];
                if (!h.active) continue;
                string? name = h.nemesisName;
                extras.Add(Option("Nemesis_Comms_TauntBack".Translate(name), myPawn, __instance, h, NemesisCommsKind.TauntBack));
                extras.Add(Option("Nemesis_Comms_OfferTruce".Translate(name), myPawn, __instance, h, NemesisCommsKind.OfferTruce));
                extras.Add(Option("Nemesis_Comms_DemandSurrender".Translate(name), myPawn, __instance, h, NemesisCommsKind.DemandSurrender));
                if (informants)
                {
                    int cost = NemesisInformants.LeadCost(h);
                    extras.Add(Option("Nemesis_Comms_BuyLeadNamed".Translate(cost, name), myPawn, __instance, h, NemesisCommsKind.BuyLead));
                    extras.Add(Option("Nemesis_Comms_PostBountyNamed".Translate(cost, name), myPawn, __instance, h, NemesisCommsKind.PostBounty));
                }
            }
            if (extras.Count == 0)
                return;

            __result = __result == null ? extras : __result.Concat(extras);
        }

        private static FloatMenuOption Option(string label, Pawn pawn, Building_CommsConsole console,
            NemesisData h, NemesisCommsKind kind)
        {
            int huntId = h.huntId;
            return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, () =>
            {
                Job job = JobMaker.MakeJob(NemesisDefOf.Nemesis_UseCommsConsole, console);
                job.count = JobDriver_NemesisComms.Encode(huntId, kind);
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }), pawn, console);
        }
    }

    /// <summary>
    /// Generates the <c>Nemesis_HuntSite</c> world object def as a copy of vanilla's Site def
    /// (same class, comps and icons), so hunt camps behave like any site while their defName
    /// carries "Nemesis" for Living World's tile avoidance (AZR-555). Implied defs get short
    /// hashes and resolve with the rest.
    /// </summary>
    [HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
    public static class Patch_DefGenerator_HuntSiteDef
    {
        [HarmonyPostfix]
        static void Postfix(bool __0)
        {
            if (DefDatabase<WorldObjectDef>.GetNamedSilentFail(NemesisHuntSites.HuntSiteDefId) != null) return;
            WorldObjectDef? site = DefDatabase<WorldObjectDef>.GetNamedSilentFail("Site");
            if (site == null)
            {
                Log.Warning("[Nemesis] Vanilla Site def missing; hunt camps are off.");
                return;
            }
            WorldObjectDef copy = (WorldObjectDef)typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(site, null)!;
            copy.defName = NemesisHuntSites.HuntSiteDefId;
            copy.shortHash = 0;
            copy.generated = true;
            copy.modContentPack = NemesisMod.Pack;
            copy.fileName = null;
            DefGenerator.AddImpliedDef(copy, __0);
        }
    }
}
