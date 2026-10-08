using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Nemesis
{
    /// <summary>AZR-301 — lean comms reply options (taunt back / truce / demand surrender).</summary>
    public static class NemesisCommsReplies
    {
        public static void DoTauntBack()
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            NemesisData data = comp?.Data;
            if (data == null || !data.active) return;

            data.aggressionLevel = Mathf.Min(data.aggressionLevel + 0.25f, 10f);
            NemesisTells.RecordNote(data, "Nemesis_Note_TauntBack".Translate());
            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_TauntBackTitle".Translate(data.nemesisName),
                "Nemesis_Letter_TauntBackBody".Translate(data.nemesisName),
                LetterDefOf.NeutralEvent);
        }

        public static void DoOfferTruce()
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            NemesisData data = comp?.Data;
            if (data == null || !data.active) return;

            float agg = data.EffectiveAggression;
            // High aggression → rarely accept; low → often.
            float accept = Mathf.Lerp(0.75f, 0.12f, (agg - 1f) / 9f);
            if (!Rand.Chance(accept))
            {
                data.aggressionLevel = Mathf.Min(data.aggressionLevel + 0.35f, 10f);
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_TruceRefuseTitle".Translate(data.nemesisName),
                    "Nemesis_Letter_TruceRefuseBody".Translate(data.nemesisName),
                    LetterDefOf.ThreatSmall);
                return;
            }

            Pawn nemesis = comp.FindNemesisPawn();
            if (nemesis != null && !nemesis.Dead)
            {
                NemesisPawnUtil.DetachFromLord(nemesis);
                if (nemesis.Spawned)
                    nemesis.DeSpawn(DestroyMode.WillReplace);
                if (!nemesis.IsWorldPawn())
                    Find.WorldPawns.PassToWorld(nemesis, PawnDiscardDecideMode.KeepForever);
            }

            int days = NemesisMod.Settings?.truceDurationDays ?? 30;
            data.active = false;
            data.pendingFakeAmbush = false;
            data.truceUntilTick = Find.TickManager.TicksGame + days * 60000;
            NemesisTells.RecordNote(data, "Nemesis_Note_TruceOffer".Translate(days));
            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_TruceTitle".Translate(data.nemesisName),
                "Nemesis_Letter_TruceBody".Translate(data.nemesisName, days),
                LetterDefOf.NeutralEvent);
            NemesisRegistry.Clear();
        }

        public static void DoDemandSurrender()
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            NemesisData data = comp?.Data;
            if (data == null || !data.active) return;

            float agg = data.EffectiveAggression;
            int maxEscapes = NemesisMod.Settings?.maxEscapes ?? 4;
            // Cornered / battered captains sometimes fold; otherwise they escalate.
            bool battered = data.escapeCount >= maxEscapes - 1 || agg >= 7f;
            float fold = battered ? 0.35f : 0.12f;
            if (Rand.Chance(fold))
            {
                // Force them to come in for a capture attempt soon.
                data.nextActionTick = Find.TickManager.TicksGame + 15000;
                data.aggressionLevel = Mathf.Max(1f, data.aggressionLevel - 0.4f);
                NemesisTells.RecordNote(data, "Nemesis_Note_SurrenderDemand".Translate());
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_SurrenderOkTitle".Translate(data.nemesisName),
                    "Nemesis_Letter_SurrenderOkBody".Translate(data.nemesisName),
                    LetterDefOf.PositiveEvent);
                return;
            }

            data.aggressionLevel = Mathf.Min(data.aggressionLevel + 0.45f, 10f);
            data.nextActionTick = Find.TickManager.TicksGame + 30000;
            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_SurrenderRefuseTitle".Translate(data.nemesisName),
                "Nemesis_Letter_SurrenderRefuseBody".Translate(data.nemesisName),
                LetterDefOf.ThreatSmall);
        }
    }
}
