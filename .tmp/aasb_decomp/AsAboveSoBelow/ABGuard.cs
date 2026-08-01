using System;
using Verse;

namespace AsAboveSoBelow;

public static class ABGuard
{
	public static readonly ABGuardSwitch Ui = new ABGuardSwitch("ui");

	public static readonly ABGuardSwitch LevelGen = new ABGuardSwitch("levelGen");

	public static readonly ABGuardSwitch Rendering = new ABGuardSwitch("rendering");

	public static readonly ABGuardSwitch Movement = new ABGuardSwitch("movement");

	public static readonly ABGuardSwitch Combat = new ABGuardSwitch("combat");

	public static readonly ABGuardSwitch Logistics = new ABGuardSwitch("logistics");

	public static readonly ABGuardSwitch RoofSync = new ABGuardSwitch("roofSync");

	public static readonly ABGuardSwitch Weather = new ABGuardSwitch("weather");

	public static readonly ABGuardSwitch Power = new ABGuardSwitch("power");

	public static readonly ABGuardSwitch Pipes = new ABGuardSwitch("pipes");

	public static readonly ABGuardSwitch Climate = new ABGuardSwitch("climate");

	public static readonly ABGuardSwitch Threats = new ABGuardSwitch("threats");

	public static readonly ABGuardSwitch HostileMove = new ABGuardSwitch("hostileMove");

	public static readonly ABGuardSwitch World = new ABGuardSwitch("world");

	public static readonly ABGuardSwitch Social = new ABGuardSwitch("social");

	public static readonly ABGuardSwitch Transit = new ABGuardSwitch("transit");

	public static readonly ABGuardSwitch Async = new ABGuardSwitch("async");

	private static readonly ABGuardSwitch[] All = new ABGuardSwitch[17]
	{
		Ui, LevelGen, Rendering, Movement, Combat, Logistics, RoofSync, Weather, Power, Pipes,
		Climate, Threats, HostileMove, World, Social, Transit, Async
	};

	public static ABGuardSwitch[] AllSwitches => All;

	public static bool On(ABGuardSwitch subsystem)
	{
		return subsystem.on;
	}

	public static void Disable(ABGuardSwitch subsystem, Exception e, string context)
	{
		if (subsystem.on)
		{
			subsystem.on = false;
			subsystem.lastContext = context;
			Log.Error("[As above, So below] Subsystem '" + subsystem.name + "' hit an error in " + context + " and shut itself down to protect your game. Other features keep running. Details: " + e);
		}
	}

	public static void ReArm(ABGuardSwitch subsystem)
	{
		if (!subsystem.on)
		{
			subsystem.on = true;
			subsystem.lastContext = null;
			ABLog.Dev("Kill switch '" + subsystem.name + "' re-armed from settings.");
		}
	}

	public static void Reset()
	{
		int num = 0;
		for (int i = 0; i < All.Length; i++)
		{
			if (!All[i].on)
			{
				All[i].on = true;
				All[i].lastContext = null;
				num++;
			}
		}
		if (num > 0)
		{
			ABLog.Dev("Cleared " + num + " tripped kill switch(es).");
		}
	}
}
