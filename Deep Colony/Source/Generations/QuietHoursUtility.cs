using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace DeepColony
{
    /// <summary>
    /// AZR-65 — cheap room-level noise. Walls contain it (room cells only).
    /// Loud rooms hurt rest, counsel, and mentoring. Not Strata infestation noise.
    /// </summary>
    public static class QuietHoursUtility
    {
        public const float NoisyThreshold = 0.25f;

        private static int cacheTick = -1;
        private static readonly Dictionary<int, float> NoiseByRoom = new Dictionary<int, float>();

        public static bool Enabled => DeepColonySettings.Get.enableQuietHours;

        public static float Intensity =>
            Mathf.Clamp(DeepColonySettings.Get.quietHoursIntensity, 0.35f, 2f);

        public static float NoiseAt(Pawn pawn)
        {
            if (!Enabled || pawn == null || !pawn.Spawned) return 0f;
            return NoiseOf(pawn.GetRoom());
        }

        public static float NoiseOf(Room room)
        {
            if (!Enabled || room == null || room.PsychologicallyOutdoors) return 0f;
            int now = Find.TickManager?.TicksGame ?? 0;
            if (now - cacheTick >= 250)
            {
                NoiseByRoom.Clear();
                cacheTick = now;
            }
            if (NoiseByRoom.TryGetValue(room.ID, out float cached))
                return cached;
            float n = Compute(room);
            NoiseByRoom[room.ID] = n;
            return n;
        }

        public static bool IsNoisy(Room room) => NoiseOf(room) >= NoisyThreshold;

        /// <summary>0 = warning (assigned noisy bedroom, awake). 1 = sleeping in noise. -1 = none.</summary>
        public static int ThoughtStage(Pawn pawn)
        {
            if (!Enabled || pawn == null || pawn.Dead || !pawn.IsColonistPlayerControlled)
                return -1;
            if (!pawn.RaceProps.Humanlike) return -1;

            bool asleep = pawn.jobs?.curDriver is JobDriver_LayDown lay && lay.asleep;
            float here = NoiseAt(pawn);
            if (asleep && here >= NoisyThreshold) return 1;

            Room bedRoom = pawn.ownership?.OwnedBed?.GetRoom();
            if (bedRoom != null && IsNoisy(bedRoom) && !asleep) return 0;
            return -1;
        }

        public static string InspectLine(Pawn pawn)
        {
            if (!Enabled || pawn == null) return null;
            int stage = ThoughtStage(pawn);
            if (stage < 0) return null;
            float n = NoiseAt(pawn);
            Room bedRoom = pawn.ownership?.OwnedBed?.GetRoom();
            if (n < NoisyThreshold && bedRoom != null)
                n = NoiseOf(bedRoom);
            string pct = ((int)(Mathf.Clamp01(n) * 100f)).ToString();
            if (stage == 1)
                return "DC_InspectNoisySleep".Translate(pct);
            return "DC_InspectNoisyBedroom".Translate(pct);
        }

        public static float TherapyMultiplier(Room room)
        {
            if (!Enabled) return 1f;
            float n = NoiseOf(room);
            if (n < NoisyThreshold) return 1f;
            return Mathf.Clamp(1f - 0.22f * n * Intensity, 0.55f, 1f);
        }

        public static float MentorXpMultiplier(Pawn pawn)
        {
            if (!Enabled) return 1f;
            float n = NoiseAt(pawn);
            if (n < NoisyThreshold) return 1f;
            return Mathf.Clamp(1f - 0.18f * n * Intensity, 0.60f, 1f);
        }

        public static void DrainRestIfNoisy(Need_Rest rest, Pawn pawn)
        {
            if (!Enabled || rest == null || pawn == null) return;
            if (pawn.jobs?.curDriver is not JobDriver_LayDown lay || !lay.asleep) return;
            float n = NoiseAt(pawn);
            if (n < NoisyThreshold) return;
            rest.CurLevel = Mathf.Max(0f, rest.CurLevel - 0.0035f * n * Intensity);
        }

        private static float Compute(Room room)
        {
            Map map = room.Map;
            if (map == null) return 0f;
            float n = 0f;
            var seen = new HashSet<int>();
            foreach (IntVec3 c in room.Cells)
            {
                List<Thing> list = map.thingGrid.ThingsListAtFast(c);
                if (list == null) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    Thing t = list[i];
                    if (t == null || t.Destroyed) continue;
                    if (!seen.Add(t.thingIDNumber)) continue;
                    if (t is Building_WorkTable) n += 0.45f;
                    else if (t is Building_Turret) n += 0.35f;
                    else if (t.TryGetComp<CompPowerPlant>() != null) n += 0.50f;
                    if (t is Pawn p && p.CurJobDef == JobDefOf.Mine) n += 0.30f;
                }
            }
            return n;
        }
    }
}
