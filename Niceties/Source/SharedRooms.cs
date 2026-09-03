using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Niceties
{
    public class CompProperties_SharedRoom : CompProperties
    {
        public CompProperties_SharedRoom()
        {
            compClass = typeof(CompSharedRoom);
        }
    }

    public class CompSharedRoom : ThingComp
    {
        private bool shared;

        public bool Shared => shared;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref shared, "nicetiesSharedRoom", false);
        }

        public void SetShared(bool value)
        {
            shared = value;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Building_Bed bed = parent as Building_Bed;
            if (!SharedRooms.ShowsGizmo(bed))
            {
                yield break;
            }

            yield return new Command_Toggle
            {
                defaultLabel = "Niceties_ShareRoom".Translate(),
                defaultDesc = "Niceties_ShareRoomTip".Translate(),
                icon = SharedRooms.GizmoIcon,
                isActive = () => SharedRooms.IsMarked(bed),
                toggleAction = () => SharedRooms.Toggle(bed)
            };
        }

        public override string CompInspectStringExtra()
        {
            Building_Bed bed = parent as Building_Bed;
            if (!SharedRooms.ShowsGizmo(bed) || !SharedRooms.IsMarked(bed))
            {
                return null;
            }

            return "Niceties_ShareRoomInspect".Translate();
        }
    }

    internal static class SharedRooms
    {
        private static Texture2D gizmoIcon;

        internal static Texture2D GizmoIcon
        {
            get
            {
                if (gizmoIcon == null)
                {
                    gizmoIcon = ContentFinder<Texture2D>.Get("UI/Commands/AssignOwner", reportFailure: false);
                }

                return gizmoIcon;
            }
        }

        internal static void InjectComps()
        {
            int injected = 0;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def?.building == null || !def.building.bed_humanlike)
                {
                    continue;
                }

                if (def.thingClass == null || !typeof(Building_Bed).IsAssignableFrom(def.thingClass))
                {
                    continue;
                }

                if (def.comps == null)
                {
                    def.comps = new List<CompProperties>();
                }

                bool already = false;
                for (int i = 0; i < def.comps.Count; i++)
                {
                    if (def.comps[i] is CompProperties_SharedRoom)
                    {
                        already = true;
                        break;
                    }
                }

                if (already)
                {
                    continue;
                }

                def.comps.Add(new CompProperties_SharedRoom());
                injected++;
            }

            Log.Message("[Niceties] Shared-room gizmo ready on " + injected + " bed def(s).");
        }

        internal static bool Enabled()
        {
            return NicetiesMod.Settings != null && NicetiesMod.Settings.enableSharedRooms;
        }

        internal static bool ShowsGizmo(Building_Bed bed)
        {
            if (!Enabled() || bed == null || !bed.Spawned)
            {
                return false;
            }

            if (bed.Medical || bed.ForPrisoners)
            {
                return false;
            }

            return bed.def?.building != null && bed.def.building.bed_humanlike
                && bed.def.building.bed_countsForBedroomOrBarracks;
        }

        internal static bool IsMarked(Building_Bed bed)
        {
            return bed != null && IsMarked(bed.GetRoom());
        }

        internal static bool IsMarked(Room room)
        {
            if (!Enabled() || room == null || room.PsychologicallyOutdoors)
            {
                return false;
            }

            foreach (Building_Bed bed in room.ContainedBeds)
            {
                if (bed.Medical || bed.ForPrisoners)
                {
                    continue;
                }

                CompSharedRoom comp = bed.GetComp<CompSharedRoom>();
                if (comp != null && comp.Shared)
                {
                    return true;
                }
            }

            return false;
        }

        internal static void Toggle(Building_Bed bed)
        {
            if (!ShowsGizmo(bed))
            {
                return;
            }

            SetMarked(bed.GetRoom(), !IsMarked(bed));
        }

        internal static void SetMarked(Room room, bool marked)
        {
            if (room == null)
            {
                return;
            }

            foreach (Building_Bed bed in room.ContainedBeds)
            {
                if (bed.Medical || bed.ForPrisoners)
                {
                    continue;
                }

                CompSharedRoom comp = bed.GetComp<CompSharedRoom>();
                if (comp != null)
                {
                    comp.SetShared(marked);
                }
            }

            room.Notify_BedTypeChanged();
        }

        internal static bool HasRoommate(Pawn sleeper)
        {
            if (sleeper == null)
            {
                return false;
            }

            Room room = sleeper.GetRoom();
            if (room == null || room.PsychologicallyOutdoors)
            {
                Building_Bed owned = sleeper.ownership?.OwnedBed;
                room = owned != null && owned.Spawned ? owned.GetRoom() : null;
            }

            if (room == null)
            {
                return false;
            }

            foreach (Building_Bed bed in room.ContainedBeds)
            {
                if (bed.def?.building == null || !bed.def.building.bed_humanlike)
                {
                    continue;
                }

                List<Pawn> owners = bed.OwnersForReading;
                if (owners == null)
                {
                    continue;
                }

                for (int i = 0; i < owners.Count; i++)
                {
                    Pawn owner = owners[i];
                    if (owner != null && owner != sleeper && owner.RaceProps != null && owner.RaceProps.Humanlike)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool ShouldSkipDisturbedSleep(Pawn sleeper)
        {
            if (!Enabled() || NicetiesMod.Settings == null || !NicetiesMod.Settings.skipDisturbedSleepWhenSharing)
            {
                return false;
            }

            if (sleeper == null)
            {
                return false;
            }

            Room room = sleeper.GetRoom();
            if (IsMarked(room) || HasRoommate(sleeper))
            {
                return true;
            }

            return false;
        }
    }
}
