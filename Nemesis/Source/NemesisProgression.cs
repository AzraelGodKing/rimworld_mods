using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Nemesis
{
    /// <summary>
    /// Hybrid captain progression: skills, gear quality, battle-hardened armor,
    /// Biotech implants, fail-open mounts/mechs. Idempotent per progressionLevel.
    /// </summary>
    public static class NemesisProgression
    {
        public static NemesisCombatFocus RollCombatFocus()
        {
            List<NemesisCombatFocus> pool = new List<NemesisCombatFocus>
            {
                NemesisCombatFocus.Destroyer,
                NemesisCombatFocus.Berserker,
                NemesisCombatFocus.Sniper,
                NemesisCombatFocus.Psycho,
                NemesisCombatFocus.Survivor,
            };
            if (ModsConfig.BiotechActive)
                pool.Add(NemesisCombatFocus.Mechanitor);
            return pool.RandomElement();
        }

        public static int MaxLevel =>
            Mathf.Clamp(NemesisMod.Settings?.maxProgressionLevel ?? 8, 1, 12);

        public static bool ProgressionEnabled =>
            NemesisMod.Settings?.enableCaptainProgression ?? true;

        /// <summary>Bring the pawn up to data.progressionLevel (safe to call repeatedly).</summary>
        public static void Apply(Pawn pawn, NemesisData data)
        {
            if (!ProgressionEnabled || pawn == null || pawn.Dead || pawn.Destroyed || data == null)
                return;

            int target = Mathf.Clamp(data.progressionLevel, 0, MaxLevel);
            if (data.appliedProgressionLevel >= target && target > 0)
            {
                EnsureBattleHardened(pawn, target);
                return;
            }

            // Level 0: light focus baseline (weapon preference) once.
            if (target == 0 && data.appliedProgressionLevel < 0)
            {
                EnsureFocusWeapon(pawn, data, QualityCategory.Normal);
                data.appliedProgressionLevel = 0;
                return;
            }

            for (int level = Mathf.Max(1, data.appliedProgressionLevel + 1); level <= target; level++)
                ApplyLevel(pawn, data, level);

            data.appliedProgressionLevel = target;
            EnsureBattleHardened(pawn, target);
        }

        public static void LevelUpOnEscape(NemesisData data, Pawn pawn)
        {
            if (!ProgressionEnabled || data == null) return;
            int cap = MaxLevel;
            if (data.progressionLevel < cap)
                data.progressionLevel++;
            Apply(pawn, data);

            if (data.progressionLevel >= 1)
            {
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_ProgressionTitle".Translate(data.nemesisName),
                    "Nemesis_Letter_ProgressionBody".Translate(
                        data.nemesisName,
                        data.progressionLevel,
                        data.FocusLabelKey.Translate()),
                    LetterDefOf.ThreatSmall);
            }
        }

        private static void ApplyLevel(Pawn pawn, NemesisData data, int level)
        {
            RaiseSkills(pawn, data.combatFocus, level);
            UpgradeGear(pawn, data, level);
            TryInstallBionic(pawn, level);
            TryAddGene(pawn, level);
            SoftCompat.TryAssignMountKind(pawn, data, level);
        }

        private static void EnsureBattleHardened(Pawn pawn, int level)
        {
            if (level < 1) return;
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail("Nemesis_BattleHardened");
            if (def == null) return;

            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            float sev = Mathf.Clamp(level, 1, 8);
            if (existing == null)
                pawn.health.AddHediff(def).Severity = sev;
            else
                existing.Severity = sev;
        }

        private static void RaiseSkills(Pawn pawn, NemesisCombatFocus focus, int level)
        {
            if (pawn.skills == null) return;
            float bump = 1.5f + level * 0.75f;
            void Raise(SkillDef skill, float mult = 1f)
            {
                SkillRecord rec = pawn.skills.GetSkill(skill);
                if (rec == null || rec.TotallyDisabled) return;
                float target = Mathf.Min(20f, rec.Level + bump * mult);
                if (target > rec.Level)
                    rec.Level = Mathf.RoundToInt(target);
            }

            switch (focus)
            {
                case NemesisCombatFocus.Destroyer:
                    Raise(SkillDefOf.Melee, 1.2f);
                    Raise(SkillDefOf.Shooting, 0.4f);
                    break;
                case NemesisCombatFocus.Berserker:
                    Raise(SkillDefOf.Melee, 1.3f);
                    break;
                case NemesisCombatFocus.Sniper:
                    Raise(SkillDefOf.Shooting, 1.3f);
                    Raise(SkillDefOf.Intellectual, 0.3f);
                    break;
                case NemesisCombatFocus.Psycho:
                    Raise(SkillDefOf.Shooting, 1.1f);
                    Raise(SkillDefOf.Melee, 0.5f);
                    break;
                case NemesisCombatFocus.Survivor:
                    Raise(SkillDefOf.Melee, 0.8f);
                    Raise(SkillDefOf.Shooting, 0.8f);
                    Raise(SkillDefOf.Medicine, 0.6f);
                    break;
                case NemesisCombatFocus.Mechanitor:
                    Raise(SkillDefOf.Shooting, 0.9f);
                    Raise(SkillDefOf.Intellectual, 1.0f);
                    Raise(SkillDefOf.Crafting, 0.5f);
                    break;
            }
        }

        private static void UpgradeGear(Pawn pawn, NemesisData data, int level)
        {
            QualityCategory quality = QualityForLevel(level, pawn.Faction?.def?.techLevel ?? TechLevel.Industrial);
            EnsureFocusWeapon(pawn, data, quality);
            UpgradeApparelQuality(pawn, quality);
            if (level >= 3)
                TryGiveArmor(pawn, data, quality);
        }

        private static QualityCategory QualityForLevel(int level, TechLevel tech)
        {
            int q = 2 + level / 2; // Normal=2 …
            if (tech <= TechLevel.Neolithic) q = Mathf.Min(q, 4); // Good
            if (tech <= TechLevel.Medieval) q = Mathf.Min(q, 5); // Excellent
            q = Mathf.Clamp(q, (int)QualityCategory.Normal, (int)QualityCategory.Legendary);
            return (QualityCategory)q;
        }

        private static void EnsureFocusWeapon(Pawn pawn, NemesisData data, QualityCategory quality)
        {
            if (pawn.equipment == null) return;
            ThingWithComps primary = pawn.equipment.Primary;
            if (primary != null && WeaponMatchesFocus(primary.def, data.combatFocus))
            {
                SetQuality(primary, quality);
                return;
            }

            ThingDef weaponDef = PickWeaponDef(data.combatFocus, pawn.Faction?.def?.techLevel ?? TechLevel.Industrial);
            if (weaponDef == null) return;

            if (primary != null)
                pawn.equipment.DestroyEquipment(primary);

            ThingWithComps weapon = (ThingWithComps)ThingMaker.MakeThing(weaponDef);
            SetQuality(weapon, quality);
            pawn.equipment.AddEquipment(weapon);
        }

        private static bool WeaponMatchesFocus(ThingDef def, NemesisCombatFocus focus)
        {
            if (def?.IsWeapon != true) return false;
            bool melee = def.IsMeleeWeapon;
            bool ranged = def.IsRangedWeapon;
            switch (focus)
            {
                case NemesisCombatFocus.Destroyer:
                case NemesisCombatFocus.Berserker:
                case NemesisCombatFocus.Survivor:
                    return melee;
                case NemesisCombatFocus.Sniper:
                case NemesisCombatFocus.Psycho:
                case NemesisCombatFocus.Mechanitor:
                    return ranged;
                default:
                    return true;
            }
        }

        private static ThingDef PickWeaponDef(NemesisCombatFocus focus, TechLevel tech)
        {
            ThingDef Named(params string[] names)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    ThingDef d = DefDatabase<ThingDef>.GetNamedSilentFail(names[i]);
                    if (d != null) return d;
                }
                return null;
            }

            switch (focus)
            {
                case NemesisCombatFocus.Destroyer:
                    return tech >= TechLevel.Industrial
                        ? Named("MeleeWeapon_Mace", "Mace", "MeleeWeapon_Club")
                        : Named("MeleeWeapon_Club", "Club");
                case NemesisCombatFocus.Berserker:
                    return tech >= TechLevel.Industrial
                        ? Named("MeleeWeapon_LongSword", "Sword", "MeleeWeapon_Ikwa")
                        : Named("MeleeWeapon_Ikwa", "MeleeWeapon_Knife");
                case NemesisCombatFocus.Sniper:
                    if (tech >= TechLevel.Spacer)
                        return Named("Gun_ChargeRifle", "Gun_BoltActionRifle", "Gun_AssaultRifle");
                    if (tech >= TechLevel.Industrial)
                        return Named("Gun_BoltActionRifle", "Gun_AssaultRifle", "Gun_Revolver");
                    return Named("Bow_Recurve", "Bow_Short", "Pila");
                case NemesisCombatFocus.Psycho:
                    return tech >= TechLevel.Industrial
                        ? Named("Gun_MachinePistol", "Gun_Autopistol", "Gun_Revolver")
                        : Named("Gun_Revolver", "Bow_Short");
                case NemesisCombatFocus.Mechanitor:
                    return Named("Gun_Autopistol", "Gun_Revolver", "Gun_MachinePistol")
                           ?? PickWeaponDef(NemesisCombatFocus.Psycho, tech);
                default: // Survivor
                    return Named("MeleeWeapon_Knife", "Knife", "MeleeWeapon_Club");
            }
        }

        private static void UpgradeApparelQuality(Pawn pawn, QualityCategory quality)
        {
            if (pawn.apparel == null) return;
            List<Apparel> worn = pawn.apparel.WornApparel;
            for (int i = 0; i < worn.Count; i++)
                SetQuality(worn[i], quality);
        }

        private static void TryGiveArmor(Pawn pawn, NemesisData data, QualityCategory quality)
        {
            if (pawn.apparel == null) return;
            TechLevel tech = pawn.Faction?.def?.techLevel ?? TechLevel.Industrial;
            ThingDef armorDef = null;
            if (tech >= TechLevel.Spacer)
                armorDef = DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_PowerArmor");
            if (armorDef == null && tech >= TechLevel.Industrial)
                armorDef = DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_FlakVest");
            if (armorDef == null && tech >= TechLevel.Medieval)
                armorDef = DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_PlateArmor");
            if (armorDef == null)
                armorDef = DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_Parka")
                           ?? DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_Jacket");
            if (armorDef == null) return;

            // Already wearing this layer/body part coverage — upgrade quality only.
            for (int i = 0; i < pawn.apparel.WornApparel.Count; i++)
            {
                Apparel a = pawn.apparel.WornApparel[i];
                if (a.def == armorDef)
                {
                    SetQuality(a, quality);
                    return;
                }
            }

            if (!ApparelUtility.HasPartsToWear(pawn, armorDef)) return;
            Apparel armor = (Apparel)ThingMaker.MakeThing(armorDef);
            SetQuality(armor, quality);
            if (pawn.apparel.CanWearWithoutDroppingAnything(armorDef))
                pawn.apparel.Wear(armor, dropReplacedApparel: true);
        }

        private static void SetQuality(Thing thing, QualityCategory quality)
        {
            CompQuality q = thing?.TryGetComp<CompQuality>();
            if (q == null) return;
            if (q.Quality < quality)
                q.SetQuality(quality, ArtGenerationContext.Outsider);
        }

        private static void TryInstallBionic(Pawn pawn, int level)
        {
            if (!ModsConfig.BiotechActive) return;
            if (level != 3 && level != 5 && level != 7) return;

            string hediffName = level switch
            {
                3 => "BionicEye",
                5 => "BionicArm",
                7 => "BionicLeg",
                _ => null,
            };
            if (hediffName == null) return;
            HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail(hediffName);
            if (hediff == null) return;
            if (pawn.health.hediffSet.HasHediff(hediff)) return;

            BodyPartRecord part = FindInstallPart(pawn, hediffName);
            if (part == null) return;
            pawn.health.AddHediff(hediff, part);
        }

        private static BodyPartRecord FindInstallPart(Pawn pawn, string hediffName)
        {
            string[] tags = hediffName switch
            {
                "BionicEye" => new[] { "Eye" },
                "BionicArm" => new[] { "Shoulder" },
                "BionicLeg" => new[] { "Leg" },
                _ => System.Array.Empty<string>(),
            };
            List<BodyPartRecord> parts = pawn.RaceProps.body.AllParts;
            for (int i = 0; i < parts.Count; i++)
            {
                BodyPartRecord p = parts[i];
                if (p.def?.defName == null) continue;
                for (int t = 0; t < tags.Length; t++)
                {
                    if (p.def.defName.IndexOf(tags[t], System.StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    if (pawn.health.hediffSet.PartIsMissing(p)) continue;
                    return p;
                }
            }
            return null;
        }

        private static void TryAddGene(Pawn pawn, int level)
        {
            if (!ModsConfig.BiotechActive || pawn.genes == null) return;
            if (level != 4 && level != 6) return;
            // Only if they already have a gene tracker with content (xenotype / genes).
            bool hasGenes = pawn.genes.Xenogenes.Count + pawn.genes.Endogenes.Count > 0;
            bool nonBaseliner = pawn.genes.Xenotype != null
                && pawn.genes.Xenotype.defName != "Baseliner";
            if (!hasGenes && !nonBaseliner)
                return;

            string geneName = level == 4 ? "Robust" : "StrongMeleeDamage";
            GeneDef gene = DefDatabase<GeneDef>.GetNamedSilentFail(geneName);
            if (gene == null) return;
            if (pawn.genes.HasActiveGene(gene)) return;
            pawn.genes.AddGene(gene, xenogene: true);
        }

        /// <summary>Raid points multiplier from captain level (clamped).</summary>
        public static float RaidPointsFactor(NemesisData data)
        {
            if (!ProgressionEnabled || data == null) return 1f;
            return Mathf.Clamp(1f + 0.12f * data.progressionLevel, 1f, 2.2f);
        }

        /// <summary>Spawn Biotech mech escorts near the captain on army-return raids.</summary>
        public static void TrySpawnMechEscort(Map map, Faction faction, NemesisData data, Pawn near)
        {
            if (!ProgressionEnabled) return;
            if (!(NemesisMod.Settings?.enableSoftMechs ?? true)) return;
            if (!ModsConfig.BiotechActive) return;
            if (data == null || data.combatFocus != NemesisCombatFocus.Mechanitor) return;
            if (map == null || faction == null) return;

            int count = Mathf.Clamp(1 + data.progressionLevel / 3, 1, 3);
            List<Pawn> mechs = new List<Pawn>();
            for (int i = 0; i < count; i++)
            {
                PawnKindDef kind = PickMechKind(data.progressionLevel, i);
                if (kind == null) continue;
                try
                {
                    PawnGenerationRequest req = new PawnGenerationRequest(
                        kind, faction, PawnGenerationContext.NonPlayer,
                        forceGenerateNewPawn: true, canGeneratePawnRelations: false);
                    Pawn mech = PawnGenerator.GeneratePawn(req);
                    IntVec3 cell = IntVec3.Invalid;
                    if (near != null && near.Spawned && near.Map == map)
                        CellFinder.TryFindRandomSpawnCellForPawnNear(near.Position, map, out cell, 4);
                    if (!cell.IsValid)
                        CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map) && !c.Fogged(map), map, 0f, out cell);
                    if (!cell.IsValid) cell = CellFinder.RandomEdgeCell(map);
                    GenSpawn.Spawn(mech, cell, map);
                    mechs.Add(mech);
                }
                catch
                {
                    /* fail open */
                }
            }

            if (mechs.Count == 0) return;

            Lord lord = near?.GetLord();
            if (lord != null)
            {
                for (int i = 0; i < mechs.Count; i++)
                    lord.AddPawn(mechs[i]);
            }
            else if (faction != null)
            {
                LordMaker.MakeNewLord(
                    faction,
                    new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: true),
                    map,
                    mechs);
            }
        }

        private static PawnKindDef PickMechKind(int level, int index)
        {
            string name = (level, index) switch
            {
                (_, 0) when level >= 6 => "Mech_Lancer",
                (_, 0) => "Mech_Scyther",
                (_, 1) when level >= 4 => "Mech_Pikeman",
                (_, 1) => "Mech_Scyther",
                (_, _) when level >= 7 => "Mech_Centipede",
                _ => "Mech_Scyther",
            };
            return DefDatabase<PawnKindDef>.GetNamedSilentFail(name)
                   ?? DefDatabase<PawnKindDef>.GetNamedSilentFail("Mech_Scyther");
        }

        /// <summary>Spawn remembered mount animal beside the captain when soft mounts are on.</summary>
        public static void TrySpawnMountEscort(Map map, Faction faction, NemesisData data, Pawn near)
        {
            SoftCompat.TrySpawnMountBeside(map, faction, data, near);
        }
    }
}
