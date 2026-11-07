#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace Nemesis
{
    /// <summary>
    /// Living World listener (AZR-556, AZR-722). Registers on Living World's soft event bus by
    /// reflection and reacts to two stable kinds, read from ground truth (<c>trueKind</c>):
    /// <list type="bullet">
    /// <item><c>Collapse</c> of a hunt's faction: the nemesis escalates (nothing left to lose)
    /// or goes dormant for a while, by the <c>livingWorldCollapseEscalates</c> setting.</item>
    /// <item><c>DecisiveVictory</c>: a winning hunt faction emboldens its nemesis, a losing one
    /// delays the next move.</item>
    /// </list>
    /// No randomness, so the handler is deterministic for Multiplayer. Fail-open throughout.
    /// </summary>
    public static class NemesisLivingWorld
    {
        public const string PackageId = "azraelgodking.livingworld";
        private const int DormantDays = 15;
        private const int LickWoundsTicks = GenDate.TicksPerDay;

        private static FieldInfo? _trueKind;
        private static FieldInfo? _factionAId;
        private static FieldInfo? _factionBId;

        public static bool Registered { get; private set; }

        public static void TryRegister()
        {
            if (Registered || ModLister.GetActiveModWithIdentifier(PackageId, ignorePostfix: true) == null) return;
            try
            {
                Type? signals = GenTypes.GetTypeInAnyAssembly("LivingWorld.LivingWorldSignals");
                Type? evType = GenTypes.GetTypeInAnyAssembly("LivingWorld.WorldEvent");
                Type? handlerType = signals?.GetNestedType("WorldEventHandler", BindingFlags.Public);
                // Prefer the stable compat API (ApiVersion 1+); fall back to the signals class.
                Type? api = GenTypes.GetTypeInAnyAssembly("LivingWorld.LivingWorldCompatApi") ?? signals;
                MethodInfo? register = api?.GetMethod("Register", BindingFlags.Public | BindingFlags.Static);
                if (evType == null || handlerType == null || register == null)
                {
                    Log.Warning("[Nemesis] Living World found but its signal API is not; hunts ignore world news.");
                    return;
                }
                _trueKind = evType.GetField("trueKind");
                _factionAId = evType.GetField("factionAId");
                _factionBId = evType.GetField("factionBId");
                if (_trueKind == null || _factionAId == null || _factionBId == null) return;

                // Parameter contravariance: a handler taking object binds to WorldEventHandler.
                MethodInfo onEvent = typeof(NemesisLivingWorld).GetMethod(nameof(OnWorldEvent),
                    BindingFlags.NonPublic | BindingFlags.Static)!;
                Delegate handler = Delegate.CreateDelegate(handlerType, onEvent);
                register.Invoke(null, new object[] { handler });
                Registered = true;
            }
            catch (Exception e)
            {
                Log.Warning("[Nemesis] Living World listener not registered: " + e.Message);
            }
        }

        private static void OnWorldEvent(object ev)
        {
            if (ev == null || _trueKind == null || _factionAId == null || _factionBId == null) return;
            GameComponent_Nemesis? comp = GameComponent_Nemesis.Instance;
            if (comp == null || comp.Hunts.Count == 0) return;

            string kind = _trueKind.GetValue(ev)?.ToString() ?? "";
            int a = (int)_factionAId.GetValue(ev)!;
            int b = (int)_factionBId.GetValue(ev)!;
            if (kind == "Collapse")
                OnCollapse(comp, a);
            else if (kind == "DecisiveVictory")
                OnVictory(comp, a, b);
        }

        private static void OnCollapse(GameComponent_Nemesis comp, int factionId)
        {
            IReadOnlyList<NemesisData> hunts = comp.Hunts;
            for (int i = 0; i < hunts.Count; i++)
            {
                NemesisData h = hunts[i];
                if (!h.active || h.faction == null || h.faction.loadID != factionId) continue;
                string name = h.nemesisName ?? "Nemesis_Phrase_Someone".Translate();
                string fac = h.factionName ?? h.faction.Name;
                if (NemesisMod.Settings?.livingWorldCollapseEscalates ?? true)
                {
                    h.aggressionLevel = Mathf.Min(h.aggressionLevel + 2f, 10f);
                    h.nextActionTick = Mathf.Min(h.nextActionTick, Find.TickManager.TicksGame + LickWoundsTicks);
                    NemesisTells.RecordNote(h, "Nemesis_Note_FactionCollapsedEscalate", fac);
                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_FactionCollapsedTitle".Translate(h.ColoredName),
                        "Nemesis_Letter_FactionCollapsedEscalate".Translate(name, fac),
                        LetterDefOf.ThreatSmall);
                }
                else
                {
                    h.active = false;
                    h.truceUntilTick = Find.TickManager.TicksGame + DormantDays * GenDate.TicksPerDay;
                    NemesisTells.RecordNote(h, "Nemesis_Note_FactionCollapsedDormant", fac);
                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_FactionCollapsedTitle".Translate(h.ColoredName),
                        "Nemesis_Letter_FactionCollapsedDormant".Translate(name, fac, DormantDays),
                        LetterDefOf.NeutralEvent);
                }
            }
        }

        private static void OnVictory(GameComponent_Nemesis comp, int winnerId, int loserId)
        {
            IReadOnlyList<NemesisData> hunts = comp.Hunts;
            for (int i = 0; i < hunts.Count; i++)
            {
                NemesisData h = hunts[i];
                if (!h.active || h.faction == null) continue;
                string fac = h.factionName ?? h.faction.Name;
                if (h.faction.loadID == winnerId)
                {
                    h.aggressionLevel = Mathf.Min(h.aggressionLevel + 0.5f, 10f);
                    NemesisTells.RecordNote(h, "Nemesis_Note_FactionWon", fac);
                }
                else if (h.faction.loadID == loserId)
                {
                    h.nextActionTick += LickWoundsTicks;
                    NemesisTells.RecordNote(h, "Nemesis_Note_FactionLost", fac);
                }
            }
        }
    }
}
