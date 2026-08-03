using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

public class ABMod : Mod
{
	public static ABSettings Settings { get; private set; }

	public static ModContentPack ModContent { get; private set; }

	public ABMod(ModContentPack content)
		: base(content)
	{
		Settings = ((Mod)this).GetSettings<ABSettings>();
		ModContent = content;
	}

	public override string SettingsCategory()
	{
		return "As above, So below";
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		Settings.DoWindowContents(inRect);
	}
}
