using RimWorld;
using Verse;
using Verse.AI;

namespace DeepColony
{
    /// <summary>B05 — short contextual flashbacks while carrying trauma.</summary>
    public static class FlashbackUtility
    {
        private const int CheckInterval = 2500;
        private const float FlashbackMtbDays = 1.8f;

        public static void GameTick()
        {
            if (!DeepColonySettings.Get.enableTrauma) return;
            if (Find.TickManager.TicksGame % CheckInterval != 0) return;

            foreach (Map map in Find.Maps)
            {
                foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
                    TryTickFlashback(p);
            }
        }

        public static void TryForceFlashback(Pawn pawn, string reason = null)
        {
            if (pawn == null || pawn.Dead || !pawn.Spawned) return;
            if (!TraumaUtility.HasAnyTrauma(pawn)) return;
            ApplyFlashback(pawn, reason);
        }

        private static void TryTickFlashback(Pawn pawn)
        {
            if (pawn.Dead || pawn.Downed || pawn.InMentalState) return;
            if (!TraumaUtility.HasAnyTrauma(pawn)) return;
            if (!TriggerPresent(pawn)) return;
            if (!Rand.MTBEventOccurs(FlashbackMtbDays, 60000f, CheckInterval)) return;
            ApplyFlashback(pawn, null);
        }

        private static bool TriggerPresent(Pawn pawn)
        {
            if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_CombatShock)
                || TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Fire))
            {
                if (pawn.Drafted) return true;
                if (pawn.IsBurning()) return true;
                if (NearbyHostile(pawn, 12f)) return true;
            }

            if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Captivity))
            {
                if (pawn.workSettings != null
                    && pawn.workSettings.WorkIsActive(WorkTypeDefOf.Warden))
                    return true;
                if (NearbyPrisoner(pawn, 10f)) return true;
            }

            if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Massacre)
                || TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_ViolentLoss)
                || TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_BereavementShock))
            {
                if (NearbyHumanCorpse(pawn, 8f)) return true;
            }

            if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Toxic))
            {
                if (pawn.health?.hediffSet?.HasHediff(HediffDefOf.ToxicBuildup) == true)
                    return true;
            }

            if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Insect))
            {
                if (NearbyInsect(pawn, 15f)) return true;
            }

            if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Betrayal))
            {
                if (NearbyHostile(pawn, 12f)) return true;
            }

            return false;
        }

        private static void ApplyFlashback(Pawn pawn, string reason)
        {
            if (DC_DefOf.DC_Hediff_Flashback != null && pawn.health != null)
            {
                Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(DC_DefOf.DC_Hediff_Flashback);
                if (existing != null)
                    existing.Severity = 1f;
                else
                    pawn.health.AddHediff(DC_DefOf.DC_Hediff_Flashback);
            }

            if (DC_DefOf.DC_Thought_Flashback != null && pawn.needs?.mood?.thoughts != null)
            {
                var thought = (Thought_Memory)ThoughtMaker.MakeThought(DC_DefOf.DC_Thought_Flashback);
                pawn.needs.mood.thoughts.memories.TryGainMemory(thought);
            }

            string msg = reason.NullOrEmpty()
                ? "DC_Flashback".Translate(pawn.LabelShort.Named("PAWN"))
                : reason;
            Messages.Message(msg, pawn, MessageTypeDefOf.NegativeEvent, false);
        }

        private static bool NearbyHostile(Pawn pawn, float radius)
        {
            foreach (IAttackTarget t in pawn.Map.attackTargetsCache.TargetsHostileToColony)
            {
                if (t.Thing is Pawn enemy && enemy.Spawned
                    && enemy.Position.DistanceTo(pawn.Position) <= radius)
                    return true;
            }
            return false;
        }

        private static bool NearbyPrisoner(Pawn pawn, float radius)
        {
            foreach (Pawn other in pawn.Map.mapPawns.PrisonersOfColonySpawned)
            {
                if (other.Spawned && other.Position.DistanceTo(pawn.Position) <= radius)
                    return true;
            }
            return false;
        }

        private static bool NearbyHumanCorpse(Pawn pawn, float radius)
        {
            foreach (Thing t in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse))
            {
                if (t is not Corpse c || c.InnerPawn == null) continue;
                if (!c.InnerPawn.RaceProps.Humanlike) continue;
                if (c.Position.DistanceTo(pawn.Position) <= radius) return true;
            }
            return false;
        }

        private static bool NearbyInsect(Pawn pawn, float radius)
        {
            foreach (Pawn other in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (other.Dead || !other.Spawned) continue;
                if (other.RaceProps?.Insect != true) continue;
                if (other.Position.DistanceTo(pawn.Position) <= radius) return true;
            }
            return false;
        }
    }
}
