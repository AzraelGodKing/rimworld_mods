using UnityEngine;
using Verse;

namespace DeepColony
{
    // Loaded once on the main thread at startup, not on every gizmo build (AZR-619).
    [StaticConstructorOnStartup]
    internal static class DeepColonyTex
    {
        internal static readonly Texture2D PerkTree =
            ContentFinder<Texture2D>.Get("UI/PerkTree", false) ?? BaseContent.BadTex;
    }
}
