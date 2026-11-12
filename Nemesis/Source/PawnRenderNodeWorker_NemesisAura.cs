#nullable enable
using System;
using UnityEngine;
using Verse;

namespace Nemesis
{
    /// <summary>
    /// Draws the Nemesis aura from <c>Nemesis_BattleHardened</c> (AZR-536): it grows and
    /// brightens from level 1 to 8, and is hidden when the visuals setting is off or the pawn
    /// is dead. Everything else is vanilla render-tree drawing.
    /// </summary>
    /// <remarks>
    /// AZR-742: never draw this MoteGlow node while vanilla is baking the shared pawn texture
    /// atlas (<see cref="PawnRenderFlags.Cache"/> / <see cref="PawnRenderFlags.DrawNow"/>).
    /// Atlas blit uses immediate <c>DrawMeshNow</c> (MaterialPropertyBlock is ignored), and an
    /// oversized additive glow can bleed into neighbouring atlas cells or throw mid-bake and
    /// leave <c>PawnCacheCamera.rect</c> unrestored — which matches "every pawn invisible" plus
    /// map-edge colour flicker when a nemesis is on the map.
    /// </remarks>
    public class PawnRenderNodeWorker_NemesisAura : PawnRenderNodeWorker
    {
        private static readonly Color AuraColor = new Color(0.92f, 0.16f, 0.2f);
        private static bool loggedAuraDrawError;

        private static float Level(PawnRenderNode node) => Mathf.Clamp(node.hediff?.Severity ?? 1f, 1f, 8f);

        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!(NemesisMod.Settings?.enableNemesisVisuals ?? true)) return false;
            if (parms.pawn == null || parms.pawn.Dead) return false;
            // Skip atlas / immediate-mode paths — live world draws use Graphics.DrawMesh + MPB.
            if (parms.flags.FlagSet(PawnRenderFlags.Cache) || parms.flags.FlagSet(PawnRenderFlags.DrawNow))
                return false;
            return base.CanDrawNow(node, parms);
        }

        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            float s = 0.75f + 0.06f * Level(node);
            return base.ScaleFor(node, parms) * s;
        }

        public override MaterialPropertyBlock GetMaterialPropertyBlock(PawnRenderNode node, Material material, PawnDrawParms parms)
        {
            try
            {
                MaterialPropertyBlock block = base.GetMaterialPropertyBlock(node, material, parms);
                // Vanilla returns null only when the graphic is missing; Draw() then calls
                // block.Clear(), so never hand back null when a material was requested.
                if (block == null)
                    block = node.MatPropBlock;
                Color c = AuraColor;
                c.a = 0.3f + 0.08f * Level(node);
                block.SetColor(ShaderPropertyIDs.Color, c);
                return block;
            }
            catch (Exception e)
            {
                if (!loggedAuraDrawError)
                {
                    loggedAuraDrawError = true;
                    Log.Error("[Nemesis] Aura material property block failed once; skipping tint for this draw. " + e);
                }
                // Prefer vanilla tint over suppressing the whole pawn draw loop.
                try
                {
                    return base.GetMaterialPropertyBlock(node, material, parms) ?? node.MatPropBlock;
                }
                catch
                {
                    return node.MatPropBlock;
                }
            }
        }
    }
}
