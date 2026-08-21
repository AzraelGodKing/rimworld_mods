using System.Linq;
using System.Text;
using LudeonTK;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// Dev-mode debug actions for Deep Colony. Auto-discovered by RimWorld's debug system
    /// and added to the debug palette under the "Deep Colony" category.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class DebugActions_DeepColony
    {
        // Needed so the class is picked up — the [DebugAction] methods must live in a loaded assembly.
        static DebugActions_DeepColony() { }

        // ── PERKS ─────────────────────────────────────────────────────────────────

        [DebugAction("Deep Colony", "Grant perk point",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void GrantPerkPoint(Pawn p)
        {
            var comp = p.TryGetComp<Comp_DeepColony>();
            if (comp == null) { Log.Warning("[DeepColony] No Comp_DeepColony on " + p.LabelShort); return; }
            comp.availablePerkPoints++;
            Messages.Message("[DeepColony] Granted 1 perk point to " + p.LabelShort +
                " (now " + comp.availablePerkPoints + ").", p, MessageTypeDefOf.NeutralEvent, false);
        }

        [DebugAction("Deep Colony", "Unlock all available perks",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void UnlockAllAvailablePerks(Pawn p)
        {
            var comp = p.TryGetComp<Comp_DeepColony>();
            if (comp == null) { Log.Warning("[DeepColony] No Comp_DeepColony on " + p.LabelShort); return; }

            // Give enough points then unlock everything that qualifies
            int unlocked = 0;
            comp.availablePerkPoints += 99;
            foreach (var perk in DefDatabase<PerkDef>.AllDefs
                .Where(pk => comp.CanUnlock(pk)).ToList())
            {
                comp.UnlockPerk(perk);
                unlocked++;
            }
            Log.Message("[DeepColony] Unlocked " + unlocked + " perks on " + p.LabelShort);
        }

        [DebugAction("Deep Colony", "Reset perk tree",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ResetPerkTree(Pawn p)
        {
            var comp = p.TryGetComp<Comp_DeepColony>();
            if (comp == null) { Log.Warning("[DeepColony] No Comp_DeepColony on " + p.LabelShort); return; }

            // Remove all perk hediffs
            foreach (string defName in comp.unlockedPerkDefNames)
            {
                var perkDef = DefDatabase<PerkDef>.GetNamedSilentFail(defName);
                if (perkDef?.hediff == null) continue;
                var h = p.health.hediffSet.GetFirstHediffOfDef(perkDef.hediff);
                if (h != null) p.health.RemoveHediff(h);
            }
            comp.unlockedPerkDefNames.Clear();
            comp.availablePerkPoints = 0;
            Messages.Message("[DeepColony] Perk tree reset for " + p.LabelShort + ".",
                p, MessageTypeDefOf.NeutralEvent, false);
        }

        [DebugAction("Deep Colony", "Log perk state",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogPerkState(Pawn p)
        {
            var comp = p.TryGetComp<Comp_DeepColony>();
            if (comp == null) { Log.Warning("[DeepColony] No Comp_DeepColony on " + p.LabelShort); return; }

            var sb = new StringBuilder();
            sb.AppendLine("[DeepColony] Perk state for " + p.LabelShort + ":");
            sb.AppendLine("  Available points: " + comp.availablePerkPoints);
            sb.AppendLine("  Unlocked (" + comp.unlockedPerkDefNames.Count + "):");
            foreach (string n in comp.unlockedPerkDefNames) sb.AppendLine("    - " + n);

            var available = DefDatabase<PerkDef>.AllDefs
                .Where(pk => comp.CanUnlock(pk)).ToList();
            sb.AppendLine("  Can unlock now (" + available.Count + "):");
            foreach (var pk in available) sb.AppendLine("    - " + pk.defName);

            Log.Message(sb.ToString());
        }

        [DebugAction("Deep Colony", "Backfill perk gate points now",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceBackfillPerkPoints(Pawn p)
        {
            var comp = p.TryGetComp<Comp_DeepColony>();
            if (comp == null) { Log.Warning("[DeepColony] No Comp_DeepColony on " + p.LabelShort); return; }
            comp.perkGatesBackfilled = false;
            comp.TryBackfillPerkGatePoints(announce: true);
        }

        [DebugAction("Deep Colony", "Open perk tree window",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void OpenPerkTreeWindow(Pawn p)
        {
            if (p.TryGetComp<Comp_DeepColony>() == null)
            {
                Log.Warning("[DeepColony] No Comp_DeepColony on " + p.LabelShort);
                return;
            }
            Find.WindowStack.Add(new Window_PerkTree(p));
        }

        // ── TRAUMA ────────────────────────────────────────────────────────────────

        [DebugAction("Deep Colony", "Apply trauma: combat shock",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ApplyCombatShock(Pawn p)
        {
            TraumaUtility.ApplyTrauma(p, DC_DefOf.DC_Trauma_CombatShock);
            Messages.Message("[DeepColony] Applied combat shock to " + p.LabelShort + ".",
                p, MessageTypeDefOf.NegativeEvent, false);
        }

        [DebugAction("Deep Colony", "Apply trauma: violent loss",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ApplyViolentLoss(Pawn p)
        {
            TraumaUtility.ApplyTrauma(p, DC_DefOf.DC_Trauma_ViolentLoss);
            Messages.Message("[DeepColony] Applied violent loss trauma to " + p.LabelShort + ".",
                p, MessageTypeDefOf.NegativeEvent, false);
        }

        [DebugAction("Deep Colony", "Apply trauma: massacre survivor",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ApplyMassacre(Pawn p)
        {
            TraumaUtility.ApplyTrauma(p, DC_DefOf.DC_Trauma_Massacre);
            Messages.Message("[DeepColony] Applied massacre trauma to " + p.LabelShort + ".",
                p, MessageTypeDefOf.NegativeEvent, false);
        }

        [DebugAction("Deep Colony", "Apply therapy to pawn",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ApplyTherapy(Pawn p)
        {
            TraumaUtility.ApplyTherapy(null, p);
            Messages.Message("[DeepColony] Applied therapy to " + p.LabelShort + ".",
                p, MessageTypeDefOf.PositiveEvent, false);
        }

        [DebugAction("Deep Colony", "Log trauma state",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogTraumaState(Pawn p)
        {
            if (p.needs?.mood?.thoughts == null) { Log.Message("[DeepColony] " + p.LabelShort + " has no mood thoughts."); return; }

            var traumas = p.needs.mood.thoughts.memories.Memories
                .OfType<Thought_Trauma>().ToList();
            if (traumas.Count == 0) { Log.Message("[DeepColony] " + p.LabelShort + " has no active traumas."); return; }

            var sb = new StringBuilder();
            sb.AppendLine("[DeepColony] Traumas on " + p.LabelShort + ":");
            foreach (var t in traumas)
                sb.AppendLine("  " + t.LabelCap + " | Stage " + t.CurStageIndex +
                    " | Age " + t.age + "/" + t.DurationTicks + " ticks | Mood " + t.MoodOffset());
            Log.Message(sb.ToString());
        }

        [DebugAction("Deep Colony", "Clear all trauma",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ClearAllTrauma(Pawn p)
        {
            if (p.needs?.mood?.thoughts == null) return;
            var memories = p.needs.mood.thoughts.memories.Memories;
            int removed = 0;
            for (int i = memories.Count - 1; i >= 0; i--)
            {
                if (memories[i] is Thought_Trauma tt)
                {
                    p.needs.mood.thoughts.memories.RemoveMemory(tt);
                    removed++;
                }
            }
            Messages.Message("[DeepColony] Cleared " + removed + " trauma(s) from " + p.LabelShort + ".",
                p, MessageTypeDefOf.PositiveEvent, false);
        }

        [DebugAction("Deep Colony", "Apply trauma: captivity",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ApplyCaptivity(Pawn p)
        {
            TraumaUtility.ApplyTrauma(p, DC_DefOf.DC_Trauma_Captivity);
            Messages.Message("[DeepColony] Applied captivity trauma to " + p.LabelShort + ".",
                p, MessageTypeDefOf.NegativeEvent, false);
        }

        [DebugAction("Deep Colony", "Apply trauma: bereavement shock",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ApplyBereavement(Pawn p)
        {
            TraumaUtility.ApplyTrauma(p, DC_DefOf.DC_Trauma_BereavementShock);
            Messages.Message("[DeepColony] Applied bereavement shock to " + p.LabelShort + ".",
                p, MessageTypeDefOf.NegativeEvent, false);
        }

        // ── APPRENTICESHIP ────────────────────────────────────────────────────────

        [DebugAction("Deep Colony", "Force set mentor (click mentor, then apprentice)",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceSetMentorPick(Pawn p)
        {
            // Two-step via selected pawns: if another colonist is selected, link them.
            Pawn other = null;
            foreach (object obj in Find.Selector.SelectedObjectsListForReading)
            {
                if (obj is Pawn sel && sel != p && sel.IsColonistPlayerControlled)
                {
                    other = sel;
                    break;
                }
            }
            if (other == null)
            {
                Messages.Message("[DeepColony] Select mentor AND apprentice, then use this on the mentor.",
                    MessageTypeDefOf.RejectInput, false);
                return;
            }

            MentorshipUtility.SetMentorRelation(p, other);
            Messages.Message("[DeepColony] Forced mentor link: " + p.LabelShort + " → " + other.LabelShort + ".",
                MessageTypeDefOf.NeutralEvent, false);
        }

        [DebugAction("Deep Colony", "Clear mentor link",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ClearMentorLink(Pawn p)
        {
            MentorshipUtility.ClearAllLinksInvolving(p);
            Messages.Message("[DeepColony] Cleared mentoring links involving " + p.LabelShort + ".",
                p, MessageTypeDefOf.NeutralEvent, false);
        }

        [DebugAction("Deep Colony", "Force graduate apprenticeship",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceGraduate(Pawn p)
        {
            var comp = p.TryGetComp<Comp_DeepColony>();
            if (comp?.mentor == null)
            {
                Messages.Message("[DeepColony] " + p.LabelShort + " has no mentor.",
                    MessageTypeDefOf.RejectInput, false);
                return;
            }
            MentorshipUtility.Graduate(comp.mentor, p);
        }

        [DebugAction("Deep Colony", "Log mentor state",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogMentorState(Pawn p)
        {
            var comp = p.TryGetComp<Comp_DeepColony>();
            if (comp == null) { Log.Warning("[DeepColony] No Comp_DeepColony on " + p.LabelShort); return; }

            string mentor = comp.mentor != null ? comp.mentor.LabelShort : "none";
            int apprenticeCount = p.relations.GetDirectRelationsCount(DC_DefOf.DC_MentorOf);
            Log.Message("[DeepColony] " + p.LabelShort +
                " | Mentor: " + mentor +
                " | Apprentices: " + apprenticeCount +
                " | Active session: " + ActiveMentoringSession.IsBeingMentored(p));
        }

        // ── GENERATIONS ───────────────────────────────────────────────────────────

        [DebugAction("Deep Colony", "Force inheritance check",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceInheritanceCheck(Pawn p)
        {
            var gameComp = GameComp_DeepColony.Instance;
            if (gameComp == null) { Log.Warning("[DeepColony] No GameComp_DeepColony."); return; }

            // Temporarily clear the processed flag so it can run again
            var field = typeof(GameComp_DeepColony)
                .GetField("inheritanceProcessed",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var set = field?.GetValue(gameComp) as System.Collections.Generic.HashSet<int>;
            set?.Remove(p.thingIDNumber);

            InheritanceUtility.TryApplyInheritance(p);
            Messages.Message("[DeepColony] Re-ran inheritance check for " + p.LabelShort + ".",
                p, MessageTypeDefOf.NeutralEvent, false);
        }

        [DebugAction("Deep Colony", "Dump inheritance roll (log only)",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void DumpInheritanceRoll(Pawn p)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[DeepColony] Inheritance dump for " + p.LabelShort + ":");
            sb.AppendLine("  Parents (relations):");
            if (p.relations != null)
            {
                foreach (Pawn other in p.relations.RelatedPawns)
                {
                    PawnRelationDef rel = p.GetMostImportantRelation(other);
                    sb.AppendLine("    " + other.LabelShort + " (" + (rel?.defName ?? "?") + ")"
                        + " colonist=" + (other.IsColonist || other.Faction == Faction.OfPlayer));
                }
            }
            sb.AppendLine("  Trait inherit chance setting: "
                + DeepColonySettings.Get.traitInheritChance.ToString("F2"));
            sb.AppendLine("  Inheritance enabled: " + DeepColonySettings.Get.enableInheritance);
            Log.Message(sb.ToString());
        }

        // ── FACTION REPUTATION ────────────────────────────────────────────────────

        [DebugAction("Deep Colony", "Inject +5 faction drift (selected pawn's faction)",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void InjectPositiveDrift(Pawn p)
        {
            if (p.Faction == null || p.Faction.IsPlayer)
            {
                Log.Warning("[DeepColony] Pawn has no non-player faction."); return;
            }
            GameComp_DeepColony.Instance?.AddFactionDrift(p.Faction, 5f, FactionRepReason.Debug);
            Messages.Message("[DeepColony] Injected +5 drift for " + p.Faction.Name + ".",
                MessageTypeDefOf.NeutralEvent, false);
        }

        [DebugAction("Deep Colony", "Inject -5 faction drift (selected pawn's faction)",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void InjectNegativeDrift(Pawn p)
        {
            if (p.Faction == null || p.Faction.IsPlayer)
            {
                Log.Warning("[DeepColony] Pawn has no non-player faction."); return;
            }
            GameComp_DeepColony.Instance?.AddFactionDrift(p.Faction, -5f, FactionRepReason.Debug);
            Messages.Message("[DeepColony] Injected -5 drift for " + p.Faction.Name + ".",
                MessageTypeDefOf.NeutralEvent, false);
        }

        [DebugAction("Deep Colony", "Log faction drift buffer",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogFactionDriftBuffer()
        {
            var gameComp = GameComp_DeepColony.Instance;
            if (gameComp == null) { Log.Warning("[DeepColony] No GameComp_DeepColony."); return; }

            var field = typeof(GameComp_DeepColony)
                .GetField("factionDriftBuffer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dict = field?.GetValue(gameComp)
                as System.Collections.Generic.Dictionary<int, float>;

            if (dict == null || dict.Count == 0)
            {
                Log.Message("[DeepColony] Faction drift buffer is empty.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("[DeepColony] Faction drift buffer:");
            foreach (var kv in dict)
            {
                Faction f = Find.FactionManager.AllFactionsListForReading
                    .Find(x => x.loadID == kv.Key);
                sb.AppendLine("  " + (f?.Name ?? "ID:" + kv.Key) + " → " +
                    (kv.Value >= 0 ? "+" : "") + kv.Value.ToString("F1"));
            }
            Log.Message(sb.ToString());
        }

        [DebugAction("Deep Colony", "Simulate raid from faction (selected pawn's faction)",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SimulateRaidRepEffect(Pawn p)
        {
            if (p.Faction == null || p.Faction.IsPlayer)
            {
                Log.Warning("[DeepColony] Pawn has no non-player faction."); return;
            }
            FactionRepUtility.OnRaidFromFaction(p.Faction);
            Messages.Message("[DeepColony] Simulated raid rep effect from " + p.Faction.Name + ".",
                MessageTypeDefOf.NeutralEvent, false);
        }

        // ── GENERAL ───────────────────────────────────────────────────────────────

        [DebugAction("Deep Colony", "Log settings snapshot",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogSettingsSnapshot()
        {
            var s = DeepColonySettings.Get;
            Log.Message("[DeepColony] Settings: perks=" + s.enablePerks
                + " trauma=" + s.enableTrauma
                + " mentoring=" + s.enableMentoring
                + " inheritance=" + s.enableInheritance
                + " factionRep=" + s.enableFactionRep
                + " familyJoin=" + s.enableFamilyJoin
                + " reconcile=" + s.enableExLoverReconcile
                + " combatShock=" + s.combatShockChance
                + " minLead=" + s.minSkillLead
                + " mentorXP=" + s.passiveMentorMultiplier + "/" + s.activeMentorMultiplier
                + " massacre=" + s.massacreDeathThreshold);
        }

        [DebugAction("Deep Colony", "Log all comp states (all colonists)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogAllCompStates()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[DeepColony] All colonist comp states:");
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn p in map.mapPawns.FreeColonists)
                {
                    var comp = p.TryGetComp<Comp_DeepColony>();
                    if (comp == null) { sb.AppendLine("  " + p.LabelShort + ": NO COMP"); continue; }
                    sb.AppendLine("  " + p.LabelShort +
                        " | Points: " + comp.availablePerkPoints +
                        " | Perks: " + comp.unlockedPerkDefNames.Count +
                        " | Gates: " + Comp_DeepColony.CountPassedPerkGates(p) +
                        " | Backfilled: " + comp.perkGatesBackfilled +
                        " | Mentor: " + (comp.mentor?.LabelShort ?? "none"));
                }
            }
            Log.Message(sb.ToString());
        }

        [DebugAction("Deep Colony", "Force family birthday letter",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceFamilyLetter(Pawn p)
        {
            var gc = GameComp_DeepColony.Instance;
            if (gc == null) return;
            gc.lastFamilyLetterTick = -1;
            string title = "DC_FamilyLetter_BirthdayLabel".Translate(p.LabelShort.Named("PAWN"));
            string body = "DC_FamilyLetter_BirthdayBody".Translate(p.LabelShort.Named("PAWN"));
            if (gc.familyLetters == null) gc.familyLetters = new System.Collections.Generic.List<FamilyLetterEntry>();
            gc.familyLetters.Add(new FamilyLetterEntry { title = title, body = body, ticksGame = Find.TickManager.TicksGame });
            Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.PositiveEvent, p);
        }

        [DebugAction("Deep Colony", "Apply child raid-witness thought",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceChildWitness(Pawn p)
        {
            if (DC_DefOf.DC_Thought_ChildRaidWitness == null || p.needs?.mood?.thoughts == null) return;
            var thought = (Thought_Memory)ThoughtMaker.MakeThought(DC_DefOf.DC_Thought_ChildRaidWitness);
            p.needs.mood.thoughts.memories.TryGainMemory(thought);
        }

        [DebugAction("Deep Colony", "Force kin join / defect",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceKinJoin(Pawn p)
        {
            if (FamilyJoinUtility.TryForceJoin(p))
            {
                Messages.Message("[DeepColony] Forced kin join for " + p.LabelShort + ".",
                    p, MessageTypeDefOf.PositiveEvent, false);
                return;
            }
            Log.Warning("[DeepColony] Could not force kin join for " + p.LabelShort
                + " (player / prisoner / no kin / not on a home map). Weight="
                + FamilyJoinUtility.MaxKinWeight(p).ToString("F2"));
        }

        [DebugAction("Deep Colony", "Force ex-lover reconcile",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceExLoverReconcile(Pawn p)
        {
            if (ExLoverReconcileUtility.TryForceReconcile(p))
            {
                Messages.Message("[DeepColony] Forced reconcile for " + p.LabelShort + ".",
                    p, MessageTypeDefOf.PositiveEvent, false);
                return;
            }
            Log.Warning("[DeepColony] No ex-lover/ex-spouse on this map for " + p.LabelShort + ".");
        }

        [DebugAction("Deep Colony", "Apply toxic relationship trauma",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceToxicRelationship(Pawn p)
        {
            if (DC_DefOf.DC_Trauma_ToxicRelationship == null)
            {
                Log.Warning("[DeepColony] DC_Trauma_ToxicRelationship is missing.");
                return;
            }
            TraumaUtility.ApplyTrauma(p, DC_DefOf.DC_Trauma_ToxicRelationship);
        }
    }
}
