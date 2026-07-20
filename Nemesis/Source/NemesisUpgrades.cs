using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Nemesis
{
    /// <summary>Visible gear / bionic upgrades applied once per escape.</summary>
    public static class NemesisUpgrades
    {
        /// <summary>
        /// Apply one upgrade and return a short keyed description for the escape letter, or null.
        /// </summary>
        public static string TryApplyEscapeUpgrade(Pawn nemesis)
        {
            if (nemesis == null || nemesis.Dead) return null;

            string result = null;
            int roll = Rand.RangeInclusive(0, 2);
            if (roll == 0)
                result = TryUpgradeArmor(nemesis);
            if (result == null && roll <= 1)
                result = TryUpgradeWeapon(nemesis);
            if (result == null)
                result = TryAddBionic(nemesis);
            if (result == null)
                result = TryUpgradeWeapon(nemesis) ?? TryUpgradeArmor(nemesis);

            NemesisData data = GameComponent_Nemesis.Instance?.Data;
            if (data != null)
                NemesisIdentity.Apply(nemesis, data);
            return result;
        }

        private static string TryUpgradeArmor(Pawn nemesis)
        {
            ThingDef[] armors =
            {
                DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_FlakVest"),
                DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_FlakPants"),
                DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_FlakJacket"),
                DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_PowerArmor"),
                DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_SimpleHelmet"),
            };

            ThingDef pick = null;
            for (int i = 0; i < 6; i++)
            {
                ThingDef d = armors[Rand.Range(0, armors.Length)];
                if (d == null) continue;
                if (nemesis.apparel?.WornApparel != null)
                {
                    bool already = false;
                    List<Apparel> worn = nemesis.apparel.WornApparel;
                    for (int w = 0; w < worn.Count; w++)
                    {
                        if (worn[w]?.def == d) { already = true; break; }
                    }
                    if (already) continue;
                }
                pick = d;
                break;
            }
            if (pick == null) return null;

            try
            {
                Apparel apparel = (Apparel)ThingMaker.MakeThing(pick, pick.MadeFromStuff ? GenStuff.DefaultStuffFor(pick) : null);
                if (apparel == null) return null;
                if (nemesis.apparel == null) return null;
                // Spawned: drop replaced. Unspawned: destroy replaced — drop needs a map.
                if (nemesis.Spawned)
                {
                    nemesis.apparel.Wear(apparel, dropReplacedApparel: true);
                }
                else
                {
                    List<Apparel> toDestroy = null;
                    List<Apparel> worn = nemesis.apparel.WornApparel;
                    if (worn != null)
                    {
                        for (int i = 0; i < worn.Count; i++)
                        {
                            Apparel w = worn[i];
                            if (w == null) continue;
                            if (!ApparelUtility.CanWearTogether(pick, w.def, nemesis.RaceProps.body))
                            {
                                toDestroy ??= new List<Apparel>();
                                toDestroy.Add(w);
                            }
                        }
                    }
                    if (toDestroy != null)
                    {
                        for (int i = 0; i < toDestroy.Count; i++)
                        {
                            Apparel w = toDestroy[i];
                            nemesis.apparel.Remove(w);
                            if (!w.Destroyed)
                                w.Destroy(DestroyMode.Vanish);
                        }
                    }
                    nemesis.apparel.Wear(apparel, dropReplacedApparel: false);
                }
                return "Nemesis_Upgrade_Armor".Translate(apparel.LabelNoCount);
            }
            catch
            {
                return null;
            }
        }

        private static string TryUpgradeWeapon(Pawn nemesis)
        {
            ThingDef[] weapons =
            {
                DefDatabase<ThingDef>.GetNamedSilentFail("Gun_AssaultRifle"),
                DefDatabase<ThingDef>.GetNamedSilentFail("Gun_ChargeRifle"),
                DefDatabase<ThingDef>.GetNamedSilentFail("Gun_SniperRifle"),
                DefDatabase<ThingDef>.GetNamedSilentFail("MeleeWeapon_LongSword"),
                DefDatabase<ThingDef>.GetNamedSilentFail("Gun_LMG"),
            };

            ThingDef pick = null;
            for (int i = 0; i < 6; i++)
            {
                ThingDef d = weapons[Rand.Range(0, weapons.Length)];
                if (d == null) continue;
                pick = d;
                break;
            }
            if (pick == null) return null;

            try
            {
                Thing weapon = ThingMaker.MakeThing(pick, pick.MadeFromStuff ? GenStuff.DefaultStuffFor(pick) : null);
                if (weapon == null) return null;
                if (nemesis.equipment == null) return null;
                // AddEquipment errors if a primary already exists — destroy it first.
                ThingWithComps primary = nemesis.equipment.Primary;
                if (primary != null)
                    nemesis.equipment.DestroyEquipment(primary);
                nemesis.equipment.AddEquipment((ThingWithComps)weapon);
                return "Nemesis_Upgrade_Weapon".Translate(weapon.LabelNoCount);
            }
            catch
            {
                return null;
            }
        }

        private static string TryAddBionic(Pawn nemesis)
        {
            if (nemesis.health?.hediffSet == null) return null;

            HediffDef[] bionics =
            {
                DefDatabase<HediffDef>.GetNamedSilentFail("BionicArm"),
                DefDatabase<HediffDef>.GetNamedSilentFail("BionicLeg"),
                DefDatabase<HediffDef>.GetNamedSilentFail("BionicEye"),
                DefDatabase<HediffDef>.GetNamedSilentFail("BionicSpine"),
                DefDatabase<HediffDef>.GetNamedSilentFail("PowerClaw"),
            };

            // Prefer missing / scarred parts.
            List<Hediff> hediffs = nemesis.health.hediffSet.hediffs;
            BodyPartRecord missingPart = null;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is Hediff_MissingPart mp && mp.Part != null)
                {
                    missingPart = mp.Part;
                    break;
                }
            }

            HediffDef pick = null;
            for (int i = 0; i < bionics.Length; i++)
            {
                if (bionics[i] != null) { pick = bionics[i]; break; }
            }
            if (pick == null) return null;

            try
            {
                if (missingPart != null)
                {
                    // Restore missing part then install bionic if compatible.
                    Hediff_MissingPart toRemove = null;
                    for (int i = 0; i < hediffs.Count; i++)
                    {
                        if (hediffs[i] is Hediff_MissingPart mp && mp.Part == missingPart)
                        {
                            toRemove = mp;
                            break;
                        }
                    }
                    if (toRemove != null)
                        nemesis.health.RemoveHediff(toRemove);
                }

                BodyPartRecord part = missingPart ?? FindInstallablePart(nemesis, pick);
                if (part == null)
                {
                    // Any recipe-less install attempt — fail soft.
                    return null;
                }

                Hediff added = HediffMaker.MakeHediff(pick, nemesis, part);
                if (added == null) return null;
                nemesis.health.AddHediff(added, part);
                return "Nemesis_Upgrade_Bionic".Translate(pick.label, part.LabelShort);
            }
            catch
            {
                return null;
            }
        }

        private static BodyPartRecord FindInstallablePart(Pawn pawn, HediffDef hediff)
        {
            List<BodyPartRecord> parts = pawn.RaceProps.body.AllParts;
            if (parts == null) return null;
            for (int i = 0; i < parts.Count; i++)
            {
                BodyPartRecord p = parts[i];
                if (p == null) continue;
                if (pawn.health.hediffSet.PartIsMissing(p)) continue;
                // Prefer limbs / eyes for common bionics.
                string dn = p.def?.defName ?? "";
                if (dn.IndexOf("Arm", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || dn.IndexOf("Leg", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || dn.IndexOf("Eye", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || dn.IndexOf("Spine", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return p;
            }
            return parts.Count > 0 ? parts[0] : null;
        }
    }
}
