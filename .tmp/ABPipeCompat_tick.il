
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.8.3928.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.


// warning : THIS IS A PARTIAL DISASSEMBLY, NOT SUITABLE FOR RE-ASSEMBLING



.class public abstract auto ansi sealed AsAboveSoBelow.ABPipeCompat
       extends [mscorlib]System.Object
{
  .custom instance void ['Assembly-CSharp']Verse.StaticConstructorOnStartup::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  TickGroundPairs(class AsAboveSoBelow.LevelComp groundComp) cil managed
  {
    // Code size       167 (0xa7)
    .maxstack  2
    .locals init (class AsAboveSoBelow.ABSettings V_0,
             class [mscorlib]System.Collections.Generic.List`1<class AsAboveSoBelow.Building_ABStairs> V_1,
             bool V_2,
             bool V_3,
             int32 V_4,
             class AsAboveSoBelow.Building_ABStairs V_5,
             bool V_6,
             bool V_7)
    IL_0000:  nop
    IL_0001:  ldsfld     bool AsAboveSoBelow.ABPipeCompat::DbhActive
    IL_0006:  brtrue.s   IL_0019

    IL_0008:  ldsfld     bool AsAboveSoBelow.ABPipeCompat::VefActive
    IL_000d:  brtrue.s   IL_0019

    IL_000f:  ldsfld     bool AsAboveSoBelow.ABPipeCompat::RimefellerActive
    IL_0014:  ldc.i4.0
    IL_0015:  ceq
    IL_0017:  br.s       IL_001a

    IL_0019:  ldc.i4.0
    IL_001a:  stloc.2
    IL_001b:  ldloc.2
    IL_001c:  brfalse.s  IL_0024

    IL_001e:  nop
    IL_001f:  br         IL_00a6

    IL_0024:  call       class AsAboveSoBelow.ABSettings AsAboveSoBelow.ABMod::get_Settings()
    IL_0029:  stloc.0
    IL_002a:  ldloc.0
    IL_002b:  brfalse.s  IL_0038

    IL_002d:  ldloc.0
    IL_002e:  ldfld      bool AsAboveSoBelow.ABSettings::crossLevelPipes
    IL_0033:  ldc.i4.0
    IL_0034:  ceq
    IL_0036:  br.s       IL_0039

    IL_0038:  ldc.i4.1
    IL_0039:  stloc.3
    IL_003a:  ldloc.3
    IL_003b:  brfalse.s  IL_0040

    IL_003d:  nop
    IL_003e:  br.s       IL_00a6

    IL_0040:  ldarg.0
    IL_0041:  callvirt   instance class [mscorlib]System.Collections.Generic.List`1<class AsAboveSoBelow.Building_ABStairs> AsAboveSoBelow.LevelComp::get_Stairs()
    IL_0046:  stloc.1
    IL_0047:  ldc.i4.0
    IL_0048:  stloc.s    V_4
    IL_004a:  br.s       IL_0096

    IL_004c:  nop
    IL_004d:  ldloc.1
    IL_004e:  ldloc.s    V_4
    IL_0050:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class AsAboveSoBelow.Building_ABStairs>::get_Item(int32)
    IL_0055:  stloc.s    V_5
    IL_0057:  ldloc.s    V_5
    IL_0059:  brfalse.s  IL_0067

    IL_005b:  ldloc.s    V_5
    IL_005d:  callvirt   instance bool ['Assembly-CSharp']Verse.Thing::get_Spawned()
    IL_0062:  ldc.i4.0
    IL_0063:  ceq
    IL_0065:  br.s       IL_0068

    IL_0067:  ldc.i4.1
    IL_0068:  stloc.s    V_6
    IL_006a:  ldloc.s    V_6
    IL_006c:  brfalse.s  IL_0071

    IL_006e:  nop
    IL_006f:  br.s       IL_0090

    IL_0071:  ldloc.s    V_5
    IL_0073:  ldloc.s    V_5
    IL_0075:  callvirt   instance class AsAboveSoBelow.Building_ABStairs AsAboveSoBelow.Building_ABStairs::get_Counterpart()
    IL_007a:  call       void AsAboveSoBelow.ABPipeCompat::BridgeLink(class AsAboveSoBelow.Building_ABStairs,
                                                                      class AsAboveSoBelow.Building_ABStairs)
    IL_007f:  nop
    IL_0080:  ldloc.s    V_5
    IL_0082:  ldloc.s    V_5
    IL_0084:  callvirt   instance class AsAboveSoBelow.Building_ABStairs AsAboveSoBelow.Building_ABStairs::get_SecondCounterpart()
    IL_0089:  call       void AsAboveSoBelow.ABPipeCompat::BridgeLink(class AsAboveSoBelow.Building_ABStairs,
                                                                      class AsAboveSoBelow.Building_ABStairs)
    IL_008e:  nop
    IL_008f:  nop
    IL_0090:  ldloc.s    V_4
    IL_0092:  ldc.i4.1
    IL_0093:  add
    IL_0094:  stloc.s    V_4
    IL_0096:  ldloc.s    V_4
    IL_0098:  ldloc.1
    IL_0099:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class AsAboveSoBelow.Building_ABStairs>::get_Count()
    IL_009e:  clt
    IL_00a0:  stloc.s    V_7
    IL_00a2:  ldloc.s    V_7
    IL_00a4:  brtrue.s   IL_004c

    IL_00a6:  ret
  } // end of method ABPipeCompat::TickGroundPairs

} // end of class AsAboveSoBelow.ABPipeCompat

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file F:\Repositories\rimworld_mods\.tmp\ABPipeCompat_tick.res
