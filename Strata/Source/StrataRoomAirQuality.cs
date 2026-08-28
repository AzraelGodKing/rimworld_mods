using RimWorld;
using Verse;

namespace Strata
{
    // Room-stats card: O₂ / CO₂ / smoke with fine / stale / dangerous stages.
    public static class StrataRoomAir
    {
        public static void Read(Room room, out float o2, out float co2, out float smoke)
        {
            o2 = AtmosphericMix.AmbientOxygen;
            co2 = AtmosphericMix.CarbonDioxideFraction;
            smoke = 0f;
            if (room?.Map == null)
            {
                return;
            }
            AtmosphereMapComponent atmos = room.Map.GetComponent<AtmosphereMapComponent>();
            if (atmos == null || !atmos.TryGetRoomDensity(room, out float[] density) || density == null)
            {
                return;
            }
            StrataGasDef oxygen = StrataGasDefOf.Strata_Oxygen;
            StrataGasDef carbon = StrataGasDefOf.Strata_CarbonDioxide;
            StrataGasDef smokeDef = StrataGasDefOf.Strata_Smoke;
            if (oxygen != null && oxygen.index >= 0 && oxygen.index < density.Length)
            {
                o2 = density[oxygen.index];
            }
            if (carbon != null && carbon.index >= 0 && carbon.index < density.Length)
            {
                co2 = density[carbon.index];
            }
            if (smokeDef != null && smokeDef.index >= 0 && smokeDef.index < density.Length)
            {
                smoke = density[smokeDef.index];
            }
        }
    }

    public class RoomStatWorker_StrataGas : RoomStatWorker
    {
        public override float GetScore(Room room)
        {
            StrataRoomAir.Read(room, out float o2, out float co2, out float smoke);
            if (def?.defName == "Strata_RoomOxygen")
            {
                return o2 * 100f;
            }
            if (def?.defName == "Strata_RoomCarbonDioxide")
            {
                return co2 * 100f;
            }
            return smoke * 100f;
        }
    }
}
