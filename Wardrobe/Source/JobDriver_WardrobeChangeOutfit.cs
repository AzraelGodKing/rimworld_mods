using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Wardrobe
{
    public class JobDriver_WardrobeChangeOutfit : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => pawn == null || pawn.Dead || pawn.Downed);
            yield return InitAndGoto();
            yield return DoSwap();
        }

        private Toil InitAndGoto()
        {
            Toil toil = new Toil();
            toil.initAction = () =>
            {
                GameComponent_Wardrobe comp = WardrobeUtility.Comp;
                WardrobePawnState state = comp?.GetState(pawn, create: false);
                Zone_Stockpile stock = state != null
                    ? WardrobeUtility.FindStockpile(pawn.Map, state.stockpileId)
                    : null;
                if (stock == null || !stock.Cells.Any())
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                IntVec3 cell = IntVec3.Invalid;
                foreach (IntVec3 c in stock.Cells)
                {
                    if (pawn.CanReach(c, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        cell = c;
                        break;
                    }
                }

                if (!cell.IsValid)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                pawn.pather.StartPath(cell, PathEndMode.ClosestTouch);
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            return toil;
        }

        private Toil DoSwap()
        {
            Toil toil = new Toil();
            toil.initAction = () =>
            {
                GameComponent_Wardrobe comp = WardrobeUtility.Comp;
                if (comp == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                WardrobePawnState state = comp.GetState(pawn, create: false);
                if (state == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                int id = pawn.thingIDNumber;
                if (comp.pendingRestore.Contains(id))
                {
                    Restore(pawn, state);
                    state.activeTrigger = WardrobeTrigger.None;
                    state.snapshotThingIds.Clear();
                    state.snapshotDefNames.Clear();
                    comp.pendingRestore.Remove(id);
                }
                else if (comp.pendingEnter.TryGetValue(id, out WardrobeTrigger trigger))
                {
                    Enter(pawn, state, trigger);
                    comp.pendingEnter.Remove(id);
                }

                state.cooldownTicks = WardrobeUtility.SwapCooldownTicks;
                EndJobWith(JobCondition.Succeeded);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private static void Enter(Pawn pawn, WardrobePawnState state, WardrobeTrigger trigger)
        {
            ApparelPolicy policy = WardrobeUtility.FindPolicy(state.PolicyIdFor(trigger));
            Zone_Stockpile stock = WardrobeUtility.FindStockpile(pawn.Map, state.stockpileId);
            if (policy == null || stock == null)
            {
                return;
            }

            WardrobeUtility.CaptureSnapshot(pawn, state);
            List<Apparel> gear = WardrobeUtility.FindPolicyApparelInStockpile(pawn, stock, policy);
            for (int i = 0; i < gear.Count; i++)
            {
                WardrobeUtility.TryWear(pawn, gear[i]);
            }

            state.activeTrigger = trigger;
        }

        private static void Restore(Pawn pawn, WardrobePawnState state)
        {
            Zone_Stockpile stock = WardrobeUtility.FindStockpile(pawn.Map, state.stockpileId);

            // Drop currently worn managed gear into wardrobe when possible.
            if (pawn.apparel?.WornApparel != null)
            {
                List<Apparel> worn = new List<Apparel>(pawn.apparel.WornApparel);
                for (int i = 0; i < worn.Count; i++)
                {
                    Apparel a = worn[i];
                    if (state.snapshotThingIds.Contains(a.thingIDNumber))
                    {
                        continue;
                    }

                    IntVec3 dropCell = stock != null && stock.Cells.Any()
                        ? stock.Cells.RandomElement()
                        : pawn.Position;
                    pawn.apparel.TryDrop(a, out _, dropCell, true);
                }
            }

            // Re-wear snapshot pieces still on the map / already worn.
            for (int i = 0; i < state.snapshotThingIds.Count; i++)
            {
                int tid = state.snapshotThingIds[i];
                Apparel already = null;
                if (pawn.apparel?.WornApparel != null)
                {
                    for (int w = 0; w < pawn.apparel.WornApparel.Count; w++)
                    {
                        if (pawn.apparel.WornApparel[w].thingIDNumber == tid)
                        {
                            already = pawn.apparel.WornApparel[w];
                            break;
                        }
                    }
                }

                if (already != null)
                {
                    continue;
                }

                Thing found = WardrobeUtility.FindThingById(pawn.Map, tid);
                if (found is Apparel apparel)
                {
                    WardrobeUtility.TryWear(pawn, apparel);
                    continue;
                }

                // Fallback: same def from stockpile.
                if (i < state.snapshotDefNames.Count && stock != null)
                {
                    ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(state.snapshotDefNames[i]);
                    if (def == null)
                    {
                        continue;
                    }

                    foreach (IntVec3 cell in stock.Cells)
                    {
                        List<Thing> things = pawn.Map.thingGrid.ThingsListAtFast(cell);
                        for (int t = 0; t < things.Count; t++)
                        {
                            if (things[t] is Apparel cand && cand.def == def)
                            {
                                WardrobeUtility.TryWear(pawn, cand);
                                goto nextSnapshot;
                            }
                        }
                    }
                }

                nextSnapshot: ;
            }
        }
    }
}
