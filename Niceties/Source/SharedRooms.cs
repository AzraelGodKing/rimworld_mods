#nullable enable
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Niceties
{
    /// <summary>Loaded once at startup instead of looked up every frame (AZR-532).</summary>
    [StaticConstructorOnStartup]
    internal static class SharedRoomTex
    {
        internal static readonly Texture2D Icon =
            ContentFinder<Texture2D>.Get("UI/Commands/AssignOwner", reportFailure: false) ?? BaseContent.BadTex;
    }

    /// <summary>
    /// Share-room toggle that works on a multi-selection: every selected bed's room is set to
    /// the opposite of the first one's state, once per room. The default grouped toggle flips
    /// each bed, so two beds in one room cancelled out (AZR-568).
    /// </summary>
    public class Command_ShareRoom : Command_Toggle
    {
        private readonly Building_Bed? bed;

        public Command_ShareRoom(Building_Bed? bed)
        {
            this.bed = bed;
        }

        public override void ProcessGroupInput(Event ev, List<Gizmo> group)
        {
            if (bed == null || group.Count <= 1)
            {
                base.ProcessGroupInput(ev, group);
                return;
            }

            bool target = !SharedRooms.IsMarked(bed);
            HashSet<int> done = new HashSet<int>();
            for (int i = 0; i < group.Count; i++)
            {
                if (group[i] is Command_ShareRoom cmd && cmd.bed != null && cmd.bed.Spawned
                    && done.Add(cmd.bed.GetRoom()?.ID ?? -1))
                {
                    SharedRooms.SetMarkedForBed(cmd.bed, target);
                }
            }

            (target ? SoundDefOf.Checkbox_TurnedOn : SoundDefOf.Checkbox_TurnedOff).PlayOneShotOnCamera();
        }
    }

    /// <summary>
    /// A titled colonist whose bed is in a room marked shared (with someone other than a
    /// partner) loses the personal bedroom their title asks for. Vanilla's own alert says the
    /// bedroom is wrong; this one says the share mark is why (AZR-568).
    /// </summary>
    public class Alert_NicetiesNobleSharedRoom : Alert
    {
        private readonly List<Pawn> culprits = new List<Pawn>();

        public Alert_NicetiesNobleSharedRoom()
        {
            defaultLabel = "Niceties_NobleSharedRoom".Translate();
            defaultPriority = AlertPriority.Medium;
        }

        public override TaggedString GetExplanation()
        {
            return "Niceties_NobleSharedRoomDesc".Translate(culprits.Select(p => p.LabelShort).ToCommaList(true));
        }

        public override AlertReport GetReport()
        {
            culprits.Clear();
            if (!ModsConfig.RoyaltyActive || !NicetiesMod.Settings.sharedRoomNobleAlert || !SharedRooms.Enabled())
            {
                return false;
            }

            foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonistsSpawned)
            {
                if (pawn.royalty == null || pawn.royalty.HighestTitleWithBedroomRequirements() == null)
                {
                    continue;
                }

                Building_Bed? bed = pawn.ownership?.OwnedBed;
                if (bed == null || !bed.Spawned || !SharedRooms.IsMarked(bed) || pawn.royalty.HasPersonalBedroom())
                {
                    continue;
                }

                culprits.Add(pawn);
            }

            return AlertReport.CulpritsAre(culprits);
        }
    }

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
            Building_Bed? bed = parent as Building_Bed;
            if (!SharedRooms.ShowsGizmo(bed))
            {
                yield break;
            }

            yield return new Command_ShareRoom(bed)
            {
                defaultLabel = "Niceties_ShareRoom".Translate(),
                defaultDesc = "Niceties_ShareRoomTip".Translate(),
                icon = SharedRoomTex.Icon,
                isActive = () => SharedRooms.IsMarked(bed),
                toggleAction = () => SharedRooms.Toggle(bed!)
            };
        }

        public override string? CompInspectStringExtra()
        {
            Building_Bed? bed = parent as Building_Bed;
            if (!SharedRooms.ShowsGizmo(bed) || !SharedRooms.IsMarked(bed))
            {
                return null;
            }

            string? names = SharedRooms.RoommateNames(bed.GetRoom());
            return string.IsNullOrEmpty(names)
                ? "Niceties_ShareRoomInspect".Translate().Resolve()
                : "Niceties_ShareRoomInspectWith".Translate(names).Resolve();
        }
    }

    internal static class SharedRooms
    {
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
            return NicetiesSim.Settings.enableSharedRooms;
        }

        internal static bool ShowsGizmo(Building_Bed? bed)
        {
            if (bed == null || !bed.Spawned || !Enabled())
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

        internal static bool IsMarked(Building_Bed? bed)
        {
            return bed != null && IsMarked(bed.GetRoom());
        }

        /// <summary>
        /// Uncached: room roles are only recalculated when a room changes, and a mark
        /// must show up in the very next recalculation.
        /// </summary>
        internal static bool IsMarked(Room? room)
        {
            if (room == null || room.PsychologicallyOutdoors || !Enabled())
            {
                return false;
            }

            return ScanMarked(room);
        }

        private static bool ScanMarked(Room room)
        {
            foreach (Building_Bed bed in room.ContainedBeds)
            {
                if (bed.Medical || bed.ForPrisoners)
                {
                    continue;
                }

                CompSharedRoom? comp = bed.GetComp<CompSharedRoom>();
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

            SetMarkedForBed(bed, !IsMarked(bed));
        }

        /// <summary>
        /// Registered as a Multiplayer sync method when Multiplayer is loaded, so a click
        /// marks the room on every client in the same tick. Takes the target state, not a
        /// flip, so a replayed call cannot invert it.
        /// </summary>
        public static void SetMarkedForBed(Building_Bed bed, bool marked)
        {
            if (!ShowsGizmo(bed))
            {
                return;
            }

            SetMarked(bed.GetRoom(), marked);
        }

        /// <summary>Cached mark (up to 250 ticks old; dropped on change), for display only.</summary>
        internal static bool IsMarkedForDisplay(Room room)
        {
            return Enabled() && !room.PsychologicallyOutdoors && Info(room).Marked;
        }

        internal static string? RoommateNames(Room? room)
        {
            return room == null ? null : Info(room).Names;
        }

        internal static void SetMarked(Room? room, bool marked)
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

                bed.GetComp<CompSharedRoom>()?.SetShared(marked);
            }

            RoomCache.Remove(room.ID);
            room.Notify_BedTypeChanged();
        }

        internal static bool HasRoommate(Pawn sleeper)
        {
            // Outdoor / caravan sleepers use vanilla disturbance — no owned-bed fallback (AZR-567).
            Room? room = sleeper.GetRoom();
            if (room == null || room.PsychologicallyOutdoors)
            {
                return false;
            }

            return Info(room).HasRoommateFor(sleeper);
        }

        /// <summary>
        /// Skip disturbance only when the disturber owns a bed in the sleeper's room (AZR-567).
        /// Visitors and non-roommate colonists still wake the sleeper. Cheap checks run first,
        /// and the room's owners come from a cache refreshed every 250 ticks (AZR-549).
        /// </summary>
        internal static bool ShouldSkipDisturbedSleep(Pawn sleeper, Pawn? source)
        {
            if (source == null || source == sleeper)
            {
                return false;
            }

            NicetiesSimFlags settings = NicetiesSim.Settings;
            if (!settings.enableSharedRooms || !settings.skipDisturbedSleepWhenSharing)
            {
                return false;
            }

            // The same early outs vanilla takes before it would disturb anyone.
            if (sleeper.needs?.mood == null || sleeper.Awake() || sleeper.Faction == null || !sleeper.Faction.IsPlayer)
            {
                return false;
            }

            Room? room = sleeper.GetRoom();
            if (room == null || room.PsychologicallyOutdoors)
            {
                return false;
            }

            RoomInfo info = Info(room);
            if (!info.Marked && !info.HasRoommateFor(sleeper))
            {
                return false;
            }

            return info.Owners.Contains(source);
        }

        private const int RoomCacheTicks = 250;
        private const int RoomCachePruneSize = 256;

        private sealed class RoomInfo
        {
            public int Tick = int.MinValue;
            public bool Marked;
            public readonly List<Pawn> Owners = new List<Pawn>();
            public string? Names;

            public bool HasRoommateFor(Pawn sleeper)
            {
                for (int i = 0; i < Owners.Count; i++)
                {
                    if (Owners[i] != sleeper)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private static readonly Dictionary<int, RoomInfo> RoomCache = new Dictionary<int, RoomInfo>();
        private static readonly List<int> PruneScratch = new List<int>();
        private static readonly List<string> NameScratch = new List<string>();

        internal static void ClearCache()
        {
            RoomCache.Clear();
        }

        /// <summary>
        /// Humanlike owners of the room's non-medical, non-prisoner beds, the shared mark and
        /// the inspect names, rebuilt at most every 250 ticks or when the mark changes.
        /// </summary>
        private static RoomInfo Info(Room room)
        {
            int tick = Find.TickManager?.TicksGame ?? 0;
            if (RoomCache.TryGetValue(room.ID, out RoomInfo? info)
                && tick >= info.Tick && tick - info.Tick < RoomCacheTicks)
            {
                return info;
            }

            if (info == null)
            {
                if (RoomCache.Count >= RoomCachePruneSize)
                {
                    Prune(tick);
                }

                info = new RoomInfo();
                RoomCache[room.ID] = info;
            }

            info.Tick = tick;
            info.Owners.Clear();
            info.Marked = false;
            NameScratch.Clear();
            foreach (Building_Bed bed in room.ContainedBeds)
            {
                if (bed.Medical || bed.ForPrisoners || bed.def?.building == null || !bed.def.building.bed_humanlike)
                {
                    continue;
                }

                CompSharedRoom? comp = bed.GetComp<CompSharedRoom>();
                if (comp != null && comp.Shared)
                {
                    info.Marked = true;
                }

                List<Pawn> owners = bed.OwnersForReading;
                for (int i = 0; i < owners.Count; i++)
                {
                    Pawn owner = owners[i];
                    if (owner?.RaceProps != null && owner.RaceProps.Humanlike && !info.Owners.Contains(owner))
                    {
                        info.Owners.Add(owner);
                        NameScratch.Add(owner.LabelShort);
                    }
                }
            }

            info.Names = NameScratch.Count == 0 ? null : string.Join(", ", NameScratch);
            NameScratch.Clear();
            return info;
        }

        private static void Prune(int tick)
        {
            PruneScratch.Clear();
            foreach (KeyValuePair<int, RoomInfo> kv in RoomCache)
            {
                if (tick < kv.Value.Tick || tick - kv.Value.Tick >= RoomCacheTicks)
                {
                    PruneScratch.Add(kv.Key);
                }
            }

            for (int i = 0; i < PruneScratch.Count; i++)
            {
                RoomCache.Remove(PruneScratch[i]);
            }

            PruneScratch.Clear();
        }
    }
}
