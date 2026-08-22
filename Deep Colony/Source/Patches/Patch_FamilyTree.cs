using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeepColony.Patches
{
    [HarmonyPatch]
    public static class Patch_CharacterCard_FamilyTree
    {
        public static bool Prepare() => TargetMethod() != null;

        public static MethodBase TargetMethod()
        {
            foreach (MethodInfo m in AccessTools.GetDeclaredMethods(typeof(CharacterCardUtility)))
            {
                if (m.Name != "DrawCharacterCard") continue;
                ParameterInfo[] ps = m.GetParameters();
                if (ps.Length < 2) continue;
                if (ps[0].ParameterType == typeof(Rect) && ps[1].ParameterType == typeof(Pawn))
                    return m;
            }
            return AccessTools.Method(typeof(CharacterCardUtility), "DrawCharacterCard");
        }

        public static void Postfix(Rect __0, Pawn pawn)
        {
            if (pawn == null) return;
            if (!FamilyTreeUtility.IsVisibleFor(pawn)) return;

            Rect rect = __0;
            Rect btn = new Rect(rect.xMax - 118f, rect.y + 2f, 114f, 24f);
            if (Widgets.ButtonText(btn, "DC_FamilyTreeButton".Translate()))
                Find.WindowStack.Add(new Window_FamilyTree(pawn));
        }
    }
}
