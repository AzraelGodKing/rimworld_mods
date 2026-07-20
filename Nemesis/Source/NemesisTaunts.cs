using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Nemesis
{
    /// <summary>Personal taunt / letter copy. Names the nemesis and optional fixation target.</summary>
    public static class NemesisTaunts
    {
        public static string TargetPhrase(NemesisData data)
        {
            if (data.targetMode == NemesisTargetMode.Pawn && !string.IsNullOrEmpty(data.targetPawnName))
                return data.targetPawnName;
            return "Nemesis_Phrase_YourColony".Translate();
        }

        public static string PickCommsLine(NemesisData data)
        {
            string target = TargetPhrase(data);
            string name = data.nemesisName ?? "Nemesis_Phrase_Someone".Translate();
            int escapes = data.escapeCount;
            float agg = data.EffectiveAggression;

            if (data.ignoredActionsCount >= (NemesisMod.Settings?.ignoredActionsThreshold ?? 3)
                && Rand.Chance(0.85f))
            {
                return "Nemesis_Taunt_Ignored".Translate(name, target);
            }

            string identity = TryPickIdentityLine(data, name, target);
            if (identity != null)
                return identity;

            // Archetype voice — prefer when present (~50%).
            if (Rand.Chance(0.5f))
            {
                string voice = TryArchetypeVoice(data, name, target);
                if (voice != null)
                    return voice;
            }

            if (agg < 3f)
            {
                return Rand.RangeInclusive(0, 5) switch
                {
                    0 => "Nemesis_Taunt_Low0".Translate(name, target),
                    1 => "Nemesis_Taunt_Low1".Translate(name, target),
                    2 => "Nemesis_Taunt_Low2".Translate(name, target),
                    3 => "Nemesis_Taunt_Low3".Translate(name, target),
                    4 => "Nemesis_Taunt_Low4".Translate(name, target),
                    _ => SoftHomesteaderTaunt(data, name, target)
                        ?? "Nemesis_Taunt_Low5".Translate(name, target),
                };
            }

            return Rand.RangeInclusive(0, 5) switch
            {
                0 => "Nemesis_Taunt_High0".Translate(name, target),
                1 => "Nemesis_Taunt_High1".Translate(name, target),
                2 => "Nemesis_Taunt_High2".Translate(name, target),
                3 => "Nemesis_Taunt_High3".Translate(name, target, escapes),
                4 => "Nemesis_Taunt_High4".Translate(name, target),
                _ => SoftStormFlavor(data, name, target)
                    ?? "Nemesis_Taunt_High5".Translate(name, target),
            };
        }

        private static string TryArchetypeVoice(NemesisData data, string name, string target)
        {
            int line = Rand.RangeInclusive(0, 2);
            return data.archetype switch
            {
                NemesisArchetype.Butcher => line switch
                {
                    0 => "Nemesis_Taunt_Butcher_0".Translate(name, target),
                    1 => "Nemesis_Taunt_Butcher_1".Translate(name, target),
                    _ => "Nemesis_Taunt_Butcher_2".Translate(name, target),
                },
                NemesisArchetype.Saboteur => line switch
                {
                    0 => "Nemesis_Taunt_Saboteur_0".Translate(name, target),
                    1 => "Nemesis_Taunt_Saboteur_1".Translate(name, target),
                    _ => "Nemesis_Taunt_Saboteur_2".Translate(name, target),
                },
                NemesisArchetype.Trickster => line switch
                {
                    0 => "Nemesis_Taunt_Trickster_0".Translate(name, target),
                    1 => "Nemesis_Taunt_Trickster_1".Translate(name, target),
                    _ => "Nemesis_Taunt_Trickster_2".Translate(name, target),
                },
                _ => line switch
                {
                    0 => "Nemesis_Taunt_Stalker_0".Translate(name, target),
                    1 => "Nemesis_Taunt_Stalker_1".Translate(name, target),
                    _ => "Nemesis_Taunt_Stalker_2".Translate(name, target),
                },
            };
        }

        /// <summary>
        /// Scar / intimacy lines. Prefer injury references when present; intimacy gated by hunt age.
        /// Walks hediffs once — no LINQ.
        /// </summary>
        private static string TryPickIdentityLine(NemesisData data, string name, string target)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            Pawn nemesis = comp?.FindNemesisPawn();
            Pawn victim = data.targetMode == NemesisTargetMode.Pawn ? comp?.FindTargetPawn() : null;

            // Scar / missing / prosthetic — prefer when present (~55%).
            if (nemesis?.health?.hediffSet?.hediffs != null && Rand.Chance(0.55f))
            {
                string injuryLine = TryScarLine(nemesis, name, target);
                if (injuryLine != null)
                    return injuryLine;
            }

            // Escalating intimacy from colony data the nemesis has "learned".
            int days = data.HuntDays;
            int harass = data.harassmentCount;
            if (victim == null || (days < 3 && harass < 2))
                return null;

            // Mid: bedroom / highest skill (agg ~2+ or a few actions).
            if ((days >= 3 || harass >= 2) && Rand.Chance(0.4f))
            {
                string mid = TryIntimacyMid(victim, name, target);
                if (mid != null)
                    return mid;
            }

            // Late: spouse/lover / bonded animal.
            if ((days >= 8 || harass >= 5 || data.EffectiveAggression >= 3.5f) && Rand.Chance(0.45f))
            {
                string late = TryIntimacyLate(victim, name, target);
                if (late != null)
                    return late;
            }

            return null;
        }

        private static string TryScarLine(Pawn nemesis, string name, string target)
        {
            List<Hediff> hediffs = nemesis.health.hediffSet.hediffs;
            Hediff missing = null;
            Hediff scar = null;
            Hediff prosthetic = null;

            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff h = hediffs[i];
                if (h == null || h.Part == null) continue;

                if (h is Hediff_MissingPart)
                {
                    if (missing == null) missing = h;
                    continue;
                }

                if (h is Hediff_AddedPart)
                {
                    if (prosthetic == null) prosthetic = h;
                    continue;
                }

                if (scar == null && IsScarLike(h))
                    scar = h;
            }

            if (missing != null)
            {
                return "Nemesis_Taunt_MissingPart".Translate(
                    name, target, missing.Part.LabelShort ?? missing.Part.def?.label ?? "Nemesis_Phrase_Someone".Translate());
            }
            if (scar != null)
            {
                return "Nemesis_Taunt_Scar".Translate(
                    name, target, scar.Part.LabelShort ?? scar.Part.def?.label ?? "Nemesis_Phrase_Someone".Translate());
            }
            if (prosthetic != null)
            {
                return "Nemesis_Taunt_Prosthetic".Translate(
                    name, target, prosthetic.Part.LabelShort ?? prosthetic.Part.def?.label ?? "Nemesis_Phrase_Someone".Translate());
            }
            return null;
        }

        private static bool IsScarLike(Hediff h)
        {
            if (h is Hediff_Injury injury && injury.IsPermanent())
                return true;
            if (h.def == null) return false;
            string dn = h.def.defName ?? "";
            if (dn.IndexOf("Scar", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (h.def.chronic && h.def.isBad)
                return true;
            return false;
        }

        private static string TryIntimacyMid(Pawn victim, string name, string target)
        {
            if (Rand.Bool)
            {
                SkillRecord best = null;
                List<SkillRecord> skills = victim.skills?.skills;
                if (skills != null)
                {
                    for (int i = 0; i < skills.Count; i++)
                    {
                        SkillRecord s = skills[i];
                        if (s == null || s.TotallyDisabled) continue;
                        if (best == null || s.Level > best.Level)
                            best = s;
                    }
                }
                if (best != null && best.Level >= 6)
                    return "Nemesis_Taunt_Skill".Translate(name, target, best.def.label, best.Level);
            }

            Room bedroom = victim.ownership?.OwnedRoom;
            if (bedroom != null && !bedroom.PsychologicallyOutdoors)
            {
                string role = bedroom.Role?.label ?? "Nemesis_Phrase_Bedroom".Translate();
                return "Nemesis_Taunt_Bedroom".Translate(name, target, role);
            }

            return null;
        }

        private static string TryIntimacyLate(Pawn victim, string name, string target)
        {
            PawnRelationDef spouseDef = DefDatabase<PawnRelationDef>.GetNamedSilentFail("Spouse");
            PawnRelationDef loverDef = DefDatabase<PawnRelationDef>.GetNamedSilentFail("Lover");
            PawnRelationDef fianceDef = DefDatabase<PawnRelationDef>.GetNamedSilentFail("Fiance");

            List<DirectPawnRelation> rels = victim.relations?.DirectRelations;
            if (rels != null)
            {
                for (int i = 0; i < rels.Count; i++)
                {
                    DirectPawnRelation r = rels[i];
                    if (r?.otherPawn == null || r.otherPawn.Dead) continue;
                    if (r.def == spouseDef || r.def == loverDef || r.def == fianceDef)
                    {
                        return "Nemesis_Taunt_Lover".Translate(name, target, r.otherPawn.LabelShort);
                    }
                }

                for (int i = 0; i < rels.Count; i++)
                {
                    DirectPawnRelation r = rels[i];
                    if (r?.otherPawn == null || r.otherPawn.Dead) continue;
                    if (r.def == PawnRelationDefOf.Bond)
                        return "Nemesis_Taunt_Bonded".Translate(name, target, r.otherPawn.LabelShort);
                }
            }

            return null;
        }

        private static string SoftHomesteaderTaunt(NemesisData data, string name, string target)
        {
            if (!SoftCompat.HomesteaderActive) return null;
            Pawn victim = GameComponent_Nemesis.Instance?.FindTargetPawn();
            string fav = SoftCompat.TryFavoriteFoodLabel(victim);
            if (fav != null)
                return "Nemesis_Taunt_FavoriteFood".Translate(name, target, fav);
            if (data != null)
                return "Nemesis_Taunt_Cellar".Translate(name);
            return null;
        }

        private static string SoftStormFlavor(NemesisData data, string name, string target)
        {
            if (!SoftCompat.StormproofActive) return null;
            return "Nemesis_Taunt_Ion".Translate(name, target);
        }

        public static string EscapeLetterBody(NemesisData data)
        {
            int n = data.escapeCount;
            string name = data.nemesisName;
            if (n == 1) return "Nemesis_Letter_Escape1".Translate(name);
            if (n == 2) return "Nemesis_Letter_Escape2".Translate(name);
            if (n <= 4) return "Nemesis_Letter_EscapeN".Translate(name, n);
            return "Nemesis_Letter_EscapeMany".Translate(name, n);
        }
    }
}
