#nullable enable
using UnityEngine;
using Verse;

namespace Nemesis
{
    /// <summary>
    /// Draws the Nemesis aura from <c>Nemesis_BattleHardened</c> (AZR-536): it grows and
    /// brightens from level 1 to 8, and is hidden when the visuals setting is off or the pawn
    /// is dead. Everything else is vanilla render-tree drawing.
    /// </summary>
    public class PawnRenderNodeWorker_NemesisAura : PawnRenderNodeWorker
    {
        private static readonly Color AuraColor = new Color(0.92f, 0.16f, 0.2f);

        private static float Level(PawnRenderNode node) => Mathf.Clamp(node.hediff?.Severity ?? 1f, 1f, 8f);

        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!(NemesisMod.Settings?.enableNemesisVisuals ?? true)) return false;
            if (parms.pawn == null || parms.pawn.Dead) return false;
            return base.CanDrawNow(node, parms);
        }

        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            float s = 0.75f + 0.06f * Level(node);
            return base.ScaleFor(node, parms) * s;
        }

        public override MaterialPropertyBlock GetMaterialPropertyBlock(PawnRenderNode node, Material material, PawnDrawParms parms)
        {
            MaterialPropertyBlock block = base.GetMaterialPropertyBlock(node, material, parms);
            Color c = AuraColor;
            c.a = 0.3f + 0.08f * Level(node);
            block.SetColor(ShaderPropertyIDs.Color, c);
            return block;
        }
    }
}
