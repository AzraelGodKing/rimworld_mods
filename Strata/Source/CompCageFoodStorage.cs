using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Strata
{
    public class CompProperties_CageFoodStorage : CompProperties
    {
        public int maxStacks = 5;

        public CompProperties_CageFoodStorage()
        {
            compClass = typeof(CompCageFoodStorage);
        }
    }

    public class CompCageFoodStorage : ThingComp, IThingHolder
    {
        private ThingOwner innerContainer;

        private CompProperties_CageFoodStorage Props => (CompProperties_CageFoodStorage)props;

        public ThingOwner InnerContainer => GetDirectlyHeldThings();

        public bool HasFood => innerContainer != null && innerContainer.Any;

        public static bool UsesSustainMode =>
            StrataMod.Settings != null && StrataMod.Settings.cageSustainHunger;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            GetDirectlyHeldThings();
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            if (innerContainer != null && innerContainer.Any && map != null)
            {
                IntVec3 dropCell = parent.PositionHeld.IsValid ? parent.PositionHeld : IntVec3.Invalid;
                if (dropCell.IsValid && dropCell.InBounds(map))
                    innerContainer.TryDropAll(dropCell, map, ThingPlaceMode.Near);
                else
                    innerContainer.ClearAndDestroyContents();
            }
            base.PostDeSpawn(map, mode);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && innerContainer == null)
            {
                GetDirectlyHeldThings();
            }
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            if (innerContainer == null)
            {
                innerContainer = new ThingOwner<Thing>(this);
            }
            return innerContainer;
        }

        public bool CanStore(Thing thing)
        {
            if (thing == null || UsesSustainMode)
            {
                return false;
            }
            if (!IsBirdFoodDef(thing.def))
            {
                return false;
            }
            if (innerContainer.TotalStackCount >= Props.maxStacks
                && !innerContainer.Contains(thing.def))
            {
                return false;
            }
            return innerContainer.CanAcceptAnyOf(thing, canMergeWithExistingStacks: true);
        }

        public bool TryFeed(Pawn bird)
        {
            if (bird?.needs?.food == null || UsesSustainMode)
            {
                return false;
            }
            Need_Food food = bird.needs.food;
            if (food.CurLevelPercentage >= 0.99f)
            {
                return true;
            }
            if (!HasFood)
            {
                return false;
            }
            for (int i = innerContainer.Count - 1; i >= 0; i--)
            {
                Thing stored = innerContainer[i];
                if (!IsBirdFood(stored, bird))
                {
                    continue;
                }
                float nutrition = FoodUtility.GetNutrition(bird, stored, stored.def);
                if (nutrition <= 0f)
                {
                    continue;
                }
                int bites = stored.stackCount;
                for (int b = 0; b < bites; b++)
                {
                    stored.SplitOff(1).Destroy();
                    food.CurLevel = Mathf.Min(food.MaxLevel, food.CurLevel + nutrition);
                    if (food.CurLevelPercentage >= 0.99f)
                    {
                        return true;
                    }
                }
            }
            return food.CurLevelPercentage >= 0.25f;
        }

        public bool TryStockFromNearby(int amount = 5)
        {
            if (UsesSustainMode || parent.Map == null)
            {
                return false;
            }
            int room = Props.maxStacks - innerContainer.TotalStackCount;
            if (room <= 0)
            {
                Messages.Message(
                    "Strata_CageFood_Full".Translate(parent.LabelShort),
                    parent,
                    MessageTypeDefOf.RejectInput,
                    historical: false);
                return false;
            }
            Thing source = GenClosest.ClosestThingReachable(
                parent.Position,
                parent.Map,
                ThingRequest.ForGroup(ThingRequestGroup.FoodSource),
                PathEndMode.ClosestTouch,
                TraverseParms.For(TraverseMode.NoPassClosedDoors),
                9999f,
                thing => CanTakeFromMap(thing));
            if (source == null)
            {
                Messages.Message(
                    "Strata_CageFood_NoFood".Translate(parent.LabelShort),
                    parent,
                    MessageTypeDefOf.RejectInput,
                    historical: false);
                return false;
            }
            int move = Mathf.Min(amount, source.stackCount, room);
            if (move <= 0)
            {
                return false;
            }
            Thing chunk = source.SplitOff(move);
            if (!innerContainer.TryAdd(chunk))
            {
                GenPlace.TryPlaceThing(chunk, parent.Position, parent.Map, ThingPlaceMode.Near);
                return false;
            }
            Messages.Message(
                "Strata_CageFood_Stocked".Translate(parent.LabelShort, chunk.Label),
                parent,
                MessageTypeDefOf.PositiveEvent);
            return true;
        }

        private bool CanTakeFromMap(Thing thing)
        {
            if (thing == null || thing.IsForbidden(Faction.OfPlayer))
            {
                return false;
            }
            return IsBirdFoodDef(thing.def);
        }

        private static bool IsBirdFoodDef(ThingDef def)
        {
            if (def?.ingestible == null || !def.IsNutritionGivingIngestible)
            {
                return false;
            }
            return def == ThingDefOf.Hay
                || def == ThingDefOf.Kibble
                || def.ingestible.preferability >= FoodPreferability.RawBad;
        }

        private static bool IsBirdFood(Thing thing, Pawn bird)
        {
            return thing != null
                && bird?.RaceProps != null
                && FoodUtility.WillEat(bird, thing);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (UsesSustainMode)
            {
                yield break;
            }
            yield return new Command_Action
            {
                defaultLabel = "Strata_CageFood_StockLabel".Translate(),
                defaultDesc = "Strata_CageFood_StockDesc".Translate(Props.maxStacks),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/LoadTransporter", reportFailure: false),
                action = () => TryStockFromNearby(Props.maxStacks),
            };
        }

        public override string CompInspectStringExtra()
        {
            if (UsesSustainMode)
            {
                return "Strata_CageFood_SustainMode".Translate();
            }
            if (!HasFood)
            {
                return "Strata_CageFood_Empty".Translate();
            }
            return "Strata_CageFood_Count".Translate(innerContainer.TotalStackCount, Props.maxStacks);
        }
    }
}
