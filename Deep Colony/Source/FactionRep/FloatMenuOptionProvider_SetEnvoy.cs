using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace DeepColony
{
    public class FloatMenuOptionProvider_SetEnvoy : FloatMenuOptionProvider
    {
        protected override bool Drafted => false;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;

        public override IEnumerable<FloatMenuOption> GetOptionsFor(
            Pawn targetPawn, FloatMenuContext context)
        {
            if (!DeepColonySettings.Get.enableFactionRep) yield break;
            if (!targetPawn.IsColonistPlayerControlled) yield break;

            Pawn actor = context.FirstSelectedPawn;
            Pawn envoy = targetPawn;
            if (actor == null) yield break;
            if (!envoy.IsColonistPlayerControlled) yield break;
            if (envoy.skills?.GetSkill(SkillDefOf.Social)?.TotallyDisabled == true) yield break;

            var factions = FactionEnvoyUtility.CandidateFactions().ToList();
            if (factions.Count == 0) yield break;

            List<FloatMenuOption> sub = new List<FloatMenuOption>();
            Faction current = FactionEnvoyUtility.GetEnvoyFaction(envoy);
            if (current != null)
            {
                sub.Add(new FloatMenuOption(
                    "DC_ClearEnvoy".Translate(envoy.LabelShort.Named("PAWN"), current.Name.Named("FACTION")),
                    () => FactionEnvoyUtility.ClearEnvoy(envoy)));
            }

            foreach (Faction f in factions.OrderBy(x => x.Name))
            {
                Faction local = f;
                string label = "DC_SetEnvoy".Translate(envoy.LabelShort.Named("PAWN"), local.Name.Named("FACTION"));
                if (current == local)
                    label += " " + "DC_EnvoyCurrent".Translate();
                sub.Add(new FloatMenuOption(label, () => FactionEnvoyUtility.SetEnvoy(envoy, local)));
            }

            yield return new FloatMenuOption(
                "DC_EnvoyMenu".Translate(envoy.LabelShort.Named("PAWN")),
                () => Find.WindowStack.Add(new FloatMenu(sub)));
        }
    }
}
