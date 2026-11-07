using System.Collections.Generic;
using Verse;

namespace Nemesis
{
    /// <summary>
    /// Stable soft-compat surface for Rimesis / Back for Vengeance / the Azrael series hub and
    /// other mods. Prefer these over digging into GameComponent_Nemesis. Several hunts can run
    /// at once (one per hostile faction). Fail-open: all members are safe when Nemesis is
    /// loaded solo or no game is running.
    /// </summary>
    public static class NemesisCompatApi
    {
        /// <summary>2 = multi-hunt (Nemesis 2.0.0).</summary>
        public const int ApiVersion = 2;

        public static bool IsActive => true;

        /// <summary>Any hunt is active, in truce, or waiting on the capture dialog.</summary>
        public static bool HasActiveHunt => GameComponent_Nemesis.Instance?.IsEngaged ?? false;

        /// <summary>Number of engaged hunts.</summary>
        public static int EngagedHuntCount
        {
            get
            {
                GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
                if (comp == null) return 0;
                int n = 0;
                IReadOnlyList<NemesisData> hunts = comp.Hunts;
                for (int i = 0; i < hunts.Count; i++)
                {
                    if (hunts[i].Engaged) n++;
                }
                return n;
            }
        }

        /// <summary>Nemesis of the first engaged hunt, or null.</summary>
        public static Pawn ActiveNemesisPawn => GameComponent_Nemesis.Instance?.FindNemesisPawn();

        /// <summary>Every engaged hunt's nemesis that can still be found (alive or dead).</summary>
        public static List<Pawn> EngagedNemesisPawns()
        {
            var list = new List<Pawn>();
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null) return list;
            IReadOnlyList<NemesisData> hunts = comp.Hunts;
            for (int i = 0; i < hunts.Count; i++)
            {
                if (!hunts[i].Engaged) continue;
                Pawn p = comp.FindNemesisPawnFor(hunts[i]);
                if (p != null) list.Add(p);
            }
            return list;
        }

        /// <summary>
        /// True while any engaged hunt (active, truce or pending capture) owns this pawn.
        /// Same meaning everywhere: Nemesis's own Kill intercept only acts on active hunts,
        /// but other mods should leave a truce or captured nemesis alone too.
        /// </summary>
        public static bool IsNemesisPawn(Pawn pawn) =>
            pawn != null && GameComponent_Nemesis.Instance?.EngagedHuntForNemesis(pawn) != null;

        public static bool WouldClaim(Pawn pawn) => IsNemesisPawn(pawn) || HasActiveHunt;

        /// <summary>
        /// True when Rimesis should treat this pawn as Missing (cannot call to
        /// action / hunt down) because Nemesis has exclusive claim on them.
        /// Prefer this over <see cref="WouldClaim"/> for Availability handshakes:
        /// WouldClaim is broader (any active hunt). Fail-open; no Rimesis dependency.
        /// Spec: docs/ideas/nemesis-rimesis-compat.md
        /// </summary>
        public static bool ShouldReportMissingToRimesis(Pawn pawn) => IsNemesisPawn(pawn);

        /// <summary>
        /// Game tick of the last nemesis raid or assault (any hunt), or a large negative number.
        /// A storyteller can space its own big threats around it (AZR-556).
        /// </summary>
        public static int LastHuntThreatTick => GameComponent_Nemesis.Instance?.LastThreatTick ?? -9999999;

        /// <summary>Ends every engaged hunt quietly, freeing the world pawns. For safe removal tools.</summary>
        public static void ClearAllHunts() =>
            GameComponent_Nemesis.Instance?.EndAllHunts(NemesisEndReason.Cleared);
    }
}
