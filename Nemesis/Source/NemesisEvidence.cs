using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Nemesis
{
    /// <summary>Readable calling card / journal — shows scribed note text or a keyed fallback.</summary>
    public class CompUseEffect_NemesisNote : CompUseEffect
    {
        public string noteText;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref noteText, "noteText", null);
        }

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            string body = !string.IsNullOrEmpty(noteText)
                ? noteText
                : "Nemesis_Note_DefaultBody".Translate();
            string title = parent?.def?.defName == "Nemesis_Journal"
                ? "Nemesis_Note_JournalTitle".Translate()
                : "Nemesis_Note_CardTitle".Translate();
            Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.NeutralEvent, usedBy);
        }
    }

    /// <summary>Physical evidence helpers — calling cards, trophy theft, death journal.</summary>
    public static class NemesisEvidence
    {
        public static Thing MakeCallingCard(NemesisData data, string bodyOverride = null)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail("Nemesis_CallingCard");
            if (def == null) return null;
            Thing card = ThingMaker.MakeThing(def);
            CompUseEffect_NemesisNote note = card.TryGetComp<CompUseEffect_NemesisNote>();
            if (note != null)
            {
                note.noteText = bodyOverride
                    ?? "Nemesis_CallingCard_Body".Translate(
                        data.nemesisName ?? "Nemesis_Phrase_Someone".Translate(),
                        NemesisTaunts.TargetPhrase(data),
                        data.ArchetypeLabelKeyed());
            }
            return card;
        }

        public static Thing MakeJournal(NemesisData data)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail("Nemesis_Journal");
            if (def == null) return null;
            Thing journal = ThingMaker.MakeThing(def);
            CompUseEffect_NemesisNote note = journal.TryGetComp<CompUseEffect_NemesisNote>();
            if (note != null)
            {
                note.noteText = "Nemesis_Journal_Body".Translate(
                    data.nemesisName ?? "Nemesis_Phrase_Someone".Translate(),
                    NemesisTaunts.TargetPhrase(data),
                    data.ArchetypeLabelKeyed(),
                    data.harassmentCount,
                    data.escapeCount,
                    data.HuntDays);
            }
            return journal;
        }

        /// <summary>Drop a calling card near map edge. Letter-only if spawn fails.</summary>
        public static void DropCallingCard(NemesisData data, Map map, string letterTitleKey, string letterBodyKey)
        {
            if (data == null) return;
            Thing card = MakeCallingCard(data);
            IntVec3 spot = IntVec3.Invalid;
            if (map != null && card != null)
            {
                if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map) && !c.Fogged(map), map, 0f, out spot))
                    spot = CellFinder.RandomEdgeCell(map);
                try
                {
                    GenPlace.TryPlaceThing(card, spot, map, ThingPlaceMode.Near);
                }
                catch
                {
                    card = null;
                }
            }

            if (card != null && spot.IsValid && map != null)
            {
                Find.LetterStack.ReceiveLetter(
                    letterTitleKey.Translate(data.nemesisName),
                    letterBodyKey.Translate(data.nemesisName, NemesisTaunts.TargetPhrase(data)),
                    LetterDefOf.NeutralEvent,
                    new RimWorld.Planet.GlobalTargetInfo(spot, map));
            }
            else
            {
                Find.LetterStack.ReceiveLetter(
                    letterTitleKey.Translate(data.nemesisName),
                    letterBodyKey.Translate(data.nemesisName, NemesisTaunts.TargetPhrase(data))
                        + "\n\n" + "Nemesis_CallingCard_LetterOnly".Translate(),
                    LetterDefOf.NeutralEvent);
            }
        }

        /// <summary>Dead animal calling card near colony edge.</summary>
        public static void DropDeadAnimalCard(NemesisData data, Map map)
        {
            if (map == null || data == null)
            {
                DropCallingCard(data, map, "Nemesis_Letter_CallingCardTitle", "Nemesis_Letter_CallingCardBody");
                return;
            }

            PawnKindDef[] kinds =
            {
                DefDatabase<PawnKindDef>.GetNamedSilentFail("Rat"),
                DefDatabase<PawnKindDef>.GetNamedSilentFail("Hare"),
                DefDatabase<PawnKindDef>.GetNamedSilentFail("Squirrel"),
                PawnKindDefOf.SpaceRefugee, // last resort — skip if humanlike
            };

            PawnKindDef kind = null;
            for (int i = 0; i < kinds.Length; i++)
            {
                if (kinds[i] == null || kinds[i].RaceProps == null) continue;
                if (kinds[i].RaceProps.Humanlike) continue;
                kind = kinds[i];
                break;
            }

            if (kind == null)
            {
                DropCallingCard(data, map, "Nemesis_Letter_CallingCardTitle", "Nemesis_Letter_CallingCardBody");
                return;
            }

            try
            {
                if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map) && !c.Fogged(map), map, 0f, out IntVec3 edge))
                    edge = CellFinder.RandomEdgeCell(map);

                Pawn animal = PawnGenerator.GeneratePawn(kind, null);
                if (animal == null)
                {
                    DropCallingCard(data, map, "Nemesis_Letter_CallingCardTitle", "Nemesis_Letter_CallingCardBody");
                    return;
                }
                GenSpawn.Spawn(animal, edge, map);
                animal.Kill(null);
                Thing card = MakeCallingCard(data);
                if (card != null)
                    GenPlace.TryPlaceThing(card, edge, map, ThingPlaceMode.Near);

                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_DeadAnimalTitle".Translate(data.nemesisName),
                    "Nemesis_Letter_DeadAnimalBody".Translate(data.nemesisName, NemesisTaunts.TargetPhrase(data)),
                    LetterDefOf.ThreatSmall,
                    new RimWorld.Planet.GlobalTargetInfo(edge, map));
            }
            catch
            {
                DropCallingCard(data, map, "Nemesis_Letter_CallingCardTitle", "Nemesis_Letter_CallingCardBody");
            }
        }

        /// <summary>Steal a high-value art / weapon; scribe recovery data. Fail-open false.</summary>
        public static bool TryStealTrophy(NemesisData data, Map map)
        {
            if (data == null || map == null || data.stolenTrophyThingId > 0) return false;

            Thing best = null;
            float bestScore = 0f;
            List<Thing> haulables = map.listerThings?.ThingsInGroup(ThingRequestGroup.HaulableEver);
            if (haulables == null) return false;

            for (int i = 0; i < haulables.Count; i++)
            {
                Thing t = haulables[i];
                if (t == null || t.Destroyed || t.Faction != Faction.OfPlayer) continue;
                if (t is Pawn) continue;
                float score = ScoreTrophy(t);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = t;
                }
            }

            if (best == null || bestScore < 200f) return false;

            data.stolenTrophyThingId = best.thingIDNumber;
            data.stolenTrophyDefName = best.def.defName;
            data.stolenTrophyLabel = best.LabelNoCount;
            QualityCategory q;
            if (best.TryGetQuality(out q))
                data.stolenTrophyQuality = (int)q;
            else
                data.stolenTrophyQuality = -1;

            try
            {
                // Despawn carefully — destroy stack reference from map.
                if (best.Spawned)
                    best.DeSpawn(DestroyMode.Vanish);
                best.Destroy(DestroyMode.Vanish);
            }
            catch
            {
                data.stolenTrophyThingId = -1;
                data.stolenTrophyDefName = null;
                return false;
            }

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_TrophyStolenTitle".Translate(data.nemesisName),
                "Nemesis_Letter_TrophyStolenBody".Translate(
                    data.nemesisName, data.stolenTrophyLabel, NemesisTaunts.TargetPhrase(data)),
                LetterDefOf.ThreatSmall);
            return true;
        }

        private static float ScoreTrophy(Thing t)
        {
            float value = t.MarketValue * t.stackCount;
            if (t.def.IsArt) value *= 2.5f;
            CompQuality cq = t.TryGetComp<CompQuality>();
            if (cq != null && cq.Quality >= QualityCategory.Excellent)
                value *= 1.8f;
            if (t.def.IsWeapon && cq != null && cq.Quality >= QualityCategory.Excellent)
                value *= 2f;
            // Persona weapons (Royalty) — soft name check.
            string dn = t.def.defName ?? "";
            if (dn.IndexOf("Persona", System.StringComparison.OrdinalIgnoreCase) >= 0)
                value *= 3f;
            return value;
        }

        /// <summary>Restore stolen trophy near corpse / drop spot. Fail-open silent.</summary>
        public static void TryRestoreTrophy(NemesisData data, Map map, IntVec3 near)
        {
            if (data == null || string.IsNullOrEmpty(data.stolenTrophyDefName)) return;
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(data.stolenTrophyDefName);
            if (def == null || map == null) return;

            try
            {
                Thing thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
                if (thing == null) return;
                if (data.stolenTrophyQuality >= 0)
                {
                    CompQuality cq = thing.TryGetComp<CompQuality>();
                    cq?.SetQuality((QualityCategory)data.stolenTrophyQuality, ArtGenerationContext.Outsider);
                }
                IntVec3 cell = near.IsValid ? near : DropCellFinder.RandomDropSpot(map);
                GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
                data.stolenTrophyThingId = -1;
                data.stolenTrophyDefName = null;
                data.stolenTrophyLabel = null;
                data.stolenTrophyQuality = -1;
            }
            catch
            {
                // Fail-open.
            }
        }

        public static void DropJournalOnDeath(NemesisData data, Map map, IntVec3 near)
        {
            if (data == null) return;
            Thing journal = MakeJournal(data);
            if (journal == null || map == null)
            {
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_JournalTitle".Translate(data.nemesisName),
                    "Nemesis_Journal_Body".Translate(
                        data.nemesisName ?? "Nemesis_Phrase_Someone".Translate(),
                        NemesisTaunts.TargetPhrase(data),
                        data.ArchetypeLabelKeyed(),
                        data.harassmentCount,
                        data.escapeCount,
                        data.HuntDays),
                    LetterDefOf.NeutralEvent);
                return;
            }

            IntVec3 cell = near.IsValid ? near : DropCellFinder.RandomDropSpot(map);
            GenPlace.TryPlaceThing(journal, cell, map, ThingPlaceMode.Near);
            TryRestoreTrophy(data, map, cell);
        }
    }
}
