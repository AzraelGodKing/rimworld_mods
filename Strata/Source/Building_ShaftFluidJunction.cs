using Verse;

namespace Strata
{
    // Shaft-side fluid tap — mirrors Building_ShaftConduit for power. Stays a Building
    // on the Building layer; pipe-net membership comes from mod pipe comps (see
    // ShaftFluid_Comps.xml), same role as CompPowerShaft + transmitsPower.
    public class Building_ShaftFluidJunction : Building
    {
    }
}
