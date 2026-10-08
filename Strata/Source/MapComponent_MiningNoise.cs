using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Strata
{
    // Accumulates industrial / mining noise on a map. Decays over time and is
    // dampened by sound-dampening walls/floors or thick-walled rooms. Feeds
    // infestation weight (AZR-272) and the seismograph forecast (AZR-271).
    public class MapComponent_MiningNoise : MapComponent
    {
        public const float MaxNoise = 100f;
        public const float MiningPulse = 4.5f;
        public const float DrillPulse = 8f;

        private float noise;
        private int lastDecayTick = -1;
        private int lastMachineryScanTick = -9999;

        public MapComponent_MiningNoise(Map map) : base(map)
        {
        }

        public float Noise => noise;

        // 0..1 pressure used by UI / infestation multiplier.
        public float Pressure01 => Mathf.Clamp01(noise / MaxNoise);

        public float InfestationNoiseMultiplier
        {
            get
            {
                if (StrataMod.Settings != null && !StrataMod.Settings.noiseInfestationEnabled)
                {
                    return 1f;
                }
                float scale = StrataMod.Settings?.noiseInfestationScale ?? 1f;
                // Quiet: 1×. Busy mine: up to ~2.2× at full noise.
                return 1f + Pressure01 * 1.2f * Mathf.Max(0f, scale);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref noise, "strataMiningNoise", 0f);
            Scribe_Values.Look(ref lastDecayTick, "strataMiningNoiseDecayTick", -1);
        }

        public override void MapComponentTick()
        {
            if (StrataMod.Settings != null && !StrataMod.Settings.noiseInfestationEnabled)
            {
                return;
            }
            if (!StrataMapUtility.IsUnderground(map))
            {
                return;
            }

            int tick = Find.TickManager.TicksGame;
            if (tick - lastDecayTick >= 250)
            {
                float hours = (tick - (lastDecayTick < 0 ? tick : lastDecayTick)) / 2500f;
                lastDecayTick = tick;
                float halfLifeHours = StrataMod.Settings?.noiseHalfLifeHours ?? 4f;
                float decay = Mathf.Pow(0.5f, hours / Mathf.Max(0.25f, halfLifeHours));
                noise *= decay;
                if (noise < 0.05f)
                {
                    noise = 0f;
                }
            }

            if (tick - lastMachineryScanTick >= 120)
            {
                lastMachineryScanTick = tick;
                AddMachineryNoise();
                AddActiveMiningNoise();
            }
        }

        public void AddRaw(float amount, IntVec3? at = null)
        {
            if (amount <= 0f)
            {
                return;
            }
            if (StrataMod.Settings != null && !StrataMod.Settings.noiseInfestationEnabled)
            {
                return;
            }
            float dampen = 1f;
            if (at.HasValue && at.Value.IsValid && at.Value.InBounds(map))
            {
                dampen = DampenFactorAt(at.Value);
            }
            noise = Mathf.Min(MaxNoise, noise + amount * dampen);
        }

        public float DampenFactorAt(IntVec3 cell)
        {
            float factor = 1f;
            TerrainDef terrain = cell.GetTerrain(map);
            if (terrain != null
                && (terrain == StrataTerrainDefOf.Strata_SoundDampeningFloor
                    || terrain.defName == "Strata_SoundDampeningFloor"))
            {
                factor *= 0.55f;
            }

            Building edificeHere = cell.GetEdifice(map);
            if (edificeHere != null && edificeHere.def == StrataThingDefOf.Strata_SoundDampeningWall)
            {
                factor *= 0.4f;
            }

            Room room = cell.GetRoom(map);
            if (room != null && !room.PsychologicallyOutdoors && RoomHasThickWalls(room))
            {
                factor *= 0.7f;
            }

            // Nearby dampening walls also mute the cell a bit.
            foreach (IntVec3 n in GenAdj.CellsAdjacent8Way(new TargetInfo(cell, map)))
            {
                if (!n.InBounds(map))
                {
                    continue;
                }
                Building edifice = n.GetEdifice(map);
                if (edifice != null && edifice.def == StrataThingDefOf.Strata_SoundDampeningWall)
                {
                    factor *= 0.75f;
                    break;
                }
            }

            return Mathf.Clamp(factor, 0.15f, 1f);
        }

        private bool RoomHasThickWalls(Room room)
        {
            int walls = 0;
            int thick = 0;
            foreach (IntVec3 border in room.BorderCells)
            {
                Building edifice = border.GetEdifice(map);
                if (edifice == null || edifice.def?.Fillage != FillCategory.Full)
                {
                    continue;
                }
                walls++;
                if (edifice.def.fillPercent >= 1f
                    || edifice.def == StrataThingDefOf.Strata_SoundDampeningWall
                    || edifice.MaxHitPoints >= 300)
                {
                    thick++;
                }
            }
            return walls > 0 && thick >= walls * 0.6f;
        }

        private void AddMachineryNoise()
        {
            float pulse = 0f;
            List<Thing> powerThings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            for (int i = 0; i < powerThings.Count; i++)
            {
                if (powerThings[i] is not ThingWithComps twc)
                {
                    continue;
                }
                CompPowerTrader power = twc.GetComp<CompPowerTrader>();
                if (power == null || !power.PowerOn || power.PowerOutput >= -1f)
                {
                    continue;
                }
                // Drawers: PowerOutput is negative while consuming.
                float draw = -power.PowerOutput;
                if (draw < 50f)
                {
                    continue;
                }
                // Heavy industry hum — scaled down so a few machines don't max the meter.
                float local = Mathf.Min(draw / 400f, 1.2f) * 0.35f;
                local *= DampenFactorAt(twc.Position);
                pulse += local;
            }
            if (pulse > 0f)
            {
                noise = Mathf.Min(MaxNoise, noise + pulse);
            }
        }

        private void AddActiveMiningNoise()
        {
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                Job job = pawn.CurJob;
                if (job == null)
                {
                    continue;
                }
                if (job.def == JobDefOf.Mine)
                {
                    AddRaw(0.4f, pawn.Position);
                }
                else if (job.def == JobDefOf.OperateDeepDrill)
                {
                    AddRaw(0.7f, pawn.Position);
                }
            }
        }

        public static MapComponent_MiningNoise For(Map map)
        {
            return map?.GetComponent<MapComponent_MiningNoise>();
        }

        public static float DepthInfestationFactor(Map map)
        {
            return Mathf.Min(1.3f + 0.35f * StrataDepth.Of(map), 3f);
        }
    }

    // Mining a cell produces a noise pulse on underground maps.
    [HarmonyPatch(typeof(Mineable), nameof(Mineable.Destroy))]
    public static class Patch_MineableDestroy_Noise
    {
        public static void Prefix(Mineable __instance, DestroyMode mode)
        {
            if (mode == DestroyMode.Vanish || __instance?.Map == null)
            {
                return;
            }
            if (!StrataMapUtility.IsUnderground(__instance.Map))
            {
                return;
            }
            MapComponent_MiningNoise.For(__instance.Map)
                ?.AddRaw(MapComponent_MiningNoise.MiningPulse, __instance.Position);
        }
    }

    // Deep drills are especially loud when a portion is produced.
    [HarmonyPatch]
    public static class Patch_DeepDrill_Noise
    {
        public static bool Prepare()
        {
            return AccessTools.Method(typeof(CompDeepDrill), "ProducePortion") != null;
        }

        private static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(CompDeepDrill), "ProducePortion");
        }

        public static void Postfix(CompDeepDrill __instance)
        {
            ThingWithComps parent = __instance?.parent;
            if (parent?.Map == null || !StrataMapUtility.IsUnderground(parent.Map))
            {
                return;
            }
            MapComponent_MiningNoise.For(parent.Map)
                ?.AddRaw(MapComponent_MiningNoise.DrillPulse, parent.Position);
        }
    }
}
