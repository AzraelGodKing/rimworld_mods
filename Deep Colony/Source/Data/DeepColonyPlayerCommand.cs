#nullable enable
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// Every player UI action that changes the game goes through <see cref="Submit"/>,
    /// which queues it; <see cref="Drain"/> applies the queue on GameComponentTick.
    /// With the Multiplayer mod, Submit is a registered sync method
    /// (<see cref="DeepColonyMultiplayer"/>), so a click runs it on every client and
    /// the command applies on the same tick everywhere.
    /// </summary>
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
            TributeThing = 9,
            SwitchPerk = 10,
            SetHeir = 11,
            ForgetPerk = 12,
            RelearnPerk = 13
        }

        private struct Entry
        {
            public Kind kind;
            public int aId;
            public int bId;
            public string? s1;
            public string? s2;
        }

        // Static, so it would outlive a save: Reset() runs from the GameComp_DeepColony
        // constructor (new game and load) so a command queued before a load can't hit the
        // next save's pawns that reuse the same id numbers.
        private static List<Entry> queue = new List<Entry>();
        private static List<Entry> draining = new List<Entry>();

        // Public and primitive-only so Multiplayer can sync it. Don't call it from
        // simulation code; it is for player clicks.
        public static void Submit(byte kind, int aId, int bId, string? s1, string? s2)
        {
            if (kind > (byte)Kind.RelearnPerk) return;
            queue.Add(new Entry { kind = (Kind)kind, aId = aId, bId = bId, s1 = s1, s2 = s2 });
        }

        internal static void Reset()
        {
            queue.Clear();
            draining.Clear();
        }

        public static void EnqueueResumeCodex(Thing notebook)
        {
            if (notebook == null) return;
            Submit((byte)Kind.ResumeCodex, notebook.thingIDNumber, 0, null, null);
        }

        public static void EnqueueRecordCodex(Thing notebook)
        {
            if (notebook == null) return;
            Submit((byte)Kind.RecordCodex, notebook.thingIDNumber, 0, null, null);
        }

        public static void EnqueueDivorce(Pawn a, Pawn b)
        {
            if (a == null || b == null) return;
            Submit((byte)Kind.Divorce, a.thingIDNumber, b.thingIDNumber, null, null);
        }

        public static void EnqueueSetEnvoy(Pawn pawn, Faction faction)
        {
            if (pawn == null || faction == null) return;
            Submit((byte)Kind.SetEnvoy, pawn.thingIDNumber, faction.loadID, null, null);
        }

        public static void EnqueueClearEnvoy(Pawn pawn)
        {
            if (pawn == null) return;
            Submit((byte)Kind.ClearEnvoy, pawn.thingIDNumber, 0, null, null);
        }

        public static void EnqueueSetMentor(Pawn mentor, Pawn apprentice, SkillDef skill)
        {
            if (mentor == null || apprentice == null) return;
            Submit((byte)Kind.SetMentor, mentor.thingIDNumber, apprentice.thingIDNumber, skill?.defName, null);
        }

        public static void EnqueueClearMentor(Pawn mentor, Pawn apprentice)
        {
            if (mentor == null || apprentice == null) return;
            Submit((byte)Kind.ClearMentor, mentor.thingIDNumber, apprentice.thingIDNumber, null, null);
        }

        public static void EnqueueBeginRetrain(Pawn mentor, Pawn apprentice, PerkDef from, PerkDef to)
        {
            if (mentor == null || apprentice == null || from == null || to == null) return;
            Submit((byte)Kind.BeginRetrain, mentor.thingIDNumber, apprentice.thingIDNumber, from.defName, to.defName);
        }

        public static void EnqueueTributeFaction(Faction faction)
        {
            if (faction == null) return;
            Submit((byte)Kind.TributeFaction, 0, faction.loadID, null, null);
        }

        public static void EnqueueTributeThing(Thing thing, Faction faction)
        {
            if (thing == null || faction == null) return;
            Submit((byte)Kind.TributeThing, thing.thingIDNumber, faction.loadID, null, null);
        }

        public static void EnqueueSwitchPerk(Pawn pawn, PerkDef perk)
        {
            EnqueuePerk(Kind.SwitchPerk, pawn, perk);
        }

        public static void EnqueueForgetPerk(Pawn pawn, PerkDef perk)
        {
            EnqueuePerk(Kind.ForgetPerk, pawn, perk);
        }

        public static void EnqueueRelearnPerk(Pawn pawn, PerkDef perk)
        {
            EnqueuePerk(Kind.RelearnPerk, pawn, perk);
        }

        private static void EnqueuePerk(Kind kind, Pawn pawn, PerkDef perk)
        {
            if (pawn == null || perk == null) return;
            Submit((byte)kind, pawn.thingIDNumber, 0, perk.defName, null);
        }

        public static void EnqueueSetHeir(Pawn owner, Pawn heir)
        {
            if (owner == null) return;
            Submit((byte)Kind.SetHeir, owner.thingIDNumber, heir?.thingIDNumber ?? -1, null, null);
        }

        public static void Drain()
        {
            if (queue.Count == 0) return;
            // Swap buffers: a command that queues another lands in the fresh list
            // and runs next tick.
            (queue, draining) = (draining, queue);
            for (int i = 0; i < draining.Count; i++)
                Apply(draining[i]);
            draining.Clear();
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
                    Pawn? a = FindPawn(e.aId);
                    Pawn? b = FindPawn(e.bId);
                    if (a != null && b != null)
                        DivorceUtility.TryDivorce(a, b);
                    break;
                }
                case Kind.SetEnvoy:
                {
                    Pawn? pawn = FindPawn(e.aId);
                    Faction? faction = FindFaction(e.bId);
                    if (pawn != null && faction != null)
                        FactionEnvoyUtility.SetEnvoy(pawn, faction);
                    break;
                }
                case Kind.ClearEnvoy:
                {
                    Pawn? pawn = FindPawn(e.aId);
                    if (pawn != null)
                        FactionEnvoyUtility.ClearEnvoy(pawn);
                    break;
                }
                case Kind.SetMentor:
                {
                    Pawn? mentor = FindPawn(e.aId);
                    Pawn? apprentice = FindPawn(e.bId);
                    SkillDef? skill = e.s1.NullOrEmpty()
                        ? null
                        : DefDatabase<SkillDef>.GetNamedSilentFail(e.s1);
                    if (mentor != null && apprentice != null)
                        MentorshipUtility.SetMentorRelation(mentor, apprentice, skill);
                    break;
                }
                case Kind.ClearMentor:
                {
                    Pawn? mentor = FindPawn(e.aId);
                    Pawn? apprentice = FindPawn(e.bId);
                    if (mentor != null && apprentice != null)
                        MentorshipUtility.ClearMentorRelation(mentor, apprentice);
                    break;
                }
                case Kind.BeginRetrain:
                {
                    Pawn? mentor = FindPawn(e.aId);
                    Pawn? apprentice = FindPawn(e.bId);
                    PerkDef from = DefDatabase<PerkDef>.GetNamedSilentFail(e.s1);
                    PerkDef to = DefDatabase<PerkDef>.GetNamedSilentFail(e.s2);
                    if (mentor != null && apprentice != null && from != null && to != null)
                        MentorshipUtility.BeginRetrain(mentor, apprentice, from, to);
                    break;
                }
                case Kind.TributeFaction:
                {
                    Faction? faction = FindFaction(e.bId);
                    if (faction != null)
                        TributeUtility.TrySendTribute(faction);
                    break;
                }
                case Kind.TributeThing:
                {
                    Thing? thing = FindThing(e.aId);
                    Faction? faction = FindFaction(e.bId);
                    if (thing != null && faction != null)
                        TributeUtility.TrySendTributeThing(thing, faction);
                    break;
                }
                case Kind.SwitchPerk:
                case Kind.ForgetPerk:
                case Kind.RelearnPerk:
                    ApplyPerk(e);
                    break;
                case Kind.SetHeir:
                {
                    Pawn? owner = FindPawn(e.aId);
                    Pawn? heir = e.bId > 0 ? FindPawn(e.bId) : null;
                    if (owner != null && (e.bId <= 0 || heir != null))
                        EstateUtility.SetHeir(owner, heir);
                    break;
                }
            }
        }

        private static void ApplyPerk(Entry e)
        {
            Comp_DeepColony? comp = FindPawn(e.aId)?.TryGetComp<Comp_DeepColony>();
            PerkDef? perk = e.s1.NullOrEmpty() ? null : DefDatabase<PerkDef>.GetNamedSilentFail(e.s1);
            if (comp == null || perk == null) return;
            if (e.kind == Kind.SwitchPerk)
                comp.SwitchToPerk(perk);
            else if (e.kind == Kind.ForgetPerk)
                comp.ForgetPerk(perk);
            else
                comp.RelearnPerk(perk);
        }

        private static void ApplyResumeCodex(int notebookId)
        {
            Thing? notebook = FindThing(notebookId);
            CompCodexNotebook? comp = notebook?.TryGetComp<CompCodexNotebook>();
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
            Thing? notebook = FindThing(notebookId);
            CompCodexNotebook? comp = notebook?.TryGetComp<CompCodexNotebook>();
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

        private static Pawn? FindPawn(int id)
        {
            return PawnIdIndex.Find(id);
        }

        private static Faction? FindFaction(int loadId)
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

        private static Thing? FindThing(int id)
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
                    Thing? carried = p.carryTracker?.CarriedThing;
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
