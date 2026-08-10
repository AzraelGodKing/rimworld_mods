using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Wardrobe
{
    public enum WardrobeTrigger : byte
    {
        None = 0,
        Sleep = 1,
        Cook = 2,
        Doctor = 3,
        Animals = 4
    }

    public class WardrobePawnState : IExposable
    {
        public int pawnId;
        public int stockpileId = -1;

        public bool sleepEnabled;
        public int sleepPolicyId = -1;
        public bool cookEnabled;
        public int cookPolicyId = -1;
        public bool doctorEnabled;
        public int doctorPolicyId = -1;
        public bool animalsEnabled;
        public int animalsPolicyId = -1;

        public WardrobeTrigger activeTrigger = WardrobeTrigger.None;
        public List<int> snapshotThingIds = new List<int>();
        public List<string> snapshotDefNames = new List<string>();
        public int cooldownTicks;

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnId, "pawnId", 0);
            Scribe_Values.Look(ref stockpileId, "stockpileId", -1);
            Scribe_Values.Look(ref sleepEnabled, "sleepEnabled", false);
            Scribe_Values.Look(ref sleepPolicyId, "sleepPolicyId", -1);
            Scribe_Values.Look(ref cookEnabled, "cookEnabled", false);
            Scribe_Values.Look(ref cookPolicyId, "cookPolicyId", -1);
            Scribe_Values.Look(ref doctorEnabled, "doctorEnabled", false);
            Scribe_Values.Look(ref doctorPolicyId, "doctorPolicyId", -1);
            Scribe_Values.Look(ref animalsEnabled, "animalsEnabled", false);
            Scribe_Values.Look(ref animalsPolicyId, "animalsPolicyId", -1);
            Scribe_Values.Look(ref activeTrigger, "activeTrigger", WardrobeTrigger.None);
            Scribe_Collections.Look(ref snapshotThingIds, "snapshotThingIds", LookMode.Value);
            Scribe_Collections.Look(ref snapshotDefNames, "snapshotDefNames", LookMode.Value);
            Scribe_Values.Look(ref cooldownTicks, "cooldownTicks", 0);
            if (snapshotThingIds == null)
            {
                snapshotThingIds = new List<int>();
            }

            if (snapshotDefNames == null)
            {
                snapshotDefNames = new List<string>();
            }
        }

        public bool AnyEnabled =>
            sleepEnabled || cookEnabled || doctorEnabled || animalsEnabled;

        public bool IsManaged => activeTrigger != WardrobeTrigger.None;

        public int PolicyIdFor(WardrobeTrigger trigger)
        {
            switch (trigger)
            {
                case WardrobeTrigger.Sleep: return sleepPolicyId;
                case WardrobeTrigger.Cook: return cookPolicyId;
                case WardrobeTrigger.Doctor: return doctorPolicyId;
                case WardrobeTrigger.Animals: return animalsPolicyId;
                default: return -1;
            }
        }

        public bool EnabledFor(WardrobeTrigger trigger)
        {
            switch (trigger)
            {
                case WardrobeTrigger.Sleep: return sleepEnabled;
                case WardrobeTrigger.Cook: return cookEnabled;
                case WardrobeTrigger.Doctor: return doctorEnabled;
                case WardrobeTrigger.Animals: return animalsEnabled;
                default: return false;
            }
        }
    }
}
