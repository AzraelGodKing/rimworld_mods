using RimWorld;

namespace Stormproof
{
    // The one place that totals a grid's batteries. Raw stored energy: an
    // EMP-stunned battery still counts, the same as the inspect numbers.
    internal static class BatteryMath
    {
        internal static void Sum(PowerNet net, out float stored, out float capacity)
        {
            stored = 0f;
            capacity = 0f;
            if (net?.batteryComps == null)
            {
                return;
            }
            var batteries = net.batteryComps;
            for (int i = 0; i < batteries.Count; i++)
            {
                CompPowerBattery battery = batteries[i];
                stored += battery.StoredEnergy;
                capacity += battery.Props.storedEnergyMax;
            }
        }

        internal static float Capacity(PowerNet net)
        {
            Sum(net, out _, out float capacity);
            return capacity;
        }
    }
}
