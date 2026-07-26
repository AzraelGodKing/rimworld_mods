using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Threat letters that land on a linked floor while you're looking at another
    // one are easy to miss — and vanilla JumpToLocation sometimes never cuts
    // the camera onto pocket maps. Force a Cut jump + normal speed when a
    // ThreatBig/ThreatSmall letter targets an off-view Strata floor.
    public static class StrataCrossLevelThreatNotify
    {
        private const int JumpCooldownTicks = 90;

        private const int LetterCooldownTicks = 2500;

        private static int lastJumpTick = -99999;

        private static readonly Dictionary<int, int> lastLetterTickByMap = new Dictionary<int, int>();

        public static void OnLetterReceived(Letter letter)
        {
            if (letter?.def == null
                || (letter.def != LetterDefOf.ThreatBig && letter.def != LetterDefOf.ThreatSmall))
            {
                return;
            }
            if (!TryGetThreatMap(letter, out Map map, out GlobalTargetInfo jumpTarget))
            {
                return;
            }
            if (!ShouldForceAttention(map))
            {
                return;
            }
            lastLetterTickByMap[map.uniqueID] = Find.TickManager.TicksGame;
            JumpTo(jumpTarget);
        }

        // Raid pursuit / silent hostile waves: promote into a letter that jumps
        // when the player isn't already on that floor.
        public static void NotifyHostilesOnLinkedFloor(Map map, LookTargets targets, string label, string text)
        {
            if (map == null || !IsLinkedFloor(map) || !ShouldForceAttention(map))
            {
                return;
            }
            int now = Find.TickManager.TicksGame;
            if (lastLetterTickByMap.TryGetValue(map.uniqueID, out int last) && now - last < LetterCooldownTicks)
            {
                JumpTo(PrimaryOrCenter(targets, map));
                return;
            }
            lastLetterTickByMap[map.uniqueID] = now;
            LookTargets look = targets ?? new LookTargets(map.Center, map);
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.ThreatSmall, look);
        }

        public static bool ShouldForceAttention(Map map)
        {
            if (map == null || Find.CurrentMap == map || !IsLinkedFloor(map))
            {
                return false;
            }
            if (StrataGravshipTravelView.ShouldBlockView(map))
            {
                return false;
            }
            return true;
        }

        public static bool IsLinkedFloor(Map map)
        {
            return map != null
                && (StrataMapUtility.IsUnderground(map) || StrataMapUtility.IsUpperLevel(map));
        }

        private static GlobalTargetInfo PrimaryOrCenter(LookTargets targets, Map map)
        {
            if (targets != null)
            {
                GlobalTargetInfo primary = targets.TryGetPrimaryTarget();
                if (primary.IsValid)
                {
                    return primary;
                }
            }
            return new GlobalTargetInfo(map.Center, map);
        }

        private static bool TryGetThreatMap(Letter letter, out Map map, out GlobalTargetInfo jumpTarget)
        {
            map = null;
            jumpTarget = GlobalTargetInfo.Invalid;
            LookTargets looks = letter.lookTargets;
            if (looks == null)
            {
                return false;
            }
            GlobalTargetInfo primary = looks.TryGetPrimaryTarget();
            if (primary.IsValid && primary.Map != null)
            {
                map = primary.Map;
                jumpTarget = primary;
                return true;
            }
            List<GlobalTargetInfo> targets = looks.targets;
            if (targets == null)
            {
                return false;
            }
            for (int i = 0; i < targets.Count; i++)
            {
                GlobalTargetInfo t = targets[i];
                if (t.IsValid && t.Map != null)
                {
                    map = t.Map;
                    jumpTarget = t;
                    return true;
                }
            }
            return false;
        }

        private static void JumpTo(GlobalTargetInfo target)
        {
            if (!target.IsValid || target.Map == null)
            {
                return;
            }
            int now = Find.TickManager.TicksGame;
            if (now - lastJumpTick < JumpCooldownTicks)
            {
                return;
            }
            lastJumpTick = now;
            Find.TickManager.slower.SignalForceNormalSpeed();
            CameraJumper.TryJump(target, CameraJumper.MovementMode.Cut);
        }
    }
}
