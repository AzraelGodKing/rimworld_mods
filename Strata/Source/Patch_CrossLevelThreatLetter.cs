using HarmonyLib;
using Verse;

namespace Strata
{
    // RW 1.6: ReceiveLetter(Letter, string debugInfo, int delayTicks, bool playSound)
    // — there is no single-arg ReceiveLetter(Letter) overload.
    [HarmonyPatch(typeof(LetterStack), nameof(LetterStack.ReceiveLetter),
        new[] { typeof(Letter), typeof(string), typeof(int), typeof(bool) })]
    public static class Patch_CrossLevelThreatLetter
    {
        public static void Postfix(Letter let)
        {
            StrataCrossLevelThreatNotify.OnLetterReceived(let);
        }
    }
}
