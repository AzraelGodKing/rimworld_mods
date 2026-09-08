using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepColony.Patches
{
    /// <summary>
    /// AZR-157 — newborns fail generation when the mother's pawnkind lists
    /// skill / work-tag gates a baby cannot meet (HAR copies the mother's kindDef).
    /// Adults are never rewritten.
    ///
    /// Verse.PawnGenerator.GeneratePawn(PawnGenerationRequest request)
    /// </summary>
    internal static class BirthSafetyNet
    {
        private static readonly Dictionary<PawnKindDef, PawnKindDef> clones =
            new Dictionary<PawnKindDef, PawnKindDef>();
        private static readonly HashSet<string> warned = new HashSet<string>();

        internal static void ResetCache()
        {
            clones.Clear();
        }

        internal static void Sanitize(ref PawnGenerationRequest request)
        {
            if (!DeepColonySettings.Get.enableBirthSafetyNet)
            {
                return;
            }

            if (!IsNewbornOrBaby(request.AllowedDevelopmentalStages))
            {
                return;
            }

            PawnKindDef kind = request.KindDef;
            if (kind == null || !NeedsSanitize(kind))
            {
                return;
            }

            request.KindDef = CloneWithoutSkillGates(kind);
        }

        private static bool IsNewbornOrBaby(DevelopmentalStage stages)
        {
            if ((stages & DevelopmentalStage.Adult) != 0)
            {
                return false;
            }
            return (stages & (DevelopmentalStage.Newborn | DevelopmentalStage.Baby)) != 0;
        }

        private static bool NeedsSanitize(PawnKindDef kind)
        {
            if (kind.requiredWorkTags != WorkTags.None)
            {
                return true;
            }
            return kind.skills != null && kind.skills.Count > 0;
        }

        private static PawnKindDef CloneWithoutSkillGates(PawnKindDef source)
        {
            if (clones.TryGetValue(source, out PawnKindDef cached))
            {
                return cached;
            }

            PawnKindDef clone = ShallowClone(source);
            clone.skills = null;
            clone.requiredWorkTags = WorkTags.None;
            clones[source] = clone;

            if (warned.Add(source.defName))
            {
                Log.Warning("[DeepColony] Birth safety net: " + source.defName
                    + " lists skill or work-tag requirements a newborn cannot meet. "
                    + "Stripping them for baby generation only. Report this pawnkind to the race mod author.");
            }

            return clone;
        }

        private static PawnKindDef ShallowClone(PawnKindDef source)
        {
            Type type = source.GetType();
            PawnKindDef clone;
            try
            {
                clone = (PawnKindDef)Activator.CreateInstance(type);
            }
            catch
            {
                clone = new PawnKindDef();
            }

            FieldInfo[] fields = type.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field.IsLiteral || field.IsStatic)
                {
                    continue;
                }
                try
                {
                    field.SetValue(clone, field.GetValue(source));
                }
                catch
                {
                    // Skip init-only / mismatched subclass fields.
                }
            }

            return clone;
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) })]
    [HarmonyPriority(Priority.Last)]
    public static class Patch_PawnGenerator_BirthSafetyNet
    {
        public static void Prefix(ref PawnGenerationRequest request)
        {
            BirthSafetyNet.Sanitize(ref request);
        }
    }
}
