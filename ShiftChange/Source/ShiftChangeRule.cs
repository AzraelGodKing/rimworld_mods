using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ShiftChange
{
    /// <summary>
    /// Per-pawn rule: when trigger T fires, change into apparel policy Z at wardrobe Y.
    /// WorkType rules also store <see cref="workTypeDefName"/> (Cooking, Doctor, Handling, …).
    /// </summary>
    public class ShiftChangeRule : IExposable
    {
        public int ruleId;
        public int pawnId;
        public ShiftChangeTriggerKind trigger = ShiftChangeTriggerKind.Sleep;
        public string workTypeDefName;
        public string apparelPolicyName;
        public int wardrobeZoneId = -1;
        public bool replaceMode = true;
        public bool enabled = true;

        public void ExposeData()
        {
            Scribe_Values.Look(ref ruleId, "ruleId");
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_Values.Look(ref trigger, "trigger", ShiftChangeTriggerKind.Sleep);
            Scribe_Values.Look(ref workTypeDefName, "workTypeDefName");
            Scribe_Values.Look(ref apparelPolicyName, "apparelPolicyName");
            Scribe_Values.Look(ref wardrobeZoneId, "wardrobeZoneId", -1);
            Scribe_Values.Look(ref replaceMode, "replaceMode", true);
            Scribe_Values.Look(ref enabled, "enabled", true);
        }

        public ApparelPolicy ResolvePolicy()
        {
            if (string.IsNullOrEmpty(apparelPolicyName) || Current.Game?.outfitDatabase == null)
            {
                return null;
            }

            List<ApparelPolicy> all = Current.Game.outfitDatabase.AllOutfits;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].label == apparelPolicyName)
                {
                    return all[i];
                }
            }

            return null;
        }

        public WorkTypeDef ResolveWorkType()
        {
            if (string.IsNullOrEmpty(workTypeDefName))
            {
                return null;
            }

            return DefDatabase<WorkTypeDef>.GetNamedSilentFail(workTypeDefName);
        }

        public string LabelShort()
        {
            switch (trigger)
            {
                case ShiftChangeTriggerKind.Sleep:
                    return "Sleep";
                case ShiftChangeTriggerKind.Ritual:
                    return "Ritual";
                case ShiftChangeTriggerKind.WorkType:
                    return ResolveWorkType()?.labelShort ?? workTypeDefName ?? "Work";
                default:
                    return trigger.ToString();
            }
        }
    }

    /// <summary>Runtime state while Shift Change is managing a pawn's apparel.</summary>
    public class PawnShiftState : IExposable
    {
        public int pawnId;
        public int activeRuleId = -1;
        public bool managed;
        public bool wantsRestore;
        public List<int> snapshotApparelIds = new List<int>();
        public List<int> reservedApparelIds = new List<int>();
        public int lastSwapTick = -99999;
        public int hysteresisUntilTick = -99999;
        public string pendingWorkTypeDefName;
        public bool applyJobQueued;

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_Values.Look(ref activeRuleId, "activeRuleId", -1);
            Scribe_Values.Look(ref managed, "managed");
            Scribe_Values.Look(ref wantsRestore, "wantsRestore");
            Scribe_Collections.Look(ref snapshotApparelIds, "snapshotApparelIds", LookMode.Value);
            Scribe_Collections.Look(ref reservedApparelIds, "reservedApparelIds", LookMode.Value);
            Scribe_Values.Look(ref lastSwapTick, "lastSwapTick", -99999);
            Scribe_Values.Look(ref hysteresisUntilTick, "hysteresisUntilTick", -99999);
            Scribe_Values.Look(ref pendingWorkTypeDefName, "pendingWorkTypeDefName");
            Scribe_Values.Look(ref applyJobQueued, "applyJobQueued");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                snapshotApparelIds ??= new List<int>();
                reservedApparelIds ??= new List<int>();
            }
        }
    }
}
