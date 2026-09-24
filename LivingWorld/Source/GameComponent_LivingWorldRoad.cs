using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LivingWorld
{
    // AZR-205 first slice — remember player caravan tiles as "the road".
    public class GameComponent_LivingWorldRoad : GameComponent
    {
        private List<int> roadTiles = new List<int>();
        private bool stretchLetterSent;

        public GameComponent_LivingWorldRoad(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref roadTiles, "lwRoadTiles", LookMode.Value);
            Scribe_Values.Look(ref stretchLetterSent, "lwRoadLetterSent", false);
            roadTiles ??= new List<int>();
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 2500 != 0)
            {
                return;
            }
            List<Caravan> caravans = Find.WorldObjects.Caravans;
            HediffDef veteran = DefDatabase<HediffDef>.GetNamedSilentFail("LW_Hediff_RoadVeteran");
            for (int i = 0; i < caravans.Count; i++)
            {
                Caravan c = caravans[i];
                if (c == null || !c.IsPlayerControlled)
                {
                    continue;
                }
                int tile = c.Tile;
                    if (!roadTiles.Contains(tile))
                {
                    roadTiles.Add(tile);
                    while (roadTiles.Count > 80)
                    {
                        roadTiles.RemoveAt(0);
                    }
                    if (roadTiles.Count % 4 == 0)
                    {
                        LivingWorldWayCamps.TryFoundOn(c.Tile);
                    }
                    if (!stretchLetterSent && roadTiles.Count >= 8)
                    {
                        stretchLetterSent = true;
                        Find.LetterStack.ReceiveLetter(
                            "LivingWorld_LetterLabel_RoadStretch".Translate(),
                            "LivingWorld_LetterText_RoadStretch".Translate(roadTiles.Count),
                            LetterDefOf.NeutralEvent);
                    }
                }
                if (veteran != null && roadTiles.Count >= 8)
                {
                    TryMarkVeterans(c, veteran);
                }
            }
        }

        private static void TryMarkVeterans(Caravan caravan, HediffDef veteran)
        {
            List<Pawn> pawns = caravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn?.health?.hediffSet == null || !pawn.IsColonist)
                {
                    continue;
                }
                if (pawn.health.hediffSet.GetFirstHediffOfDef(veteran) != null)
                {
                    continue;
                }
                if (!Rand.Chance(0.04f))
                {
                    continue;
                }
                pawn.health.AddHediff(veteran);
                Messages.Message("LivingWorld_RoadVeteran".Translate(pawn.LabelShortCap),
                    pawn, MessageTypeDefOf.PositiveEvent);
            }
        }

        public IReadOnlyList<int> Tiles => roadTiles;
    }
}
