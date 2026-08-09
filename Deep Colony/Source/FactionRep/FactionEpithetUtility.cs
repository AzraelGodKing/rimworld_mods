using RimWorld;
using Verse;

namespace DeepColony
{
    public static class FactionEpithetUtility
    {
        public static string TryGetEpithet(Faction faction)
        {
            if (!DeepColonySettings.Get.enableFactionRep) return null;
            if (faction == null || faction.IsPlayer || faction.defeated) return null;

            // When attitude consequences are on, prefer attitude label as the epithet.
            if (DeepColonySettings.Get.enableAttitudeConsequences)
            {
                FactionAttitude att = FactionAttitudeUtility.GetAttitude(faction);
                if (att != FactionAttitude.Neutral)
                    return FactionAttitudeUtility.AttitudeLabel(att);
            }

            int goodwill = faction.GoodwillWith(Faction.OfPlayer);
            if (goodwill >= 75)
                return "DC_Epithet_TrustedPartners".Translate();
            if (goodwill >= 40)
                return "DC_Epithet_Cordial".Translate();
            if (goodwill <= -75)
                return "DC_Epithet_SwornEnemies".Translate();
            if (goodwill <= -40)
                return "DC_Epithet_DeepEnmity".Translate();
            return null;
        }
    }
}
