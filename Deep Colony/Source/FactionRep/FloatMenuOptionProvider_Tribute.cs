using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace DeepColony
{
    public class FloatMenuOptionProvider_Tribute : FloatMenuOptionProvider
    {
        protected override bool Drafted => false;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;

        public override IEnumerable<FloatMenuOption> GetOptionsFor(
            Thing clickedThing, FloatMenuContext context)
        {
            if (!DeepColonySettings.Get.enableFactionRep) yield break;
            if (!DeepColonySettings.Get.enableApologyTribute) yield break;
            if (!TributeUtility.IsTributeGood(clickedThing)) yield break;

            Pawn actor = context.FirstSelectedPawn;
            if (actor == null || !actor.IsColonistPlayerControlled) yield break;
            if (clickedThing.IsForbidden(actor)) yield break;

            var factions = TributeUtility.TributeFactions().OrderBy(f => f.Name).ToList();
            if (factions.Count == 0) yield break;

            var sub = new List<FloatMenuOption>();
            foreach (Faction f in factions)
            {
                Faction local = f;
                Thing thing = clickedThing;
                if (!TributeUtility.CanTributeThing(thing, local, out string reason))
                {
                    sub.Add(new FloatMenuOption(
                        "DC_TributeThingTo".Translate(local.Name.Named("FACTION")) + " (" + reason + ")",
                        null)
                    { Disabled = true });
                }
                else
                {
                    sub.Add(new FloatMenuOption(
                        "DC_TributeThingTo".Translate(local.Name.Named("FACTION")),
                        () => TributeUtility.TrySendTributeThing(thing, local)));
                }
            }

            yield return new FloatMenuOption(
                "DC_TributeThingMenu".Translate(clickedThing.LabelNoCount.Named("GIFT")),
                () => Find.WindowStack.Add(new FloatMenu(sub)));
        }
    }
}
