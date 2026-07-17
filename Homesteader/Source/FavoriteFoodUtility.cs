using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Homesteader
{
    public class GameComponent_HomesteaderFavorites : GameComponent
    {
        private Dictionary<int, string> favoritesByPawnId = new Dictionary<int, string>();

        public GameComponent_HomesteaderFavorites(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref favoritesByPawnId, "favoritesByPawnId", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && favoritesByPawnId == null)
            {
                favoritesByPawnId = new Dictionary<int, string>();
            }
        }

        public ThingDef GetFavorite(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            EnsureFavorite(pawn);
            if (!favoritesByPawnId.TryGetValue(pawn.thingIDNumber, out string defName))
            {
                return null;
            }

            return DefDatabase<ThingDef>.GetNamedSilentFail(defName);
        }

        public void EnsureFavorite(Pawn pawn)
        {
            if (pawn?.RaceProps?.Humanlike != true || pawn.Faction != Faction.OfPlayer)
            {
                return;
            }

            if (favoritesByPawnId.ContainsKey(pawn.thingIDNumber))
            {
                return;
            }

            ThingDef pick = FavoriteFoodUtility.PickRandomFavorite();
            if (pick == null)
            {
                return;
            }

            favoritesByPawnId[pawn.thingIDNumber] = pick.defName;
        }

        public void RerollFavorite(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            favoritesByPawnId.Remove(pawn.thingIDNumber);
            EnsureFavorite(pawn);
        }
    }

    public static class FavoriteFoodUtility
    {
        public static readonly string[] FavoriteDefNames =
        {
            "Homesteader_Flapjacks",
            "Homesteader_ToastAndJam",
            "Homesteader_PloughmansLunch",
            "Homesteader_HoneyPorridge",
            "Homesteader_PumpkinPie",
            "Homesteader_ButtermilkBiscuits",
            "Homesteader_HeartyStew",
            "Homesteader_TrailStew",
            "Homesteader_Bread",
            "Homesteader_Porridge",
            "Homesteader_Jam",
            "Homesteader_Cheese",
            "Homesteader_SmokedCheese",
            "Homesteader_WaxedCheese",
            "Homesteader_Mead",
            "Homesteader_Cider",
            "Homesteader_MapleSyrup",
            "Homesteader_Honey",
        };

        public static GameComponent_HomesteaderFavorites Comp =>
            Current.Game?.GetComponent<GameComponent_HomesteaderFavorites>();

        public static ThingDef PickRandomFavorite()
        {
            List<ThingDef> pool = FavoriteDefNames
                .Select(DefDatabase<ThingDef>.GetNamedSilentFail)
                .Where(d => d?.ingestible != null)
                .ToList();
            if (pool.Count == 0)
            {
                return null;
            }

            return pool.RandomElement();
        }

        public static ThingDef GetFavorite(Pawn pawn) => Comp?.GetFavorite(pawn);

        public static bool IsFavorite(Pawn pawn, ThingDef foodDef)
        {
            if (pawn == null || foodDef == null)
            {
                return false;
            }

            ThingDef fav = GetFavorite(pawn);
            return fav != null && fav == foodDef;
        }
    }
}
