using System.Collections.Generic;
using Verse;

namespace Strata
{
    public static class StrataCagedBirdRegistry
    {
        private static readonly HashSet<Pawn> Contained = new HashSet<Pawn>();

        public static bool IsContained(Pawn pawn) => pawn != null && Contained.Contains(pawn);

        public static void Register(Pawn pawn)
        {
            if (pawn != null)
            {
                Contained.Add(pawn);
            }
        }

        public static void Unregister(Pawn pawn)
        {
            if (pawn != null)
            {
                Contained.Remove(pawn);
            }
        }
    }
}
