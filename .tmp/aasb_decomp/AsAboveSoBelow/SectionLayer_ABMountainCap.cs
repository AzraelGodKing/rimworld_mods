using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

public class SectionLayer_ABMountainCap : SectionLayer
{
	private static int lowQueue;

	private static bool queueReady;

	private static readonly FieldRef<Graphic_Linked, Graphic> SubGraphicRef = AccessTools.FieldRefAccess<Graphic_Linked, Graphic>("subGraphic");

	private static readonly FieldRef<Graphic_Random, Graphic[]> SubGraphicsRef = AccessTools.FieldRefAccess<Graphic_Random, Graphic[]>("subGraphics");

	private static readonly Dictionary<ThingDef, (Graphic graphic, Material mat)> atlasBase = new Dictionary<ThingDef, (Graphic, Material)>();

	private static readonly Dictionary<ThingDef, (Graphic graphic, Material[] mats)> variantMats = new Dictionary<ThingDef, (Graphic, Material[])>();

	private static readonly Dictionary<Material, Material> queueClones = new Dictionary<Material, Material>();

	private static Material skirtMatCached;

	private const float SkirtDepth = 0.8f;

	private const float SkirtAltBias = 0.035f;

	private const byte SkirtNearAlpha = 150;

	internal static bool CornerFillersEnabled = true;

	private const float FillerCornerOffset = 0.25f;

	private const float FillerNorthShift = 0.09f;

	private const float FillerAltBias = 0.03f;

	private static readonly Vector2 CornerFillUV = new Vector2(0.5f, 0.6f);

	private static readonly Color32 White = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

	private const float NorthAltBias = 0.01f;

	public override bool Visible => ABGuard.On(ABGuard.Rendering);

