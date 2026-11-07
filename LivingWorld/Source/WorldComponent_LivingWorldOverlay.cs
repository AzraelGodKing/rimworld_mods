using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace LivingWorld
{
    /// <summary>
    /// World-map overlay: faction territory around settlements in a war, tension or
    /// alliance; war fronts between the nearest settlements of two factions at war
    /// (pulsing by intensity); refugee routes from pending fallout to your colony;
    /// and your own caravan trail. Draw-only: no Rand, no game state changes, so it is
    /// safe in Multiplayer. Geometry is rebuilt at most every 250 ticks, not per frame.
    /// </summary>
    public class WorldComponent_LivingWorldOverlay : WorldComponent
    {
        private const int RebuildEveryTicks = 250;
        private const float LineWidth = 1.4f;
        private const int AlphaSteps = 8;

        private struct Segment
        {
            public Vector3 a;
            public Vector3 b;
            public int style; // see Style* constants
        }

        private const int StyleWar1 = 0; // intensity 1..3 => StyleWar1 + intensity - 1
        private const int StyleRefugees = 3;
        private const int StyleTrail = 4;
        private const int StyleTerritoryBase = 5; // + territory colour index

        private readonly List<Segment> segments = new List<Segment>();
        private readonly List<Color> territoryColors = new List<Color>();
        private readonly Dictionary<long, Material> materials = new Dictionary<long, Material>();
        private readonly List<PlanetTile> neighbours = new List<PlanetTile>();
        private int builtTick = int.MinValue;
        private Game builtFor;

        public WorldComponent_LivingWorldOverlay(World world) : base(world)
        {
        }

        public static bool Visible => LivingWorldMod.Global?.overlaysEnabled == true;

        public override void WorldComponentUpdate()
        {
            if (!Visible || !WorldRendererUtility.WorldRendered || GameComponent_LivingWorld.Get == null)
            {
                return;
            }
            int now = Find.TickManager.TicksGame;
            if (builtFor != Current.Game || now - builtTick >= RebuildEveryTicks || now < builtTick)
            {
                Rebuild();
                builtTick = now;
                builtFor = Current.Game;
            }

            float t = Time.realtimeSinceStartup;
            for (int i = 0; i < segments.Count; i++)
            {
                Segment s = segments[i];
                GenDraw.DrawWorldLineBetween(s.a, s.b, MaterialFor(s.style, t), LineWidth);
            }
        }

        private Material MaterialFor(int style, float time)
        {
            Color c;
            int alphaStep = AlphaSteps - 1;
            if (style >= StyleWar1 && style < StyleWar1 + 3)
            {
                int intensity = style - StyleWar1 + 1;
                c = new Color(0.95f, 0.3f, 0.25f);
                // Hotter wars pulse faster.
                float pulse = 0.5f + 0.5f * Mathf.Sin(time * (1.5f + intensity));
                alphaStep = Mathf.Clamp(Mathf.RoundToInt((0.45f + 0.5f * pulse) * (AlphaSteps - 1)), 1, AlphaSteps - 1);
            }
            else if (style == StyleRefugees)
            {
                c = new Color(1f, 0.75f, 0.3f);
                alphaStep = AlphaSteps - 2;
            }
            else if (style == StyleTrail)
            {
                c = new Color(0.85f, 0.85f, 0.7f);
                alphaStep = AlphaSteps / 2;
            }
            else
            {
                int idx = style - StyleTerritoryBase;
                c = idx >= 0 && idx < territoryColors.Count ? territoryColors[idx] : Color.white;
                alphaStep = AlphaSteps / 2;
            }
            c.a = alphaStep / (float)(AlphaSteps - 1);
            long key = ((long)style << 8) | (uint)alphaStep;
            if (style >= StyleTerritoryBase)
            {
                // Territory colours can change between rebuilds; key by colour too.
                key ^= (long)c.GetHashCode() << 20;
            }
            if (!materials.TryGetValue(key, out Material mat))
            {
                mat = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.WorldOverlayTransparent, c,
                    WorldMaterials.WorldLineRenderQueue);
                materials[key] = mat;
            }
            return mat;
        }

        private void Rebuild()
        {
            segments.Clear();
            territoryColors.Clear();
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            if (comp == null)
            {
                return;
            }
            WorldGrid grid = Find.WorldGrid;

            // Factions that are in a war, tension or alliance get their territory drawn.
            var involved = new List<Faction>();
            IReadOnlyList<FactionPairState> pairs = comp.Pairs;
            for (int i = 0; i < pairs.Count; i++)
            {
                FactionPairState p = pairs[i];
                if (p == null || p.tone == FactionRelationTone.Peace)
                {
                    continue;
                }
                Faction a = p.FactionA();
                Faction b = p.FactionB();
                AddOnce(involved, a);
                AddOnce(involved, b);
                if (p.tone == FactionRelationTone.War && a != null && b != null
                    && NearestPair(a, b, out Settlement sa, out Settlement sb))
                {
                    int style = StyleWar1 + Mathf.Clamp(p.intensity, 1, 3) - 1;
                    segments.Add(new Segment { a = grid.GetTileCenter(sa.Tile), b = grid.GetTileCenter(sb.Tile), style = style });
                }
            }

            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int f = 0; f < involved.Count; f++)
            {
                Faction faction = involved[f];
                territoryColors.Add(faction.Color);
                int style = StyleTerritoryBase + f;
                for (int i = 0; i < settlements.Count; i++)
                {
                    Settlement s = settlements[i];
                    if (s?.Faction != faction)
                    {
                        continue;
                    }
                    AddRing(grid, s.Tile, style);
                }
            }

            // Refugee routes: from the battle to the colony they are heading for.
            List<PendingFallout> fallout = comp.PendingFalloutForDrawing;
            for (int i = 0; i < fallout.Count; i++)
            {
                PlanetTile from = fallout[i].Where;
                if (!from.Valid)
                {
                    continue;
                }
                Map home = GameComponent_LivingWorld.NearestPlayerHome(from);
                if (home == null || home.Tile.Layer != from.Layer)
                {
                    continue;
                }
                segments.Add(new Segment { a = grid.GetTileCenter(from), b = grid.GetTileCenter(home.Tile), style = StyleRefugees });
            }

            // Your caravan trail, in the order it was walked.
            IReadOnlyList<int> road = Current.Game.GetComponent<GameComponent_LivingWorldRoad>()?.Tiles;
            if (road != null)
            {
                for (int i = 1; i < road.Count; i++)
                {
                    PlanetTile a = road[i - 1];
                    PlanetTile b = road[i];
                    if (grid.ApproxDistanceInTiles(a, b) <= 1.6f)
                    {
                        segments.Add(new Segment { a = grid.GetTileCenter(a), b = grid.GetTileCenter(b), style = StyleTrail });
                    }
                }
            }
        }

        /// <summary>A small hexagon through the centres of the settlement's neighbouring tiles.</summary>
        private void AddRing(WorldGrid grid, PlanetTile tile, int style)
        {
            neighbours.Clear();
            grid.GetTileNeighbors(tile, neighbours);
            for (int i = 0; i < neighbours.Count; i++)
            {
                PlanetTile a = neighbours[i];
                PlanetTile b = neighbours[(i + 1) % neighbours.Count];
                segments.Add(new Segment { a = grid.GetTileCenter(a), b = grid.GetTileCenter(b), style = style });
            }
        }

        private static void AddOnce(List<Faction> list, Faction f)
        {
            if (f != null && !list.Contains(f))
            {
                list.Add(f);
            }
        }

        private static bool NearestPair(Faction a, Faction b, out Settlement sa, out Settlement sb)
        {
            sa = null;
            sb = null;
            float best = float.MaxValue;
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = 0; i < all.Count; i++)
            {
                Settlement x = all[i];
                if (x?.Faction != a)
                {
                    continue;
                }
                for (int j = 0; j < all.Count; j++)
                {
                    Settlement y = all[j];
                    if (y?.Faction != b || y.Tile.Layer != x.Tile.Layer)
                    {
                        continue;
                    }
                    float d = Find.WorldGrid.ApproxDistanceInTiles(x.Tile, y.Tile);
                    if (d < best)
                    {
                        best = d;
                        sa = x;
                        sb = y;
                    }
                }
            }
            return sa != null;
        }
    }

    /// <summary>World-map toggle next to the vanilla overlay buttons.</summary>
    [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    public static class Patch_LivingWorldOverlayToggle
    {
        private static Texture2D icon;

        public static void Postfix(WidgetRow row, bool worldView)
        {
            if (!worldView || row == null || LivingWorldMod.Global == null)
            {
                return;
            }
            icon ??= ContentFinder<Texture2D>.Get("LivingWorld/UI/OverlayToggle", reportFailure: false)
                ?? BaseContent.BadTex;
            bool on = LivingWorldMod.Global.overlaysEnabled;
            row.ToggleableIcon(ref on, icon, "LivingWorld_Overlay_Toggle".Translate());
            if (on != LivingWorldMod.Global.overlaysEnabled)
            {
                LivingWorldMod.Global.overlaysEnabled = on;
                LivingWorldMod.Global.Write();
            }
        }
    }
}
