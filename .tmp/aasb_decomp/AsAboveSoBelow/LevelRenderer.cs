using System;
using System.Collections.Generic;
using System.Threading;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace AsAboveSoBelow;

public static class LevelRenderer
{
	public const float BelowOffset = -2.5f;

	private const float MaxSubMeshAltitude = 10f;

	private const float MaskAltitude = -0.1f;

	private const float SkirtAltitude = -0.05f;

	internal const float SkirtLedgeWidth = 0.08f;

	private const int MaskRebuildIntervalFrames = 15;

	private const int MaskPadCells = 8;

	public static volatile bool OffsetActive;

	private static int transformFrame = -1;

	private static readonly Matrix4x4 belowMatrixConst = Matrix4x4.Translate(new Vector3(0f, -2.5f, 0f));

	internal static float BelowThingScale = 0.85f;

	private static readonly FieldRef<Section, List<SectionLayer>> LayersRef = AccessTools.FieldRefAccess<Section, List<SectionLayer>>("layers");

	private static readonly FieldRef<MapDrawer, Section[,]> SectionsRef = AccessTools.FieldRefAccess<MapDrawer, Section[,]>("sections");

	private static readonly HashSet<Type> ContentLayerTypes = BuildContentLayerTypes();

	private static readonly Dictionary<Type, int> BelowLayerOffsets = BuildBelowLayerOffsets();

	private const int BelowThingsOffset = 200;

	private const int BelowDefaultOffset = 150;

	private static int belowQueueCeiling = -1;

	private static readonly Dictionary<(Material, int), Material> belowMats = new Dictionary<(Material, int), Material>();

	private static Mesh maskMesh;

	private static Material maskMat;

	private static int maskLastFrame = -999;

	private static CellRect maskLastRect;

	private static int maskLastLowerId = -1;

	private static readonly List<Vector3> maskVerts = new List<Vector3>();

	private static readonly List<int> maskTris = new List<int>();

	private static readonly List<Color32> maskColors = new List<Color32>();

	private static Mesh skirtMesh;

	private static Material skirtMat;

	private static readonly List<Vector3> skirtVerts = new List<Vector3>();

	private static readonly List<int> skirtTris = new List<int>();

	private static readonly List<Color32> skirtColors = new List<Color32>();

	private static volatile bool maskJobRunning;

	private static volatile bool maskJobDone;

	private static Exception maskJobError;

	private static CellRect maskJobRect;

	private static int maskJobLowerId;

	private static readonly List<Vector3> jobVerts = new List<Vector3>();

	private static readonly List<int> jobTris = new List<int>();

	private static readonly List<Color32> jobColors = new List<Color32>();

	private static readonly List<Vector3> jobSkirtVerts = new List<Vector3>();

	private static readonly List<int> jobSkirtTris = new List<int>();

	private static readonly List<Color32> jobSkirtColors = new List<Color32>();

	private static bool maskJobSkirtOn;

	private const int FacadeQueueOffset = 55;

	private static int wallRevealQueue = -1;

	private static int maskLastStep = 1;

	private static readonly Color32 SkirtLedge = new Color32((byte)16, (byte)16, (byte)14, (byte)110);

	private static FieldRef<DynamicDrawManager, List<Thing>> drawThingsRef;

	private static bool drawThingsRefFailed;

