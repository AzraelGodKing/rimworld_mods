using Verse;

namespace Strata
{
    public class Building_StrataCagedBird : Building
    {
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            ReleaseBird();
            base.Destroy(mode);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            ReleaseBird();
            base.DeSpawn(mode);
        }

        private void ReleaseBird()
        {
            GetComp<CompCanaryCage>()?.ReleaseContained();
            GetComp<CompBirdCage>()?.ReleaseContained();
        }
    }
}
