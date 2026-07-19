using System.Collections.Generic;
using Verse;

namespace Strata
{
    [StaticConstructorOnStartup]
    public static class ShaftFluidRegistry
    {
        private static readonly Dictionary<string, ShaftFluidBackend> byChannel = new Dictionary<string, ShaftFluidBackend>();

        static ShaftFluidRegistry()
        {
            Register(new DbhPlumbingBackend());
            Register(new DchPlumbingBackend("dch_heating", "DCH heating", "Heating"));
            Register(new DchPlumbingBackend("dch_air", "DCH air-con", "Air"));
            Register(new RimatomicsCoolingBackend());
            Register(new VefPipeSystemBackend("vhge_helixien", "Helixien gas", "VHGE_HelixienNet"));
            Register(new VteAcPipeBackend());
            Register(new RimefellerPipelineBackend("rimefeller_crude", "Rimefeller crude", "Crude", fuelMode: false));
            Register(new RimefellerPipelineBackend("rimefeller_fuel", "Rimefeller chemfuel", "Fuel", fuelMode: true));
            foreach (VefPipeSystemBackend backend in VefPipeNetDiscovery.DiscoverBackends())
            {
                if (!byChannel.ContainsKey(backend.ChannelId))
                {
                    Register(backend);
                }
            }
        }

        public static IReadOnlyDictionary<string, ShaftFluidBackend> All => byChannel;

        public static ShaftFluidBackend Get(string channelId)
        {
            if (channelId.NullOrEmpty())
            {
                return null;
            }
            byChannel.TryGetValue(channelId, out ShaftFluidBackend backend);
            return backend;
        }

        private static void Register(ShaftFluidBackend backend)
        {
            byChannel[backend.ChannelId] = backend;
        }
    }
}