	internal static Matrix4x4 BelowMatrix
	{
		get
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			RefreshBelowTransform();
			return belowMatrixConst;
		}
	}

	internal static int DebugQueueCeiling => BelowQueueCeiling;

	private static int BelowQueueCeiling
	{
		get
		{
			if (belowQueueCeiling < 0)
			{
				int num = int.MaxValue;
				int current = num;
				TerrainDef soil = TerrainDefOf.Soil;
				object m;
				if (soil == null)
				{
					m = null;
				}
				else
				{
					Graphic graphic = ((BuildableDef)soil).graphic;
					m = ((graphic != null) ? graphic.MatSingle : null);
				}
				num = MinQueue(current, (Material)m);
				int current2 = num;
				TerrainDef aB_RoofSurface = ABDefOf.AB_RoofSurface;
				object m2;
				if (aB_RoofSurface == null)
				{
					m2 = null;
				}
				else
				{
					Graphic graphic2 = ((BuildableDef)aB_RoofSurface).graphic;
					m2 = ((graphic2 != null) ? graphic2.MatSingle : null);
				}
				num = MinQueue(current2, (Material)m2);
				int current3 = num;
				TerrainDef aB_MountainTop = ABDefOf.AB_MountainTop;
				object m3;
				if (aB_MountainTop == null)
				{
					m3 = null;
				}
				else
				{
					Graphic graphic3 = ((BuildableDef)aB_MountainTop).graphic;
					m3 = ((graphic3 != null) ? graphic3.MatSingle : null);
				}
				num = MinQueue(current3, (Material)m3);
				int current4 = num;
				TerrainDef metalTile = TerrainDefOf.MetalTile;
				object m4;
				if (metalTile == null)
				{
					m4 = null;
				}
				else
				{
					Graphic graphic4 = ((BuildableDef)metalTile).graphic;
					m4 = ((graphic4 != null) ? graphic4.MatSingle : null);
				}
				num = MinQueue(current4, (Material)m4);
				int current5 = num;
				TerrainDef woodPlankFloor = TerrainDefOf.WoodPlankFloor;
				object m5;
				if (woodPlankFloor == null)
				{
					m5 = null;
				}
				else
				{
					Graphic graphic5 = ((BuildableDef)woodPlankFloor).graphic;
					m5 = ((graphic5 != null) ? graphic5.MatSingle : null);
				}
				num = MinQueue(current5, (Material)m5);
				if ((Object)(object)ShaderDatabase.TerrainHard != (Object)null)
				{
					int renderQueue = ShaderDatabase.TerrainHard.renderQueue;
					if (renderQueue >= 500)
					{
						num = Mathf.Min(num, renderQueue);
					}
				}
				belowQueueCeiling = ((num >= 500 && num != int.MaxValue) ? num : 2000);
			}
			return belowQueueCeiling;
		}
	}

	private static int WallRevealQueue
	{
		get
		{
			if (wallRevealQueue < 0)
			{
				int num = 0;
				int current = num;
				TerrainDef soil = TerrainDefOf.Soil;
				object m;
				if (soil == null)
				{
					m = null;
				}
				else
				{
					Graphic graphic = ((BuildableDef)soil).graphic;
					m = ((graphic != null) ? graphic.MatSingle : null);
				}
				num = MaxQueue(current, (Material)m);
				int current2 = num;
				TerrainDef aB_RoofSurface = ABDefOf.AB_RoofSurface;
				object m2;
				if (aB_RoofSurface == null)
				{
					m2 = null;
				}
				else
				{
					Graphic graphic2 = ((BuildableDef)aB_RoofSurface).graphic;
					m2 = ((graphic2 != null) ? graphic2.MatSingle : null);
				}
				num = MaxQueue(current2, (Material)m2);
				int current3 = num;
				TerrainDef aB_MountainTop = ABDefOf.AB_MountainTop;
				object m3;
				if (aB_MountainTop == null)
				{
					m3 = null;
				}
				else
				{
					Graphic graphic3 = ((BuildableDef)aB_MountainTop).graphic;
					m3 = ((graphic3 != null) ? graphic3.MatSingle : null);
				}
				num = MaxQueue(current3, (Material)m3);
				int current4 = num;
				TerrainDef metalTile = TerrainDefOf.MetalTile;
				object m4;
				if (metalTile == null)
				{
					m4 = null;
				}
				else
				{
					Graphic graphic4 = ((BuildableDef)metalTile).graphic;
					m4 = ((graphic4 != null) ? graphic4.MatSingle : null);
				}
				num = MaxQueue(current4, (Material)m4);
				int current5 = num;
				TerrainDef woodPlankFloor = TerrainDefOf.WoodPlankFloor;
				object m5;
				if (woodPlankFloor == null)
				{
					m5 = null;
				}
				else
				{
					Graphic graphic5 = ((BuildableDef)woodPlankFloor).graphic;
					m5 = ((graphic5 != null) ? graphic5.MatSingle : null);
				}
				num = MaxQueue(current5, (Material)m5);
				if (num < 500)
				{
					num = 2000;
				}
				int num2 = (((Object)(object)ShaderDatabase.Cutout != (Object)null && ShaderDatabase.Cutout.renderQueue >= 500) ? ShaderDatabase.Cutout.renderQueue : (num + 450));
				wallRevealQueue = Mathf.Min(num + 1, num2 - 1);
			}
			return wallRevealQueue;
		}
	}

	internal static void ApplyDrawShift(ref Vector3 v)
	{
		v.y += -2.5f;
	}

	internal static void EnsureBelowTransform()
	{
		RefreshBelowTransform();
	}

	internal static Vector3 ShiftedBelowDrawPos(Vector3 drawPos)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		RefreshBelowTransform();
		ApplyDrawShift(ref drawPos);
		return drawPos;
	}

	internal static Vector3 ScreenToBelowPos(Vector3 screenPos)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		return screenPos;
	}

	private static void RefreshBelowTransform()
	{
		int frameCount = Time.frameCount;
		if (frameCount != transformFrame)
		{
			transformFrame = frameCount;
			BelowThingScale = Mathf.Clamp(ABMod.Settings?.belowThingScale ?? 0.85f, 0.5f, 1f);
		}
	}

	internal static bool DrawerReady(Map m)
	{
		return m?.mapDrawer != null && SectionsRef.Invoke(m.mapDrawer) != null;
	}

	internal static Section[,] DebugSections(Map m)
	{
		return (m?.mapDrawer != null) ? SectionsRef.Invoke(m.mapDrawer) : null;
	}

	internal static List<SectionLayer> DebugLayers(Section s)
	{
		return LayersRef.Invoke(s);
	}

	private static HashSet<Type> BuildContentLayerTypes()
	{
		HashSet<Type> hashSet = new HashSet<Type>();
		hashSet.Add(typeof(SectionLayer_Terrain));
		hashSet.Add(typeof(SectionLayer_Snow));
		hashSet.Add(typeof(SectionLayer_Gas));
		hashSet.Add(typeof(SectionLayer_PollutionCloud));
		hashSet.Add(typeof(SectionLayer_EdgeShadows));
		HashSet<Type> hashSet2 = hashSet;
		AddByName(hashSet2, "Verse.SectionLayer_SunShadows");
		AddByName(hashSet2, "Verse.SectionLayer_Sand");
		AddByName(hashSet2, "RimWorld.SectionLayer_TerrainEdges");
		AddByName(hashSet2, "Verse.SectionLayer_TerrainScatter");
		AddByName(hashSet2, "RimWorld.SectionLayer_BridgeProps");
		return hashSet2;
	}

	private static void AddByName(HashSet<Type> set, string typeName)
	{
		Type type = AccessTools.TypeByName(typeName);
		if (type != null)
		{
			set.Add(type);
		}
	}

	private static Dictionary<Type, int> BuildBelowLayerOffsets()
	{
		Dictionary<Type, int> dictionary = new Dictionary<Type, int>();
		dictionary.Add(typeof(SectionLayer_Terrain), 300);
		dictionary.Add(typeof(SectionLayer_EdgeShadows), 260);
		dictionary.Add(typeof(SectionLayer_Snow), 90);
		dictionary.Add(typeof(SectionLayer_Gas), 85);
		dictionary.Add(typeof(SectionLayer_PollutionCloud), 80);
		Dictionary<Type, int> dictionary2 = dictionary;
		AddOffsetByName(dictionary2, "Verse.SectionLayer_Sand", 285);
		AddOffsetByName(dictionary2, "RimWorld.SectionLayer_TerrainEdges", 280);
		AddOffsetByName(dictionary2, "Verse.SectionLayer_TerrainScatter", 275);
		AddOffsetByName(dictionary2, "Verse.SectionLayer_SunShadows", 255);
		AddOffsetByName(dictionary2, "RimWorld.SectionLayer_BridgeProps", 250);
		return dictionary2;
	}

	private static void AddOffsetByName(Dictionary<Type, int> map, string typeName, int offset)
	{
		Type type = AccessTools.TypeByName(typeName);
		if (type != null)
		{
			map[type] = offset;
		}
	}

	private static int MinQueue(int current, Material m)
	{
		if ((Object)(object)m == (Object)null)
		{
			return current;
		}
		int renderQueue = m.renderQueue;
		if (renderQueue <= 0 && (Object)(object)m.shader != (Object)null)
		{
			renderQueue = m.shader.renderQueue;
		}
		return (renderQueue >= 500) ? Mathf.Min(current, renderQueue) : current;
	}

	private static Material BelowMaterialFor(Material source, int queue)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Expected O, but got Unknown
		if ((Object)(object)source == (Object)null)
		{
			return null;
		}
		(Material, int) key = (source, queue);
		if (belowMats.TryGetValue(key, out var value))
		{
			return value;
		}
		if (belowMats.Count > 1024)
		{
			belowMats.Clear();
		}
		value = new Material(source)
		{
			renderQueue = queue
		};
		belowMats[key] = value;
		return value;
	}

	internal static void DrawBelowSubMesh(LayerSubMesh sub)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		int num = Mathf.Max(BelowQueueCeiling - 200, 1);
		Bounds bounds = sub.mesh.bounds;
		float y = ((Bounds)(ref bounds)).center.y;
		int queue = num + Mathf.Clamp((int)(y * 14f), 0, 99);
		Material val = BelowMaterialFor(sub.material, queue);
		if ((Object)(object)val != (Object)null)
		{
			Graphics.DrawMesh(sub.mesh, BelowMatrix, val, 0);
		}
	}

	internal static void DrawWallFacadeSubMesh(LayerSubMesh sub)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		Material val = BelowMaterialFor(sub.material, Mathf.Max(BelowQueueCeiling - 55, 1));
		if ((Object)(object)val != (Object)null)
		{
			Graphics.DrawMesh(sub.mesh, Matrix4x4.identity, val, 0);
		}
	}

	private static int MaxQueue(int current, Material m)
	{
		if ((Object)(object)m == (Object)null)
		{
			return current;
		}
		int renderQueue = m.renderQueue;
		if (renderQueue <= 0 && (Object)(object)m.shader != (Object)null)
		{
			renderQueue = m.shader.renderQueue;
		}
		return (renderQueue >= 500) ? Mathf.Max(current, renderQueue) : current;
	}

	internal static void DrawWallRevealSubMesh(LayerSubMesh sub)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		Material val = BelowMaterialFor(sub.material, WallRevealQueue);
		if ((Object)(object)val != (Object)null)
		{
			Graphics.DrawMesh(sub.mesh, Matrix4x4.identity, val, 0);
		}
	}

	public static void DrawBelowStatic(Map map)
	{
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		if (!ABGuard.On(ABGuard.Rendering) || map == null || map != Find.CurrentMap)
		{
			return;
		}
		LevelComp levelComp = map.Levels();
		if (levelComp == null || levelComp.level <= 0)
		{
			return;
		}
		Map lowerMap = levelComp.lowerMap;
		if (lowerMap == null || lowerMap.Disposed)
		{
			return;
		}
		try
		{
			Camera camera = Find.Camera;
			if ((Object)(object)camera != (Object)null && camera.farClipPlane < 70f)
			{
				camera.farClipPlane = 70f;
			}
			WaterInfo waterInfo = lowerMap.waterInfo;
			if (waterInfo != null)
			{
				waterInfo.SetTextures();
			}
			lowerMap.mapDrawer.MapMeshDrawerUpdate_First();
			CellRect val = Find.CameraDriver.CurrentViewRect;
			val = ((CellRect)(ref val)).ExpandedBy(1);
			CellRect view = ((CellRect)(ref val)).ClipInsideMap(lowerMap);
			DrawSections(lowerMap, view);
			DrawBelowMask(map, lowerMap, view);
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Rendering, e, "see-below rendering");
		}
	}

	private static void DrawSections(Map lower, CellRect view)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		Section[,] array = SectionsRef.Invoke(lower.mapDrawer);
		int upperBound = array.GetUpperBound(0);
		int upperBound2 = array.GetUpperBound(1);
		int num = Mathf.Max(0, view.minX / 17);
		int num2 = Mathf.Max(0, view.minZ / 17);
		int num3 = Mathf.Min(upperBound, view.maxX / 17);
		int num4 = Mathf.Min(upperBound2, view.maxZ / 17);
		for (int i = num; i <= num3; i++)
		{
			for (int j = num2; j <= num4; j++)
			{
				List<SectionLayer> list = LayersRef.Invoke(array[i, j]);
				for (int k = 0; k < list.Count; k++)
				{
					SectionLayer val = list[k];
					Type type = ((object)val).GetType();
					if (!ContentLayerTypes.Contains(type) || !((MapDrawLayer)val).Visible)
					{
						continue;
					}
					if (!BelowLayerOffsets.TryGetValue(type, out var value))
					{
						value = 150;
					}
					int num5 = Mathf.Max(BelowQueueCeiling - value, 1);
					bool flag = type == typeof(SectionLayer_Terrain);
					List<LayerSubMesh> subMeshes = ((MapDrawLayer)val).subMeshes;
					for (int l = 0; l < subMeshes.Count; l++)
					{
						LayerSubMesh val2 = subMeshes[l];
						Bounds bounds = val2.mesh.bounds;
						float y = ((Bounds)(ref bounds)).center.y;
						if (val2.finalized && !val2.disabled && !(y > 10f))
						{
							int num6 = num5;
							if (flag && (Object)(object)val2.material != (Object)null)
							{
								num6 += Mathf.Clamp((val2.material.renderQueue - 2000) / 25, 0, 14);
							}
							Material val3 = BelowMaterialFor(val2.material, num6);
							if ((Object)(object)val3 != (Object)null)
							{
								Graphics.DrawMesh(val2.mesh, BelowMatrix, val3, 0);
							}
						}
					}
				}
			}
		}
	}

	private static void DrawBelowMask(Map sky, Map lower, CellRect view)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)maskMat == (Object)null)
		{
			maskMat = new Material(MatBases.FogOfWar)
			{
				mainTexture = (Texture)(object)BaseContent.WhiteTex,
				color = Color.white
			};
		}
		if ((Object)(object)skirtMat == (Object)null)
		{
			skirtMat = SolidColorMaterials.NewSolidColorMaterial(Color.white, ShaderDatabase.VertexColor);
			skirtMat.renderQueue = Mathf.Max(BelowQueueCeiling - 60, 1);
		}
		TryApplyMaskJob();
		int frameCount = Time.frameCount;
		if (!((Object)(object)maskMesh != (Object)null) || !((CellRect)(ref maskLastRect)).Contains(new IntVec3(view.minX, 0, view.minZ)) || !((CellRect)(ref maskLastRect)).Contains(new IntVec3(view.maxX, 0, view.maxZ)) || frameCount - maskLastFrame >= 15 || lower.uniqueID != maskLastLowerId)
		{
			CellRect val = ((CellRect)(ref view)).ExpandedBy(8);
			CellRect rect = ((CellRect)(ref val)).ClipInsideMap(sky);
			if (ABGuard.On(ABGuard.Async))
			{
				StartMaskJob(sky, lower, rect);
				maskLastFrame = frameCount;
			}
			else
			{
				RebuildMask(sky, lower, rect);
				maskLastFrame = frameCount;
				maskLastRect = rect;
				maskLastLowerId = lower.uniqueID;
			}
		}
		if ((Object)(object)maskMesh != (Object)null && maskMesh.vertexCount > 0)
		{
			Graphics.DrawMesh(maskMesh, Matrix4x4.identity, maskMat, 0);
		}
		if ((Object)(object)skirtMesh != (Object)null && skirtMesh.vertexCount > 0)
		{
			Graphics.DrawMesh(skirtMesh, Matrix4x4.identity, skirtMat, 0);
		}
	}

	private static void StartMaskJob(Map sky, Map lower, CellRect rect)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		if (maskJobRunning)
		{
			return;
		}
		maskJobRunning = true;
		maskJobDone = false;
		maskJobError = null;
		maskJobRect = rect;
		maskJobLowerId = lower.uniqueID;
		TerrainGrid skyTerrain = sky.terrainGrid;
		FogGrid lowerFog = lower.fogGrid;
		int sizeX = sky.Size.x;
		int sizeZ = sky.Size.z;
		float baseDim = Mathf.Clamp(ABMod.Settings?.belowDim ?? 0.12f, 0f, 0.6f);
		int step = NextMaskStep(rect);
		maskJobSkirtOn = CurrentSkirtOn();
		ThreadPool.QueueUserWorkItem(delegate
		{
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				BuildMaskBuffers(skyTerrain, lowerFog, sizeX, sizeZ, maskJobRect, step, (byte)(255f * baseDim), jobVerts, jobTris, jobColors);
				BuildSkirtBuffers(skyTerrain, sizeX, sizeZ, maskJobRect, maskJobSkirtOn, jobSkirtVerts, jobSkirtTris, jobSkirtColors);
			}
			catch (Exception ex)
			{
				maskJobError = ex;
			}
			finally
			{
				maskJobDone = true;
			}
		});
	}

	private static void TryApplyMaskJob()
	{
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		if (!maskJobRunning || !maskJobDone)
		{
			return;
		}
		maskJobRunning = false;
		if (maskJobError != null)
		{
			ABGuard.Disable(ABGuard.Async, maskJobError, "async mask build");
			return;
		}
		EnsureMaskMesh();
		maskMesh.Clear();
		if (jobVerts.Count > 0)
		{
			maskMesh.SetVertices(jobVerts);
			maskMesh.SetColors(jobColors);
			maskMesh.SetTriangles(jobTris, 0);
			maskMesh.RecalculateBounds();
		}
		UploadSkirt(jobSkirtVerts, jobSkirtTris, jobSkirtColors);
		maskLastRect = maskJobRect;
		maskLastLowerId = maskJobLowerId;
	}

	private static int NextMaskStep(CellRect rect)
	{
		int num = maskLastStep;
		if (num == 1 && ((CellRect)(ref rect)).Width > 150)
		{
			num = 2;
		}
		else if (num == 2 && ((CellRect)(ref rect)).Width < 125)
		{
			num = 1;
		}
		maskLastStep = num;
		return num;
	}

	private static void EnsureMaskMesh()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		if ((Object)(object)maskMesh == (Object)null)
		{
			maskMesh = new Mesh
			{
				name = "AB_BelowMask",
				indexFormat = (IndexFormat)1
			};
		}
	}

	private static void RebuildMask(Map sky, Map lower, CellRect rect)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		EnsureMaskMesh();
		float num = Mathf.Clamp(ABMod.Settings?.belowDim ?? 0.12f, 0f, 0.6f);
		BuildMaskBuffers(sky.terrainGrid, lower.fogGrid, sky.Size.x, sky.Size.z, rect, NextMaskStep(rect), (byte)(255f * num), maskVerts, maskTris, maskColors);
		maskMesh.Clear();
		if (maskVerts.Count > 0)
		{
			maskMesh.SetVertices(maskVerts);
			maskMesh.SetColors(maskColors);
			maskMesh.SetTriangles(maskTris, 0);
			maskMesh.RecalculateBounds();
		}
		BuildSkirtBuffers(sky.terrainGrid, sky.Size.x, sky.Size.z, rect, CurrentSkirtOn(), skirtVerts, skirtTris, skirtColors);
		UploadSkirt(skirtVerts, skirtTris, skirtColors);
	}

	private static void BuildMaskBuffers(TerrainGrid skyTerrain, FogGrid lowerFog, int sizeX, int sizeZ, CellRect rect, int step, byte dimAlpha, List<Vector3> verts, List<int> tris, List<Color32> colors)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		verts.Clear();
		tris.Clear();
		colors.Clear();
		TerrainDef aB_OpenAir = ABDefOf.AB_OpenAir;
		int num = rect.minX - (rect.minX % step + step) % step;
		int num2 = rect.minZ - (rect.minZ % step + step) % step;
		IntVec3 val = default(IntVec3);
		Color32 item = default(Color32);
		for (int i = num; i <= rect.maxX; i += step)
		{
			for (int j = num2; j <= rect.maxZ; j += step)
			{
				int num3 = Mathf.Clamp(i, 0, sizeX - 1);
				int num4 = Mathf.Clamp(j, 0, sizeZ - 1);
				((IntVec3)(ref val))._002Ector(num3, 0, num4);
				if (skyTerrain.TerrainAt(val) == aB_OpenAir)
				{
					byte b = (lowerFog.IsFogged(val) ? byte.MaxValue : dimAlpha);
					int count = verts.Count;
					float num5 = Mathf.Max(i, 0);
					float num6 = Mathf.Max(j, 0);
					float num7 = Mathf.Min(i + step, sizeX);
					float num8 = Mathf.Min(j + step, sizeZ);
					if (!(num7 <= num5) && !(num8 <= num6))
					{
						verts.Add(new Vector3(num5, -0.1f, num6));
						verts.Add(new Vector3(num5, -0.1f, num8));
						verts.Add(new Vector3(num7, -0.1f, num8));
						verts.Add(new Vector3(num7, -0.1f, num6));
						((Color32)(ref item))._002Ector((byte)0, (byte)0, (byte)0, b);
						colors.Add(item);
						colors.Add(item);
						colors.Add(item);
						colors.Add(item);
						tris.Add(count);
						tris.Add(count + 1);
						tris.Add(count + 2);
						tris.Add(count);
						tris.Add(count + 2);
						tris.Add(count + 3);
					}
				}
			}
		}
	}

	private static bool CurrentSkirtOn()
	{
		return ABMod.Settings?.drawSlabEdge ?? true;
	}

	private static void EnsureSkirtMesh()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		if ((Object)(object)skirtMesh == (Object)null)
		{
			skirtMesh = new Mesh
			{
				name = "AB_SlabSkirt",
				indexFormat = (IndexFormat)1
			};
		}
	}

	private static void UploadSkirt(List<Vector3> verts, List<int> tris, List<Color32> colors)
	{
		EnsureSkirtMesh();
		skirtMesh.Clear();
		if (verts.Count > 0)
		{
			skirtMesh.SetVertices(verts);
			skirtMesh.SetColors(colors);
			skirtMesh.SetTriangles(tris, 0);
			skirtMesh.RecalculateBounds();
		}
	}

	private static void BuildSkirtBuffers(TerrainGrid skyTerrain, int sizeX, int sizeZ, CellRect rect, bool enabled, List<Vector3> verts, List<int> tris, List<Color32> colors)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		verts.Clear();
		tris.Clear();
		colors.Clear();
		if (!enabled)
		{
			return;
		}
		TerrainDef aB_OpenAir = ABDefOf.AB_OpenAir;
		TerrainDef aB_MountainTop = ABDefOf.AB_MountainTop;
		int num = Mathf.Max(rect.minX, 0);
		int num2 = Mathf.Min(rect.maxX, sizeX - 1);
		int num3 = Mathf.Max(rect.minZ, 0);
		int num4 = Mathf.Min(rect.maxZ, sizeZ - 1);
		for (int i = num; i <= num2; i++)
		{
			for (int j = num3; j <= num4; j++)
			{
				if (skyTerrain.TerrainAt(new IntVec3(i, 0, j)) == aB_OpenAir)
				{
					if (j + 1 < sizeZ && IsSlab(skyTerrain, i, j + 1, aB_OpenAir, aB_MountainTop))
					{
						AddSkirtQuad(verts, tris, colors, i, (float)i + 1f, (float)j + 1f - 0.08f, (float)j + 1f, SkirtLedge, SkirtLedge, SkirtLedge, SkirtLedge);
					}
					if (i + 1 < sizeX && IsSlab(skyTerrain, i + 1, j, aB_OpenAir, aB_MountainTop))
					{
						AddSkirtQuad(verts, tris, colors, (float)i + 1f - 0.08f, (float)i + 1f, j, (float)j + 1f, SkirtLedge, SkirtLedge, SkirtLedge, SkirtLedge);
					}
					if (i - 1 >= 0 && IsSlab(skyTerrain, i - 1, j, aB_OpenAir, aB_MountainTop))
					{
						AddSkirtQuad(verts, tris, colors, i, (float)i + 0.08f, j, (float)j + 1f, SkirtLedge, SkirtLedge, SkirtLedge, SkirtLedge);
					}
					if (j - 1 >= 0 && IsSlab(skyTerrain, i, j - 1, aB_OpenAir, aB_MountainTop))
					{
						AddSkirtQuad(verts, tris, colors, i, (float)i + 1f, j, (float)j + 0.08f, SkirtLedge, SkirtLedge, SkirtLedge, SkirtLedge);
					}
				}
			}
		}
	}

	private static bool IsSlab(TerrainGrid grid, int x, int z, TerrainDef air, TerrainDef cap)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		TerrainDef val = grid.TerrainAt(new IntVec3(x, 0, z));
		return val != null && val != air && val != cap;
	}

	private static void AddSkirtQuad(List<Vector3> verts, List<int> tris, List<Color32> colors, float x0, float x1, float z0, float z1, Color32 c00, Color32 c01, Color32 c11, Color32 c10)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		int count = verts.Count;
		verts.Add(new Vector3(x0, -0.05f, z0));
		verts.Add(new Vector3(x0, -0.05f, z1));
		verts.Add(new Vector3(x1, -0.05f, z1));
		verts.Add(new Vector3(x1, -0.05f, z0));
		colors.Add(c00);
		colors.Add(c01);
		colors.Add(c11);
		colors.Add(c10);
		tris.Add(count);
		tris.Add(count + 1);
		tris.Add(count + 2);
		tris.Add(count);
		tris.Add(count + 2);
		tris.Add(count + 3);
	}

	public static void DrawBelowDynamic(Map map)
	{
		if (!ABGuard.On(ABGuard.Rendering) || map == null || map != Find.CurrentMap)
		{
			return;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.showLiveBelow)
		{
			return;
		}
		LevelComp levelComp = map.Levels();
		if (levelComp == null || levelComp.level <= 0)
		{
			return;
		}
		Map lowerMap = levelComp.lowerMap;
		if (lowerMap == null || lowerMap.Disposed)
		{
			return;
		}
		try
		{
			RefreshBelowTransform();
			OffsetActive = true;
			if (!TryDrawFilteredDynamic(map, lowerMap))
			{
				lowerMap.dynamicDrawManager.DrawDynamicThings();
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Rendering, e, "see-below dynamic rendering");
		}
		finally
		{
			OffsetActive = false;
		}
	}

	private static bool TryDrawFilteredDynamic(Map sky, Map lower)
	{
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		if (drawThingsRefFailed)
		{
			return false;
		}
		if (drawThingsRef == null)
		{
			try
			{
				drawThingsRef = AccessTools.FieldRefAccess<DynamicDrawManager, List<Thing>>("drawThings");
			}
			catch (Exception)
			{
				drawThingsRef = null;
			}
			if (drawThingsRef == null)
			{
				drawThingsRefFailed = true;
				Log.Warning("[As above, So below] DynamicDrawManager.drawThings not found; the below view falls back to unfiltered dynamic drawing.");
				return false;
			}
		}
		List<Thing> list = drawThingsRef.Invoke(lower.dynamicDrawManager);
		if (list == null)
		{
			drawThingsRefFailed = true;
			return false;
		}
		CellRect val = Find.CameraDriver.CurrentViewRect;
		val = ((CellRect)(ref val)).ExpandedBy(1);
		CellRect val2 = ((CellRect)(ref val)).ClipInsideMap(lower);
		RoofGrid roofGrid = lower.roofGrid;
		FogGrid fogGrid = lower.fogGrid;
		TerrainGrid terrainGrid = sky.terrainGrid;
		TerrainDef aB_OpenAir = ABDefOf.AB_OpenAir;
		for (int i = 0; i < list.Count; i++)
		{
			Thing val3 = list[i];
			if (val3 == null || !val3.Spawned)
			{
				continue;
			}
			IntVec3 position = val3.Position;
			if (GenGrid.InBounds(position, lower) && !roofGrid.Roofed(position) && GenGrid.InBounds(position, sky) && terrainGrid.TerrainAt(position) == aB_OpenAir && !fogGrid.IsFogged(position) && (((CellRect)(ref val2)).Contains(position) || val3.def.drawOffscreen))
			{
				try
				{
					val3.DynamicDrawPhase((DrawPhase)2);
				}
				catch (Exception ex2)
				{
					Log.WarningOnce("[As above, So below] Below draw failed for " + ((Entity)val3).LabelCap + ": " + ex2.Message, val3.thingIDNumber ^ 0x2D6E2F86);
				}
			}
		}
		return true;
	}
}
