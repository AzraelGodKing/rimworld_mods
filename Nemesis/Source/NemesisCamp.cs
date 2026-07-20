using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Nemesis
{
    /// <summary>World marker for a real or false-lead nemesis camp. Resolved on caravan visit (no map gen).</summary>
    public class WorldObject_NemesisCamp : WorldObject
    {
        public bool isReal;
        public bool isTrap;
        public int placedTick;

        public override string Label
        {
            get
            {
                GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
                if (comp?.Data?.active == true && !string.IsNullOrEmpty(comp.Data.nemesisName))
                    return "Nemesis_Camp_Label".Translate(comp.Data.nemesisName);
                return "Nemesis_Camp_LabelGeneric".Translate();
            }
        }

        public override string GetInspectString()
        {
            return "Nemesis_Camp_Inspect".Translate();
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            foreach (FloatMenuOption opt in base.GetFloatMenuOptions(caravan))
                yield return opt;

            yield return new FloatMenuOption(
                "Nemesis_Camp_Investigate".Translate(Label),
                () => caravan.pather.StartPath(Tile, new CaravanArrivalAction_VisitNemesisCamp(this)));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref isReal, "isReal", false);
            Scribe_Values.Look(ref isTrap, "isTrap", false);
            Scribe_Values.Look(ref placedTick, "placedTick", 0);
        }
    }

    public class CaravanArrivalAction_VisitNemesisCamp : CaravanArrivalAction
    {
        private WorldObject_NemesisCamp camp;

        public CaravanArrivalAction_VisitNemesisCamp()
        {
        }

        public CaravanArrivalAction_VisitNemesisCamp(WorldObject_NemesisCamp camp)
        {
            this.camp = camp;
        }

        public override string Label => "Nemesis_Camp_Investigate".Translate(camp?.Label ?? "camp");
        public override string ReportString => "Nemesis_Camp_Investigating".Translate();

        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport baseReport = base.StillValid(caravan, destinationTile);
            if (!baseReport) return baseReport;
            if (camp == null || camp.Destroyed) return false;
            return camp.Tile == destinationTile;
        }

        public override void Arrived(Caravan caravan)
        {
            NemesisCampUtility.ResolveVisit(caravan, camp);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref camp, "camp");
        }
    }

    /// <summary>Progressive intel scraps → last known tile → camp confront (real / empty / trap).</summary>
    public static class NemesisCampUtility
    {
        public const int MaxIntel = 3;

        public static void TickCaravanTracking(NemesisData data)
        {
            if (data == null || !data.active) return;
            if (data.Phase < NemesisHuntPhase.Testing) return;
            if (Find.TickManager.TicksGame < data.nextCaravanTrackTick) return;

            data.nextCaravanTrackTick = Find.TickManager.TicksGame + Rand.RangeInclusive(90000, 180000);
            if (!Rand.Chance(0.35f)) return;
            TryTrackEncounter(data);
        }

        /// <summary>Immediate track beat used by CaravanHarass / debug.</summary>
        public static bool TryTrackEncounter(NemesisData data)
        {
            Caravan target = FindPlayerCaravan();
            if (target == null) return false;
            if (Rand.Chance(0.55f))
                DropAbandonedCache(data, target);
            else
                SoftAmbush(data, target);
            return true;
        }

        private static Caravan FindPlayerCaravan()
        {
            Caravan target = null;
            List<Caravan> caravans = Find.WorldObjects.Caravans;
            for (int i = 0; i < caravans.Count; i++)
            {
                Caravan c = caravans[i];
                if (c == null || c.Faction != Faction.OfPlayer || c.PawnsListForReading.Count == 0) continue;
                if (c.pather != null && c.pather.Moving)
                    return c;
                if (target == null) target = c;
            }
            return target;
        }

        private static void DropAbandonedCache(NemesisData data, Caravan caravan)
        {
            GiveToCaravan(caravan, NemesisEvidence.MakeCallingCard(data));
            ThingDef silver = ThingDefOf.Silver;
            if (silver != null)
            {
                Thing pile = ThingMaker.MakeThing(silver);
                pile.stackCount = Rand.RangeInclusive(15, 45);
                GiveToCaravan(caravan, pile);
            }

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_CacheTitle".Translate(data.nemesisName),
                "Nemesis_Letter_CacheBody".Translate(data.nemesisName, NemesisTaunts.TargetPhrase(data), caravan.LabelCap),
                LetterDefOf.NeutralEvent,
                caravan);

            TryAdvanceIntel(data, forceLetter: false);
        }

        private static void SoftAmbush(NemesisData data, Caravan caravan)
        {
            List<Pawn> pawns = caravan.PawnsListForReading;
            Pawn victim = null;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p != null && !p.Dead && p.RaceProps.Humanlike)
                {
                    victim = p;
                    break;
                }
            }
            if (victim?.health != null)
            {
                BodyPartRecord part = victim.health.hediffSet.GetRandomNotMissingPart(
                    DamageDefOf.Bullet, BodyPartHeight.Undefined, BodyPartDepth.Outside);
                if (part != null)
                    victim.TakeDamage(new DamageInfo(DamageDefOf.Bullet, Rand.Range(4f, 9f), 0f, -1f, null, part));
            }

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_TrackAmbushTitle".Translate(data.nemesisName),
                "Nemesis_Letter_TrackAmbushBody".Translate(data.nemesisName, NemesisTaunts.TargetPhrase(data), caravan.LabelCap),
                LetterDefOf.ThreatSmall,
                caravan);
            data.aggressionLevel = Mathf.Min(data.aggressionLevel + 0.15f, 10f);
        }

        /// <summary>Raise intel 0→1 scraps, 1→2 last tile, 2→3 place camp. Returns true if advanced.</summary>
        public static bool TryAdvanceIntel(NemesisData data, bool forceLetter = true)
        {
            if (data == null || !data.active) return false;
            if (data.intelLevel >= MaxIntel) return false;

            data.intelLevel++;
            if (data.intelLevel == 1)
            {
                if (forceLetter)
                {
                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_Intel1Title".Translate(data.nemesisName),
                        "Nemesis_Letter_Intel1Body".Translate(data.nemesisName, NemesisTaunts.TargetPhrase(data)),
                        LetterDefOf.NeutralEvent);
                }
                return true;
            }

            if (data.intelLevel == 2)
            {
                RememberNearbyTile(data);
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_Intel2Title".Translate(data.nemesisName),
                    "Nemesis_Letter_Intel2Body".Translate(
                        data.nemesisName, NemesisTaunts.TargetPhrase(data), TileLabel(data.lastKnownTile)),
                    LetterDefOf.ThreatSmall);
                return true;
            }

            return PlaceCamp(data);
        }

        private static void RememberNearbyTile(NemesisData data)
        {
            if (TileFinder.TryFindNewSiteTile(out PlanetTile site))
                data.lastKnownTile = site.tileId;
            else
                data.lastKnownTile = -1;
        }

        public static bool PlaceCamp(NemesisData data)
        {
            if (data == null || data.campWorldObjectId >= 0) return false;

            PlanetTile tile = PlanetTile.Invalid;
            if (data.lastKnownTile >= 0)
                tile = new PlanetTile(data.lastKnownTile);
            if (!tile.Valid && !TileFinder.TryFindNewSiteTile(out tile))
                return false;

            WorldObjectDef def = DefDatabase<WorldObjectDef>.GetNamedSilentFail("Nemesis_CampSite");
            if (def == null) return false;

            WorldObject_NemesisCamp camp = (WorldObject_NemesisCamp)WorldObjectMaker.MakeWorldObject(def);
            camp.Tile = tile;
            if (data.faction != null)
                camp.SetFaction(data.faction);

            float roll = Rand.Value;
            camp.isReal = roll < 0.45f;
            camp.isTrap = !camp.isReal && roll >= 0.70f;
            camp.placedTick = Find.TickManager.TicksGame;
            Find.WorldObjects.Add(camp);
            camp.GetComponent<TimeoutComp>()?.StartTimeout(20 * GenDate.TicksPerDay);

            data.campWorldObjectId = camp.ID;
            data.campIsReal = camp.isReal;
            data.campIsTrap = camp.isTrap;
            data.lastKnownTile = tile.tileId;
            data.intelLevel = MaxIntel;

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_Intel3Title".Translate(data.nemesisName),
                "Nemesis_Letter_Intel3Body".Translate(
                    data.nemesisName, NemesisTaunts.TargetPhrase(data), TileLabel(tile.tileId)),
                LetterDefOf.ThreatSmall,
                camp);
            return true;
        }

        public static void ResolveVisit(Caravan caravan, WorldObject_NemesisCamp camp)
        {
            if (caravan == null || camp == null || camp.Destroyed) return;
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            NemesisData data = comp?.Data;
            if (data == null || !data.active)
            {
                camp.Destroy();
                return;
            }

            data.campResolved = true;
            data.campWorldObjectId = -1;

            if (camp.isTrap)
            {
                SoftAmbush(data, caravan);
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_CampTrapTitle".Translate(data.nemesisName),
                    "Nemesis_Letter_CampTrapBody".Translate(data.nemesisName, NemesisTaunts.TargetPhrase(data)),
                    LetterDefOf.ThreatBig,
                    caravan);
                data.aggressionLevel = Mathf.Min(data.aggressionLevel + 0.4f, 10f);
            }
            else if (camp.isReal)
            {
                GiveToCaravan(caravan, NemesisEvidence.MakeCallingCard(data));
                ThingDef silver = ThingDefOf.Silver;
                if (silver != null)
                {
                    Thing pile = ThingMaker.MakeThing(silver);
                    pile.stackCount = Rand.RangeInclusive(80, 180);
                    GiveToCaravan(caravan, pile);
                }
                ThingDef medicine = DefDatabase<ThingDef>.GetNamedSilentFail("MedicineIndustrial")
                    ?? ThingDefOf.MedicineHerbal;
                if (medicine != null)
                {
                    Thing med = ThingMaker.MakeThing(medicine);
                    med.stackCount = Rand.RangeInclusive(2, 5);
                    GiveToCaravan(caravan, med);
                }

                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_CampRealTitle".Translate(data.nemesisName),
                    "Nemesis_Letter_CampRealBody".Translate(data.nemesisName, NemesisTaunts.TargetPhrase(data)),
                    LetterDefOf.PositiveEvent,
                    caravan);
                data.aggressionLevel = Mathf.Min(data.aggressionLevel + 0.5f, 10f);
                data.nextActionTick = Find.TickManager.TicksGame + 30000;
            }
            else
            {
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_CampEmptyTitle".Translate(data.nemesisName),
                    "Nemesis_Letter_CampEmptyBody".Translate(data.nemesisName, NemesisTaunts.TargetPhrase(data)),
                    LetterDefOf.NeutralEvent,
                    caravan);
                data.aggressionLevel = Mathf.Min(data.aggressionLevel + 0.2f, 10f);
            }

            camp.Destroy();
        }

        public static void ClearCamp(NemesisData data)
        {
            if (data == null || data.campWorldObjectId < 0) return;
            List<WorldObject> all = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < all.Count; i++)
            {
                WorldObject wo = all[i];
                if (wo != null && wo.ID == data.campWorldObjectId)
                {
                    wo.Destroy();
                    break;
                }
            }
            data.campWorldObjectId = -1;
        }

        private static bool GiveToCaravan(Caravan caravan, Thing thing)
        {
            if (caravan == null || thing == null) return false;
            List<Pawn> pawns = caravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p?.inventory?.innerContainer == null) continue;
                if (p.inventory.innerContainer.TryAdd(thing, canMergeWithExistingStacks: true))
                    return true;
            }
            return false;
        }

        private static string TileLabel(int tileId)
        {
            if (tileId < 0) return "Nemesis_Phrase_YourColony".Translate();
            try
            {
                PlanetTile tile = new PlanetTile(tileId);
                Tile t = Find.WorldGrid[tile];
                return t.PrimaryBiome?.label ?? ("tile " + tileId);
            }
            catch
            {
                return "tile " + tileId;
            }
        }
    }
}
