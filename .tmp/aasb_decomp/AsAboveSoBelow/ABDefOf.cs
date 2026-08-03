using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[DefOf]
public static class ABDefOf
{
	public static KeyBindingDef AB_ViewLevelUp;

	public static KeyBindingDef AB_ViewLevelDown;

	public static KeyBindingDef AB_CameraLevelLock;

	public static JobDef AB_UseStairs;

	public static JobDef AB_CrossLevelAttack;

	public static JobDef AB_HaulAcrossLevels;

	public static JobDef AB_RescueAcrossLevels;

	public static JobDef AB_CaptureAcrossLevels;

	public static JobDef AB_TakePrisonerAcrossLevels;

	public static MapGeneratorDef AB_Basement;

	public static MapGeneratorDef AB_Sky;

	public static TerrainDef AB_OpenAir;

	public static TerrainDef AB_RoofSurface;

	public static TerrainDef AB_MountainTop;

	public static MapMeshFlagDef AB_BelowThings;

	static ABDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(ABDefOf));
	}
}