	private static void EnsureQueue()
	{
		if (!queueReady)
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
			num = MaxQ(current, (Material)m);
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
			num = MaxQ(current2, (Material)m2);
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
			num = MaxQ(current3, (Material)m3);
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
			num = MaxQ(current4, (Material)m4);
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
			num = MaxQ(current5, (Material)m5);
			if ((Object)(object)ShaderDatabase.TerrainHard != (Object)null && ShaderDatabase.TerrainHard.renderQueue >= 500)
			{
				num = Mathf.Max(num, ShaderDatabase.TerrainHard.renderQueue);
			}
			if (num < 500)
			{
				num = 2000;
			}
			int num2 = (((Object)(object)MatBases.EdgeShadow != (Object)null) ? MatBases.EdgeShadow.renderQueue : num);
			int num3 = (((Object)(object)ShaderDatabase.Cutout != (Object)null) ? ShaderDatabase.Cutout.renderQueue : (num + 450));
			lowQueue = Mathf.Clamp(Mathf.Max(num, num2) + 1, Mathf.Min(num + 1, num3 - 1), num3 - 1);
			queueReady = true;
		}
	}

	private static int MaxQ(int current, Material m)
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

	internal static string DebugCapFillInfo(Map sky, Map ground, IntVec3 c)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			EnsureQueue();
			ThingDef val = GroundRockAt(ground, c) ?? FallbackRock(sky);
			Graphic val2 = LiveGraphicFor(val);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("fill: rock=").Append(((Def)(val?)).defName ?? "null").Append(" graphic=")
				.Append(((object)val2)?.GetType().Name ?? "null")
				.Append(" drawSize=")
				.Append((val2 != null) ? ((object)System.Runtime.CompilerServices.Unsafe.As<Vector2, Vector2>(ref val2.drawSize)/*cast due to .constrained prefix*/).ToString() : "-");
			if (val2 is Graphic_Linked)
			{
				Material val3 = AtlasBaseFor(val);
				stringBuilder.Append(" branch=atlas baseMat=").Append(((Object)(object)val3 != (Object)null) ? ((Object)val3).name : "NULL");
			}
			else
			{
				Material[] array = VariantsFor(val);
				if (array != null)
				{
					Material val4 = array[StableCellIndex(c, array.Length)];
					Material val5 = QueueClone(val4);
					stringBuilder.Append(" branch=variant count=").Append(array.Length).Append(" mat=")
						.Append(((Object)(object)val4 != (Object)null) ? ((Object)val4).name : "NULL")
						.Append(" shader=")
						.Append(((Object)(object)val4 != (Object)null && (Object)(object)val4.shader != (Object)null) ? ((Object)val4.shader).name : "-")
						.Append(" color=")
						.Append(((Object)(object)val4 != (Object)null) ? ((object)val4.color/*cast due to .constrained prefix*/).ToString() : "-")
						.Append(" cloneQueue=")
						.Append(((Object)(object)val5 != (Object)null) ? val5.renderQueue : (-1));
				}
				else
				{
					Material val6 = AtlasBaseFor(val);
					stringBuilder.Append(" branch=FALLBACK-FLAT mat=").Append(((Object)(object)val6 != (Object)null) ? ((Object)val6).name : "NULL");
				}
			}
			TerrainDef aB_MountainTop = ABDefOf.AB_MountainTop;
			object obj;
			if (aB_MountainTop == null)
			{
				obj = null;
			}
			else
			{
				Graphic graphic = ((BuildableDef)aB_MountainTop).graphic;
				obj = ((graphic != null) ? graphic.MatSingle : null);
			}
			Material val7 = (Material)obj;
			stringBuilder.Append(" lowQueue=").Append(lowQueue).Append(" capTerrainQueue=")
				.Append(((Object)(object)val7 != (Object)null) ? val7.renderQueue : (-1))
				.Append(" guard=")
				.Append(ABGuard.On(ABGuard.Rendering) ? "on" : "OFF");
			return stringBuilder.ToString();
		}
		catch (Exception ex)
		{
			return "fill probe failed: " + ex.Message;
		}
	}

	private static Graphic LiveGraphicFor(ThingDef rockDef)
	{
		GraphicData val = rockDef?.graphicData;
		if (val != null)
		{
			try
			{
				Graphic graphic = val.Graphic;
				if (graphic != null && graphic != BaseContent.BadGraphic)
				{
					return graphic;
				}
			}
			catch
			{
			}
		}
		return ((BuildableDef)(rockDef?)).graphic;
	}

	private static Material AtlasBaseFor(ThingDef rockDef)
	{
		if (rockDef == null)
		{
			return null;
		}
		Graphic val = LiveGraphicFor(rockDef);
		if (atlasBase.TryGetValue(rockDef, out (Graphic, Material) value) && value.Item1 == val)
		{
			return value.Item2;
		}
		Material val2 = null;
		try
		{
			Graphic_Linked val3 = (Graphic_Linked)(object)((val is Graphic_Linked) ? val : null);
			if (val3 != null)
			{
				Graphic val4 = SubGraphicRef.Invoke(val3);
				val2 = ((val4 != null) ? val4.MatSingle : null);
			}
			val2 = val2 ?? ((val != null) ? val.MatSingle : null);
		}
		catch
		{
			val2 = ((val != null) ? val.MatSingle : null);
		}
		atlasBase[rockDef] = (val, val2);
		return val2;
	}

	private static Material[] VariantsFor(ThingDef rockDef)
	{
		if (rockDef == null)
		{
			return null;
		}
		Graphic val = LiveGraphicFor(rockDef);
		if (variantMats.TryGetValue(rockDef, out (Graphic, Material[]) value) && value.Item1 == val)
		{
			return value.Item2;
		}
		Material[] array = null;
		try
		{
			Graphic_Random val2 = (Graphic_Random)(object)((val is Graphic_Random) ? val : null);
			if (val2 != null)
			{
				Graphic[] array2 = SubGraphicsRef.Invoke(val2);
				if (array2 != null && array2.Length != 0)
				{
					List<Material> list = new List<Material>(array2.Length);
					foreach (Graphic obj in array2)
					{
						Material val3 = ((obj != null) ? obj.MatSingle : null);
						if ((Object)(object)val3 != (Object)null)
						{
							list.Add(val3);
						}
					}
					if (list.Count > 0)
					{
						array = list.ToArray();
					}
				}
			}
		}
		catch
		{
			array = null;
		}
		variantMats[rockDef] = (val, array);
		return array;
	}

	private static int StableCellIndex(IntVec3 c, int count)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		int num = (c.x * 73856093) ^ (c.z * 19349663);
		num &= 0x7FFFFFFF;
		return num % count;
	}

	private static Material QueueClone(Material source)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		if ((Object)(object)source == (Object)null)
		{
			return null;
		}
		if (queueClones.TryGetValue(source, out var value))
		{
			return value;
		}
		if (queueClones.Count > 512)
		{
			queueClones.Clear();
		}
		value = new Material(source)
		{
			renderQueue = lowQueue
		};
		queueClones[source] = value;
		return value;
	}

	public SectionLayer_ABMountainCap(Section section)
		: base(section)
	{
		((MapDrawLayer)this).relevantChangeTypes = MapMeshFlagDef.op_Implicit(MapMeshFlagDefOf.Terrain) | MapMeshFlagDef.op_Implicit(MapMeshFlagDefOf.Buildings) | MapMeshFlagDef.op_Implicit(ABDefOf.AB_BelowThings);
	}

	public unsafe override void Regenerate()
	{
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_035e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0377: Unknown result type (might be due to invalid IL or missing references)
		//IL_037f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0387: Unknown result type (might be due to invalid IL or missing references)
		//IL_0391: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03da: Unknown result type (might be due to invalid IL or missing references)
		//IL_03df: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0404: Unknown result type (might be due to invalid IL or missing references)
		//IL_0409: Unknown result type (might be due to invalid IL or missing references)
		//IL_0418: Unknown result type (might be due to invalid IL or missing references)
		//IL_041a: Unknown result type (might be due to invalid IL or missing references)
		//IL_041f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0424: Unknown result type (might be due to invalid IL or missing references)
		//IL_0429: Unknown result type (might be due to invalid IL or missing references)
		//IL_0447: Unknown result type (might be due to invalid IL or missing references)
		//IL_0466: Unknown result type (might be due to invalid IL or missing references)
		//IL_0485: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a4: Unknown result type (might be due to invalid IL or missing references)
		((MapDrawLayer)this).ClearSubMeshes((MeshParts)63);
		Map map = base.section.map;
		if (!ABGuard.On(ABGuard.Rendering) || map.Level() != 1)
		{
			return;
		}
		try
		{
			EnsureQueue();
			TerrainGrid terrainGrid = map.terrainGrid;
			TerrainDef aB_MountainTop = ABDefOf.AB_MountainTop;
			Map ground = map.LowerMap();
			ThingDef val = FallbackRock(map);
			float y = Altitudes.AltitudeFor((AltitudeLayer)7);
			bool flag = false;
			CellRect cellRect = base.section.CellRect;
			Enumerator enumerator = ((CellRect)(ref cellRect)).GetEnumerator();
			try
			{
				while (((Enumerator)(ref enumerator)).MoveNext())
				{
					IntVec3 current = ((Enumerator)(ref enumerator)).Current;
					TerrainDef val2 = terrainGrid.TerrainAt(current);
					ThingDef rockDef;
					bool flag2 = LevelSync.TryGetMinedRockDef(val2, out rockDef);
					if (val2 != aB_MountainTop && !flag2)
					{
						continue;
					}
					Building edifice = GridsUtility.GetEdifice(current, map);
					if (edifice != null && (((Thing)edifice).def.mineable || (((Thing)edifice).def.building != null && ((Thing)edifice).def.building.isNaturalRock)))
					{
						continue;
					}
					ThingDef val3 = GroundRockAt(ground, current) ?? val;
					Graphic val4 = LiveGraphicFor(val3);
					if (!(val4 is Graphic_Linked))
					{
						EmitSkirts(map, terrainGrid, current, SkirtTone(val3), y);
						Material[] array = VariantsFor(val3);
						if (array != null)
						{
							Material val5 = QueueClone(array[StableCellIndex(current, array.Length)]);
							if ((Object)(object)val5 != (Object)null)
							{
								Vector2 val6 = val4?.drawSize ?? Vector2.one;
								float num = Mathf.Max(val6.x, 1f) * 0.5f;
								float num2 = Mathf.Max(val6.y, 1f) * 0.5f;
								LayerSubMesh subMesh = ((MapDrawLayer)this).GetSubMesh(val5);
								AddQuad(subMesh, (float)current.x + 0.5f - num, (float)current.z + 0.5f - num2, (float)current.x + 0.5f + num, (float)current.z + 0.5f + num2, y);
								flag = true;
							}
						}
						else
						{
							Material val7 = QueueClone(AtlasBaseFor(val3));
							if ((Object)(object)val7 != (Object)null)
							{
								LayerSubMesh subMesh2 = ((MapDrawLayer)this).GetSubMesh(val7);
								AddQuad(subMesh2, current.x, current.z, current.x + 1, current.z + 1, y);
								flag = true;
							}
						}
						continue;
					}
					Material val8 = AtlasBaseFor(val3);
					if ((Object)(object)val8 == (Object)null)
					{
						continue;
					}
					bool flag3 = Linked(map, terrainGrid, aB_MountainTop, current + IntVec3.North);
					bool flag4 = Linked(map, terrainGrid, aB_MountainTop, current + IntVec3.East);
					bool flag5 = Linked(map, terrainGrid, aB_MountainTop, current + IntVec3.South);
					bool flag6 = Linked(map, terrainGrid, aB_MountainTop, current + IntVec3.West);
					int num3 = (flag3 ? 1 : 0) | (flag4 ? 2 : 0) | (flag5 ? 4 : 0) | (flag6 ? 8 : 0);
					Material val9 = QueueClone(MaterialAtlasPool.SubMaterialFromAtlas(val8, (LinkDirections)(byte)num3));
					if ((Object)(object)val9 == (Object)null)
					{
						continue;
					}
					EmitSkirts(map, terrainGrid, current, SkirtTone(val3), y);
					LayerSubMesh subMesh3 = ((MapDrawLayer)this).GetSubMesh(val9);
					AddQuad(subMesh3, current.x, current.z, current.x + 1, current.z + 1, y);
					flag = true;
					if (CornerFillersEnabled)
					{
						bool flag7 = Linked(map, terrainGrid, aB_MountainTop, current + IntVec3.North + IntVec3.West);
						bool flag8 = Linked(map, terrainGrid, aB_MountainTop, current + IntVec3.North + IntVec3.East);
						bool flag9 = Linked(map, terrainGrid, aB_MountainTop, current + IntVec3.South + IntVec3.West);
						bool flag10 = Linked(map, terrainGrid, aB_MountainTop, current + IntVec3.South + IntVec3.East);
						if (flag9 && flag5 && flag6)
						{
							AddCornerFiller(subMesh3, map, current, -1, -1, y);
						}
						if (flag7 && flag3 && flag6)
						{
							AddCornerFiller(subMesh3, map, current, -1, 1, y);
						}
						if (flag8 && flag3 && flag4)
						{
							AddCornerFiller(subMesh3, map, current, 1, 1, y);
						}
						if (flag10 && flag5 && flag4)
						{
							AddCornerFiller(subMesh3, map, current, 1, -1, y);
						}
					}
				}
			}
			finally
			{
				((IDisposable)(*(Enumerator*)(&enumerator))/*cast due to .constrained prefix*/).Dispose();
			}
			if (flag)
			{
				((MapDrawLayer)this).FinalizeMesh((MeshParts)63);
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Rendering, e, "mountain cap layer");
		}
	}

	internal static bool Linked(Map map, TerrainGrid grid, TerrainDef cap, IntVec3 c)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		if (!GenGrid.InBounds(c, map))
		{
			return true;
		}
		if (IsMassCell(map, grid, cap, c))
		{
			return true;
		}
		return IsMeadowGround(grid.TerrainAt(c));
	}

	internal static bool IsMeadowGround(TerrainDef t)
	{
		return t == TerrainDefOf.Soil || t == TerrainDefOf.Gravel;
	}

	private static Material SkirtMat()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)skirtMatCached == (Object)null)
		{
			skirtMatCached = SolidColorMaterials.NewSolidColorMaterial(Color.white, ShaderDatabase.VertexColor);
			skirtMatCached.renderQueue = lowQueue + 1;
		}
		return skirtMatCached;
	}

	private static Color SkirtTone(ThingDef rock)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		TerrainDef val = rock?.building?.leaveTerrain;
		if (val != null && LevelSync.TryGetMinedRockColor(val, out var color))
		{
			return color;
		}
		return new Color(0.44f, 0.41f, 0.38f);
	}

	private void EmitSkirts(Map map, TerrainGrid grid, IntVec3 c, Color tone, float y)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		Color32 val = default(Color32);
		((Color32)(ref val))._002Ector((byte)(tone.r * 255f), (byte)(tone.g * 255f), (byte)(tone.b * 255f), (byte)150);
		Color32 val2 = default(Color32);
		((Color32)(ref val2))._002Ector(val.r, val.g, val.b, (byte)0);
		for (int i = 0; i < 4; i++)
		{
			IntVec3 val3 = c + GenAdj.CardinalDirections[i];
			if (!GenGrid.InBounds(val3, map) || !IsMeadowGround(grid.TerrainAt(val3)) || map.edificeGrid[val3] != null)
			{
				continue;
			}
			LayerSubMesh subMesh = ((MapDrawLayer)this).GetSubMesh(SkirtMat());
			float y2 = y + 0.035f;
			int num = val3.x - c.x;
			switch (val3.z - c.z)
			{
			case 1:
				AddFadeQuad(subMesh, val3.x, val3.z, val3.x + 1, (float)val3.z + 0.8f, y2, val, val2, val2, val);
				continue;
			case -1:
				AddFadeQuad(subMesh, val3.x, (float)val3.z + 1f - 0.8f, val3.x + 1, val3.z + 1, y2, val2, val, val, val2);
				continue;
			}
			if (num == 1)
			{
				AddFadeQuad(subMesh, val3.x, val3.z, (float)val3.x + 0.8f, val3.z + 1, y2, val, val, val2, val2);
			}
			else
			{
				AddFadeQuad(subMesh, (float)val3.x + 1f - 0.8f, val3.z, val3.x + 1, val3.z + 1, y2, val2, val2, val, val);
			}
		}
	}

	private static void AddFadeQuad(LayerSubMesh sub, float x0, float z0, float x1, float z1, float y, Color32 c00, Color32 c01, Color32 c11, Color32 c10)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		int count = sub.verts.Count;
		sub.verts.Add(new Vector3(x0, y, z0));
		sub.verts.Add(new Vector3(x0, y + 0.01f, z1));
		sub.verts.Add(new Vector3(x1, y + 0.01f, z1));
		sub.verts.Add(new Vector3(x1, y, z0));
		for (int i = 0; i < 4; i++)
		{
			sub.uvs.Add(Vector2.op_Implicit(new Vector2(0.5f, 0.5f)));
		}
		sub.colors.Add(c00);
		sub.colors.Add(c01);
		sub.colors.Add(c11);
		sub.colors.Add(c10);
		sub.tris.Add(count);
		sub.tris.Add(count + 1);
		sub.tris.Add(count + 2);
		sub.tris.Add(count);
		sub.tris.Add(count + 2);
		sub.tris.Add(count + 3);
	}

	internal static bool IsMassCell(Map map, TerrainGrid grid, TerrainDef cap, IntVec3 c)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		Building val = map.edificeGrid[c];
		if (val != null && (((Thing)val).def.mineable || (((Thing)val).def.building != null && ((Thing)val).def.building.isNaturalRock)))
		{
			return true;
		}
		TerrainDef val2 = grid.TerrainAt(c);
		ThingDef rockDef;
		return val2 == cap || LevelSync.TryGetMinedRockDef(val2, out rockDef);
	}

	private static ThingDef GroundRockAt(Map ground, IntVec3 c)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		if (ground == null || ground.Disposed || !GenGrid.InBounds(c, ground))
		{
			return null;
		}
		Building val = ground.edificeGrid[c];
		if (val != null && ((Thing)val).def.mineable)
		{
			return ((Thing)val).def;
		}
		if (LevelSync.TryGetMinedRockDef(ground.terrainGrid.TerrainAt(c), out var rockDef))
		{
			return rockDef;
		}
		IntVec3[] adjacentCells = GenAdj.AdjacentCells;
		for (int i = 0; i < adjacentCells.Length; i++)
		{
			IntVec3 val2 = c + adjacentCells[i];
			if (GenGrid.InBounds(val2, ground))
			{
				Building val3 = ground.edificeGrid[val2];
				if (val3 != null && ((Thing)val3).def.mineable)
				{
					return ((Thing)val3).def;
				}
			}
		}
		return null;
	}

	private static ThingDef FallbackRock(Map map)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			using IEnumerator<ThingDef> enumerator = Find.World.NaturalRockTypesIn(map.Tile).GetEnumerator();
			if (enumerator.MoveNext())
			{
				return enumerator.Current;
			}
		}
		catch
		{
		}
		return ThingDefOf.Granite;
	}

	private static void AddCornerFiller(LayerSubMesh sub, Map map, IntVec3 c, int dx, int dz, float y)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		float num = (float)c.x + 0.5f + (float)dx * 0.25f;
		float num2 = (float)c.z + 0.5f + (float)dz * 0.25f + 0.09f;
		float num3 = 0.5f;
		float num4 = 0.5f;
		IntVec3 val = default(IntVec3);
		((IntVec3)(ref val))._002Ector(c.x + dx, 0, c.z + dz);
		if (!GenGrid.InBounds(val, map))
		{
			if (val.x < 0)
			{
				num -= 1f;
				num3 *= 5f;
			}
			if (val.z < 0)
			{
				num2 -= 1f;
				num4 *= 5f;
			}
			if (val.x >= map.Size.x)
			{
				num += 1f;
				num3 *= 5f;
			}
			if (val.z >= map.Size.z)
			{
				num2 += 1f;
				num4 *= 5f;
			}
		}
		AddCornerQuad(sub, num - num3 * 0.5f, num2 - num4 * 0.5f, num + num3 * 0.5f, num2 + num4 * 0.5f, y + 0.03f);
	}

	private static void AddCornerQuad(LayerSubMesh sub, float x0, float z0, float x1, float z1, float y)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		int count = sub.verts.Count;
		sub.verts.Add(new Vector3(x0, y, z0));
		sub.verts.Add(new Vector3(x0, y + 0.01f, z1));
		sub.verts.Add(new Vector3(x1, y + 0.01f, z1));
		sub.verts.Add(new Vector3(x1, y, z0));
		for (int i = 0; i < 4; i++)
		{
			sub.uvs.Add(Vector2.op_Implicit(CornerFillUV));
			sub.colors.Add(White);
		}
		sub.tris.Add(count);
		sub.tris.Add(count + 1);
		sub.tris.Add(count + 2);
		sub.tris.Add(count);
		sub.tris.Add(count + 2);
		sub.tris.Add(count + 3);
	}

	private static void AddQuad(LayerSubMesh sub, float x0, float z0, float x1, float z1, float y)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		int count = sub.verts.Count;
		sub.verts.Add(new Vector3(x0, y, z0));
		sub.verts.Add(new Vector3(x0, y + 0.01f, z1));
		sub.verts.Add(new Vector3(x1, y + 0.01f, z1));
		sub.verts.Add(new Vector3(x1, y, z0));
		sub.uvs.Add(Vector2.op_Implicit(new Vector2(0f, 0f)));
		sub.uvs.Add(Vector2.op_Implicit(new Vector2(0f, 1f)));
		sub.uvs.Add(Vector2.op_Implicit(new Vector2(1f, 1f)));
		sub.uvs.Add(Vector2.op_Implicit(new Vector2(1f, 0f)));
		sub.colors.Add(White);
		sub.colors.Add(White);
		sub.colors.Add(White);
		sub.colors.Add(White);
		sub.tris.Add(count);
		sub.tris.Add(count + 1);
		sub.tris.Add(count + 2);
		sub.tris.Add(count);
		sub.tris.Add(count + 2);
		sub.tris.Add(count + 3);
	}
}
