using RimWorld;
using Verse;

namespace Nemesis
{
    /// <summary>
    /// Persistent alert while a hunt is active. Auto-discovered by RimWorld's AlertManager.
    /// </summary>
    public class Alert_NemesisHunt : Alert
    {
        public Alert_NemesisHunt()
        {
            defaultLabel = "Nemesis_Alert_Hunt_Label".Translate("?", "?");
            defaultExplanation = "Nemesis_Alert_Hunt_Explanation".Translate("?", "?", "?", "?", "?", "?");
            defaultPriority = AlertPriority.Medium;
        }

        public override AlertPriority Priority
        {
            get
            {
                NemesisData data = GameComponent_Nemesis.Instance?.Data;
                if (data != null && data.active && data.Phase >= NemesisHuntPhase.Reckoning)
                    return AlertPriority.High;
                return AlertPriority.Medium;
            }
        }

        public override string GetLabel()
        {
            NemesisData data = GameComponent_Nemesis.Instance?.Data;
            if (data == null || !data.active)
                return defaultLabel;
            string name = data.nemesisName ?? "?";
            return "Nemesis_Alert_Hunt_Label".Translate(name, data.PhaseLabelKeyed());
        }

        public override TaggedString GetExplanation()
        {
            NemesisData data = GameComponent_Nemesis.Instance?.Data;
            if (data == null || !data.active)
                return defaultExplanation;

            string name = data.nemesisName ?? "?";
            string phase = data.PhaseLabelKeyed();
            string target = "?";
            if (data.targetMode == NemesisTargetMode.Pawn)
            {
                if (!string.IsNullOrEmpty(data.targetPawnName))
                    target = data.targetPawnName;
                else
                {
                    Pawn p = GameComponent_Nemesis.Instance?.FindTargetPawn();
                    if (p != null)
                        target = p.LabelShort;
                }
            }
            else
                target = "Nemesis_Alert_Hunt_TargetColony".Translate();

            int maxEsc = NemesisMod.Settings?.maxEscapes ?? 4;
            return "Nemesis_Alert_Hunt_Explanation".Translate(
                name,
                phase,
                target,
                data.escapeCount,
                maxEsc,
                data.HuntDays);
        }

        public override AlertReport GetReport()
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || !comp.IsEngaged)
                return false;
            NemesisData data = comp.Data;
            if (data == null || !data.active)
                return false;

            if (data.targetMode == NemesisTargetMode.Pawn)
            {
                Pawn target = comp.FindTargetPawn();
                if (target != null)
                    return AlertReport.CulpritIs(target);
            }

            return AlertReport.Active;
        }
    }
}
