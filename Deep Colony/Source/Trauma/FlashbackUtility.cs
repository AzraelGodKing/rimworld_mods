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
            if (!TickPhase.Due(1336)) return;

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

        internal static bool CombatTriggerPresent(Pawn pawn)
        {
            if (pawn.Drafted) return true;
            if (NearbyHostile(pawn, 12f)) return true;
            return false;
        }

        internal static bool FireTriggerPresent(Pawn pawn)
        {
            if (pawn.IsBurning()) return true;
            if (pawn.Drafted) return true;
            return false;
        }

        internal static bool CaptivityTriggerPresent(Pawn pawn)
        {
            if (pawn.workSettings != null
                && pawn.workSettings.WorkIsActive(WorkTypeDefOf.Warden))
                return true;
            return NearbyPrisoner(pawn, 10f);
        }

        internal static bool CasualtyTriggerPresent(Pawn pawn) => NearbyHumanCorpse(pawn, 8f);

        internal static bool ToxinTriggerPresent(Pawn pawn) =>
            pawn.health?.hediffSet?.HasHediff(HediffDefOf.ToxicBuildup) == true;

        internal static bool InsectTriggerPresent(Pawn pawn) => NearbyInsect(pawn, 15f);

        internal static bool BetrayalTriggerPresent(Pawn pawn) => NearbyHostile(pawn, 12f);

        private static bool TriggerPresent(Pawn pawn)
        {
            if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_CombatShock)
                && CombatTriggerPresent(pawn))
                return true;
            if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Fire)
                && FireTriggerPresent(pawn))
                return true;
            if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Captivity)
                && CaptivityTriggerPresent(pawn))
                return true;
            if ((TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Massacre)
                    || TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_ViolentLoss)
                    || TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_BereavementShock))
                && CasualtyTriggerPresent(pawn))
                return true;
            if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Toxic)
                && ToxinTriggerPresent(pawn))
                return true;
            if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Insect)
                && InsectTriggerPresent(pawn))
                return true;
            if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Betrayal)
                && BetrayalTriggerPresent(pawn))
                return true;
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
            TraumaTriggerUtility.DiscoverFromFlashback(pawn);
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
