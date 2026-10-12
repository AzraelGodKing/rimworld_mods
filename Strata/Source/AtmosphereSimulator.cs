using RimWorld;
using Verse;

namespace Strata
{
    // V3.5 atmosphere helpers: first-descent letter.
    // Edge flow / inject still live on AtmosphereMapComponent; this keeps the
    // player-facing polish in one place.
    public static class AtmosphereSimulator
    {
        public static void MaybeSendFirstDescentLetter(Map map)
        {
            if (map == null || Current.Game == null)
            {
                return;
            }
            if (StrataMod.Settings?.NaturalGasesActive != true)
            {
                return;
            }
            if (!StrataMapUtility.IsUnderground(map) || StrataMapUtility.IsUpperLevel(map))
            {
                return;
            }

            GameComponent_StrataAtmosphere gc = Current.Game.GetComponent<GameComponent_StrataAtmosphere>();
            if (gc == null || gc.firstB1AtmosphereLetterSent)
            {
                return;
            }
            gc.firstB1AtmosphereLetterSent = true;
            Find.LetterStack.ReceiveLetter(
                "Strata_Letter_FirstB1Air_Label".Translate(),
                "Strata_Letter_FirstB1Air_Text".Translate(),
                LetterDefOf.NeutralEvent,
                new TargetInfo(map.Center, map));
        }
    }

    public class GameComponent_StrataAtmosphere : GameComponent
    {
        public bool firstB1AtmosphereLetterSent;

        public GameComponent_StrataAtmosphere(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref firstB1AtmosphereLetterSent, "firstB1AtmosphereLetterSent", defaultValue: false);
        }
    }
}
