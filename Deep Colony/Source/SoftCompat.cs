using System;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// Fail-open DLC / sibling-mod probes. No project references.
    /// </summary>
    public static class SoftCompat
    {
        public const string HomesteaderPackage = "AzraelGodKing.Homesteader";
        public const string DateNightPackage = "azraelgodking.DateNight";
        public const string StrataPackage = "AzraelGodKing.Strata";
        public const string StormproofPackage = "AzraelGodKing.Stormproof";
        public const string Despicable2Package = "DCSzar.Despicable2.Core";
        public const string RimPactsPackage = "wowgag.RimPacts";

        public static bool HomesteaderLoaded => ModsConfig.IsActive(HomesteaderPackage);
        public static bool DateNightLoaded => ModsConfig.IsActive(DateNightPackage);
        public static bool StrataLoaded => ModsConfig.IsActive(StrataPackage);
        public static bool StormproofLoaded => ModsConfig.IsActive(StormproofPackage);
        public static bool Despicable2Loaded => ModsConfig.IsActive(Despicable2Package);
        public static bool RimPactsLoaded => ModsConfig.IsActive(RimPactsPackage);

        public static bool IsHomesteaderFood(Thing food)
        {
            if (food?.def?.defName == null) return false;
            if (!food.def.IsIngestible) return false;
            return food.def.defName.StartsWith("Homesteader_");
        }

        public static bool HasGrandChef(Pawn pawn)
        {
            var comp = pawn?.TryGetComp<Comp_DeepColony>();
            return comp != null && comp.unlockedPerkDefNames != null
                && comp.unlockedPerkDefNames.Contains("DC_Perk_GrandChef");
        }

        public static bool AreLovePartners(Pawn a, Pawn b)
        {
            if (a?.relations == null || b == null) return false;
            return LovePartnerRelationUtility.LovePartnerRelationExists(a, b);
        }

        public static int RoyalTitleSeniority(Pawn pawn)
        {
            if (!ModsConfig.RoyaltyActive || pawn?.royalty == null) return 0;
            RoyalTitle title = pawn.royalty.MostSeniorTitle;
            return title?.def?.seniority ?? 0;
        }

        public static bool HasAnyRoyalTitle(Pawn pawn)
        {
            return RoyalTitleSeniority(pawn) > 0;
        }

        public static bool MapHasIonStorm(Map map)
        {
            if (map?.GameConditionManager == null || !StormproofLoaded) return false;
            GameConditionDef ion = DefDatabase<GameConditionDef>.GetNamedSilentFail("Stormproof_IonStorm");
            GameConditionDef dry = DefDatabase<GameConditionDef>.GetNamedSilentFail("Stormproof_DryLightning");
            if (ion != null && map.GameConditionManager.ConditionIsActive(ion)) return true;
            if (dry != null && map.GameConditionManager.ConditionIsActive(dry)) return true;
            return false;
        }

        public static bool IsAnomalyEntity(Thing instigator)
        {
            if (!ModsConfig.AnomalyActive || instigator == null) return false;
            if (instigator.def?.modContentPack != null
                && instigator.def.modContentPack.PackageId != null
                && instigator.def.modContentPack.PackageId.IndexOf("Anomaly", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            Pawn p = instigator as Pawn;
            if (p == null) return false;
            string kind = p.kindDef?.defName ?? "";
            if (kind.IndexOf("Revenant", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (kind.IndexOf("Sightstealer", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (kind.IndexOf("Nociosphere", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (kind.IndexOf("Metalhorror", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (kind.IndexOf("Shambler", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (kind.IndexOf("Ghoul", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (kind.IndexOf("Noctol", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (kind.IndexOf("Gorehulk", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        public static bool IsOdysseyNonSurface(Map map)
        {
            if (!OdysseyActive || map == null) return false;
            string biome = map.Biome?.defName ?? "";
            if (biome.IndexOf("Orbit", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (biome.IndexOf("Space", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            string gen = map.generatorDef?.defName ?? "";
            if (gen.IndexOf("Orbit", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (gen.IndexOf("Gravship", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        public static bool OdysseyActive =>
            ModsConfig.IsActive("Ludeon.RimWorld.Odyssey");

        public static bool PlayerIdeoHasPrecept(string preceptDefName)
        {
            if (!ModsConfig.IdeologyActive) return false;
            if (preceptDefName.NullOrEmpty()) return false;
            Ideo ideo = Faction.OfPlayer?.ideos?.PrimaryIdeo;
            if (ideo?.PreceptsListForReading == null) return false;
            for (int i = 0; i < ideo.PreceptsListForReading.Count; i++)
            {
                Precept p = ideo.PreceptsListForReading[i];
                if (p?.def != null && p.def.defName == preceptDefName)
                    return true;
            }
            return false;
        }
    }
}
