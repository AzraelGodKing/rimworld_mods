using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Wardrobe
{
    public static class WardrobeUtility
    {
        public const int SwapCooldownTicks = 600; // 10s
        public const int ThinkInterval = 60;

        public static GameComponent_Wardrobe Comp =>
            Current.Game?.GetComponent<GameComponent_Wardrobe>();

        public static bool IsManaged(Pawn pawn)
        {
            WardrobePawnState state = Comp?.GetState(pawn, create: false);
            return state != null && state.IsManaged;
        }

        public static WorkTypeDef CookWorkType() =>
            DefDatabase<WorkTypeDef>.GetNamedSilentFail("Cooking")
            ?? DefDatabase<WorkTypeDef>.GetNamedSilentFail("Cook");

        public static WorkTypeDef DoctorWorkType() =>
            DefDatabase<WorkTypeDef>.GetNamedSilentFail("Doctor");

        public static WorkTypeDef AnimalsWorkType() =>
            DefDatabase<WorkTypeDef>.GetNamedSilentFail("Handling")
            ?? DefDatabase<WorkTypeDef>.GetNamedSilentFail("Animals");

        public static WardrobeTrigger DesiredTrigger(Pawn pawn)
        {
            if (pawn?.timetable == null)
            {
                return WardrobeTrigger.None;
            }

            WardrobePawnState state = Comp?.GetState(pawn, create: false);
            if (state == null || !state.AnyEnabled)
            {
                return WardrobeTrigger.None;
            }

            if (state.sleepEnabled
                && pawn.timetable.CurrentAssignment == TimeAssignmentDefOf.Sleep)
            {
                return WardrobeTrigger.Sleep;
            }

            WorkTypeDef work = pawn.CurJob?.workGiverDef?.workType;
            if (work != null)
            {
                WorkTypeDef cook = CookWorkType();
                if (state.cookEnabled && cook != null && work == cook)
                {
                    return WardrobeTrigger.Cook;
                }

                WorkTypeDef doctor = DoctorWorkType();
                if (state.doctorEnabled && doctor != null && work == doctor)
                {
                    return WardrobeTrigger.Doctor;
                }

                WorkTypeDef animals = AnimalsWorkType();
                if (state.animalsEnabled && animals != null && work == animals)
                {
                    return WardrobeTrigger.Animals;
                }
            }

            return WardrobeTrigger.None;
        }

        public static ApparelPolicy FindPolicy(int policyId)
        {
            if (policyId < 0 || Current.Game?.outfitDatabase == null)
            {
                return null;
            }

            List<ApparelPolicy> all = Current.Game.outfitDatabase.AllOutfits;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].id == policyId)
                {
                    return all[i];
                }
            }

            return null;
        }

        public static Zone_Stockpile FindStockpile(Map map, int zoneId)
        {
            if (map?.zoneManager == null || zoneId < 0)
            {
                return null;
            }

            foreach (Zone z in map.zoneManager.AllZones)
            {
                if (z is Zone_Stockpile stock && stock.ID == zoneId)
                {
                    return stock;
                }
            }

            return null;
        }

        public static List<Zone_Stockpile> AllStockpiles(Map map)
        {
            List<Zone_Stockpile> list = new List<Zone_Stockpile>();
            if (map?.zoneManager == null)
            {
                return list;
            }

            foreach (Zone z in map.zoneManager.AllZones)
            {
                if (z is Zone_Stockpile stock)
                {
                    list.Add(stock);
                }
            }

            return list;
        }

        public static void CaptureSnapshot(Pawn pawn, WardrobePawnState state)
        {
            state.snapshotThingIds.Clear();
            state.snapshotDefNames.Clear();
            if (pawn?.apparel?.WornApparel == null)
            {
                return;
            }

            foreach (Apparel a in pawn.apparel.WornApparel)
            {
                if (a == null)
                {
                    continue;
                }

                state.snapshotThingIds.Add(a.thingIDNumber);
                state.snapshotDefNames.Add(a.def.defName);
            }
        }

        public static List<Apparel> FindPolicyApparelInStockpile(
            Pawn pawn, Zone_Stockpile stock, ApparelPolicy policy, int max = 8)
        {
            List<Apparel> result = new List<Apparel>();
            if (pawn?.Map == null || stock == null || policy?.filter == null)
            {
                return result;
            }

            HashSet<ApparelLayerDef> claimedLayers = new HashSet<ApparelLayerDef>();
            foreach (IntVec3 cell in stock.Cells)
            {
                List<Thing> things = pawn.Map.thingGrid.ThingsListAtFast(cell);
                for (int i = 0; i < things.Count; i++)
                {
                    if (!(things[i] is Apparel apparel))
                    {
                        continue;
                    }

                    if (!apparel.def.IsApparel || !policy.filter.Allows(apparel))
                    {
                        continue;
                    }

                    if (!ApparelUtility.HasPartsToWear(pawn, apparel.def))
                    {
                        continue;
                    }

                    if (!pawn.CanReserveAndReach(apparel, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        continue;
                    }

                    bool layerConflict = false;
                    if (apparel.def.apparel?.layers != null)
                    {
                        foreach (ApparelLayerDef layer in apparel.def.apparel.layers)
                        {
                            if (claimedLayers.Contains(layer))
                            {
                                layerConflict = true;
                                break;
                            }
                        }
                    }

                    if (layerConflict)
                    {
                        continue;
                    }

                    result.Add(apparel);
                    if (apparel.def.apparel?.layers != null)
                    {
                        foreach (ApparelLayerDef layer in apparel.def.apparel.layers)
                        {
                            claimedLayers.Add(layer);
                        }
                    }

                    if (result.Count >= max)
                    {
                        return result;
                    }
                }
            }

            return result;
        }

        public static void DropConflicting(Pawn pawn, Apparel incoming)
        {
            if (pawn?.apparel == null || incoming == null)
            {
                return;
            }

            List<Apparel> worn = pawn.apparel.WornApparel.ToList();
            for (int i = 0; i < worn.Count; i++)
            {
                Apparel have = worn[i];
                if (!ApparelUtility.CanWearTogether(incoming.def, have.def, pawn.RaceProps.body))
                {
                    pawn.apparel.TryDrop(have, out _, pawn.Position, true);
                }
            }
        }

        public static bool TryWear(Pawn pawn, Apparel apparel)
        {
            if (pawn?.apparel == null || apparel == null || apparel.Destroyed)
            {
                return false;
            }

            DropConflicting(pawn, apparel);
            if (apparel.Spawned)
            {
                apparel.DeSpawn();
            }

            pawn.apparel.Wear(apparel, true);
            return true;
        }

        public static Thing FindThingById(Map map, int id)
        {
            if (map == null || id <= 0)
            {
                return null;
            }

            foreach (Thing t in map.listerThings.AllThings)
            {
                if (t.thingIDNumber == id)
                {
                    return t;
                }
            }

            return null;
        }

    }
}
