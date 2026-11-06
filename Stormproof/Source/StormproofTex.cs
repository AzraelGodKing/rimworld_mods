using UnityEngine;
using Verse;

namespace Stormproof
{
    // Gizmo icons, loaded once. Stormproof's own art, so no gizmo borrows a
    // vanilla placeholder.
    [StaticConstructorOnStartup]
    internal static class StormproofTex
    {
        public static readonly Texture2D Almanac = ContentFinder<Texture2D>.Get("Stormproof/Buildings/WeatherForecaster");
        public static readonly Texture2D Fulgurite = ContentFinder<Texture2D>.Get("Stormproof/Items/Fulgurite");
        public static readonly Texture2D Schedule = ContentFinder<Texture2D>.Get("Stormproof/Buildings/GridMonitor");
        public static readonly Texture2D Hold = ContentFinder<Texture2D>.Get("Stormproof/Buildings/LoadShedder");
        public static readonly Texture2D StormCaller = ContentFinder<Texture2D>.Get("Stormproof/Buildings/StormCaller");
        public static readonly Texture2D CutoffLower = ContentFinder<Texture2D>.Get("UI/Commands/TempLower");
        public static readonly Texture2D CutoffRaise = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise");
    }
}
