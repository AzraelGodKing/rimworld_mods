using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DateNight
{
    /// <summary>
    /// Per-couple memory of where good dates happened. High scores pull the next
    /// date back; a ruined evening can sour the place.
    /// </summary>
    public static class DateNightVenues
    {
        private const float PreferScore = 1.5f;
        private const float OurSpotScore = 2.5f;
        private const float MinScore = -5f;
        private const float MaxScore = 10f;

        private static List<FavoriteVenue> venues = new List<FavoriteVenue>();

        public static void ExposeData()
        {
            Scribe_Collections.Look(ref venues, "dateNightFavoriteVenues", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && venues == null)
            {
                venues = new List<FavoriteVenue>();
            }
        }

        public static bool Remembering
        {
            get { return DateNightMod.Settings == null || DateNightMod.Settings.rememberFavoriteSpot; }
        }

        public static FavoriteVenue Get(Pawn a, Pawn b)
        {
            if (a == null || b == null || venues == null)
            {
                return null;
            }
            long key = DateNightActivities.CoupleKey(a, b);
            for (int i = 0; i < venues.Count; i++)
            {
                if (venues[i] != null && venues[i].CoupleKey == key)
                {
                    return venues[i];
                }
            }
            return null;
        }

        public static bool TryGetPreferredRoot(Pawn pawn, Pawn partner, out LocalTargetInfo root)
        {
            root = LocalTargetInfo.Invalid;
            if (!Remembering)
            {
                return false;
            }

            FavoriteVenue fav = Get(pawn, partner);
            if (fav == null || fav.score < PreferScore)
            {
                return false;
            }

            Map map = pawn?.Map;
            if (map == null || fav.mapId != map.uniqueID)
            {
                return false;
            }

            LocalTargetInfo resolved = fav.Resolve(map, pawn, partner);
            if (!resolved.IsValid)
            {
                return false;
            }
            root = resolved;
            return true;
        }

        public static bool IsAtFavorite(Pawn pawn, Pawn partner, LocalTargetInfo spot)
        {
            FavoriteVenue fav = Get(pawn, partner);
            if (fav == null || pawn?.Map == null || fav.score < PreferScore)
            {
                return false;
            }
            if (fav.mapId != pawn.Map.uniqueID)
            {
                return false;
            }
            IntVec3 cell = spot.IsValid ? spot.Cell : pawn.Position;
            return fav.Near(cell);
        }

        public static void NotifyFinished(Pawn pawn, Pawn partner, LocalTargetInfo spot, ThoughtDef quality)
        {
            if (!Remembering || pawn?.Map == null || partner == null)
            {
                return;
            }

            float delta = 0.75f;
            if (quality == DateNightDefOf.DateNight_DateWonderful)
            {
                delta = 2.5f;
            }
            else if (quality == DateNightDefOf.DateNight_DateAwkward)
            {
                delta = -0.4f;
            }
            if (DateNightAnniversaries.IsAnniversaryToday(pawn, partner))
            {
                delta += 1f;
            }

            FavoriteVenue fav = GetOrCreate(pawn, partner, pawn.Map, spot);
            bool wasFavorite = fav.score >= PreferScore && fav.Near(spot.IsValid ? spot.Cell : pawn.Position);
            fav.Capture(pawn.Map, spot);
            fav.score = Clamp(fav.score + delta);

            if (wasFavorite && fav.score >= OurSpotScore && DateNightDefOf.DateNight_OurSpot != null)
            {
                TryGain(pawn, partner, DateNightDefOf.DateNight_OurSpot);
            }
        }

        public static void NotifyRuined(Pawn pawn, Pawn partner, LocalTargetInfo spot)
        {
            if (!Remembering || pawn?.Map == null || partner == null)
            {
                return;
            }

            FavoriteVenue fav = GetOrCreate(pawn, partner, pawn.Map, spot);
            bool wasHere = fav.Near(spot.IsValid ? spot.Cell : pawn.Position);
            fav.Capture(pawn.Map, spot);
            fav.score = Clamp(fav.score - 2.5f);
            if (wasHere && fav.score < 0f && DateNightDefOf.DateNight_VenueSoured != null)
            {
                TryGain(pawn, partner, DateNightDefOf.DateNight_VenueSoured);
            }
        }

        public static string DebugDescribe(Pawn pawn)
        {
            Pawn partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, allowDead: false);
            FavoriteVenue fav = Get(pawn, partner);
            if (fav == null)
            {
                return "none";
            }
            return $"score={fav.score:0.0} thing={fav.thingId} cell={fav.cell} map={fav.mapId}";
        }

        private static FavoriteVenue GetOrCreate(Pawn a, Pawn b, Map map, LocalTargetInfo spot)
        {
            FavoriteVenue existing = Get(a, b);
            if (existing != null)
            {
                return existing;
            }

            var created = new FavoriteVenue
            {
                pawnA = a.thingIDNumber,
                pawnB = b.thingIDNumber,
                mapId = map.uniqueID
            };
            created.Capture(map, spot);
            venues.Add(created);
            return created;
        }

        private static void TryGain(Pawn pawn, Pawn other, ThoughtDef def)
        {
            if (pawn?.needs?.mood?.thoughts?.memories == null || def == null || other == null)
            {
                return;
            }
            if (pawn.ageTracker == null || !pawn.ageTracker.Adult || !pawn.DevelopmentalStage.Adult())
            {
                return;
            }
            pawn.needs.mood.thoughts.memories.TryGainMemory(def, other);
        }

        private static float Clamp(float v)
        {
            if (v < MinScore)
            {
                return MinScore;
            }
            if (v > MaxScore)
            {
                return MaxScore;
            }
            return v;
        }
    }

    public class FavoriteVenue : IExposable
    {
        public int pawnA;
        public int pawnB;
        public int mapId;
        public int thingId;
        public IntVec3 cell = IntVec3.Invalid;
        public float score;

        public long CoupleKey
        {
            get { return DateNightActivities.CoupleKey(pawnA, pawnB); }
        }

        public void Capture(Map map, LocalTargetInfo spot)
        {
            mapId = map.uniqueID;
            if (spot.HasThing && spot.Thing != null && !spot.Thing.Destroyed)
            {
                Thing root = TableOrBuilding(spot.Thing) ?? spot.Thing;
                thingId = root.thingIDNumber;
                cell = root.Position;
                return;
            }

            thingId = 0;
            cell = spot.IsValid ? spot.Cell : IntVec3.Invalid;
        }

        public LocalTargetInfo Resolve(Map map, Pawn pawn, Pawn partner)
        {
            if (thingId != 0)
            {
                Thing thing = FindThing(map, thingId);
                if (thing != null && DateNightActivities.BothCanReachPublic(pawn, partner, thing))
                {
                    return thing;
                }
            }

            if (cell.IsValid && cell.InBounds(map) && cell.Standable(map)
                && pawn.CanReach(cell, PathEndMode.OnCell, Danger.Some)
                && (partner == null || partner.Map != map
                    || partner.CanReach(cell, PathEndMode.OnCell, Danger.Some)))
            {
                return cell;
            }
            return LocalTargetInfo.Invalid;
        }

        public bool Near(IntVec3 other)
        {
            if (!cell.IsValid || !other.IsValid)
            {
                return false;
            }
            return cell.DistanceToSquared(other) <= 25;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnA, "pawnA");
            Scribe_Values.Look(ref pawnB, "pawnB");
            Scribe_Values.Look(ref mapId, "mapId");
            Scribe_Values.Look(ref thingId, "thingId");
            Scribe_Values.Look(ref cell, "cell");
            Scribe_Values.Look(ref score, "score", 0f);
        }

        private static Thing TableOrBuilding(Thing thing)
        {
            if (thing is Building)
            {
                return thing;
            }
            return thing.Position.GetEdifice(thing.Map);
        }

        private static Thing FindThing(Map map, int id)
        {
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (building.thingIDNumber == id)
                {
                    return building;
                }
            }
            return null;
        }
    }
}
