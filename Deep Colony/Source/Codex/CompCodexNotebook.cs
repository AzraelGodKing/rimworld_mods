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
            yield return new Command_Action
            {
                defaultLabel = "DC_Codex_Resume".Translate(),
                defaultDesc = "DC_Codex_ResumeDesc".Translate(),
                action = ResumeStored,
            };
        }

        private void ResumeStored()
        {
            if (string.IsNullOrEmpty(projectDefName))
            {
                Messages.Message("DC_Codex_Empty".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            ResearchProjectDef proj = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(projectDefName);
            if (proj == null)
            {
                Messages.Message("DC_Codex_Unknown".Translate(projectDefName), MessageTypeDefOf.RejectInput);
                return;
            }
            if (proj.IsFinished)
            {
                Messages.Message("DC_Codex_AlreadyDone".Translate(proj.LabelCap), MessageTypeDefOf.NeutralEvent);
                return;
            }
            Find.ResearchManager.SetCurrentProject(proj);
            Messages.Message("DC_Codex_Resumed".Translate(proj.LabelCap), parent, MessageTypeDefOf.TaskCompletion);
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
