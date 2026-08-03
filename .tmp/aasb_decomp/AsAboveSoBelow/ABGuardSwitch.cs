namespace AsAboveSoBelow;

public sealed class ABGuardSwitch
{
	internal readonly string name;

	internal bool on = true;

	internal string lastContext;

	public string Name => name;

	public bool IsOn => on;

	public string LastContext => lastContext;

	internal ABGuardSwitch(string name)
	{
		this.name = name;
	}
}
