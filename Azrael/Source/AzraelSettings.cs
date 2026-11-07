#nullable enable
using Verse;

namespace Azrael
{
    public class AzraelSettings : ModSettings
    {
        /// <summary>
        /// Series incident pick weights under the Azrael storyteller (AZR-727). Off gives
        /// plain Azrael pacing: every factor reads 1.
        /// </summary>
        public bool seriesIncidentWeights = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref seriesIncidentWeights, "seriesIncidentWeights", true);
        }
    }
}
