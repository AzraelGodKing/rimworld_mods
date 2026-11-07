#nullable enable
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace DateNight
{
    /// <summary>A one-off date the player planned with the "Ask out tonight" gizmo.</summary>
    public class PlannedDate : IExposable
    {
        public int pawnA;
        public int pawnB;
        public int startTick;
        public int hour;

        public bool Involves(int id)
        {
            return pawnA == id || pawnB == id;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnA, "pawnA");
            Scribe_Values.Look(ref pawnB, "pawnB");
            Scribe_Values.Look(ref startTick, "startTick");
            Scribe_Values.Look(ref hour, "hour");
        }
    }

    /// <summary>
    /// "Ask out tonight" (AZR-524): one date at the next free evening hour for
    /// both partners, without painting Date on the schedule. Saved as
    /// <c>dateNightPlannedDates</c>. <see cref="Plan"/> and <see cref="Cancel"/>
    /// are the only entry points from UI, so Multiplayer syncs just those two.
    /// </summary>
    public static class DateNightAskOut
    {
        public const int FirstEveningHour = 18;
        public const int LastEveningHour = 23;
        private const int GraceTicks = GenDate.TicksPerHour * 2;

        private static List<PlannedDate> plans = new List<PlannedDate>();

        public static void Reset()
        {
            plans = new List<PlannedDate>();
        }

        public static void ExposeData()
        {
            Scribe_Collections.Look(ref plans, "dateNightPlannedDates", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                plans ??= new List<PlannedDate>();
                plans.RemoveAll(p => p == null);
            }
        }

        public static PlannedDate? PlanFor(Pawn pawn)
        {
            int id = pawn.thingIDNumber;
            for (int i = 0; i < plans.Count; i++)
            {
                if (plans[i].Involves(id))
                {
                    return plans[i];
                }
            }
            return null;
        }

        /// <summary>Synced in Multiplayer. Replaces any plan either pawn already had.</summary>
        public static void Plan(Pawn pawn, Pawn partner)
        {
            if (pawn?.Map == null || partner == null)
            {
                return;
            }
            plans.RemoveAll(p => p.Involves(pawn.thingIDNumber) || p.Involves(partner.thingIDNumber));

            int hour = NextFreeEveningHour(pawn, partner, out int startTick);
            plans.Add(new PlannedDate
            {
                pawnA = pawn.thingIDNumber,
                pawnB = partner.thingIDNumber,
                startTick = startTick,
                hour = hour
            });
            Messages.Message(
                "DateNight_AskOut_Planned".Translate(pawn.LabelShort, partner.LabelShort, hour),
                new LookTargets(pawn, partner), MessageTypeDefOf.TaskCompletion, historical: false);
        }

        /// <summary>Synced in Multiplayer.</summary>
        public static void Cancel(Pawn pawn)
        {
            if (pawn != null)
            {
                plans.RemoveAll(p => p.Involves(pawn.thingIDNumber));
            }
        }

        /// <summary>Every 60 ticks from the GameComponent.</summary>
        public static void Tick()
        {
            if (plans.Count == 0)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            for (int i = plans.Count - 1; i >= 0; i--)
            {
                PlannedDate plan = plans[i];
                if (now < plan.startTick)
                {
                    continue;
                }

                Pawn? a = DateNightPawns.Colonist(plan.pawnA);
                Pawn? b = DateNightPawns.Colonist(plan.pawnB);
                if (a == null || b == null || a.Dead || b.Dead)
                {
                    plans.RemoveAt(i);
                    continue;
                }

                if (a.CurJobDef == DateNightDefOf.DateNight_GoOnDate
                    || TryStartBoth(a, b))
                {
                    plans.RemoveAt(i);
                    continue;
                }

                if (now > plan.startTick + GraceTicks)
                {
                    plans.RemoveAt(i);
                    Messages.Message("DateNight_AskOut_Missed".Translate(a.LabelShort, b.LabelShort),
                        new LookTargets(a, b), MessageTypeDefOf.NegativeEvent, historical: false);
                }
            }
        }

        private static bool TryStartBoth(Pawn a, Pawn b)
        {
            if (a.Drafted || b.Drafted || !a.Spawned || !b.Spawned || a.Map != b.Map)
            {
                return false;
            }
            if (DateNightUtility.IsBusyWithRitual(a) || DateNightUtility.IsBusyWithRitual(b))
            {
                return false;
            }
            if (DateNightUtility.IsDoingNeedJob(a) || DateNightUtility.IsDoingNeedJob(b))
            {
                return false;
            }
            if (!DateNightDateUtility.CanDate(a, b, force: true))
            {
                return false;
            }
            // A planned date ignores the post-date cooldown, so force; drafted and
            // ritual were checked above.
            bool started = DateNightDateUtility.TryStartDateNow(a, force: true);
            if (started)
            {
                DateNightDateUtility.TryStartDateNow(b, force: true);
            }
            return started;
        }

        /// <summary>
        /// The first whole hour from now, between 18:00 and 23:00 local time, that
        /// neither pawn has painted Sleep or Work. Falls back to the first evening
        /// hour if every evening hour is busy. Returns the hour and its start tick.
        /// </summary>
        public static int NextFreeEveningHour(Pawn a, Pawn b, out int startTick)
        {
            int now = Find.TickManager.TicksGame;
            int hourNow = GenLocalDate.HourInteger(a.Map);
            int intoHour = GenLocalDate.DayTick(a.Map) % GenDate.TicksPerHour;
            int fallback = -1;
            for (int k = 0; k < 48; k++)
            {
                int hour = (hourNow + k) % 24;
                if (hour < FirstEveningHour || hour > LastEveningHour)
                {
                    continue;
                }
                int tick = k == 0 ? now : now - intoHour + k * GenDate.TicksPerHour;
                if (fallback < 0)
                {
                    fallback = k;
                }
                if (FreeAt(a, hour) && FreeAt(b, hour))
                {
                    startTick = tick;
                    return hour;
                }
            }
            int fk = Mathf.Max(0, fallback);
            startTick = fk == 0 ? now : now - intoHour + fk * GenDate.TicksPerHour;
            return (hourNow + fk) % 24;
        }

        private static bool FreeAt(Pawn pawn, int hour)
        {
            TimeAssignmentDef? ta = pawn.timetable?.GetAssignment(hour);
            return ta != TimeAssignmentDefOf.Sleep && ta != TimeAssignmentDefOf.Work;
        }
    }
}
