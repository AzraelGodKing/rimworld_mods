using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>E04 — lasting mood while this colonist is the last blood kin in the colony.</summary>
    public class ThoughtWorker_LastOfTheLine : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!DeepColonySettings.Get.enableFamilyJoin) return false;
            if (p == null || p.Dead || !p.IsColonistPlayerControlled) return false;
            if (!p.RaceProps.Humanlike) return false;
            var comp = p.TryGetComp<Comp_DeepColony>();
            if (comp == null || !comp.lastOfTheLine) return false;
            return ThoughtState.ActiveDefault;
        }
    }
}
