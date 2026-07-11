using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    [DefOf]
    public static class StrataSmokeDefOf
    {
        public static HediffDef Strata_SmokeInhalation;

        static StrataSmokeDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StrataSmokeDefOf));
        }
    }

    // A lightweight, room-based combustion-smoke simulation. Burners add smoke
    // to the room they sit in; it lingers in enclosed rooms and disperses fast
    // where the room is open to the sky, has an open roof, or a powered exhaust
    // fan. Colonists breathing thick smoke take on a worsening inhalation
    // hediff. Deliberately room-scale (not per-cell) so a tall base stays cheap.
    public class SmokeMapComponent : MapComponent
    {
        private const int CycleTicks = 60;
        private const float BaseLeak = 0.02f;      // slow seepage from any enclosed room
        private const float OpenRoofVent = 0.06f;  // per open-roof cell (capped)
        private const float OutdoorDisperse = 0.6f; // fraction cleared per cycle in open air
        private const float HarmThreshold = 0.15f;
        private const float SeverityGain = 0.02f;
        private const float SeverityDecay = 0.03f;
        private const float MoteThreshold = 0.2f;

        public readonly HashSet<CompExhaust> Emitters = new HashSet<CompExhaust>();
        public readonly HashSet<CompExhaustVent> Vents = new HashSet<CompExhaustVent>();

        private struct Cloud
        {
            public float density;
            public IntVec3 sample;
        }

        private readonly Dictionary<int, Cloud> clouds = new Dictionary<int, Cloud>();
        private readonly Dictionary<int, float> ventPower = new Dictionary<int, float>();

        public SmokeMapComponent(Map map) : base(map)
        {
        }

        public float DensityInRoom(Room room)
        {
            return room != null && clouds.TryGetValue(room.ID, out Cloud c) ? c.density : 0f;
        }

        public override void MapComponentTick()
        {
            if ((Find.TickManager.TicksGame + map.uniqueID) % CycleTicks != 0)
            {
                return;
            }

            // 1. Tally powered vents by room.
            ventPower.Clear();
            foreach (CompExhaustVent vent in Vents)
            {
                if (!vent.parent.Spawned || !vent.Active)
                {
                    continue;
                }
                Room r = vent.parent.GetRoom();
                if (r != null)
                {
                    ventPower[r.ID] = ventPower.TryGetValue(r.ID, out float v) ? v + vent.Props.ventPower : vent.Props.ventPower;
                }
            }

            // 2. Disperse / vent existing smoke.
            foreach (int id in clouds.Keys.ToList())
            {
                Cloud c = clouds[id];
                Room r = c.sample.IsValid && c.sample.InBounds(map) ? c.sample.GetRoom(map) : null;
                if (r == null || r.UsesOutdoorTemperature)
                {
                    c.density *= 1f - OutdoorDisperse;
                }
                else
                {
                    float vent = BaseLeak
                        + OpenRoofVent * Mathf.Min(r.OpenRoofCount, 5)
                        + (ventPower.TryGetValue(r.ID, out float vp) ? vp : 0f);
                    c.density *= 1f - Mathf.Clamp01(vent);
                }
                if (c.density < 0.01f)
                {
                    clouds.Remove(id);
                }
                else
                {
                    clouds[id] = c;
                }
            }

            // 3. Emit from active burners in enclosed rooms.
            foreach (CompExhaust emitter in Emitters)
            {
                if (!emitter.parent.Spawned || !emitter.Active)
                {
                    continue;
                }
                Room r = emitter.parent.GetRoom();
                if (r == null || r.UsesOutdoorTemperature)
                {
                    continue; // vents straight to open air
                }
                float add = emitter.Props.emissionPerCycle / Mathf.Max(r.CellCount, 1);
                Cloud c = clouds.TryGetValue(r.ID, out Cloud existing) ? existing : new Cloud();
                c.density = Mathf.Min(1f, c.density + add);
                c.sample = emitter.parent.Position;
                clouds[r.ID] = c;
            }

            AffectPawns();
            ThrowMotes();
        }

        private void AffectPawns()
        {
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn.RaceProps == null || !pawn.RaceProps.IsFlesh || pawn.Dead)
                {
                    continue;
                }
                Room room = pawn.GetRoom();
                float density = DensityInRoom(room);
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(StrataSmokeDefOf.Strata_SmokeInhalation);
                if (density > HarmThreshold)
                {
                    hediff ??= pawn.health.GetOrAddHediff(StrataSmokeDefOf.Strata_SmokeInhalation);
                    hediff.Severity += (density - HarmThreshold) * SeverityGain;
                }
                else if (hediff != null)
                {
                    hediff.Severity -= SeverityDecay;
                    if (hediff.Severity <= 0f)
                    {
                        pawn.health.RemoveHediff(hediff);
                    }
                }
            }
        }

        private void ThrowMotes()
        {
            foreach (Cloud c in clouds.Values)
            {
                if (c.density > MoteThreshold && c.sample.InBounds(map) && Rand.Value < 0.5f)
                {
                    FleckMaker.ThrowSmoke(c.sample.ToVector3Shifted(), map, c.density * 1.6f);
                }
            }
        }
    }
}
