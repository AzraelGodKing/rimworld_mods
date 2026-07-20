using Verse;

namespace Strata
{
    public class CompProperties_ShaftFluidTie : CompProperties
    {
        public string channel = "vhge_helixien";

        public CompProperties_ShaftFluidTie()
        {
            compClass = typeof(CompShaftFluidTie);
        }
    }

    public class CompShaftFluidTie : ThingComp
    {
        private float shaftCoolingBuffer;

        public CompProperties_ShaftFluidTie Props => (CompProperties_ShaftFluidTie)props;

        public ShaftFluidBackend Backend => ShaftFluidRegistry.Get(Props.channel);

        public float ShaftCoolingBuffer => shaftCoolingBuffer;

        public static CompShaftFluidTie FindOn(Thing thing, string channel)
        {
            if (thing is not ThingWithComps twc || channel.NullOrEmpty())
            {
                return null;
            }
            for (int i = 0; i < twc.AllComps.Count; i++)
            {
                if (twc.AllComps[i] is CompShaftFluidTie tie && tie.Props.channel == channel)
                {
                    return tie;
                }
            }
            return null;
        }

        public static bool HasChannel(Thing thing, string channel)
        {
            return FindOn(thing, channel) != null;
        }

        public static bool HasChannelPrefix(Thing thing, string prefix)
        {
            if (thing is not ThingWithComps twc || prefix.NullOrEmpty())
            {
                return false;
            }
            for (int i = 0; i < twc.AllComps.Count; i++)
            {
                if (twc.AllComps[i] is CompShaftFluidTie tie
                    && tie.Props.channel != null
                    && tie.Props.channel.StartsWith(prefix))
                {
                    return true;
                }
            }
            return false;
        }

        public void DriveTie(CompShaftFluidTie partner)
        {
            ShaftFluidBackend backend = Backend;
            if (backend == null || !backend.IsActive || partner?.Backend == null)
            {
                return;
            }
            object topNet = backend.GetNetFromJunction(parent);
            object botNet = partner.Backend.GetNetFromJunction(partner.parent);
            backend.DriveTie(topNet, botNet, this, partner);
        }

        internal float TakeShaftCoolingBuffer(float amount)
        {
            float moved = UnityEngine.Mathf.Min(amount, shaftCoolingBuffer);
            shaftCoolingBuffer -= moved;
            return moved;
        }

        internal void AddShaftCoolingBuffer(float amount)
        {
            if (amount > 0f)
            {
                shaftCoolingBuffer += amount;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref shaftCoolingBuffer, "strataShaftCoolingBuffer", 0f);
        }

        public override string CompInspectStringExtra()
        {
            ShaftFluidBackend backend = Backend;
            // Soft-skip inactive optional networks (Omni carries every channel).
            if (backend == null || !backend.IsActive)
            {
                return null;
            }
            object net = backend.GetNetFromJunction(parent);
            if (net == null)
            {
                return $"Fluid tie ({backend.Label}): not connected — wire pipes to this junction";
            }
            return $"Fluid tie ({backend.Label}): linked to local {backend.Label} network";
        }
    }
}
