
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.8.3928.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.


// warning : THIS IS A PARTIAL DISASSEMBLY, NOT SUITABLE FOR RE-ASSEMBLING



.class public auto ansi beforefieldinit DubsBadHygiene.CompPipe
       extends ['Assembly-CSharp']Verse.ThingComp
{
  .method public hidebysig virtual instance void 
          PostSpawnSetup(bool respawningAfterLoad) cil managed
  {
    // Code size       359 (0x167)
    .maxstack  3
    .locals init (valuetype ['Assembly-CSharp']Verse.CellRect/Enumerator V_0,
             valuetype ['Assembly-CSharp']Verse.CellRect V_1,
             valuetype ['Assembly-CSharp']Verse.IntVec3 V_2,
             valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class ['Assembly-CSharp']Verse.Thing> V_3,
             class ['Assembly-CSharp']Verse.Thing V_4)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void ['Assembly-CSharp']Verse.ThingComp::PostSpawnSetup(bool)
    IL_0007:  ldarg.0
    IL_0008:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_000d:  isinst     DubsBadHygiene.Building_Valve
    IL_0012:  brfalse.s  IL_0025

    IL_0014:  ldarg.0
    IL_0015:  ldarg.0
    IL_0016:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_001b:  callvirt   instance !!0 ['Assembly-CSharp']Verse.ThingWithComps::GetComp<class ['Assembly-CSharp']RimWorld.CompFlickable>()
    IL_0020:  stfld      class ['Assembly-CSharp']RimWorld.CompFlickable DubsBadHygiene.CompPipe::flicker
    IL_0025:  ldarg.0
    IL_0026:  ldarg.0
    IL_0027:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_002c:  callvirt   instance !!0 ['Assembly-CSharp']Verse.ThingWithComps::GetComp<class ['Assembly-CSharp']RimWorld.CompRefuelable>()
    IL_0031:  stfld      class ['Assembly-CSharp']RimWorld.CompRefuelable DubsBadHygiene.CompPipe::fuel
    IL_0036:  ldarg.0
    IL_0037:  ldfld      class ['Assembly-CSharp']RimWorld.CompRefuelable DubsBadHygiene.CompPipe::fuel
    IL_003c:  brfalse.s  IL_005d

    IL_003e:  ldarg.0
    IL_003f:  ldfld      class ['Assembly-CSharp']RimWorld.CompRefuelable DubsBadHygiene.CompPipe::fuel
    IL_0044:  callvirt   instance class ['Assembly-CSharp']RimWorld.CompProperties_Refuelable ['Assembly-CSharp']RimWorld.CompRefuelable::get_Props()
    IL_0049:  ldfld      class ['Assembly-CSharp']Verse.ThingFilter ['Assembly-CSharp']RimWorld.CompProperties_Refuelable::fuelFilter
    IL_004e:  callvirt   instance int32 ['Assembly-CSharp']Verse.ThingFilter::get_AllowedDefCount()
    IL_0053:  ldc.i4.0
    IL_0054:  ble.s      IL_005d

    IL_0056:  ldarg.0
    IL_0057:  ldnull
    IL_0058:  stfld      class ['Assembly-CSharp']RimWorld.CompRefuelable DubsBadHygiene.CompPipe::fuel
    IL_005d:  ldarg.0
    IL_005e:  ldarg.0
    IL_005f:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0064:  callvirt   instance !!0 ['Assembly-CSharp']Verse.ThingWithComps::GetComp<class ['Assembly-CSharp']RimWorld.CompPowerTrader>()
    IL_0069:  stfld      class ['Assembly-CSharp']RimWorld.CompPowerTrader DubsBadHygiene.CompPipe::power
    IL_006e:  ldarg.0
    IL_006f:  ldarg.0
    IL_0070:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0075:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_007a:  callvirt   instance !!0 ['Assembly-CSharp']Verse.Map::GetComponent<class DubsBadHygiene.MapComponent_Hygiene>()
    IL_007f:  stfld      class DubsBadHygiene.MapComponent_Hygiene DubsBadHygiene.CompPipe::MapComp
    IL_0084:  ldarg.0
    IL_0085:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_008a:  call       valuetype ['Assembly-CSharp']Verse.CellRect ['Assembly-CSharp']Verse.GenAdj::OccupiedRect(class ['Assembly-CSharp']Verse.Thing)
    IL_008f:  stloc.1
    IL_0090:  ldloca.s   V_1
    IL_0092:  call       instance valuetype ['Assembly-CSharp']Verse.CellRect/Enumerator ['Assembly-CSharp']Verse.CellRect::GetEnumerator()
    IL_0097:  stloc.0
    .try
    {
      IL_0098:  br.s       IL_0111

      IL_009a:  ldloca.s   V_0
      IL_009c:  call       instance valuetype ['Assembly-CSharp']Verse.IntVec3 ['Assembly-CSharp']Verse.CellRect/Enumerator::get_Current()
      IL_00a1:  stloc.2
      IL_00a2:  ldloc.2
      IL_00a3:  ldarg.0
      IL_00a4:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
      IL_00a9:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
      IL_00ae:  call       class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.Thing> ['Assembly-CSharp']Verse.GridsUtility::GetThingList(valuetype ['Assembly-CSharp']Verse.IntVec3,
                                                                                                                                                                       class ['Assembly-CSharp']Verse.Map)
      IL_00b3:  call       class [mscorlib]System.Collections.Generic.List`1<!!0> [System.Core]System.Linq.Enumerable::ToList<class ['Assembly-CSharp']Verse.Thing>(class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>)
      IL_00b8:  callvirt   instance valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<!0> class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.Thing>::GetEnumerator()
      IL_00bd:  stloc.3
      .try
      {
        IL_00be:  br.s       IL_00f8

        IL_00c0:  ldloca.s   V_3
        IL_00c2:  call       instance !0 valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class ['Assembly-CSharp']Verse.Thing>::get_Current()
        IL_00c7:  stloc.s    V_4
        IL_00c9:  ldloc.s    V_4
        IL_00cb:  ldarg.0
        IL_00cc:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
        IL_00d1:  beq.s      IL_00f8

        IL_00d3:  ldloc.s    V_4
        IL_00d5:  call       !!0 ['Assembly-CSharp']Verse.ThingCompUtility::TryGetComp<class DubsBadHygiene.CompPipe>(class ['Assembly-CSharp']Verse.Thing)
        IL_00da:  brfalse.s  IL_00f8

        IL_00dc:  ldloc.s    V_4
        IL_00de:  call       !!0 ['Assembly-CSharp']Verse.ThingCompUtility::TryGetComp<class DubsBadHygiene.CompPipe>(class ['Assembly-CSharp']Verse.Thing)
        IL_00e3:  callvirt   instance valuetype DubsBadHygiene.PipeType DubsBadHygiene.CompPipe::get_mode()
        IL_00e8:  ldarg.0
        IL_00e9:  call       instance valuetype DubsBadHygiene.PipeType DubsBadHygiene.CompPipe::get_mode()
        IL_00ee:  bne.un.s   IL_00f8

        IL_00f0:  ldloc.s    V_4
        IL_00f2:  ldc.i4.0
        IL_00f3:  callvirt   instance void ['Assembly-CSharp']Verse.Thing::Destroy(valuetype ['Assembly-CSharp']Verse.DestroyMode)
        IL_00f8:  ldloca.s   V_3
        IL_00fa:  call       instance bool valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class ['Assembly-CSharp']Verse.Thing>::MoveNext()
        IL_00ff:  brtrue.s   IL_00c0

        IL_0101:  leave.s    IL_0111

      }  // end .try
      finally
      {
        IL_0103:  ldloca.s   V_3
        IL_0105:  constrained. valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class ['Assembly-CSharp']Verse.Thing>
        IL_010b:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
        IL_0110:  endfinally
      }  // end handler
      IL_0111:  ldloca.s   V_0
      IL_0113:  call       instance bool ['Assembly-CSharp']Verse.CellRect/Enumerator::MoveNext()
      IL_0118:  brtrue.s   IL_009a

      IL_011a:  leave.s    IL_012a

    }  // end .try
    finally
    {
      IL_011c:  ldloca.s   V_0
      IL_011e:  constrained. ['Assembly-CSharp']Verse.CellRect/Enumerator
      IL_0124:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
      IL_0129:  endfinally
    }  // end handler
    IL_012a:  ldarg.0
    IL_012b:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0130:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_0135:  ldfld      class ['Assembly-CSharp']Verse.MapDrawer ['Assembly-CSharp']Verse.Map::mapDrawer
    IL_013a:  ldarg.0
    IL_013b:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0140:  callvirt   instance valuetype ['Assembly-CSharp']Verse.IntVec3 ['Assembly-CSharp']Verse.Thing::get_Position()
    IL_0145:  ldsfld     class ['Assembly-CSharp']RimWorld.MapMeshFlagDef ['Assembly-CSharp']RimWorld.MapMeshFlagDefOf::PowerGrid
    IL_014a:  call       uint64 ['Assembly-CSharp']RimWorld.MapMeshFlagDef::op_Implicit(class ['Assembly-CSharp']RimWorld.MapMeshFlagDef)
    IL_014f:  callvirt   instance void ['Assembly-CSharp']Verse.MapDrawer::MapMeshDirty(valuetype ['Assembly-CSharp']Verse.IntVec3,
                                                                                        uint64)
    IL_0154:  ldarg.0
    IL_0155:  ldfld      class DubsBadHygiene.MapComponent_Hygiene DubsBadHygiene.CompPipe::MapComp
    IL_015a:  callvirt   instance class DubsBadHygiene.HygienePipeMapComp DubsBadHygiene.MapComponent_Hygiene::get_PipeComp()
    IL_015f:  ldarg.0
    IL_0160:  ldarg.1
    IL_0161:  callvirt   instance void DubsBadHygiene.HygienePipeMapComp::RegisterPipe(class DubsBadHygiene.CompPipe,
                                                                                       bool)
    IL_0166:  ret
  } // end of method CompPipe::PostSpawnSetup

} // end of class DubsBadHygiene.CompPipe

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file F:\Repositories\rimworld_mods\.tmp\dbh_compipe_spawn.res
