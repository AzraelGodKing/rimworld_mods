using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// AZR-72 — name a colonist's known flashback triggers only after the
    /// colony has seen them fire at least once.
    /// </summary>
    public static class TraumaTriggerUtility
    {
        public const string Combat = "combat";
        public const string Fire = "fire";
        public const string Capture = "capture";
        public const string Casualties = "casualties";
        public const string Toxins = "toxins";
        public const string Insects = "insects";
        public const string Betrayal = "betrayal";

        public static void DiscoverFromFlashback(Pawn pawn)
        {
            if (pawn == null || !DeepColonySettings.Get.enableTrauma) return;
            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;
            if (comp.discoveredTraumaTriggers == null)
                comp.discoveredTraumaTriggers = new List<string>();

            List<string> keys = ActiveTriggerKeys(pawn);
            for (int i = 0; i < keys.Count; i++)
            {
                if (!comp.discoveredTraumaTriggers.Contains(keys[i]))
                    comp.discoveredTraumaTriggers.Add(keys[i]);
            }
        }

        public static string InspectLine(Pawn pawn)
        {
            if (!DeepColonySettings.Get.enableTrauma) return null;
            var comp = pawn?.TryGetComp<Comp_DeepColony>();
            if (comp?.discoveredTraumaTriggers == null || comp.discoveredTraumaTriggers.Count == 0)
                return null;
            var labels = new List<string>();
            for (int i = 0; i < comp.discoveredTraumaTriggers.Count; i++)
            {
                string lab = LabelFor(comp.discoveredTraumaTriggers[i]);
                if (!lab.NullOrEmpty() && !labels.Contains(lab))
                    labels.Add(lab);
            }
            if (labels.Count == 0) return null;
            return "DC_InspectKnownTriggers".Translate(string.Join(", ", labels));
        }

        public static string LabelFor(string key)
        {
            if (key.NullOrEmpty()) return null;
            switch (key)
            {
                case Combat: return "DC_Trigger_Combat".Translate();
                case Fire: return "DC_Trigger_Fire".Translate();
                case Capture: return "DC_Trigger_Capture".Translate();
                case Casualties: return "DC_Trigger_Casualties".Translate();
                case Toxins: return "DC_Trigger_Toxins".Translate();
                case Insects: return "DC_Trigger_Insects".Translate();
                case Betrayal: return "DC_Trigger_Betrayal".Translate();
                default: return key;
            }
        }

        internal static List<string> ActiveTriggerKeys(Pawn pawn)
        {
            var keys = new List<string>();
            if (pawn == null) return keys;

            if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_CombatShock)
                && FlashbackUtility.CombatTriggerPresent(pawn)
                && !keys.Contains(Combat))
                keys.Add(Combat);

            if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Fire)
                && FlashbackUtility.FireTriggerPresent(pawn)
                && !keys.Contains(Fire))
                keys.Add(Fire);

            if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Captivity)
                && FlashbackUtility.CaptivityTriggerPresent(pawn)
                && !keys.Contains(Capture))
                keys.Add(Capture);

            if ((TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Massacre)
                    || TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_ViolentLoss)
                    || TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_BereavementShock))
                && FlashbackUtility.CasualtyTriggerPresent(pawn)
                && !keys.Contains(Casualties))
                keys.Add(Casualties);

            if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Toxic)
                && FlashbackUtility.ToxinTriggerPresent(pawn)
                && !keys.Contains(Toxins))
                keys.Add(Toxins);

            if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Insect)
                && FlashbackUtility.InsectTriggerPresent(pawn)
                && !keys.Contains(Insects))
                keys.Add(Insects);

            if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Betrayal)
                && FlashbackUtility.BetrayalTriggerPresent(pawn)
                && !keys.Contains(Betrayal))
                keys.Add(Betrayal);

            // Forced flashbacks (grudge raid, debug) still reveal something if they
            // carry trauma but no situational trigger is currently present.
            if (keys.Count == 0)
            {
                if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Fire)) keys.Add(Fire);
                else if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Insect)) keys.Add(Insects);
                else if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Captivity)) keys.Add(Capture);
                else if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Toxic)) keys.Add(Toxins);
                else if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Betrayal)) keys.Add(Betrayal);
                else if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Massacre)
                    || TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_ViolentLoss)
                    || TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_BereavementShock))
                    keys.Add(Casualties);
                else if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_CombatShock))
                    keys.Add(Combat);
            }
            return keys;
        }
    }
}
