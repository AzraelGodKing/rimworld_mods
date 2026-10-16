using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepColony.Patches
{
    internal static class TrackerFields
    {
        internal static readonly AccessTools.FieldRef<Pawn_HealthTracker, Pawn> HealthPawn =
            AccessTools.FieldRefAccess<Pawn_HealthTracker, Pawn>("pawn");

        internal static readonly AccessTools.FieldRef<Pawn_RelationsTracker, Pawn> RelationsPawn =
            AccessTools.FieldRefAccess<Pawn_RelationsTracker, Pawn>("pawn");

        internal static readonly AccessTools.FieldRef<Pawn_AgeTracker, Pawn> AgePawn =
            AccessTools.FieldRefAccess<Pawn_AgeTracker, Pawn>("pawn");

        internal static readonly AccessTools.FieldRef<Pawn_GuestTracker, Pawn> GuestPawn =
            AccessTools.FieldRefAccess<Pawn_GuestTracker, Pawn>("pawn");

        internal static readonly AccessTools.FieldRef<Need, Pawn> NeedPawn =
            AccessTools.FieldRefAccess<Need, Pawn>("pawn");
    }
}
