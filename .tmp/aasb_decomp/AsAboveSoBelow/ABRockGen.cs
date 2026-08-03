using System.Collections.Generic;
using Verse;
using Verse.Noise;

namespace AsAboveSoBelow;

internal static class ABRockGen
{
	internal static List<Perlin> MakeNoises(int count)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		List<Perlin> list = new List<Perlin>(count);
		for (int i = 0; i < count; i++)
		{
			list.Add(new Perlin(0.005, 2.0, 0.5, 6, Rand.Range(0, int.MaxValue), (QualityMode)1));
		}
		return list;
	}

	internal static int PickIndex(List<Perlin> noises, IntVec3 c)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		int result = 0;
		double num = double.MinValue;
		for (int i = 0; i < noises.Count; i++)
		{
			double value = ((ModuleBase)noises[i]).GetValue((double)c.x, 0.0, (double)c.z);
			if (value > num)
			{
				num = value;
				result = i;
			}
		}
		return result;
	}
}
