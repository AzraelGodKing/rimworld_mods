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
            var sb = new System.Text.StringBuilder();
            string link = parent.TryGetComp<CompShaftFluidJunctionLink>()?.LinkInspectString();
            if (!link.NullOrEmpty())
            {
                sb.AppendLine(link);
            }
            ShaftFluidBackend backend = Backend;
            if (backend == null)
            {
                return sb.Length > 0 ? sb.ToString().TrimEnd() : null;
            }
            if (!backend.IsActive)
            {
                sb.Append($"Fluid tie ({backend.Label}): mod not loaded");
                return sb.ToString().TrimEnd();
            }
            object net = backend.GetNetFromJunction(parent);
            if (net == null)
            {
                sb.Append($"Fluid tie ({backend.Label}): not connected — wire pipes to this junction");
            }
            else
            {
                sb.Append($"Fluid tie ({backend.Label}): linked to local {backend.Label} network");
            }
            return sb.ToString().TrimEnd();
        }
    }
}
