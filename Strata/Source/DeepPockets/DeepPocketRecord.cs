using System.Collections.Generic;
using Verse;

namespace Strata
{
    public class DeepPocketRecord : IExposable
    {
        public int id;

        public DeepPocketKind kind;

        public IntVec3 center;

        public float radius;

        public bool breached;

        public List<IntVec3> cells = new List<IntVec3>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id");
            Scribe_Values.Look(ref kind, "kind");
            Scribe_Values.Look(ref center, "center");
            Scribe_Values.Look(ref radius, "radius", 2.5f);
            Scribe_Values.Look(ref breached, "breached");
            Scribe_Collections.Look(ref cells, "cells", LookMode.Value);
            cells ??= new List<IntVec3>();
        }
    }
}
