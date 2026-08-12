using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ShiftChange
{
    /// <summary>
    /// Per-pawn rule: when trigger T fires, change into apparel policy Z at wardrobe Y.
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

        public Pawn ResolvePawn() => pawnId <= 0 ? null : FindPawnById(pawnId);

        private static Pawn FindPawnById(int id)
        {
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                IReadOnlyList<Pawn> pawns = maps[m]?.mapPawns?.AllPawnsSpawned;
                if (pawns == null)
                {
                    continue;
                }

                for (int i = 0; i < pawns.Count; i++)
                {
                    if (pawns[i] != null && pawns[i].thingIDNumber == id)
                    {
                        return pawns[i];
                    }
                }
            }

            return null;
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
        public int lastSwapTick = -99999;
        public bool triggerActive;

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_Values.Look(ref activeRuleId, "activeRuleId", -1);
            Scribe_Values.Look(ref managed, "managed");
            Scribe_Values.Look(ref wantsRestore, "wantsRestore");
            Scribe_Collections.Look(ref snapshotApparelIds, "snapshotApparelIds", LookMode.Value);
            Scribe_Values.Look(ref lastSwapTick, "lastSwapTick", -99999);
            Scribe_Values.Look(ref triggerActive, "triggerActive");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                snapshotApparelIds ??= new List<int>();
            }
        }
    }
}
