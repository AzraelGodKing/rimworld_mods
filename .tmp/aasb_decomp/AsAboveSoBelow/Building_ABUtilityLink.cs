using System.Collections.Generic;
using Verse;

namespace AsAboveSoBelow;

public class Building_ABUtilityLink : Building_ABStairs
{
	protected override List<FloatMenuOption> BuildUseOptions(Pawn selPawn)
	{
		return new List<FloatMenuOption>();
	}

	protected override string LinkLine()
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		if (base.Counterpart != null)
		{
			return TaggedString.op_Implicit(Translator.Translate((base.DeltaLevel > 0) ? "AB_LinkedAbove" : "AB_LinkedBelow"));
		}
		return TaggedString.op_Implicit(Translator.Translate("AB_NotLinkedLine"));
	}
}
