using RimWorld;
using Verse;

namespace Homesteader
{
    public class CompProperties_BeautyWhenFueled : CompProperties
    {
        public float fueledBeauty = 40f;

        public float dryBeauty = 0f;

        public CompProperties_BeautyWhenFueled()
        {
            compClass = typeof(CompBeautyWhenFueled);
        }
    }

    /// <summary>
    /// Running-water vanity: +beauty only while CompRefuelable has fuel (water jugs).
    /// </summary>
    public class CompBeautyWhenFueled : ThingComp
    {
        public CompProperties_BeautyWhenFueled Props => (CompProperties_BeautyWhenFueled)props;

        public override float GetStatOffset(StatDef stat)
        {
            if (stat != StatDefOf.Beauty)
            {
                return 0f;
            }

            CompRefuelable fuel = parent.GetComp<CompRefuelable>();
            return fuel != null && fuel.HasFuel ? Props.fueledBeauty : Props.dryBeauty;
        }

        public override string CompInspectStringExtra()
        {
            CompRefuelable fuel = parent.GetComp<CompRefuelable>();
            if (fuel != null && !fuel.HasFuel)
            {
                return "Homesteader_FountainDry".Translate();
            }

            return null;
        }
    }
}
