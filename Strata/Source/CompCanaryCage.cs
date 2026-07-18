using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Strata
{
    public class CompProperties_CanaryCage : CompProperties_CagedBird
    {
        public float hayCost = 10f;

        public CompProperties_CanaryCage()
        {
            compClass = typeof(CompCanaryCage);
        }
    }

    public class CompCanaryCage : CompCagedBirdBase
    {
        private bool distressed;
        private StrataGasDef threatGas;
        private bool legacyCanaryAlive = true;

        private CompProperties_CanaryCage CanaryProps => (CompProperties_CanaryCage)props;

        public Pawn ContainedCanary => contained;

        public bool HasLiveCanary => HasLiveBird;

        public bool NeedsAttention =>
            (HasLiveBird && distressed) || (contained != null && contained.Dead);

        public new static bool IsContained(Pawn pawn) => StrataCagedBirdRegistry.IsContained(pawn);

        protected override string OccupantNoun => "mine canary";

        protected override bool CanAccept(Pawn pawn) =>
            StrataCanaryBirdUtility.IsMineCanary(pawn);

        protected override string GetAssignRejectReason(Pawn pawn)
        {
            if (pawn != null && StrataCanaryBirdUtility.IsAssignableBird(pawn)
                && !StrataCanaryBirdUtility.IsMineCanary(pawn))
            {
                return "Strata_Canary_WrongBirdCage".Translate();
            }
            return "Strata_Canary_NotMineCanary".Translate();
        }

        protected override IEnumerable<Pawn> EnumerateAssignCandidates()
        {
            Map map = parent.Map;
            if (map == null)
            {
                yield break;
            }
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (!StrataCanaryBirdUtility.IsMineCanary(pawn))
                {
                    continue;
                }
                if (pawn.Dead || IsContained(pawn))
                {
                    continue;
                }
                if (pawn.Faction != Faction.OfPlayer)
                {
                    continue;
                }
                if (!pawn.Position.InHorDistOf(parent.Position, Props.assignRadius))
                {
                    continue;
                }
                yield return pawn;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            EnsureLegacyMigration();
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref distressed, "strataCanaryDistressed", defaultValue: false);
            Scribe_Defs.Look(ref threatGas, "strataCanaryThreatGas");
            Scribe_Values.Look(ref legacyCanaryAlive, "strataCanaryAlive", defaultValue: true);
            Scribe_References.Look(ref contained, "strataCagedCanary");
            Scribe_References.Look(ref contained, "strataCagedBird");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (HasLiveBird)
                {
                    StrataCagedBirdRegistry.Register(contained);
                }
                else if (contained != null && contained.Dead)
                {
                    StrataCagedBirdRegistry.Unregister(contained);
                }
            }
        }

        public override void CompTick()
        {
            if (!parent.Spawned)
            {
                return;
            }
            EnsureLegacyMigration();
            base.CompTick();
        }

        private void EnsureLegacyMigration()
        {
            if (contained != null || !legacyCanaryAlive || !parent.Spawned)
            {
                return;
            }
            AcquireCanary(silent: true, consumeHay: false);
        }

        protected override void OnRareTick()
        {
            Map map = parent.Map;
            Room room = parent.Position.GetRoom(map);
            AtmosphereMapComponent atmosphere = map?.GetComponent<AtmosphereMapComponent>();
            StrataCanaryUtility.ThreatLevel level =
                StrataCanaryUtility.EvaluateRoom(atmosphere, room, out StrataGasDef gas);
            if (level == StrataCanaryUtility.ThreatLevel.Lethal)
            {
                KillCanary(gas);
                return;
            }
            if (level == StrataCanaryUtility.ThreatLevel.Distress)
            {
                if (!distressed || gas != threatGas)
                {
                    distressed = true;
                    threatGas = gas;
                }
                return;
            }
            distressed = false;
            threatGas = null;
        }

        private void KillCanary(StrataGasDef gas)
        {
            if (!HasLiveBird)
            {
                return;
            }
            distressed = false;
            threatGas = gas;
            string gasLabel = gas?.label ?? "bad air";
            Pawn victim = contained;
            StrataCagedBirdRegistry.Unregister(victim);
            victim.Kill(null);
            contained = null;
            Messages.Message(
                "Strata_Canary_Died".Translate(parent.LabelShort, gasLabel),
                parent,
                MessageTypeDefOf.NegativeEvent);
        }

        public void AcquireCanary(bool silent = false, bool consumeHay = true)
        {
            Map map = parent.Map;
            if (map == null || HasLiveBird)
            {
                return;
            }
            if (consumeHay && !TryConsumeHay(out _))
            {
                if (!silent)
                {
                    Messages.Message(
                        "Strata_Canary_NeedHay".Translate(CanaryProps.hayCost, parent.LabelShort),
                        parent,
                        MessageTypeDefOf.RejectInput,
                        historical: false);
                }
                return;
            }
            PawnKindDef birdKind = StrataPawnKindDefOf.Strata_Canary;
            if (birdKind == null)
            {
                if (!silent)
                {
                    Messages.Message(
                        "Strata_Canary_KindMissing".Translate(),
                        parent,
                        MessageTypeDefOf.RejectInput,
                        historical: false);
                }
                return;
            }
            Pawn newCanary = PawnGenerator.GeneratePawn(
                new PawnGenerationRequest(
                    birdKind,
                    Faction.OfPlayer,
                    PawnGenerationContext.NonPlayer,
                    -1,
                    forceGenerateNewPawn: true));
            GenSpawn.Spawn(newCanary, parent.Position, map);
            contained = newCanary;
            StrataCagedBirdRegistry.Register(newCanary);
            newCanary.jobs?.StopAll();
            newCanary.pather?.StopDead();
            StrataCagedBirdUtility.FeedCagedBird(parent, newCanary);
            distressed = false;
            threatGas = null;
            legacyCanaryAlive = true;
            if (!silent)
            {
                Messages.Message(
                    "Strata_Canary_Acquired".Translate(parent.LabelShort),
                    parent,
                    MessageTypeDefOf.PositiveEvent);
            }
        }

        private bool TryConsumeHay(out Thing hay)
        {
            hay = GenClosest.ClosestThingReachable(
                parent.Position,
                parent.Map,
                ThingRequest.ForDef(ThingDefOf.Hay),
                PathEndMode.ClosestTouch,
                TraverseParms.For(TraverseMode.NoPassClosedDoors),
                CanaryProps.hayCost,
                thing => !thing.IsForbidden(Faction.OfPlayer) && thing.stackCount >= CanaryProps.hayCost);
            if (hay == null)
            {
                return false;
            }
            hay.SplitOff((int)CanaryProps.hayCost).Destroy();
            return true;
        }

        protected override IEnumerable<Gizmo> ExtraEmptyGizmos()
        {
            string acquireLabel = contained != null && contained.Dead
                ? "Strata_Canary_ReplaceLabel".Translate()
                : "Strata_Canary_AcquireLabel".Translate();
            yield return new Command_Action
            {
                defaultLabel = acquireLabel,
                defaultDesc = "Strata_Canary_AcquireDesc".Translate(CanaryProps.hayCost),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/ReleaseAnimalToWild", reportFailure: false),
                action = () => AcquireCanary(),
            };
        }

        protected override string EmptyInspectExtra()
        {
            if (contained != null && contained.Dead)
            {
                string cause = threatGas != null ? $" ({threatGas.label})" : "";
                return "Strata_Canary_DeadInspect".Translate(cause, CanaryProps.hayCost);
            }
            return "Strata_Canary_EmptyInspect".Translate(CanaryProps.hayCost);
        }

        protected override string OccupiedInspectExtra()
        {
            if (distressed)
            {
                string gas = threatGas?.label ?? "bad air";
                return "Strata_Canary_Distressed".Translate(gas);
            }
            string feeding = StrataCagedBirdUtility.FeedingInspectHint(parent, contained);
            Room room = parent.Position.GetRoom(parent.Map);
            if (room != null && !room.PsychologicallyOutdoors)
            {
                return feeding + "Strata_Canary_AirSafe".Translate();
            }
            return feeding;
        }
    }
}
