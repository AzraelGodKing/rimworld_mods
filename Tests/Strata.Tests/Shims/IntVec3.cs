using System;

namespace Verse
{
    // The parts of Verse.IntVec3 the cave geometry uses, with the game's maths.
    public struct IntVec3 : IEquatable<IntVec3>
    {
        public int x;
        public int y;
        public int z;

        public IntVec3(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static IntVec3 Invalid => new IntVec3(-1000, -1000, -1000);

        public bool IsValid => y >= 0;

        public float LengthHorizontal => (float)Math.Sqrt(x * x + z * z);

        public float DistanceTo(IntVec3 b) => (this - b).LengthHorizontal;

        public static IntVec3 operator +(IntVec3 a, IntVec3 b) => new IntVec3(a.x + b.x, a.y + b.y, a.z + b.z);

        public static IntVec3 operator -(IntVec3 a, IntVec3 b) => new IntVec3(a.x - b.x, a.y - b.y, a.z - b.z);

        public static bool operator ==(IntVec3 a, IntVec3 b) => a.x == b.x && a.y == b.y && a.z == b.z;

        public static bool operator !=(IntVec3 a, IntVec3 b) => !(a == b);

        public bool Equals(IntVec3 other) => this == other;

        public override bool Equals(object obj) => obj is IntVec3 other && this == other;

        public override int GetHashCode() => HashCode.Combine(x, y, z);
    }
}
