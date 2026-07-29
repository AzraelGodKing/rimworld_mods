using Verse;

namespace Strata
{
    // V3.5: one source of truth per sealed room (or outdoor/cavern volume).
    // AtmosphereMapComponent.Cloud is the backing store; this type is the
    // public-facing volume identity for ventilation labels and tooling.
    public enum AtmosphereVolumeKind : byte
    {
        SealedRoom = 0,
        OpenCavern = 1,
        OutdoorAnchor = 2,
        AmbientLocked = 3, // surface / A+ enclosed — no O₂–CO₂ metabolism threat
    }

    public static class AtmosphereVolumeUtility
    {
        public static AtmosphereVolumeKind KindForRoom(Map map, Room room)
        {
            if (map == null || room == null)
            {
                return AtmosphereVolumeKind.SealedRoom;
            }
            if (AtmosphericMix.ForcesAmbientInEnclosedRooms(map))
            {
                return room.UsesOutdoorTemperature
                    ? AtmosphereVolumeKind.OutdoorAnchor
                    : AtmosphereVolumeKind.AmbientLocked;
            }
            if (room.UsesOutdoorTemperature)
            {
                return AtmosphereVolumeKind.OpenCavern;
            }
            return AtmosphereVolumeKind.SealedRoom;
        }

        // Metabolism (pawn inhale / plant exchange) only on B1+ sealed volumes.
        public static bool AllowsBreathMetabolism(Map map, Room room)
        {
            if (map == null || room == null || room.UsesOutdoorTemperature)
            {
                return false;
            }
            if (AtmosphericMix.ForcesAmbientInEnclosedRooms(map))
            {
                return false;
            }
            return StrataMapUtility.IsUnderground(map) && !StrataMapUtility.IsUpperLevel(map);
        }
    }
}
