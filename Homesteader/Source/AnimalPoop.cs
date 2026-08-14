using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Homesteader
{
    /// <summary>
    /// Fresh manure expires so unhauled piles do not clog maps.
    /// Outdoor piles age twice as fast as roofed ones.
    /// </summary>
    public class CompProperties_AnimalPoopExpire : CompProperties
    {
        public int lifespanTicks = 240000;

        public CompProperties_AnimalPoopExpire()
        {
            compClass = typeof(CompAnimalPoopExpire);
        }
    }

    public class CompAnimalPoopExpire : ThingComp
    {
        private int ageTicks;

        public CompProperties_AnimalPoopExpire Props => (CompProperties_AnimalPoopExpire)props;

        public override void CompTickRare()
        {
            if (!parent.Spawned)
            {
                return;
            }

            int step = 250;
            Map map = parent.Map;
            if (map != null && !parent.Position.Roofed(map))
            {
                step = 500;
            }

            ageTicks += step;
            if (ageTicks >= Props.lifespanTicks)
            {
                parent.Destroy(DestroyMode.Vanish);
            }
        }

        public override string CompInspectStringExtra()
        {
            int left = Props.lifespanTicks - ageTicks;
            if (left < 0)
            {
                left = 0;
            }

            return "Homesteader_PoopExpires".Translate(left.ToStringTicksToPeriod());
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref ageTicks, "homesteaderPoopAgeTicks", 0);
        }
    }

    public static class AnimalPoopUtility
    {
        public const string PoopDefName = "Homesteader_AnimalPoop";
        public const int MaxLoosePiles = 80;
        private const int RareTicksPerDay = 240;

        private static ThingDef cachedPoopDef;

        public static ThingDef PoopDef
        {
            get
            {
                if (cachedPoopDef == null)
                {
                    cachedPoopDef = DefDatabase<ThingDef>.GetNamedSilentFail(PoopDefName);
                }

                return cachedPoopDef;
            }
        }

        public static void TryDrop(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Destroyed)
            {
                return;
            }

            if (pawn.Suspended || pawn.InContainerEnclosed)
            {
                return;
            }

            RaceProperties race = pawn.RaceProps;
            if (race == null || !race.Animal || race.Humanlike || race.IsMechanoid || race.Insect)
            {
                return;
            }

            if (!race.EatsFood)
            {
                return;
            }

            ThingDef def = PoopDef;
            if (def == null)
            {
                return;
            }

            Map map = pawn.Map;
            if (map == null)
            {
                return;
            }

            if (CountSpawned(map, def) >= MaxLoosePiles)
            {
                return;
            }

            float body = Mathf.Clamp(pawn.BodySize, 0.2f, 4f);
            float dropsPerDay = 1.2f + body * 0.6f;
            float chance = dropsPerDay / RareTicksPerDay;
            if (pawn.Faction != Faction.OfPlayer)
            {
                chance *= 0.35f;
            }

            if (!Rand.Chance(chance))
            {
                return;
            }

            int count = Mathf.Clamp(Mathf.RoundToInt(body), 1, 4);
            IntVec3 cell = pawn.Position;
            if (!cell.InBounds(map))
            {
                return;
            }

            Thing existing = cell.GetFirstThing(map, def);
            if (existing != null && existing.stackCount < existing.def.stackLimit)
            {
                existing.stackCount = Mathf.Min(existing.def.stackLimit, existing.stackCount + count);
                return;
            }

            Thing poop = ThingMaker.MakeThing(def);
            poop.stackCount = count;
            GenPlace.TryPlaceThing(poop, cell, map, ThingPlaceMode.Near);
        }

        private static int CountSpawned(Map map, ThingDef def)
        {
            return map.listerThings.ThingsOfDef(def).Count;
        }
    }

    [HarmonyPatch]
    public static class Patch_Pawn_TickInterval_AnimalPoop
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Pawn), "TickInterval", new[] { typeof(int) });
        }

        public static void Postfix(Pawn __instance, int delta)
        {
            if (__instance == null || !__instance.IsHashIntervalTick(250, delta))
            {
                return;
            }

            AnimalPoopUtility.TryDrop(__instance);
        }
    }
}
