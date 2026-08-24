using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    public static class ConfidantUtility
    {
        private const int SessionsToBondDefault = 3;
        private const int SessionsToBondLovers = 2;

        public static int SessionsToBondFor(Pawn patient, Pawn counselor)
        {
            if (counselor != null && SoftCompat.DateNightLoaded
                && SoftCompat.AreLovePartners(patient, counselor))
                return SessionsToBondLovers;
            return SessionsToBondDefault;
        }

        public static void NotifyCounselSession(Pawn counselor, Pawn patient)
        {
            if (!DeepColonySettings.Get.enableTrauma) return;
            if (counselor == null || patient == null || counselor == patient) return;
            if (!counselor.RaceProps.Humanlike || !patient.RaceProps.Humanlike) return;

            var patientComp = patient.TryGetComp<Comp_DeepColony>();
            if (patientComp == null) return;

            int count = patientComp.IncrementCounselCount(counselor);
            if (count < SessionsToBondFor(patient, counselor)) return;
            if (patient.relations == null || counselor.relations == null) return;
            if (patient.relations.DirectRelationExists(DC_DefOf.DC_Confidant, counselor)) return;

            patient.relations.AddDirectRelation(DC_DefOf.DC_Confidant, counselor);
            if (!counselor.relations.DirectRelationExists(DC_DefOf.DC_Confidant, patient))
                counselor.relations.AddDirectRelation(DC_DefOf.DC_Confidant, patient);

            TouchAverseUtility.NotifyTrustedBond(counselor, patient);

            Messages.Message(
                "DC_ConfidantFormed".Translate(
                    counselor.LabelShort.Named("A"),
                    patient.LabelShort.Named("B")),
                new LookTargets(counselor, patient),
                MessageTypeDefOf.PositiveEvent,
                false);
        }

        public static bool AreConfidants(Pawn a, Pawn b)
        {
            if (a?.relations == null || b == null) return false;
            return a.relations.DirectRelationExists(DC_DefOf.DC_Confidant, b);
        }

        public static float TherapyBonusBetween(Pawn counselor, Pawn patient)
        {
            return AreConfidants(counselor, patient) ? 1.25f : 1f;
        }
    }
}
