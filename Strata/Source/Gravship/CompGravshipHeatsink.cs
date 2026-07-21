using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    public class CompProperties_GravshipHeatsink : CompProperties
    {
        public float maxHeat = 100f;
        public float dissipatePerHour = 8f;
        public float heatPushPerSecond = 12f;

        public CompProperties_GravshipHeatsink()
        {
            compClass = typeof(CompGravshipHeatsink);
        }
    }

    /// <summary>VGE-style heatsink: stores launch heat and bleeds it into the room as warmth.</summary>
    public class CompGravshipHeatsink : ThingComp
    {
        public CompProperties_GravshipHeatsink Props => (CompProperties_GravshipHeatsink)props;

        private float storedHeat;

        public float StoredHeat => storedHeat;
        public float MaxHeat => Props.maxHeat;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref storedHeat, "strataGravHeat", 0f);
        }

        public float AcceptHeat(float amount)
        {
            if (amount <= 0f)
            {
                return 0f;
            }
            float space = Mathf.Max(0f, Props.maxHeat - storedHeat);
            float take = Mathf.Min(space, amount);
            storedHeat += take;
            return take;
        }

        public override void CompTick()
        {
            base.CompTick();
            if (storedHeat <= 0f || parent?.Map == null)
            {
                return;
            }
            CompPowerTrader power = parent.GetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
            {
                return;
            }

            // ~ dissipatePerHour over 2500 ticks.
            float burn = Props.dissipatePerHour / 2500f;
            float step = Mathf.Min(storedHeat, burn);
            storedHeat -= step;
            if (step > 0f)
            {
                Room room = parent.GetRoom();
                room?.PushHeat(Props.heatPushPerSecond / 60f * (step / Mathf.Max(burn, 0.0001f)));
            }
        }

        public override string CompInspectStringExtra()
        {
            return "Strata_GravshipHeatsinkStored".Translate(
                storedHeat.ToString("F0"), Props.maxHeat.ToString("F0"));
        }
    }
}
