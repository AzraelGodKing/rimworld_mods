using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Nemesis
{
    public enum NemesisCommsKind
    {
        TauntBack,
        OfferTruce,
        DemandSurrender,
        BuyLead,
        PostBounty,
    }

    /// <summary>
    /// Go to the console's interaction cell, then run the chosen reply or purchase. The hunt
    /// and choice ride in <see cref="Job.count"/> (see <see cref="Encode"/>), which the job
    /// saves, so a reload mid-walk still knows what was asked.
    /// </summary>
    public class JobDriver_NemesisComms : JobDriver
    {
        private const int KindSlots = 8;

        public static int Encode(int huntId, NemesisCommsKind kind) => huntId * KindSlots + (int)kind;

        private int HuntId => job.count / KindSlots;
        private NemesisCommsKind Kind => (NemesisCommsKind)(job.count % KindSlots);

        private Building_CommsConsole Console => (Building_CommsConsole)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed) =>
            pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !Console.CanUseCommsNow);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            Toil call = ToilMaker.MakeToil("NemesisComms");
            call.initAction = () =>
            {
                int huntId = HuntId;
                Map map = pawn.Map;
                switch (Kind)
                {
                    case NemesisCommsKind.TauntBack:
                        NemesisPlayerCommand.Reply(huntId, NemesisReply.TauntBack);
                        break;
                    case NemesisCommsKind.OfferTruce:
                        NemesisPlayerCommand.Reply(huntId, NemesisReply.OfferTruce);
                        break;
                    case NemesisCommsKind.DemandSurrender:
                        NemesisPlayerCommand.Reply(huntId, NemesisReply.DemandSurrender);
                        break;
                    case NemesisCommsKind.BuyLead:
                        NemesisPlayerCommand.BuyLead(huntId, map);
                        break;
                    case NemesisCommsKind.PostBounty:
                        NemesisData h = GameComponent_Nemesis.Instance?.HuntById(huntId);
                        if (h != null)
                            NemesisPlayerCommand.PostBounty(huntId, map, NemesisInformants.LeadCost(h));
                        break;
                }
            };
            call.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return call;
        }
    }
}
