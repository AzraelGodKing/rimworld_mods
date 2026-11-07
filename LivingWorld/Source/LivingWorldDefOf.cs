using RimWorld;
using Verse;

namespace LivingWorld
{
    [DefOf]
    public static class LivingWorldDefOf
    {
        public static IncidentDef LivingWorld_Refugees;
        public static IncidentDef LivingWorld_Warband;
        public static WorldObjectDef LivingWorld_WarSite;
        public static WorldObjectDef LivingWorld_WayCamp;
        public static WorldObjectDef LivingWorld_TrafficCaravan;
        public static HediffDef LW_Hediff_RoadVeteran;
        public static LetterDef LivingWorld_WorldNews;

        static LivingWorldDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LivingWorldDefOf));
        }
    }
}
