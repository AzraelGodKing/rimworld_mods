using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AsAboveSoBelow;

internal static class ABBelowGotoDrag
{
	private static Pawn pawn;

	private static Material circleMat;

	internal static bool Active => pawn != null;

	internal static void Start(Pawn p)
	{
		pawn = p;
	}

	internal static void Cancel()
	{
		pawn = null;
	}

	internal static void FrameUpdate(Map cur)
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		if (pawn == null)
		{
			return;
		}
		try
		{
			if (pawn.Dead || !((Thing)pawn).Spawned || !pawn.Drafted || !BelowSelection.TryGetBelowView(out var sky, out var lower) || sky != cur || ((Thing)pawn).Map != lower)
			{
				Cancel();
				return;
			}
			Vector3 val = UI.MouseMapPosition();
			IntVec3 val2 = IntVec3Utility.ToIntVec3(val);
			bool flag = GenGrid.InBounds(val2, sky) && sky.terrainGrid.TerrainAt(val2) == ABDefOf.AB_OpenAir;
			IntVec3 val3 = IntVec3Utility.ToIntVec3(LevelRenderer.ScreenToBelowPos(val));
			bool flag2 = flag && GenGrid.InBounds(val3, lower) && GenGrid.Standable(val3, lower) && !GridsUtility.Fogged(val3, lower);
			if (!Input.GetMouseButton(1))
			{
				Pawn val4 = pawn;
				Cancel();
				if (flag2)
				{
					IntVec3 val5 = RCellFinder.BestOrderedGotoDestNear(val3, val4, (Predicate<IntVec3>)null, true);
					if (((IntVec3)(ref val5)).IsValid)
					{
						FloatMenuOptionProvider_DraftedMove.PawnGotoAction(val3, val4, val5);
						SoundStarter.PlayOneShotOnCamera(SoundDefOf.ColonistOrdered, (Map)null);
					}
				}
			}
			else if (flag2)
			{
				Vector3 val6 = LevelRenderer.ShiftedBelowDrawPos(((IntVec3)(ref val3)).ToVector3Shifted());
				val6.y = Altitudes.AltitudeFor((AltitudeLayer)39);
				pawn.Drawer.renderer.RenderPawnAt(val6, (Rot4?)Rot4.South, false);
				if ((Object)(object)circleMat == (Object)null)
				{
					circleMat = MaterialPool.MatFrom("UI/Overlays/Circle75Solid", ShaderDatabase.Transparent, GenColor.FromBytes(153, 207, 135, 255) * new Color(1f, 1f, 1f, 0.4f));
				}
				Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(val6 + new Vector3(0f, 0.03f, 0f), Quaternion.identity, new Vector3(1.7f, 1f, 1.7f)), circleMat, 0);
			}
		}
		catch (Exception e)
		{
			Cancel();
			ABGuard.Disable(ABGuard.Ui, e, "below goto drag");
		}
	}
}
