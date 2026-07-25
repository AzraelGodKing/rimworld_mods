using UnityEngine;
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
        // Rimatomics coolant loop bonus (existing).
        private float shaftCoolingBuffer;

        // Tankless pocket buffer for storage-based channels (DBH / VEF / Rimefeller).
        private float shaftResourceBuffer;

        public const float MaxResourceBuffer = 250f;

        public CompProperties_ShaftFluidTie Props => (CompProperties_ShaftFluidTie)props;

        public ShaftFluidBackend Backend => ShaftFluidRegistry.Get(Props.channel);

        public float ShaftCoolingBuffer => shaftCoolingBuffer;

        public float ShaftResourceBuffer => shaftResourceBuffer;

        public float ShaftResourceBufferRoom => Mathf.Max(0f, MaxResourceBuffer - shaftResourceBuffer);

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
            // Lazy VEF reconnect only when a side has no net yet (not every pulse).
            if (backend is VefPipeSystemBackend && (topNet == null || botNet == null))
            {
                if (topNet == null)
                {
                    VefPipeNetworkUtil.ReconnectJunction(parent);
                    topNet = backend.GetNetFromJunction(parent);
                }
                if (botNet == null)
                {
                    VefPipeNetworkUtil.ReconnectJunction(partner.parent);
                    botNet = partner.Backend.GetNetFromJunction(partner.parent);
                }
            }
            backend.DriveTie(topNet, botNet, this, partner);
        }

        internal float TakeShaftCoolingBuffer(float amount)
        {
            float moved = Mathf.Min(amount, shaftCoolingBuffer);
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

        internal float TakeShaftResourceBuffer(float amount)
        {
            float moved = Mathf.Min(amount, shaftResourceBuffer);
            shaftResourceBuffer -= moved;
            return moved;
        }

        internal void AddShaftResourceBuffer(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }
            shaftResourceBuffer = Mathf.Min(MaxResourceBuffer, shaftResourceBuffer + amount);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref shaftCoolingBuffer, "strataShaftCoolingBuffer", 0f);
            Scribe_Values.Look(ref shaftResourceBuffer, "strataShaftResourceBuffer", 0f);
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
                return "Strata_FluidTie_NotConnected".Translate(backend.Label);
            }
            string line = "Strata_FluidTie_Linked".Translate(backend.Label);
            if (shaftResourceBuffer > 0.1f)
            {
                line += "\n" + "Strata_FluidTie_Buffer".Translate(
                    backend.Label, shaftResourceBuffer.ToString("F0"), MaxResourceBuffer.ToString("F0"));
            }
            return line;
        }
    }
}
