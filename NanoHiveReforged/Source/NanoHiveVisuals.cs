using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace NanoHiveReforged
{
    // Repair shimmer (AZR-612): a pulsing glow on a hive while it repairs, and a
    // spark that fades on each thing it repaired in the last cycle. Draw-only:
    // nothing here ticks, saves or uses Rand, so a player who turns it off
    // never desyncs in Multiplayer. Vanilla shaders only, so the same code runs
    // on 1.6 and 1.7.
    [StaticConstructorOnStartup]
    public static class NanoHiveVisuals
    {
        private const string TexRoot = "NanoHive/Effects/";
        private const int AlphaSteps = 20;
        private const float SparkSeconds = 1.6f;

        private static readonly Color HiveColor = new Color(0.45f, 0.85f, 1f);
        private static readonly Color SparkColor = new Color(0.7f, 0.95f, 1f);

        private static readonly Texture2D GlowTex = Load("Glow");
        private static readonly Texture2D SparkTex = Load("Spark");

        private static readonly Dictionary<(Texture2D, int), Material> faded = new Dictionary<(Texture2D, int), Material>();

        public static bool Enabled => NanoHiveReforgedMod.Settings.repairShimmer;

        public static void DrawMap(Map map, List<Building_NanoHiveReforged> hives)
        {
            if (!Enabled || map != Find.CurrentMap || WorldRendererUtility.WorldSelected || hives.Count == 0)
            {
                return;
            }
            CellRect view = Find.CameraDriver.CurrentViewRect.ExpandedBy(2);
            float now = Time.realtimeSinceStartup;
            for (int i = 0; i < hives.Count; i++)
            {
                Building_NanoHiveReforged hive = hives[i];
                if (!hive.Working)
                {
                    continue;
                }
                if (view.Contains(hive.Position))
                {
                    float pulse = 0.7f + 0.3f * Mathf.Sin(now * 3f + hive.thingIDNumber);
                    DrawQuad(GlowTex, hive.DrawPos, 1.9f, HiveColor, 0.55f * pulse);
                }
                DrawSparks(hive, view, now);
            }
        }

        private static void DrawSparks(Building_NanoHiveReforged hive, CellRect view, float now)
        {
            float age = (now - hive.LastCycleRealtime) / SparkSeconds;
            if (age < 0f || age >= 1f)
            {
                return;
            }
            float alpha = 0.8f * (1f - age);
            float size = 0.55f + 0.35f * age;
            List<Thing> repaired = hive.LastRepaired;
            for (int i = 0; i < repaired.Count; i++)
            {
                Thing holder = repaired[i].SpawnedParentOrMe;
                if (holder == null || !holder.Spawned || holder.Map != hive.Map || !view.Contains(holder.Position))
                {
                    continue;
                }
                DrawQuad(SparkTex, holder.DrawPos, size, SparkColor, alpha);
            }
        }

        private static void DrawQuad(Texture2D tex, Vector3 center, float size, Color color, float alpha)
        {
            Material mat = Faded(tex, color, alpha);
            if (mat == null)
            {
                return;
            }
            Vector3 pos = center.WithY(AltitudeLayer.MoteOverhead.AltitudeFor());
            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(size, 1f, size));
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }

        // One material per (texture, alpha step), made once and reused.
        private static Material Faded(Texture2D tex, Color color, float alpha)
        {
            if (tex == null)
            {
                return null;
            }
            int step = Mathf.RoundToInt(Mathf.Clamp01(alpha) * AlphaSteps);
            if (step <= 0)
            {
                return null;
            }
            if (!faded.TryGetValue((tex, step), out Material mat))
            {
                mat = MaterialPool.MatFrom(tex, ShaderDatabase.MoteGlow, new Color(color.r, color.g, color.b, (float)step / AlphaSteps));
                faded[(tex, step)] = mat;
            }
            return mat;
        }

        private static Texture2D Load(string name)
        {
            Texture2D tex = ContentFinder<Texture2D>.Get(TexRoot + name, reportFailure: false);
            if (tex == null)
            {
                Log.WarningOnce("[NanoHive Reforged] Missing effect texture " + TexRoot + name + "; the shimmer is skipped.",
                    name.GetHashCode());
            }
            return tex;
        }
    }

    // Keeps the spawned hives of one map for the shimmer. Holds no saved data.
    public class MapComponent_NanoHive : MapComponent
    {
        public readonly List<Building_NanoHiveReforged> Hives = new List<Building_NanoHiveReforged>();

        public MapComponent_NanoHive(Map map) : base(map)
        {
        }

        public static MapComponent_NanoHive For(Map map) => map?.GetComponent<MapComponent_NanoHive>();

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();
            NanoHiveVisuals.DrawMap(map, Hives);
        }
    }
}
