#nullable enable
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace DateNight
{
    /// <summary>
    /// Date flecks, venue glows and the "our spot" ring (AZR-558).
    /// Glows and rings are drawn per frame for the map on screen only: no Rand,
    /// no game state touched, so Multiplayer clients cannot drift. Vanilla
    /// shaders only (MoteGlow), so the same code runs on 1.6 and 1.7.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class DateNightVisuals
    {
        private const string TexRoot = "DateNight/Effects/";
        private const int AlphaSteps = 16;
        private const int BurstCount = 3;

        private static readonly Color CandleColor = new Color(1f, 0.72f, 0.38f);
        private static readonly Color StarfieldColor = new Color(0.62f, 0.72f, 1f);
        private static readonly Color DanceColor = new Color(1f, 0.55f, 0.7f);
        private static readonly Color SpotColor = new Color(1f, 0.45f, 0.6f);
        private static readonly Color BedColor = new Color(0.95f, 0.6f, 0.85f);

        private static readonly Texture2D? GlowTex = Load("Glow");
        private static readonly Texture2D? RingTex = Load("Ring");

        private static readonly Dictionary<(Texture2D, Color, int), Material> Faded =
            new Dictionary<(Texture2D, Color, int), Material>();

        public static bool Enabled => DateNightMod.Settings.dateVisuals;

        /// <summary>The Date Night fleck, or the vanilla heart when effects are off.</summary>
        public static FleckDef Fleck(FleckDef? special)
        {
            return Enabled && special != null ? special : FleckDefOf.Heart;
        }

        /// <summary>
        /// A small burst over the pawn. Always the same number of throws, with or
        /// without effects, so the setting never changes how often Rand is used.
        /// </summary>
        public static void Burst(Pawn pawn, FleckDef? special)
        {
            if (pawn?.Map == null || !pawn.Spawned)
            {
                return;
            }
            FleckDef def = Fleck(special);
            for (int i = 0; i < BurstCount; i++)
            {
                FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, def, 0.6f + 0.3f * i);
            }
        }

        /// <summary>Called every frame from <see cref="GameComponent_DateNight"/>.</summary>
        public static void Draw()
        {
            if (!Enabled || GlowTex == null || WorldRendererUtility.WorldSelected)
            {
                return;
            }
            Map? map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }

            IReadOnlyList<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                DrawVenueGlow(colonists[i]);
            }
            DrawOurSpot(map);
        }

        private static void DrawVenueGlow(Pawn pawn)
        {
            if (!(pawn.jobs?.curDriver is JobDriver_Date date) || !date.AtVenue)
            {
                return;
            }
            Pawn? partner = pawn.CurJob?.GetTarget(TargetIndex.A).Pawn;
            // One glow per couple, drawn by the stable leader.
            if (partner == null || !partner.Spawned || !DateNightActivities.IsInitiator(pawn, partner))
            {
                return;
            }

            Vector3 mid = (pawn.DrawPos + partner.DrawPos) * 0.5f;
            float t = Time.realtimeSinceStartup;
            float seed = (pawn.thingIDNumber % 97) * 0.37f;
            switch (date.Activity)
            {
                case DateActivity.Dinner:
                    // Candle flicker on the table, above the table top.
                    DrawQuad(GlowTex!, mid, AltitudeLayer.BuildingOnTop, 1.6f + 0.08f * Mathf.Sin(t * 9f + seed),
                        CandleColor, 0.55f + 0.1f * Mathf.Sin(t * 13f + seed));
                    break;
                case DateActivity.Stargaze:
                    DrawQuad(GlowTex!, mid, AltitudeLayer.FloorEmplacement, 3.2f,
                        StarfieldColor, 0.25f + 0.08f * Mathf.Sin(t * 1.3f + seed));
                    break;
                case DateActivity.Dance:
                    DrawQuad(GlowTex!, mid, AltitudeLayer.FloorEmplacement, 3.6f + 0.3f * Mathf.Sin(t * 3f + seed),
                        DanceColor, 0.3f + 0.1f * Mathf.Sin(t * 3f + seed));
                    break;
            }
        }

        /// <summary>With one colonist selected: a ring on the couple's favourite spot and Lovin bed.</summary>
        private static void DrawOurSpot(Map map)
        {
            if (RingTex == null || !(Find.Selector.SingleSelectedThing is Pawn pawn)
                || pawn.Map != map || !pawn.IsColonist)
            {
                return;
            }
            Pawn? partner = DateNightRomance.FindCommittedPartner(pawn);
            if (partner == null)
            {
                return;
            }

            float pulse = 0.55f + 0.15f * Mathf.Sin(Time.realtimeSinceStartup * 2f);
            FavoriteVenue? fav = DateNightVenues.Get(pawn, partner);
            if (fav != null && fav.mapId == map.uniqueID && fav.cell.IsValid && fav.cell.InBounds(map))
            {
                DrawQuad(RingTex, fav.cell.ToVector3Shifted(), AltitudeLayer.MetaOverlays, 2.2f, SpotColor, pulse);
            }
            Building_Bed? bed = DateNightUtility.PeekRendezvousBed(pawn, partner);
            if (bed != null)
            {
                DrawQuad(RingTex, bed.TrueCenter(), AltitudeLayer.MetaOverlays, 2.6f, BedColor, pulse);
            }
        }

        private static void DrawQuad(Texture2D tex, Vector3 center, AltitudeLayer layer, float size, Color color, float alpha)
        {
            Material? mat = FadedMat(tex, color, alpha);
            if (mat == null)
            {
                return;
            }
            Vector3 pos = center.WithY(layer.AltitudeFor());
            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(size, 1f, size));
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }

        // One material per (texture, colour, alpha step), made once and reused.
        private static Material? FadedMat(Texture2D tex, Color color, float alpha)
        {
            int step = Mathf.RoundToInt(Mathf.Clamp01(alpha) * AlphaSteps);
            if (step <= 0)
            {
                return null;
            }
            var key = (tex, color, step);
            if (!Faded.TryGetValue(key, out Material? mat) || mat == null)
            {
                mat = MaterialPool.MatFrom(tex, ShaderDatabase.MoteGlow,
                    new Color(color.r, color.g, color.b, (float)step / AlphaSteps));
                Faded[key] = mat;
            }
            return mat;
        }

        private static Texture2D? Load(string name)
        {
            Texture2D? tex = ContentFinder<Texture2D>.Get(TexRoot + name, reportFailure: false);
            if (tex == null)
            {
                Log.WarningOnce("[DateNight] Missing effect texture " + TexRoot + name + "; that effect is skipped.",
                    ("DateNight" + name).GetHashCode());
            }
            return tex;
        }
    }
}
