using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Homesteader
{
    public class CompProperties_AgeInCellar : CompProperties
    {
        public int ticksPerStage = 180000; // 3 days
        public int maxStage = 2;

        public CompProperties_AgeInCellar()
        {
            compClass = typeof(CompAgeInCellar);
        }
    }

    public class CompAgeInCellar : ThingComp
    {
        public int ageTicks;
        public int stage;

        public CompProperties_AgeInCellar Props => (CompProperties_AgeInCellar)props;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref ageTicks, "homesteadAgeTicks");
            Scribe_Values.Look(ref stage, "homesteadAgeStage");
        }

        public override void PostSplitOff(Thing piece)
        {
            CompAgeInCellar other = piece?.TryGetComp<CompAgeInCellar>();
            if (other == null)
            {
                return;
            }

            other.ageTicks = ageTicks;
            other.stage = stage;
        }

        public override void PreAbsorbStack(Thing otherThing, int count)
        {
            CompAgeInCellar other = otherThing?.TryGetComp<CompAgeInCellar>();
            if (other == null)
            {
                return;
            }

            if (other.stage > stage)
            {
                stage = other.stage;
                ageTicks = other.ageTicks;
            }
            else if (other.stage == stage)
            {
                ageTicks = Mathf.Min(ageTicks, other.ageTicks);
            }
        }

        public override float GetStatOffset(StatDef stat)
        {
            if (stat != StatDefOf.MarketValue || stage <= 0)
            {
                return 0f;
            }

            return parent.def.GetStatValueAbstract(stat) * (0.18f * stage);
        }

        public override void CompTickRare()
        {
            if (HomesteaderMod.Settings == null || !HomesteaderMod.Settings.agingEnabled)
            {
                return;
            }

            if (parent?.Map == null || !parent.Spawned)
            {
                return;
            }

            float rate = CellarAgeRate(parent);
            if (rate <= 0f || stage >= Props.maxStage)
            {
                return;
            }

            ageTicks += (int)(250f * rate);
            int needed = Props.ticksPerStage;
            while (ageTicks >= needed && stage < Props.maxStage)
            {
                ageTicks -= needed;
                stage++;
            }
        }

        public override string CompInspectStringExtra()
        {
            if (stage <= 0)
            {
                return null;
            }

            return stage >= 2
                ? "Homesteader_AgedVintage".Translate()
                : "Homesteader_AgedReady".Translate();
        }

        private static float CellarAgeRate(Thing thing)
        {
            if (thing?.Position == null || thing.Map == null)
            {
                return 0f;
            }

            foreach (Thing t in thing.Map.thingGrid.ThingsListAt(thing.Position))
            {
                if (t?.def == null)
                {
                    continue;
                }

                if (t.def.defName == "Homesteader_RootCellar")
                {
                    return 1f;
                }

                if (t.def.defName == "Homesteader_Springhouse")
                {
                    return 0.7f;
                }

                if (t.def.defName == "Homesteader_Icehouse")
                {
                    return 0.35f;
                }
            }

            return 0f;
        }
    }

    [HarmonyPatch(typeof(Thought), nameof(Thought.MoodOffset))]
    public static class Patch_HomesteaderMoodScales
    {
        public static void Postfix(Thought __instance, ref float __result)
        {
            HomesteaderSettings settings = HomesteaderMod.Settings;
            if (settings == null || __instance?.def == null)
            {
                return;
            }

            if (Mathf.Approximately(settings.favoriteFoodMood, 1f)
                && Mathf.Approximately(settings.allergyFlareIntensity, 1f))
            {
                return;
            }

            string name = __instance.def.defName;
            if (name == "Homesteader_AteFavoriteFood")
            {
                __result *= settings.favoriteFoodMood;
            }
            else if (name == "Homesteader_AteAllergen")
            {
                __result *= settings.allergyFlareIntensity;
            }
        }
    }

    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.ThoughtsFromIngesting))]
    public static class Patch_AgedFoodThoughts
    {
        public static void Postfix(Pawn ingester, Thing foodSource, List<FoodUtility.ThoughtFromIngesting> __result)
        {
            if (ingester?.needs?.mood == null || __result == null)
            {
                return;
            }

            if (HomesteaderMod.Settings == null || !HomesteaderMod.Settings.agingEnabled)
            {
                return;
            }

            CompAgeInCellar age = foodSource?.TryGetComp<CompAgeInCellar>();
            if (age == null || age.stage <= 0)
            {
                return;
            }

            string thoughtName = age.stage >= 2 ? "Homesteader_AteVintagePreserve" : "Homesteader_AteAgedPreserve";
            ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamedSilentFail(thoughtName);
            if (thought == null)
            {
                return;
            }

            for (int i = 0; i < __result.Count; i++)
            {
                if (__result[i].thought == thought)
                {
                    return;
                }
            }

            __result.Add(new FoodUtility.ThoughtFromIngesting
            {
                thought = thought,
                fromPrecept = null
            });
        }
    }
}
