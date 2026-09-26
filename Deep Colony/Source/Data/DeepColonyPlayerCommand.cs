using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>AZR-307 — enqueue player UI mutations; drain on GameComponentTick (MP-safer, no Multiplayer.API).</summary>
    public static class DeepColonyPlayerCommand
    {
        private enum Kind : byte
        {
            ResumeCodex = 0,
            RecordCodex = 1,
            Divorce = 2,
            SetEnvoy = 3,
            ClearEnvoy = 4,
            SetMentor = 5,
            ClearMentor = 6,
            BeginRetrain = 7,
            TributeFaction = 8,
            TributeThing = 9
        }

        private struct Entry
        {
            public Kind kind;
            public int aId;
            public int bId;
            public string s1;
            public string s2;
        }

        private static readonly List<Entry> queue = new List<Entry>();

        public static void EnqueueResumeCodex(Thing notebook)
        {
            if (notebook == null) return;
            queue.Add(new Entry { kind = Kind.ResumeCodex, aId = notebook.thingIDNumber });
        }

        public static void EnqueueRecordCodex(Thing notebook)
        {
            if (notebook == null) return;
            queue.Add(new Entry { kind = Kind.RecordCodex, aId = notebook.thingIDNumber });
        }

        public static void EnqueueDivorce(Pawn a, Pawn b)
        {
            if (a == null || b == null) return;
            queue.Add(new Entry { kind = Kind.Divorce, aId = a.thingIDNumber, bId = b.thingIDNumber });
        }

        public static void EnqueueSetEnvoy(Pawn pawn, Faction faction)
        {
            if (pawn == null || faction == null) return;
            queue.Add(new Entry
            {
                kind = Kind.SetEnvoy,
                aId = pawn.thingIDNumber,
                bId = faction.loadID
            });
        }

        public static void EnqueueClearEnvoy(Pawn pawn)
        {
            if (pawn == null) return;
            queue.Add(new Entry { kind = Kind.ClearEnvoy, aId = pawn.thingIDNumber });
        }

        public static void EnqueueSetMentor(Pawn mentor, Pawn apprentice, SkillDef skill)
        {
            if (mentor == null || apprentice == null) return;
            queue.Add(new Entry
            {
                kind = Kind.SetMentor,
                aId = mentor.thingIDNumber,
                bId = apprentice.thingIDNumber,
                s1 = skill?.defName
            });
        }

        public static void EnqueueClearMentor(Pawn mentor, Pawn apprentice)
        {
            if (mentor == null || apprentice == null) return;
            queue.Add(new Entry
            {
                kind = Kind.ClearMentor,
                aId = mentor.thingIDNumber,
                bId = apprentice.thingIDNumber
            });
        }

        public static void EnqueueBeginRetrain(Pawn mentor, Pawn apprentice, PerkDef from, PerkDef to)
        {
            if (mentor == null || apprentice == null || from == null || to == null) return;
            queue.Add(new Entry
            {
                kind = Kind.BeginRetrain,
                aId = mentor.thingIDNumber,
                bId = apprentice.thingIDNumber,
                s1 = from.defName,
                s2 = to.defName
            });
        }

        public static void EnqueueTributeFaction(Faction faction)
        {
            if (faction == null) return;
            queue.Add(new Entry { kind = Kind.TributeFaction, bId = faction.loadID });
        }

        public static void EnqueueTributeThing(Thing thing, Faction faction)
        {
            if (thing == null || faction == null) return;
            queue.Add(new Entry
            {
                kind = Kind.TributeThing,
                aId = thing.thingIDNumber,
                bId = faction.loadID
            });
        }

        public static void Drain()
        {
            if (queue.Count == 0) return;
            List<Entry> batch = new List<Entry>(queue);
            queue.Clear();
            for (int i = 0; i < batch.Count; i++)
                Apply(batch[i]);
        }

        private static void Apply(Entry e)
        {
            switch (e.kind)
            {
                case Kind.ResumeCodex:
                    ApplyResumeCodex(e.aId);
                    break;
                case Kind.RecordCodex:
                    ApplyRecordCodex(e.aId);
                    break;
                case Kind.Divorce:
                {
                    Pawn a = FindPawn(e.aId);
                    Pawn b = FindPawn(e.bId);
                    if (a != null && b != null)
                        DivorceUtility.TryDivorce(a, b);
                    break;
                }
                case Kind.SetEnvoy:
                {
                    Pawn pawn = FindPawn(e.aId);
                    Faction faction = FindFaction(e.bId);
                    if (pawn != null && faction != null)
                        FactionEnvoyUtility.SetEnvoy(pawn, faction);
                    break;
                }
                case Kind.ClearEnvoy:
                {
                    Pawn pawn = FindPawn(e.aId);
                    if (pawn != null)
                        FactionEnvoyUtility.ClearEnvoy(pawn);
                    break;
                }
                case Kind.SetMentor:
                {
                    Pawn mentor = FindPawn(e.aId);
                    Pawn apprentice = FindPawn(e.bId);
                    SkillDef skill = e.s1.NullOrEmpty()
                        ? null
                        : DefDatabase<SkillDef>.GetNamedSilentFail(e.s1);
                    if (mentor != null && apprentice != null)
                        MentorshipUtility.SetMentorRelation(mentor, apprentice, skill);
                    break;
                }
                case Kind.ClearMentor:
                {
                    Pawn mentor = FindPawn(e.aId);
                    Pawn apprentice = FindPawn(e.bId);
                    if (mentor != null && apprentice != null)
                        MentorshipUtility.ClearMentorRelation(mentor, apprentice);
                    break;
                }
                case Kind.BeginRetrain:
                {
                    Pawn mentor = FindPawn(e.aId);
                    Pawn apprentice = FindPawn(e.bId);
                    PerkDef from = DefDatabase<PerkDef>.GetNamedSilentFail(e.s1);
                    PerkDef to = DefDatabase<PerkDef>.GetNamedSilentFail(e.s2);
                    if (mentor != null && apprentice != null && from != null && to != null)
                        MentorshipUtility.BeginRetrain(mentor, apprentice, from, to);
                    break;
                }
                case Kind.TributeFaction:
                {
                    Faction faction = FindFaction(e.bId);
                    if (faction != null)
                        TributeUtility.TrySendTribute(faction);
                    break;
                }
                case Kind.TributeThing:
                {
                    Thing thing = FindThing(e.aId);
                    Faction faction = FindFaction(e.bId);
                    if (thing != null && faction != null)
                        TributeUtility.TrySendTributeThing(thing, faction);
                    break;
                }
            }
        }

        private static void ApplyResumeCodex(int notebookId)
        {
            Thing notebook = FindThing(notebookId);
            CompCodexNotebook comp = notebook?.TryGetComp<CompCodexNotebook>();
            if (comp == null) return;
            if (string.IsNullOrEmpty(comp.projectDefName))
            {
                Messages.Message("DC_Codex_Empty".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            ResearchProjectDef proj = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(comp.projectDefName);
            if (proj == null)
            {
                Messages.Message("DC_Codex_Unknown".Translate(comp.projectDefName), MessageTypeDefOf.RejectInput);
                return;
            }
            if (proj.IsFinished)
            {
                Messages.Message("DC_Codex_AlreadyDone".Translate(proj.LabelCap), MessageTypeDefOf.NeutralEvent);
                return;
            }
            Find.ResearchManager.SetCurrentProject(proj);
            Messages.Message("DC_Codex_Resumed".Translate(proj.LabelCap), notebook, MessageTypeDefOf.TaskCompletion);
        }

        private static void ApplyRecordCodex(int notebookId)
        {
            Thing notebook = FindThing(notebookId);
            CompCodexNotebook comp = notebook?.TryGetComp<CompCodexNotebook>();
            if (comp == null) return;
            ResearchProjectDef cur = Find.ResearchManager.GetProject();
            if (cur == null)
            {
                Messages.Message("DC_Codex_NoProject".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            comp.projectDefName = cur.defName;
            Messages.Message("DC_Codex_Wrote".Translate(cur.LabelCap), notebook, MessageTypeDefOf.TaskCompletion);
        }

        private static Pawn FindPawn(int id)
        {
            return FamilyTreeUtility.FindPawnById(id);
        }

        private static Faction FindFaction(int loadId)
        {
            if (loadId < 0 || Find.FactionManager == null) return null;
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].loadID == loadId)
                    return all[i];
            }
            return null;
        }

        private static Thing FindThing(int id)
        {
            if (id <= 0 || Find.Maps == null) return null;
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map map = Find.Maps[i];
                if (map?.listerThings?.AllThings == null) continue;
                List<Thing> things = map.listerThings.AllThings;
                for (int j = 0; j < things.Count; j++)
                {
                    if (things[j] != null && things[j].thingIDNumber == id)
                        return things[j];
                }
                if (map.mapPawns?.AllPawnsSpawned == null) continue;
                foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
                {
                    if (p == null) continue;
                    if (p.thingIDNumber == id) return p;
                    Thing carried = p.carryTracker?.CarriedThing;
                    if (carried != null && carried.thingIDNumber == id) return carried;
                    if (p.inventory?.innerContainer != null)
                    {
                        foreach (Thing t in p.inventory.innerContainer)
                        {
                            if (t != null && t.thingIDNumber == id) return t;
                        }
                    }
                    if (p.apparel?.WornApparel != null)
                    {
                        for (int a = 0; a < p.apparel.WornApparel.Count; a++)
                        {
                            Apparel ap = p.apparel.WornApparel[a];
                            if (ap != null && ap.thingIDNumber == id) return ap;
                        }
                    }
                    if (p.equipment?.AllEquipmentListForReading != null)
                    {
                        List<ThingWithComps> eq = p.equipment.AllEquipmentListForReading;
                        for (int e = 0; e < eq.Count; e++)
                        {
                            if (eq[e] != null && eq[e].thingIDNumber == id) return eq[e];
                        }
                    }
                }
            }
            return null;
        }
    }
}
