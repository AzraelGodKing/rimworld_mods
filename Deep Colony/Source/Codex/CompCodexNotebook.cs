using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    public class CompProperties_CodexNotebook : CompProperties
    {
        public CompProperties_CodexNotebook()
        {
            compClass = typeof(CompCodexNotebook);
        }
    }

    public class CompCodexNotebook : ThingComp
    {
        public string projectDefName = string.Empty;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref projectDefName, "dcCodexProject");
        }

        public override string CompInspectStringExtra()
        {
            if (!DeepColonySettings.Get.enableCodex)
            {
                return null;
            }
            if (string.IsNullOrEmpty(projectDefName))
            {
                return "DC_Codex_Empty".Translate();
            }
            ResearchProjectDef proj = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(projectDefName);
            return "DC_Codex_Holds".Translate(proj?.LabelCap.ToString() ?? projectDefName);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!DeepColonySettings.Get.enableCodex)
            {
                yield break;
            }
            yield return new Command_Action
            {
                defaultLabel = "DC_Codex_Record".Translate(),
                defaultDesc = "DC_Codex_RecordDesc".Translate(),
                action = RecordCurrent,
            };
        }

        private void RecordCurrent()
        {
            ResearchProjectDef cur = Find.ResearchManager.GetProject();
            if (cur == null)
            {
                Messages.Message("DC_Codex_NoProject".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            projectDefName = cur.defName;
            Messages.Message("DC_Codex_Wrote".Translate(cur.LabelCap), parent, MessageTypeDefOf.TaskCompletion);
        }
    }
}
