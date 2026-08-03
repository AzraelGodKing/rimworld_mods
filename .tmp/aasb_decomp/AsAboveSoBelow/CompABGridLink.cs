using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

public class CompABGridLink : CompPowerTrader
{
	private const int UpdateInterval = 15;

	private const float MinFlow = 1f;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		((CompPowerTrader)this).PostSpawnSetup(respawningAfterLoad);
		((CompPowerTrader)this).PowerOn = true;
	}

	public override string CompInspectStringExtra()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		float powerOutput = ((CompPowerTrader)this).PowerOutput;
		if (powerOutput > 1f)
		{
			return TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate("AB_GridLinkReceiving", NamedArgument.op_Implicit(powerOutput.ToString("F0"))));
		}
		if (powerOutput < -1f)
		{
			return TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate("AB_GridLinkSending", NamedArgument.op_Implicit((0f - powerOutput).ToString("F0"))));
		}
		return TaggedString.op_Implicit(Translator.Translate("AB_GridLinkIdle"));
	}

	public override void CompTick()
	{
		((ThingComp)this).CompTick();
		if (!Gen.IsHashIntervalTick((Thing)(object)((ThingComp)this).parent, 15))
		{
			return;
		}
		if (!ABGuard.On(ABGuard.Power))
		{
			if (((CompPowerTrader)this).PowerOutput != 0f)
			{
				((CompPowerTrader)this).PowerOutput = 0f;
			}
			return;
		}
		try
		{
			Recompute();
		}
		catch (Exception e)
		{
			((CompPowerTrader)this).PowerOutput = 0f;
			ABGuard.Disable(ABGuard.Power, e, "direct power link");
		}
	}

	private void Recompute()
	{
		object obj;
		if (!(((ThingComp)this).parent is Building_ABStairs building_ABStairs))
		{
			obj = null;
		}
		else
		{
			Building_ABStairs counterpart = building_ABStairs.Counterpart;
			obj = ((counterpart != null) ? ((ThingWithComps)counterpart).GetComp<CompABGridLink>() : null);
		}
		CompABGridLink compABGridLink = (CompABGridLink)obj;
		if (compABGridLink == null || ((Thing)((ThingComp)compABGridLink).parent).Map == null)
		{
			((CompPowerTrader)this).PowerOutput = 0f;
		}
		else
		{
			if (((Thing)((ThingComp)this).parent).Map.uniqueID > ((Thing)((ThingComp)compABGridLink).parent).Map.uniqueID)
			{
				return;
			}
			PowerNet powerNet = ((CompPower)this).PowerNet;
			PowerNet powerNet2 = ((CompPower)compABGridLink).PowerNet;
			if (powerNet == null || powerNet2 == null || powerNet == powerNet2)
			{
				((CompPowerTrader)this).PowerOutput = 0f;
				((CompPowerTrader)compABGridLink).PowerOutput = 0f;
				return;
			}
			Measure(powerNet, this, compABGridLink, out var gain, out var offWanting, out var otherLinks);
			Measure(powerNet2, this, compABGridLink, out var gain2, out var offWanting2, out var otherLinks2);
			float senderStored = powerNet.CurrentStoredEnergy();
			float senderStored2 = powerNet2.CurrentStoredEnergy();
			float deficit = Mathf.Max(0f, Mathf.Max(0f, 0f - gain) + offWanting - Mathf.Max(0f, otherLinks));
			float deficit2 = Mathf.Max(0f, Mathf.Max(0f, 0f - gain2) + offWanting2 - Mathf.Max(0f, otherLinks2));
			float num = FlowToward(deficit2, gain, senderStored, powerNet2);
			float num2 = FlowToward(deficit, gain2, senderStored2, powerNet);
			float num3 = num - num2;
			if (Mathf.Abs(num3) < 1f)
			{
				num3 = 0f;
			}
			((CompPowerTrader)this).PowerOutput = 0f - num3;
			((CompPowerTrader)compABGridLink).PowerOutput = num3;
			if (!((CompPowerTrader)compABGridLink).PowerOn)
			{
				((CompPowerTrader)compABGridLink).PowerOn = true;
			}
			if (!((CompPowerTrader)this).PowerOn)
			{
				((CompPowerTrader)this).PowerOn = true;
			}
		}
	}

	private static float FlowToward(float deficit, float senderGain, float senderStored, PowerNet receiver)
	{
		float num = Mathf.Max(0f, senderGain);
		float num2 = ((senderStored > 1f) ? (senderStored * 4000f) : 0f);
		float num3 = Mathf.Min(deficit, num + num2);
		float num4 = Mathf.Max(0f, num - num3);
		if (num4 > 1f && BatteryRoom(receiver) > 1f)
		{
			num3 += num4;
		}
		return num3;
	}

	private static float BatteryRoom(PowerNet net)
	{
		float num = 0f;
		for (int i = 0; i < net.batteryComps.Count; i++)
		{
			CompPowerBattery val = net.batteryComps[i];
			num += val.Props.storedEnergyMax - val.StoredEnergy;
		}
		return num;
	}

	private static void Measure(PowerNet net, CompABGridLink self, CompABGridLink counterpart, out float gain, out float offWanting, out float otherLinks)
	{
		gain = 0f;
		offWanting = 0f;
		otherLinks = 0f;
		for (int i = 0; i < net.powerComps.Count; i++)
		{
			CompPowerTrader val = net.powerComps[i];
			if (val is CompABGridLink compABGridLink)
			{
				if (compABGridLink != self && compABGridLink != counterpart && ((CompPowerTrader)compABGridLink).PowerOn)
				{
					otherLinks += ((CompPowerTrader)compABGridLink).PowerOutput;
				}
				continue;
			}
			if (val.PowerOn)
			{
				gain += val.PowerOutput;
				continue;
			}
			float powerConsumption = ((CompPower)val).Props.PowerConsumption;
			if (powerConsumption > 0f && FlickUtility.WantsToBeOn((Thing)(object)((ThingComp)val).parent) && !BreakdownableUtility.IsBrokenDown((Thing)(object)((ThingComp)val).parent))
			{
				offWanting += powerConsumption;
			}
		}
	}
}
