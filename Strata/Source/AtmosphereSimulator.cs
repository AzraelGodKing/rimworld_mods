using RimWorld;
using Verse;

namespace Strata
{
    // V3.5 atmosphere helpers: ventilation labels, first-descent letter.
    // Edge flow / inject still live on AtmosphereMapComponent; this keeps the
    // player-facing polish in one place.
    public static class AtmosphereSimulator
    {
        public static string VentilationLabel(Map map, Room room, AtmosphereMapComponent atmosphere)
        {
            if (map == null || room == null)
            {
                return "Strata_Atmos_Unknown".Translate();
            }

            AtmosphereVolumeKind kind = AtmosphereVolumeUtility.KindForRoom(map, room);
            switch (kind)
            {
                case AtmosphereVolumeKind.AmbientLocked:
                    return "Strata_Atmos_AmbientLocked".Translate();
                case AtmosphereVolumeKind.OutdoorAnchor:
                    return "Strata_Atmos_Outdoor".Translate();
                case AtmosphereVolumeKind.OpenCavern:
                    return "Strata_Atmos_OpenCavern".Translate();
            }

            if (atmosphere != null && atmosphere.RoomIsProperlyVentilated(room))
            {
                return "Strata_Atmos_Ventilated".Translate();
            }
            if (RoomTouchesOpenShaft(map, room))
            {
                return "Strata_Atmos_ShaftLinked".Translate();
            }
            return "Strata_Atmos_Sealed".Translate();
        }

        public static string FormatRoomStatusLine(Map map, Room room, AtmosphereMapComponent atmosphere)
        {
            string vent = VentilationLabel(map, room, atmosphere);
            if (atmosphere == null || room == null)
            {
                return vent;
            }
            float o2 = atmosphere.DensityInRoom(room, StrataGasDefOf.Strata_Oxygen);
            float co2 = atmosphere.DensityInRoom(room, StrataGasDefOf.Strata_CarbonDioxide);
            return "Strata_Atmos_StatusLine".Translate(
                vent,
                (o2 * 100f).ToString("F0"),
                (co2 * 100f).ToString("F0"));
        }

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

        private static bool RoomTouchesOpenShaft(Map map, Room room)
        {
            if (map == null || room == null)
            {
                return false;
            }
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing == null || !thing.Spawned)
                {
                    continue;
                }
                if (thing.GetRoom() != room)
                {
                    continue;
                }
                if (thing is Building_StairsDown down)
                {
                    return !down.Sealed;
                }
                if (thing is MapPortal)
                {
                    return true;
                }
            }
            return false;
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
