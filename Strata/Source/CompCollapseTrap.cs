using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    public class CompProperties_CollapseTrap : CompProperties
    {
        public int minBlobCells = 3;
        public int maxBlobCells = 7;
        public float blobRadius = 2.0f;

        public CompProperties_CollapseTrap()
        {
            compClass = typeof(CompCollapseTrap);
        }
    }

    // Player-rigged cave-in: place under thick rock roof underground, arm it,
    // and it drops a CaveIn-sized roof blob when a hostile steps adjacent —
    // or when triggered manually (AZR-273).
    public class CompCollapseTrap : ThingComp
    {
        private bool armed = true;
        private bool triggered;

        public CompProperties_CollapseTrap Props => (CompProperties_CollapseTrap)props;

        public bool Armed => armed && !triggered;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref armed, "strataCollapseArmed", defaultValue: true);
            Scribe_Values.Look(ref triggered, "strataCollapseTriggered", defaultValue: false);
        }

        public override void CompTickRare()
        {
            if (!Armed || parent?.Map == null)
            {
                return;
            }
            if (TryFindHostileAdjacent(out _))
            {
                Trigger("Strata_CollapseTrap_TriggeredHostile".Translate(parent.LabelShort));
            }
        }

        public override string CompInspectStringExtra()
        {
            if (triggered)
            {
                return "Strata_CollapseTrap_Spent".Translate();
            }
            if (!HasThickRoof())
            {
                return "Strata_CollapseTrap_NoRoof".Translate();
            }
            return armed
                ? "Strata_CollapseTrap_Armed".Translate()
                : "Strata_CollapseTrap_Disarmed".Translate();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (triggered)
            {
                yield break;
            }

            yield return new Command_Toggle
            {
                defaultLabel = "Strata_CollapseTrap_ArmLabel".Translate(),
                defaultDesc = "Strata_CollapseTrap_ArmDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", reportFailure: false)
                    ?? BaseContent.BadTex,
                isActive = () => armed,
                toggleAction = () => armed = !armed,
            };

            var trigger = new Command_Action
            {
                defaultLabel = "Strata_CollapseTrap_ManualLabel".Translate(),
                defaultDesc = "Strata_CollapseTrap_ManualDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/DestroyThing", reportFailure: false)
                    ?? BaseContent.BadTex,
                action = () => Trigger("Strata_CollapseTrap_TriggeredManual".Translate(parent.LabelShort)),
            };
            if (!HasThickRoof())
            {
                trigger.Disable("Strata_CollapseTrap_NoRoof".Translate());
            }
            yield return trigger;
        }

        private bool HasThickRoof()
        {
            Map map = parent?.Map;
            return map != null
                && parent.Position.InBounds(map)
                && map.roofGrid.RoofAt(parent.Position) == RoofDefOf.RoofRockThick;
        }

        private bool TryFindHostileAdjacent(out Pawn hostile)
        {
            hostile = null;
            Map map = parent.Map;
            foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(parent))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is Pawn pawn
                        && pawn.HostileTo(Faction.OfPlayer)
                        && !pawn.Downed
                        && !pawn.Dead)
                    {
                        hostile = pawn;
                        return true;
                    }
                }
            }
            return false;
        }

        public void Trigger(string message)
        {
            if (triggered || parent?.Map == null)
            {
                return;
            }
            triggered = true;
            armed = false;
            Map map = parent.Map;
            IntVec3 root = parent.Position;
            var blob = new List<IntVec3>();
            if (map.roofGrid.RoofAt(root) == RoofDefOf.RoofRockThick)
            {
                blob.Add(root);
            }
            int target = Rand.RangeInclusive(Props.minBlobCells, Props.maxBlobCells);
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(root, Props.blobRadius, useCenter: false))
            {
                if (blob.Count >= target)
                {
                    break;
                }
                if (cell.InBounds(map)
                    && map.roofGrid.RoofAt(cell) == RoofDefOf.RoofRockThick
                    && cell.GetEdifice(map) == null)
                {
                    blob.Add(cell);
                }
            }

            if (blob.Count > 0)
            {
                RoofCollapserImmediate.DropRoofInCells(blob, map);
            }

            if (!message.NullOrEmpty())
            {
                Messages.Message(message, new TargetInfo(root, map), MessageTypeDefOf.ThreatBig);
            }

            if (!parent.Destroyed)
            {
                parent.Destroy(DestroyMode.KillFinalize);
            }
        }
    }

    public class PlaceWorker_CollapseTrap : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(
            BuildableDef def,
            IntVec3 center,
            Rot4 rot,
            Map map,
            Thing thingToIgnore = null,
            Thing thing = null)
        {
            if (!StrataMapUtility.IsUnderground(map))
            {
                return "Strata_CollapseTrap_NeedUnderground".Translate();
            }
            if (map.roofGrid.RoofAt(center) != RoofDefOf.RoofRockThick)
            {
                return "Strata_CollapseTrap_NeedThickRoof".Translate();
            }
            return true;
        }

        public override void DrawGhost(
            ThingDef def,
            IntVec3 center,
            Rot4 rot,
            Color ghostCol,
            Thing thing = null)
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }
            var cells = new List<IntVec3> { center };
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 2f, useCenter: false))
            {
                if (cells.Count >= 7)
                {
                    break;
                }
                if (cell.InBounds(map) && map.roofGrid.RoofAt(cell) == RoofDefOf.RoofRockThick)
                {
                    cells.Add(cell);
                }
            }
            GenDraw.DrawFieldEdges(cells, new Color(0.85f, 0.35f, 0.15f, 0.85f));
        }
    }
}
