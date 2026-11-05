using System;

namespace UnityEngine
{
    // The parts of UnityEngine.Mathf the cave geometry uses, matching Unity's
    // implementation (float casts of System.Math, banker's rounding).
    public static class Mathf
    {
        public const float Deg2Rad = (float)(Math.PI * 2.0 / 360.0);

        public static float Sin(float f) => (float)Math.Sin(f);

        public static float Cos(float f) => (float)Math.Cos(f);

        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);

        public static float Sqrt(float f) => (float)Math.Sqrt(f);

        public static float Abs(float f) => Math.Abs(f);

        public static int Abs(int value) => Math.Abs(value);

        public static float Min(float a, float b) => a < b ? a : b;

        public static int Min(int a, int b) => a < b ? a : b;

        public static float Max(float a, float b) => a > b ? a : b;

        public static int Max(int a, int b) => a > b ? a : b;

        public static int RoundToInt(float f) => (int)Math.Round(f);

        public static int CeilToInt(float f) => (int)Math.Ceiling(f);

        public static float Clamp(float value, float min, float max) =>
            value < min ? min : value > max ? max : value;

        public static float Clamp01(float value) => Clamp(value, 0f, 1f);
    }
}
