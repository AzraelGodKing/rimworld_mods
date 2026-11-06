#if RIMWORLD_1_7
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace Strata
{
    // RimWorld 1.7 draws with Vulkan and loads mod shaders as GLSL. Strata's
    // ShaderDefs are in 1.7/Defs/ShaderDefs and the GLSL in 1.7/Shaders/Strata.
    // Every caller keeps its 1.6 path as the fallback when a shader is missing.
    internal static class StrataShaders17
    {
        public const string BelowMask = "Strata_BelowMask";
        public const string GasOverlay = "Strata_GasOverlay";
        public const string Caustics = "Strata_Caustics";
        public const string DepthVignette = "Strata_DepthVignette";

        public static Material Make(string shaderDefName, Color color)
        {
            ShaderDef def = DefDatabase<ShaderDef>.GetNamedSilentFail(shaderDefName);
            Shader shader = def?.Shader;
            if (shader == null)
            {
                Log.WarningOnce("[Strata] 1.7 shader " + shaderDefName + " did not load; using the 1.6 drawing instead.",
                    shaderDefName.GetHashCode() ^ 0x5A17);
                return null;
            }
            return new Material(shader) { color = color };
        }

        // x = seconds, yz = map size in cells, w = how much the effect moves.
        public static void SetParams(Material material, Map map, float motion)
        {
            material.SetVector("_StrataParams",
                new Vector4(Time.realtimeSinceStartup, map.Size.x, map.Size.z, motion));
        }

        // Draws a mesh at an explicit render queue (1.7 takes the queue per
        // draw, so no material copy per queue is needed).
        public static void DrawAtQueue(Mesh mesh, Matrix4x4 matrix, Material material, int queue)
        {
            Graphics.DrawMesh(mesh, matrix, material, 0, null, 0, (MaterialPropertyBlock)null,
                ShadowCastingMode.Off, false, queue);
        }

        // One quad over the whole map, so texel (x, z) lands on cell (x, z).
        public static Matrix4x4 MapQuad(Map map, float altitude)
        {
            return Matrix4x4.TRS(
                new Vector3(map.Size.x * 0.5f, altitude, map.Size.z * 0.5f),
                Quaternion.identity,
                new Vector3(map.Size.x, 1f, map.Size.z));
        }
    }

    // A texture with one texel per map cell, drawn as one quad over the map.
    internal sealed class StrataMapTexture
    {
        private readonly string shaderDefName;
        private readonly Color color;
        private Texture2D texture;
        private Color32[] pixels;
        private Material material;
        private bool materialFailed;

        public StrataMapTexture(string shaderDefName, Color color)
        {
            this.shaderDefName = shaderDefName;
            this.color = color;
        }

        public Material Material
        {
            get
            {
                if (material == null && !materialFailed)
                {
                    material = StrataShaders17.Make(shaderDefName, color);
                    materialFailed = material == null;
                }
                return material;
            }
        }

        public bool Available => Material != null;

        // Cleared pixel buffer sized to the map; fill it, then Upload.
        public Color32[] BeginWrite(Map map)
        {
            int count = map.Size.x * map.Size.z;
            if (pixels == null || pixels.Length != count)
            {
                pixels = new Color32[count];
            }
            else
            {
                System.Array.Clear(pixels, 0, pixels.Length);
            }
            return pixels;
        }

        public void Upload(Map map)
        {
            if (texture == null || texture.width != map.Size.x || texture.height != map.Size.z)
            {
                texture = new Texture2D(map.Size.x, map.Size.z, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "Strata_" + shaderDefName,
                };
                if (Material != null)
                {
                    Material.mainTexture = texture;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false);
        }

        public bool HasTexture => texture != null;

        public void Draw(Map map, float altitude, float motion)
        {
            Material mat = Material;
            if (mat == null || texture == null)
            {
                return;
            }
            StrataShaders17.SetParams(mat, map, motion);
            Graphics.DrawMesh(MeshPool.plane10, StrataShaders17.MapQuad(map, altitude), mat, 0);
        }
    }
}
#endif
