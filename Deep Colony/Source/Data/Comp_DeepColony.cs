using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeepColony
{
    public class CompProperties_DeepColony : CompProperties
    {
        public CompProperties_DeepColony() => compClass = typeof(Comp_DeepColony);
    }

    /// <summary>
    /// Per-pawn perk data and apprenticeship mentor link. Injected into humanlike defs at startup.
    /// </summary>
    public class Comp_DeepColony : ThingComp
    {
        public List<string> unlockedPerkDefNames = new List<string>();
        public int availablePerkPoints;
        public bool perkGatesBackfilled;
        public int unspentPerkPointsSinceTick = -1;
        public int untreatedTraumaSinceTick = -1;
        public string lastCounselorName;
        public int lastCounselorId = -1;
        public int totalCounselSessions;
        public bool bornInColony;
        public bool grewInGrowthVat;
        public bool childhoodMemoryGranted;
        public int isolationSinceTick = -1;
        public int lastFamilyMealTick = -1;
        public bool parentReunionGranted;
        public bool familyJoinRolled;

        /// <summary>D21 — last tick this colonist despawned from a player home map (-1 = never).</summary>
        public int leftColonyMapTick = -1;

        /// <summary>D21 — last kin-homecoming thought tick (-1 = never).</summary>
        public int lastHomecomingTick = -1;

        /// <summary>E03 — last family-tend comfort tick (-1 = never).</summary>
        public int lastFamilyTendTick = -1;

        /// <summary>E02 — last kidnapped/captured tick (-1 = not currently taken).</summary>
        public int kinTakenTick = -1;

        /// <summary>E04 — true once this pawn has had living blood kin as colonists.</summary>
        public bool sawColonyBloodKin;

        /// <summary>E04 — true while this colonist is the last blood kin in the colony.</summary>
        public bool lastOfTheLine;

        /// <summary>E06 — last family prison-visit tick (-1 = never).</summary>
        public int lastFamilyVisitTick = -1;

        /// <summary>E09 — last kin-downed-beside-you tick (-1 = never).</summary>
        public int lastKinDownedTick = -1;

        /// <summary>E10 — last empty-nest thought tick (-1 = never).</summary>
        public int lastEmptyNestTick = -1;

        /// <summary>E08 — one-shot family-tradition teach letter.</summary>
        public bool traditionTeachNoted;

        /// <summary>D18 — how many times this pawn got back together with another (keyed by thingIDNumber).</summary>
        public Dictionary<int, int> reconcileCountsByPawn = new Dictionary<int, int>();

        /// <summary>F01 — touch-comfort 0–1 keyed by other pawn thingIDNumber.</summary>
        public Dictionary<int, float> touchComfortByPawn = new Dictionary<int, float>();

        /// <summary>F02 — last tick a touch-starved pawn had trusted contact (-1 = never).</summary>
        public int lastTrustedTouchTick = -1;

        public Pawn mentor;
        public string mentoredSkillDefName;
        public string perkBeingTaughtDefName;
        public int perkTeachProgress;
        public bool elderPerkGranted;
        public string familyTraditionSkillDefName;

        /// <summary>Past mentor display names for teaching-lineage flavor (newest last).</summary>
        public List<string> teacherLineage = new List<string>();

        /// <summary>Peak skill levels for muscle-memory double XP when rusting back up.</summary>
        public Dictionary<string, int> peakSkillLevels = new Dictionary<string, int>();

        /// <summary>Counsel session counts keyed by counselor thingIDNumber.</summary>
        public Dictionary<int, int> counselCountsByPawn = new Dictionary<int, int>();

        /// <summary>B06 — recoveries per TraumaDef.defName.</summary>
        public Dictionary<string, int> recoveredTraumaCounts = new Dictionary<string, int>();

        /// <summary>Active trauma defNames for natural-expiry detection.</summary>
        public List<string> trackedTraumaDefNames = new List<string>();

        /// <summary>B17 — faction loadIDs remembered from trauma.</summary>
        public List<int> grudgeFactionIds = new List<int>();

        public bool seasonedGrowthGranted;

        /// <summary>A17 — Faction.loadID this pawn is envoy for, or -1.</summary>
        public int envoyFactionId = -1;

        /// <summary>A03 — last respec tick (-1 = never).</summary>
        public int lastRespecTick = -1;

        /// <summary>B02 — active archetype defName, if any.</summary>
        public string activeArchetypeDefName;

        public Pawn Pawn => parent as Pawn;

        public bool HasPerk(PerkDef perk) =>
            unlockedPerkDefNames.Contains(perk.defName);

        public bool CanUnlock(PerkDef perk)
        {
            if (!DeepColonySettings.Get.enablePerks) return false;
            if (perk == null || HasPerk(perk)) return false;
            if (availablePerkPoints <= 0) return false;
            if (!PerkVisible(perk)) return false;

            var pawn = Pawn;
            if (pawn == null || pawn.skills == null) return false;
            if (pawn.skills.GetSkill(perk.skill).Level < perk.requiredLevel) return false;

            if (IsExclusiveBlocked(perk)) return false;

            if (perk.capstone || perk.requiredLevel >= 20)
            {
                if (!HasUnlockedAnyAtLevel(perk.skill, 15)) return false;
            }
            else if (perk.HasPrerequisite)
            {
                var prereq = DefDatabase<PerkDef>.GetNamedSilentFail(perk.prerequisitePerk);
                if (prereq != null && !HasPerk(prereq)) return false;
            }
            return true;
        }

        public static bool PerkVisible(PerkDef perk)
        {
            if (perk == null) return false;
            var s = DeepColonySettings.Get;
            if (perk.capstone || perk.requiredLevel >= 20)
                return s.enableSkill20Capstones;
            if (perk.alternateBranch)
                return s.enableBranchingPerks;
            return true;
        }

        private bool IsExclusiveBlocked(PerkDef perk)
        {
            if (perk.exclusiveWith != null)
            {
                for (int i = 0; i < perk.exclusiveWith.Count; i++)
                {
                    if (unlockedPerkDefNames.Contains(perk.exclusiveWith[i]))
                        return true;
                }
            }
            foreach (PerkDef other in DefDatabase<PerkDef>.AllDefs)
            {
                if (other?.exclusiveWith == null) continue;
                if (!other.exclusiveWith.Contains(perk.defName)) continue;
                if (HasPerk(other)) return true;
            }
            return false;
        }

        public bool HasUnlockedAnyAtLevel(SkillDef skill, int level)
        {
            if (skill == null) return false;
            for (int i = 0; i < unlockedPerkDefNames.Count; i++)
            {
                PerkDef p = DefDatabase<PerkDef>.GetNamedSilentFail(unlockedPerkDefNames[i]);
                if (p?.skill == skill && p.requiredLevel == level) return true;
            }
            return false;
        }

        public void UnlockPerk(PerkDef perk)
        {
            if (!CanUnlock(perk)) return;
            var pawn = Pawn;
            if (pawn == null) return;

            unlockedPerkDefNames.Add(perk.defName);
            availablePerkPoints--;
            NoteUnspentPointsChanged();
            ApplyPerkHediff(perk);
            ArchetypeUtility.TryRefresh(pawn);

            Messages.Message(
                "DC_PerkUnlocked".Translate(pawn.LabelShort.Named("PAWN"), perk.LabelCap.Named("PERK")),
                pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        /// <summary>Grant a perk without spending a point (perk apprenticeship).</summary>
        public void UnlockPerkFree(PerkDef perk)
        {
            if (perk == null || HasPerk(perk)) return;
            if (!DeepColonySettings.Get.enablePerks) return;
            if (!PerkVisible(perk)) return;
            var pawn = Pawn;
            if (pawn == null) return;
            unlockedPerkDefNames.Add(perk.defName);
            ApplyPerkHediff(perk);
            ArchetypeUtility.TryRefresh(pawn);
        }

        public bool CanForget(PerkDef perk)
        {
            if (!DeepColonySettings.Get.enablePerks) return false;
            if (!DeepColonySettings.Get.enablePerkRespec) return false;
            if (perk == null || !HasPerk(perk)) return false;
            if (HasDependentUnlocked(perk)) return false;

            int cooldown = Mathf.RoundToInt(DeepColonySettings.Get.respecCooldownDays * 60000f);
            if (lastRespecTick >= 0 && Find.TickManager != null
                && Find.TickManager.TicksGame - lastRespecTick < cooldown)
                return false;
            return true;
        }

        public bool HasDependentUnlocked(PerkDef perk)
        {
            if (perk == null) return false;
            foreach (PerkDef other in DefDatabase<PerkDef>.AllDefs)
            {
                if (other == null || !HasPerk(other)) continue;
                if (other.prerequisitePerk == perk.defName) return true;
                // Capstones depend on any L15 — block forgetting L15 if a capstone is owned for that skill.
                if ((other.capstone || other.requiredLevel >= 20)
                    && other.skill == perk.skill
                    && perk.requiredLevel == 15)
                    return true;
            }
            return false;
        }

        public void ForgetPerk(PerkDef perk)
        {
            if (!CanForget(perk)) return;
            var pawn = Pawn;
            if (pawn == null) return;

            unlockedPerkDefNames.Remove(perk.defName);
            availablePerkPoints++;
            NoteUnspentPointsChanged();
            lastRespecTick = Find.TickManager?.TicksGame ?? 0;

            if (perk.hediff != null && pawn.health != null)
            {
                Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(perk.hediff);
                if (h != null) pawn.health.RemoveHediff(h);
            }

            if (DC_DefOf.DC_Thought_PerkReflection != null && pawn.needs?.mood?.thoughts != null)
            {
                var thought = (Thought_Memory)ThoughtMaker.MakeThought(DC_DefOf.DC_Thought_PerkReflection);
                pawn.needs.mood.thoughts.memories.TryGainMemory(thought);
            }

            ArchetypeUtility.TryRefresh(pawn);

            Messages.Message(
                "DC_PerkForgotten".Translate(pawn.LabelShort.Named("PAWN"), perk.LabelCap.Named("PERK")),
                pawn, MessageTypeDefOf.NeutralEvent, false);
        }

        /// <summary>B14 — auto-spend backfilled points on L5 perks only (conservative).</summary>
        public void TryAutoSpendRecruitPerks()
        {
            if (!DeepColonySettings.Get.enablePerks) return;
            if (!DeepColonySettings.Get.enableRecruitPrePerks) return;
            var pawn = Pawn;
            if (pawn?.skills == null || !pawn.IsColonistPlayerControlled) return;

            bool spent = false;
            foreach (PerkDef perk in DefDatabase<PerkDef>.AllDefsListForReading)
            {
                if (perk.requiredLevel != 5) continue;
                if (!CanUnlock(perk)) continue;
                UnlockPerk(perk);
                spent = true;
                if (availablePerkPoints <= 0) break;
            }
            if (spent)
            {
                Messages.Message(
                    "DC_RecruitPrePerked".Translate(pawn.LabelShort.Named("PAWN")),
                    pawn, MessageTypeDefOf.PositiveEvent, false);
            }
        }

        public SkillDef GetMentoredSkill()
        {
            if (mentoredSkillDefName.NullOrEmpty()) return null;
            return DefDatabase<SkillDef>.GetNamedSilentFail(mentoredSkillDefName);
        }

        public void SetMentoredSkill(SkillDef skill)
        {
            mentoredSkillDefName = skill?.defName;
        }

        public void NotifySkillLevelUp(SkillDef skill, int newLevel)
        {
            var pawn = Pawn;
            if (pawn != null)
                FamilyEchoUtility.NotifyTraditionGate(pawn, skill, newLevel);
            if (!DeepColonySettings.Get.enablePerks) return;
            availablePerkPoints++;
            NoteUnspentPointsChanged();
            if (pawn == null) return;

            Messages.Message(
                "DC_PerkPointGained".Translate(pawn.LabelShort.Named("PAWN"), skill.LabelCap.Named("SKILL")),
                pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        public void NoteUnspentPointsChanged()
        {
            if (availablePerkPoints > 0)
            {
                if (unspentPerkPointsSinceTick < 0)
                    unspentPerkPointsSinceTick = Find.TickManager?.TicksGame ?? 0;
            }
            else
            {
                unspentPerkPointsSinceTick = -1;
            }
        }

        public void TryBackfillPerkGatePoints(bool announce = true)
        {
            if (perkGatesBackfilled) return;
            perkGatesBackfilled = true;

            if (!DeepColonySettings.Get.enablePerks) return;

            var pawn = Pawn;
            if (pawn?.skills == null || !pawn.IsColonistPlayerControlled) return;

            int gatesPassed = CountPassedPerkGates(pawn);
            int accounted = unlockedPerkDefNames.Count + availablePerkPoints;
            int deficit = gatesPassed - accounted;
            if (deficit <= 0)
            {
                NoteUnspentPointsChanged();
                return;
            }

            availablePerkPoints += deficit;
            NoteUnspentPointsChanged();
            if (announce)
            {
                Messages.Message(
                    "DC_PerkPointsBackfilled".Translate(pawn.LabelShort.Named("PAWN"), deficit),
                    pawn, MessageTypeDefOf.PositiveEvent, false);
            }
        }

        public static int CountPassedPerkGates(Pawn pawn)
        {
            if (pawn?.skills == null) return 0;
            bool capstones = DeepColonySettings.Get.enableSkill20Capstones;
            int gates = 0;
            foreach (SkillRecord skill in pawn.skills.skills)
            {
                if (skill.TotallyDisabled) continue;
                if (skill.Level >= 5) gates++;
                if (skill.Level >= 15) gates++;
                if (capstones && skill.Level >= 20) gates++;
            }
            return gates;
        }

        public int HighestUnlockedPerkTierForSkill(SkillDef skill)
        {
            if (skill == null) return 0;
            int best = 0;
            for (int i = 0; i < unlockedPerkDefNames.Count; i++)
            {
                PerkDef perk = DefDatabase<PerkDef>.GetNamedSilentFail(unlockedPerkDefNames[i]);
                if (perk?.skill != skill) continue;
                if (perk.requiredLevel >= 20) best = Mathf.Max(best, 3);
                else if (perk.requiredLevel >= 15) best = Mathf.Max(best, 2);
                else if (perk.requiredLevel >= 5) best = Mathf.Max(best, 1);
            }
            return best;
        }

        public void RecordPeakSkill(SkillDef skill, int level)
        {
            if (skill == null) return;
            string key = skill.defName;
            if (!peakSkillLevels.TryGetValue(key, out int peak) || level > peak)
                peakSkillLevels[key] = level;
        }

        public int GetPeakSkill(SkillDef skill)
        {
            if (skill == null) return 0;
            return peakSkillLevels.TryGetValue(skill.defName, out int peak) ? peak : 0;
        }

        public void RecordTeacher(Pawn teacher)
        {
            if (teacher == null) return;
            string name = teacher.Name?.ToStringShort ?? teacher.LabelShort;
            if (teacherLineage == null) teacherLineage = new List<string>();
            if (teacherLineage.Count > 0 && teacherLineage[teacherLineage.Count - 1] == name)
                return;
            teacherLineage.Add(name);
            if (teacherLineage.Count > 8)
                teacherLineage.RemoveAt(0);
        }

        public string TeachingLineageInspect()
        {
            if (teacherLineage == null || teacherLineage.Count == 0) return null;
            if (teacherLineage.Count == 1)
                return "DC_InspectTaughtBy".Translate(teacherLineage[0]);
            // newest last
            string chain = string.Join(" ← ", teacherLineage);
            return "DC_InspectLineage".Translate(chain);
        }

        public int IncrementCounselCount(Pawn counselor)
        {
            if (counselor == null) return 0;
            if (counselCountsByPawn == null) counselCountsByPawn = new Dictionary<int, int>();
            int id = counselor.thingIDNumber;
            counselCountsByPawn.TryGetValue(id, out int count);
            count++;
            counselCountsByPawn[id] = count;
            lastCounselorName = counselor.LabelShort;
            lastCounselorId = counselor.thingIDNumber;
            totalCounselSessions++;
            return count;
        }

        public Pawn TryGetLastCounselor()
        {
            if (lastCounselorId < 0) return null;
            foreach (Map map in Find.Maps)
            {
                if (map?.mapPawns?.AllPawnsSpawned == null) continue;
                foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
                {
                    if (p.thingIDNumber == lastCounselorId) return p;
                }
            }
            return null;
        }

        public bool RollTraumaApplyChance(TraumaDef def)
        {
            if (def == null) return false;
            if (recoveredTraumaCounts == null) recoveredTraumaCounts = new Dictionary<string, int>();
            if (!recoveredTraumaCounts.TryGetValue(def.defName, out int n) || n <= 0)
                return true;
            // Each prior recovery of this trauma reduces re-apply chance.
            float chance = 1f - 0.15f * Mathf.Min(n, 4);
            return Rand.Chance(Mathf.Clamp(chance, 0.35f, 1f));
        }

        public float TraumaDurationSkipFraction(TraumaDef def)
        {
            if (def == null || recoveredTraumaCounts == null) return 0f;
            if (!recoveredTraumaCounts.TryGetValue(def.defName, out int n) || n <= 0)
                return 0f;
            return 0.08f * Mathf.Min(n, 4); // up to 32% shorter
        }

        public int RecordTraumaRecovery(TraumaDef def)
        {
            if (def == null) return 0;
            if (recoveredTraumaCounts == null) recoveredTraumaCounts = new Dictionary<string, int>();
            recoveredTraumaCounts.TryGetValue(def.defName, out int n);
            n++;
            recoveredTraumaCounts[def.defName] = n;
            return n;
        }

        public int TotalTraumaRecoveries()
        {
            if (recoveredTraumaCounts == null) return 0;
            int total = 0;
            foreach (var kv in recoveredTraumaCounts)
                total += kv.Value;
            return total;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            TryBackfillPerkGatePoints(announce: !respawningAfterLoad);
            if (!respawningAfterLoad)
                TryAutoSpendRecruitPerks();
            SeedPeakSkillsFromCurrent();
            ReapplyPerkHediffs();
            ArchetypeUtility.TryRefresh(Pawn);
            NoteUnspentPointsChanged();
        }

        private void SeedPeakSkillsFromCurrent()
        {
            var pawn = Pawn;
            if (pawn?.skills == null) return;
            foreach (SkillRecord sr in pawn.skills.skills)
            {
                if (sr.TotallyDisabled) continue;
                RecordPeakSkill(sr.def, sr.Level);
            }
        }

        private void ReapplyPerkHediffs()
        {
            var pawn = Pawn;
            if (pawn?.health == null) return;
            if (!DeepColonySettings.Get.enablePerks) return;

            for (int i = 0; i < unlockedPerkDefNames.Count; i++)
            {
                PerkDef perk = DefDatabase<PerkDef>.GetNamedSilentFail(unlockedPerkDefNames[i]);
                if (perk != null) ApplyPerkHediff(perk);
            }
        }

        private void ApplyPerkHediff(PerkDef perk)
        {
            var pawn = Pawn;
            if (pawn?.health == null || perk?.hediff == null) return;
            if (pawn.health.hediffSet.HasHediff(perk.hediff)) return;

            Hediff h = HediffMaker.MakeHediff(perk.hediff, pawn);
            pawn.health.AddHediff(h);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            var pawn = Pawn;
            if (pawn == null || !pawn.IsColonistPlayerControlled) yield break;
            if (!DeepColonySettings.Get.enablePerks) yield break;

            yield return new Command_Action
            {
                defaultLabel = "DC_ViewPerks".Translate(),
                defaultDesc = "DC_ViewPerksDesc".Translate(
                    availablePerkPoints,
                    unlockedPerkDefNames.Count),
                icon = ContentFinder<Texture2D>.Get("UI/PerkTree", false) ?? BaseContent.BadTex,
                action = () => Find.WindowStack.Add(new Window_PerkTree(pawn))
            };
        }

        public override string CompInspectStringExtra()
        {
            var pawn = Pawn;
            if (pawn == null || !pawn.IsColonistPlayerControlled) return null;

            var parts = new List<string>();
            if (DeepColonySettings.Get.enablePerks
                && (availablePerkPoints > 0 || unlockedPerkDefNames.Count > 0))
            {
                parts.Add("DC_InspectPerks".Translate(availablePerkPoints, unlockedPerkDefNames.Count));
            }
            if (DeepColonySettings.Get.enableMentoring && mentor != null)
            {
                SkillDef focus = GetMentoredSkill();
                if (focus != null)
                    parts.Add("DC_InspectMentorSkill".Translate(mentor.LabelShort, focus.LabelCap));
                else
                    parts.Add("DC_InspectMentor".Translate(mentor.LabelShort));
            }
            int apprentices = 0;
            if (DeepColonySettings.Get.enableMentoring && pawn.relations != null)
            {
                foreach (DirectPawnRelation rel in pawn.relations.DirectRelations)
                {
                    if (rel.def == DC_DefOf.DC_MentorOf) apprentices++;
                }
            }
            if (apprentices > 0)
            {
                parts.Add("DC_InspectApprentices".Translate(apprentices));
            }
            if (DeepColonySettings.Get.enableMentoring)
            {
                string lineage = TeachingLineageInspect();
                if (!lineage.NullOrEmpty()) parts.Add(lineage);
            }
            if (DeepColonySettings.Get.enableTrauma && TraumaUtility.HasAnyTrauma(pawn))
            {
                parts.Add("DC_InspectTrauma".Translate());
                string types = TraumaTypesInspect();
                if (!types.NullOrEmpty()) parts.Add(types);
                if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_ToxicRelationship))
                    parts.Add("DC_InspectToxicRelationship".Translate());
                string history = CounselingHistoryInspect();
                if (!history.NullOrEmpty()) parts.Add(history);
            }
            else if (DeepColonySettings.Get.enableTrauma)
            {
                string history = CounselingHistoryInspect();
                if (!history.NullOrEmpty()) parts.Add(history);
            }
            if (DeepColonySettings.Get.enableMentoring)
            {
                string teach = TeachProgressInspect();
                if (!teach.NullOrEmpty()) parts.Add(teach);
            }
            if (DeepColonySettings.Get.enableFactionRep)
            {
                string envoy = EnvoyInspect();
                if (!envoy.NullOrEmpty()) parts.Add(envoy);
            }
            if (DeepColonySettings.Get.enableMentoring)
            {
                string rival = RivalInspect();
                if (!rival.NullOrEmpty()) parts.Add(rival);
            }
            if (DeepColonySettings.Get.enableInheritance)
            {
                string gene = GeneVsBloodInspect();
                if (!gene.NullOrEmpty()) parts.Add(gene);
            }
            if (DeepColonySettings.Get.enableTrauma)
            {
                string grudge = GrudgeUtility.InspectString(pawn);
                if (!grudge.NullOrEmpty()) parts.Add(grudge);
            }
            if (DeepColonySettings.Get.enableCrossSkillArchetypes
                && !activeArchetypeDefName.NullOrEmpty())
            {
                ArchetypeDef arch = DefDatabase<ArchetypeDef>.GetNamedSilentFail(activeArchetypeDefName);
                if (arch != null)
                    parts.Add("DC_InspectArchetype".Translate(arch.LabelCap));
            }
            if (DeepColonySettings.Get.enableTouchAverse)
            {
                string touch = TouchAverseUtility.InspectString(pawn);
                if (!touch.NullOrEmpty()) parts.Add(touch);
            }
            return parts.Count == 0 ? null : string.Join("\n", parts);
        }

        public string CounselingHistoryInspect()
        {
            if (totalCounselSessions <= 0 && lastCounselorName.NullOrEmpty()) return null;
            int best = 0;
            if (counselCountsByPawn != null)
            {
                foreach (var kv in counselCountsByPawn)
                    if (kv.Value > best) best = kv.Value;
            }
            int need = ConfidantUtility.SessionsToBondFor(Pawn, TryGetLastCounselor());
            string counselor = lastCounselorName.NullOrEmpty() ? "—" : lastCounselorName;
            return "DC_InspectCounsel".Translate(counselor, totalCounselSessions, best, need);
        }

        public string TraumaTypesInspect()
        {
            var pawn = Pawn;
            if (pawn?.needs?.mood?.thoughts == null) return null;
            var labels = new List<string>();
            foreach (Thought_Memory mem in pawn.needs.mood.thoughts.memories.Memories)
            {
                if (mem is not Thought_Trauma tt || tt.traumaDef == null) continue;
                string lab = tt.traumaDef.LabelCap;
                if (!labels.Contains(lab)) labels.Add(lab);
                if (labels.Count >= 3) break;
            }
            if (labels.Count == 0) return null;
            return "DC_InspectTraumaTypes".Translate(string.Join(", ", labels));
        }

        public string TeachProgressInspect()
        {
            if (perkTeachProgress <= 0 || perkBeingTaughtDefName.NullOrEmpty()) return null;
            PerkDef perk = DefDatabase<PerkDef>.GetNamedSilentFail(perkBeingTaughtDefName);
            string name = perk?.LabelCap ?? perkBeingTaughtDefName;
            return "DC_InspectTeachProgress".Translate(name, perkTeachProgress, 3);
        }

        public string EnvoyInspect()
        {
            Faction f = FactionEnvoyUtility.GetEnvoyFaction(Pawn);
            if (f == null) return null;
            return "DC_InspectEnvoy".Translate(f.Name);
        }

        public string RivalInspect()
        {
            Pawn rival = RivalryUtility.FirstLivingRival(Pawn);
            if (rival == null) return null;
            return "DC_InspectRival".Translate(rival.LabelShort);
        }

        public void NoteCounselingSession()
        {
            var pawn = Pawn;
            if (pawn != null && TraumaUtility.HasAnyTrauma(pawn))
                untreatedTraumaSinceTick = Find.TickManager?.TicksGame ?? 0;
            else
                untreatedTraumaSinceTick = -1;
        }

        public string GeneVsBloodInspect()
        {
            if (familyTraditionSkillDefName.NullOrEmpty()) return null;
            var pawn = Pawn;
            if (pawn?.genes == null || pawn.genes.Xenogenes == null || pawn.genes.Xenogenes.Count == 0)
                return null;
            SkillDef skill = DefDatabase<SkillDef>.GetNamedSilentFail(familyTraditionSkillDefName);
            if (skill == null) return null;
            SkillRecord rec = pawn.skills?.GetSkill(skill);
            if (rec == null) return null;
            // Flavor only: xenogenes present vs a family tradition skill.
            return "DC_InspectGeneVsBlood".Translate(skill.LabelCap);
        }

        public void NoteUntreatedTrauma()
        {
            if (untreatedTraumaSinceTick < 0)
                untreatedTraumaSinceTick = Find.TickManager?.TicksGame ?? 0;
        }

        public void ClearUntreatedTraumaIfHealed()
        {
            var pawn = Pawn;
            if (pawn != null && !TraumaUtility.HasAnyTrauma(pawn))
                untreatedTraumaSinceTick = -1;
        }

        public override void PostExposeData()
        {
            Scribe_Collections.Look(ref unlockedPerkDefNames, "unlockedPerks", LookMode.Value);
            Scribe_Values.Look(ref availablePerkPoints, "availablePerkPoints", 0);
            Scribe_Values.Look(ref perkGatesBackfilled, "perkGatesBackfilled", false);
            Scribe_Values.Look(ref unspentPerkPointsSinceTick, "unspentPerkPointsSinceTick", -1);
            Scribe_Values.Look(ref untreatedTraumaSinceTick, "untreatedTraumaSinceTick", -1);
            Scribe_Values.Look(ref lastCounselorName, "lastCounselorName");
            Scribe_Values.Look(ref lastCounselorId, "lastCounselorId", -1);
            Scribe_Values.Look(ref totalCounselSessions, "totalCounselSessions", 0);
            Scribe_Values.Look(ref bornInColony, "bornInColony", false);
            Scribe_Values.Look(ref grewInGrowthVat, "grewInGrowthVat", false);
            Scribe_Values.Look(ref childhoodMemoryGranted, "childhoodMemoryGranted", false);
            Scribe_Values.Look(ref isolationSinceTick, "isolationSinceTick", -1);
            Scribe_Values.Look(ref lastFamilyMealTick, "lastFamilyMealTick", -1);
            Scribe_Values.Look(ref parentReunionGranted, "parentReunionGranted", false);
            Scribe_Values.Look(ref familyJoinRolled, "familyJoinRolled", false);
            Scribe_Values.Look(ref leftColonyMapTick, "leftColonyMapTick", -1);
            Scribe_Values.Look(ref lastHomecomingTick, "lastHomecomingTick", -1);
            Scribe_Values.Look(ref lastFamilyTendTick, "lastFamilyTendTick", -1);
            Scribe_Values.Look(ref kinTakenTick, "kinTakenTick", -1);
            Scribe_Values.Look(ref sawColonyBloodKin, "sawColonyBloodKin", false);
            Scribe_Values.Look(ref lastOfTheLine, "lastOfTheLine", false);
            Scribe_Values.Look(ref lastFamilyVisitTick, "lastFamilyVisitTick", -1);
            Scribe_Values.Look(ref lastKinDownedTick, "lastKinDownedTick", -1);
            Scribe_Values.Look(ref lastEmptyNestTick, "lastEmptyNestTick", -1);
            Scribe_Values.Look(ref traditionTeachNoted, "traditionTeachNoted", false);
            Scribe_Collections.Look(ref reconcileCountsByPawn, "reconcileCountsByPawn", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref touchComfortByPawn, "touchComfortByPawn", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref lastTrustedTouchTick, "lastTrustedTouchTick", -1);
            Scribe_References.Look(ref mentor, "mentor");
            Scribe_Values.Look(ref mentoredSkillDefName, "mentoredSkillDefName");
            Scribe_Values.Look(ref perkBeingTaughtDefName, "perkBeingTaughtDefName");
            Scribe_Values.Look(ref perkTeachProgress, "perkTeachProgress", 0);
            Scribe_Values.Look(ref elderPerkGranted, "elderPerkGranted", false);
            Scribe_Values.Look(ref familyTraditionSkillDefName, "familyTraditionSkillDefName");
            Scribe_Collections.Look(ref teacherLineage, "teacherLineage", LookMode.Value);
            Scribe_Collections.Look(ref peakSkillLevels, "peakSkillLevels", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref counselCountsByPawn, "counselCountsByPawn", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref recoveredTraumaCounts, "recoveredTraumaCounts", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref trackedTraumaDefNames, "trackedTraumaDefNames", LookMode.Value);
            Scribe_Collections.Look(ref grudgeFactionIds, "grudgeFactionIds", LookMode.Value);
            Scribe_Values.Look(ref seasonedGrowthGranted, "seasonedGrowthGranted", false);
            Scribe_Values.Look(ref envoyFactionId, "envoyFactionId", -1);
            Scribe_Values.Look(ref lastRespecTick, "lastRespecTick", -1);
            Scribe_Values.Look(ref activeArchetypeDefName, "activeArchetypeDefName");

            if (unlockedPerkDefNames == null) unlockedPerkDefNames = new List<string>();
            if (teacherLineage == null) teacherLineage = new List<string>();
            if (peakSkillLevels == null) peakSkillLevels = new Dictionary<string, int>();
            if (counselCountsByPawn == null) counselCountsByPawn = new Dictionary<int, int>();
            if (reconcileCountsByPawn == null) reconcileCountsByPawn = new Dictionary<int, int>();
            if (touchComfortByPawn == null) touchComfortByPawn = new Dictionary<int, float>();
            if (recoveredTraumaCounts == null) recoveredTraumaCounts = new Dictionary<string, int>();
            if (trackedTraumaDefNames == null) trackedTraumaDefNames = new List<string>();
            if (grudgeFactionIds == null) grudgeFactionIds = new List<int>();

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                TryBackfillPerkGatePoints(announce: false);
                SeedPeakSkillsFromCurrent();
                ReapplyPerkHediffs();
                ArchetypeUtility.TryRefresh(Pawn);
                NoteUnspentPointsChanged();
            }
        }
    }
}
