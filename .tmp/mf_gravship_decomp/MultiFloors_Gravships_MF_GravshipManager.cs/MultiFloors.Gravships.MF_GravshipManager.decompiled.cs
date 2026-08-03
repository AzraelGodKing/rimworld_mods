using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MultiFloors.LevelWorkSettings;
using RimWorld;
using Verse;

namespace MultiFloors.Gravships;

[HotSwappable]
[UsedImplicitly]
public class MF_GravshipManager : GameComponent
{
	private int nextSubGravshipUniqueID;

	private List<SubGravship> subGravships = new List<SubGravship>();

	private Dictionary<Building_GravEngine, TileWorkSettings> tileWorkSettings = new Dictionary<Building_GravEngine, TileWorkSettings>();

	private List<Building_GravEngine> tmpGravEngines;

	private List<TileWorkSettings> tmpWorkSettings;

	public List<SubGravship> SubGravships => subGravships;

	public MF_GravshipManager()
	{
	}

	public MF_GravshipManager(Game game)
	{
	}

	public override void ExposeData()
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Invalid comparison between Unknown and I4
		((GameComponent)this).ExposeData();
		subGravships.RemoveAll((SubGravship ship) => ship == null);
		Scribe_Values.Look<int>(ref nextSubGravshipUniqueID, "nextSubGravshipUniqueID", 0, false);
		Scribe_Collections.Look<SubGravship>(ref subGravships, "mf_subGravships", (LookMode)2, Array.Empty<object>());
		Scribe_Collections.Look<Building_GravEngine, TileWorkSettings>(ref tileWorkSettings, "mf_tileWorkSettings", (LookMode)3, (LookMode)2, ref tmpGravEngines, ref tmpWorkSettings, true, false, false);
		if ((int)Scribe.mode == 4)
		{
			if (subGravships == null)
			{
				subGravships = new List<SubGravship>();
			}
			if (tileWorkSettings == null)
			{
				tileWorkSettings = new Dictionary<Building_GravEngine, TileWorkSettings>();
			}
		}
	}

	public int GetSubGravshipUniqueID()
	{
		int result = nextSubGravshipUniqueID;
		nextSubGravshipUniqueID++;
		return result;
	}

	public void RegisterSubGravship(SubGravship subGravship)
	{
		subGravships.Add(subGravship);
	}

	public void RemoveSubGravship(SubGravship subGravship)
	{
		subGravships.Remove(subGravship);
	}

	public void StoreTileWorkSettings(Building_GravEngine engine, TileWorkSettings settings)
	{
		tileWorkSettings.TryAdd(engine, settings);
	}

	public void RestoreTileWorkSettings(Building_GravEngine engine, MF_LevelMapComp controller)
	{
		if (controller != null && tileWorkSettings.TryGetValue(engine, out var value))
		{
			value.ReparentTo(controller);
		}
	}
}
