using Verse;

namespace AsAboveSoBelow;

[StaticConstructorOnStartup]
internal static class NeedMigrationBuiltins
{
	static NeedMigrationBuiltins()
	{
		ABApi.RegisterNeedJobGiver("RimWorld.JobGiver_BingeDrug", allowInMentalState: true);
		ABApi.RegisterNeedJobGiver("RimWorld.JobGiver_BingeFood", allowInMentalState: true);
		ABApi.RegisterNeedJobGiver("RimWorld.JobGiver_Berserk", allowInMentalState: true);
		ABApi.RegisterNeedJobGiver("RimWorld.JobGiver_MurderousRage", allowInMentalState: true);
		if (ABDetect.Active("rim.job.world"))
		{
			ABApi.RegisterNeedJobGiver("rjw.JobGiver_JoinInBed");
			ABApi.RegisterNeedJobGiver("rjw.JobGiver_DoQuickie");
			ABApi.RegisterNeedJobGiver("rjw.JobGiver_Breed");
			ABApi.RegisterNeedJobGiver("rjw.JobGiver_Bestiality");
			ABApi.RegisterNeedJobGiver("rjw.JobGiver_ComfortPrisonerRape");
			ABApi.RegisterNeedJobGiver("rjw.JobGiver_RandomRape", allowInMentalState: true);
			ABApi.RegisterNeedJobGiver("rjw.JobGiver_RapeEnemy");
			ABLog.Dev("RJW detected: sex, breeding and rape givers registered for cross-level migration.");
		}
		if (ABDetect.Active("LovelyDovey.Sex.WithEuterpe"))
		{
			ABApi.RegisterNeedJobGiver("LoveyDoveySexWithEuterpe.JobGiver_GetIntimacy");
			ABLog.Dev("Intimacy detected: intimacy giver registered for cross-level migration.");
		}
	}
}
