using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace DeepColony
{
    /// <summary>
    /// God-mode Family tab: spawn living kin on the focus pawn's map and make them colonists.
    /// Hidden unless DevMode and god mode are both on.
    /// </summary>
    public static class FamilyTreeDevUtility
    {
        public static bool GodMode => Prefs.DevMode && DebugSettings.godMode;

        public static bool TryOpenMenu(Rect rect, Pawn focus, Pawn clicked)
        {
            if (!GodMode || focus == null) return false;
            if (!Mouse.IsOver(rect)) return false;
            if (Event.current.type != EventType.MouseDown || Event.current.button != 1)
                return false;
            Event.current.Use();
            ShowMenu(focus, clicked);
            return true;
        }

        public static void ShowMenu(Pawn focus, Pawn clicked)
        {
            if (!GodMode || focus == null) return;
            var opts = new List<FloatMenuOption>();
            if (clicked != null && clicked != focus)
            {
                if (clicked.Dead)
                {
                    opts.Add(new FloatMenuOption(
                        "DC_FamilyTree_DevDead".Translate(clicked.LabelShort.Named("PAWN")),
                        null)
                    { Disabled = true });
                }
                else
                {
                    Pawn one = clicked;
                    opts.Add(new FloatMenuOption(
                        "DC_FamilyTree_DevBringPawn".Translate(one.LabelShort.Named("PAWN")),
                        () => BringOne(focus, one)));
                }
            }

            opts.Add(new FloatMenuOption(
                "DC_FamilyTree_DevBringAll".Translate(focus.LabelShort.Named("PAWN")),
                () => BringAll(focus)));
            Find.WindowStack.Add(new FloatMenu(opts));
        }

        public static void BringOne(Pawn focus, Pawn pawn)
        {
            if (!GodMode || focus == null || pawn == null) return;
            if (pawn.Dead)
            {
                Messages.Message(
                    "DC_FamilyTree_DevDead".Translate(pawn.LabelShort.Named("PAWN")),
                    MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (!TryGetMap(focus, out Map map, out IntVec3 near))
            {
                Messages.Message("DC_FamilyTree_DevNeedMap".Translate(),
                    MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (IsColonistOnMap(pawn, map))
            {
                FamilyTreeUtility.JumpTo(pawn);
                Messages.Message(
                    "DC_FamilyTree_DevAlreadyHere".Translate(pawn.LabelShort.Named("PAWN")),
                    pawn, MessageTypeDefOf.NeutralEvent, false);
                return;
            }
            if (!TryBring(pawn, map, near))
            {
                Messages.Message(
                    "DC_FamilyTree_DevFailed".Translate(pawn.LabelShort.Named("PAWN")),
                    MessageTypeDefOf.RejectInput, false);
                return;
            }
            Messages.Message(
                "DC_FamilyTree_DevBroughtOne".Translate(pawn.LabelShort.Named("PAWN")),
                pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        public static void BringAll(Pawn focus)
        {
            if (!GodMode || focus == null) return;
            if (!TryGetMap(focus, out Map map, out IntVec3 near))
            {
                Messages.Message("DC_FamilyTree_DevNeedMap".Translate(),
                    MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<Pawn> kin = CollectLivingKin(focus);
            var brought = new List<Pawn>();
            for (int i = 0; i < kin.Count; i++)
            {
                Pawn pawn = kin[i];
                if (IsColonistOnMap(pawn, map)) continue;
                if (TryBring(pawn, map, near))
                    brought.Add(pawn);
            }

            if (brought.Count == 0)
            {
                Messages.Message("DC_FamilyTree_DevNone".Translate(),
                    MessageTypeDefOf.NeutralEvent, false);
                return;
            }
            Messages.Message(
                "DC_FamilyTree_DevBrought".Translate(brought.Count.Named("COUNT")),
                new LookTargets(brought),
                MessageTypeDefOf.PositiveEvent,
                false);
        }

        public static List<Pawn> CollectLivingKin(Pawn focus)
        {
            var list = new List<Pawn>();
            FamilyTreeSnapshot snap = FamilyTreeUtility.Build(focus);
            if (snap == null) return list;
            AddAlive(list, snap.grandparents, focus);
            AddAlive(list, snap.parents, focus);
            AddAlive(list, snap.siblings, focus);
            AddAlive(list, snap.partners, focus);
            AddAlive(list, snap.children, focus);
            AddAlive(list, snap.grandchildren, focus);
            AddAlive(list, snap.mentor, focus);
            AddAlive(list, snap.apprentices, focus);
            return list;
        }

        private static void AddAlive(List<Pawn> dest, List<Pawn> src, Pawn focus)
        {
            if (src == null) return;
            for (int i = 0; i < src.Count; i++)
                AddAlive(dest, src[i], focus);
        }

        private static void AddAlive(List<Pawn> dest, Pawn pawn, Pawn focus)
        {
            if (pawn == null || pawn == focus || pawn.Dead || pawn.Destroyed) return;
            if (dest.Contains(pawn)) return;
            dest.Add(pawn);
        }

        private static bool TryGetMap(Pawn focus, out Map map, out IntVec3 near)
        {
            map = focus?.MapHeld ?? Find.CurrentMap;
            near = IntVec3.Invalid;
            if (map == null) return false;
            if (focus != null && focus.Spawned && focus.Map == map)
                near = focus.Position;
            else
                near = map.Center;
            return near.IsValid;
        }

        private static bool IsColonistOnMap(Pawn pawn, Map map)
        {
            if (pawn == null || map == null || !pawn.Spawned || pawn.Map != map) return false;
            if (pawn.IsPrisoner || pawn.IsSlave) return false;
            return pawn.Faction != null && pawn.Faction.IsPlayer;
        }

        private static bool TryBring(Pawn pawn, Map map, IntVec3 near)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed || map == null) return false;
            if (!PlaceOnMap(pawn, map, near)) return false;
            FamilyJoinUtility.ForceMakeColonist(pawn);
            return pawn.Spawned && pawn.Faction != null && pawn.Faction.IsPlayer;
        }

        private static bool PlaceOnMap(Pawn pawn, Map map, IntVec3 near)
        {
            if (pawn.Spawned && pawn.Map == map)
                return true;

            try
            {
                Caravan caravan = pawn.GetCaravan();
                caravan?.RemovePawn(pawn);
            }
            catch { /* Caravan extract is best-effort. */ }

            try
            {
                Lord lord = pawn.GetLord();
                lord?.Notify_PawnLost(pawn, PawnLostCondition.ExitedMap);
            }
            catch { /* Leave-lord is best-effort. */ }

            if (pawn.Spawned)
                pawn.DeSpawn(DestroyMode.Vanish);
            else if (pawn.holdingOwner != null)
                pawn.holdingOwner.Remove(pawn);

            try
            {
                if (Find.WorldPawns != null
                    && Find.WorldPawns.GetSituation(pawn) != WorldPawnSituation.None)
                {
                    Find.WorldPawns.RemovePawn(pawn);
                }
            }
            catch { /* World-pawn bookkeeping is best-effort. */ }

            IntVec3 cell = near;
            if (!CellFinder.TryFindRandomSpawnCellForPawnNear(near, map, out cell, 8))
                cell = CellFinder.RandomClosewalkCellNear(near, map, 8);
            if (!cell.IsValid)
                cell = near.ClampInsideMap(map);

            GenSpawn.Spawn(pawn, cell, map, WipeMode.Vanish);
            try
            {
                if (pawn.IsWorldPawn())
                    Find.WorldPawns.RemovePawn(pawn);
            }
            catch { }

            if (pawn.Spawned)
                pawn.Notify_Teleported(endCurrentJob: true, resetTweenedPos: true);
            return pawn.Spawned;
        }
    }
}
