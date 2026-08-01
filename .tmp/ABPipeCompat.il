
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.8.3928.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.


// warning : THIS IS A PARTIAL DISASSEMBLY, NOT SUITABLE FOR RE-ASSEMBLING



.class public abstract auto ansi sealed AsAboveSoBelow.ABPipeCompat
       extends [mscorlib]System.Object
{
  .custom instance void ['Assembly-CSharp']Verse.StaticConstructorOnStartup::.ctor() = ( 01 00 00 00 ) 
  .field public static initonly bool DbhActive
  .field public static initonly bool VefActive
  .field public static initonly bool RimefellerActive
  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       113 (0x71)
    .maxstack  1
    .locals init (bool V_0,
             bool V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldstr      "Dubwise.DubsBadHygiene"
    IL_0006:  call       bool AsAboveSoBelow.ABDetect::Active(string)
    IL_000b:  stsfld     bool AsAboveSoBelow.ABPipeCompat::DbhActive
    IL_0010:  ldstr      "OskarPotocki.VanillaFactionsExpanded.Core"
    IL_0015:  call       bool AsAboveSoBelow.ABDetect::Active(string)
    IL_001a:  stsfld     bool AsAboveSoBelow.ABPipeCompat::VefActive
    IL_001f:  ldstr      "Dubwise.Rimefeller"
    IL_0024:  call       bool AsAboveSoBelow.ABDetect::Active(string)
    IL_0029:  stsfld     bool AsAboveSoBelow.ABPipeCompat::RimefellerActive
    IL_002e:  ldsfld     bool AsAboveSoBelow.ABPipeCompat::DbhActive
    IL_0033:  stloc.0
    IL_0034:  ldloc.0
    IL_0035:  brfalse.s  IL_0044

    IL_0037:  nop
    IL_0038:  ldstr      "Dubs Bad Hygiene detected, direct water bridging e"
    + "nabled."
    IL_003d:  call       void AsAboveSoBelow.ABLog::Dev(string)
    IL_0042:  nop
    IL_0043:  nop
    IL_0044:  ldsfld     bool AsAboveSoBelow.ABPipeCompat::VefActive
    IL_0049:  stloc.1
    IL_004a:  ldloc.1
    IL_004b:  brfalse.s  IL_005a

    IL_004d:  nop
    IL_004e:  ldstr      "VEF pipe system detected, direct resource bridging"
    + " enabled (VPE, Helixien, VTE)."
    IL_0053:  call       void AsAboveSoBelow.ABLog::Dev(string)
    IL_0058:  nop
    IL_0059:  nop
    IL_005a:  ldsfld     bool AsAboveSoBelow.ABPipeCompat::RimefellerActive
    IL_005f:  stloc.2
    IL_0060:  ldloc.2
    IL_0061:  brfalse.s  IL_0070

    IL_0063:  nop
    IL_0064:  ldstr      "Rimefeller detected, oil and fuel bridging enabled."
    IL_0069:  call       void AsAboveSoBelow.ABLog::Dev(string)
    IL_006e:  nop
    IL_006f:  nop
    IL_0070:  ret
  } // end of method ABPipeCompat::.cctor

  .method private hidebysig static void  BridgeLink(class AsAboveSoBelow.Building_ABStairs a,
                                                    class AsAboveSoBelow.Building_ABStairs b) cil managed
  {
    // Code size       154 (0x9a)
    .maxstack  2
    .locals init (class AsAboveSoBelow.ABStairsExtension V_0,
             bool V_1,
             bool V_2,
             bool V_3,
             bool V_4,
             bool V_5)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  brfalse.s  IL_0021

    IL_0004:  ldarg.1
    IL_0005:  callvirt   instance bool ['Assembly-CSharp']Verse.Thing::get_Spawned()
    IL_000a:  brfalse.s  IL_0021

    IL_000c:  ldarg.1
    IL_000d:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_0012:  brfalse.s  IL_0021

    IL_0014:  ldarg.1
    IL_0015:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_001a:  callvirt   instance bool ['Assembly-CSharp']Verse.Map::get_Disposed()
    IL_001f:  br.s       IL_0022

    IL_0021:  ldc.i4.1
    IL_0022:  stloc.1
    IL_0023:  ldloc.1
    IL_0024:  brfalse.s  IL_0029

    IL_0026:  nop
    IL_0027:  br.s       IL_0099

    IL_0029:  ldarg.0
    IL_002a:  callvirt   instance class AsAboveSoBelow.ABStairsExtension AsAboveSoBelow.Building_ABStairs::get_Ext()
    IL_002f:  stloc.0
    IL_0030:  ldloc.0
    IL_0031:  ldnull
    IL_0032:  ceq
    IL_0034:  stloc.2
    IL_0035:  ldloc.2
    IL_0036:  brfalse.s  IL_003b

    IL_0038:  nop
    IL_0039:  br.s       IL_0099

    IL_003b:  ldsfld     bool AsAboveSoBelow.ABPipeCompat::DbhActive
    IL_0040:  brfalse.s  IL_004a

    IL_0042:  ldloc.0
    IL_0043:  ldfld      bool AsAboveSoBelow.ABStairsExtension::bridgeWater
    IL_0048:  br.s       IL_004b

    IL_004a:  ldc.i4.0
    IL_004b:  stloc.3
    IL_004c:  ldloc.3
    IL_004d:  brfalse.s  IL_0059

    IL_004f:  nop
    IL_0050:  ldarg.0
    IL_0051:  ldarg.1
    IL_0052:  call       void AsAboveSoBelow.DBHWaterBridge::BridgePair(class AsAboveSoBelow.Building_ABStairs,
                                                                        class AsAboveSoBelow.Building_ABStairs)
    IL_0057:  nop
    IL_0058:  nop
    IL_0059:  ldsfld     bool AsAboveSoBelow.ABPipeCompat::VefActive
    IL_005e:  brfalse.s  IL_0068

    IL_0060:  ldloc.0
    IL_0061:  ldfld      bool AsAboveSoBelow.ABStairsExtension::bridgeVef
    IL_0066:  br.s       IL_0069

    IL_0068:  ldc.i4.0
    IL_0069:  stloc.s    V_4
    IL_006b:  ldloc.s    V_4
    IL_006d:  brfalse.s  IL_0079

    IL_006f:  nop
    IL_0070:  ldarg.0
    IL_0071:  ldarg.1
    IL_0072:  call       void AsAboveSoBelow.VEFPipeBridge::BridgePair(class AsAboveSoBelow.Building_ABStairs,
                                                                       class AsAboveSoBelow.Building_ABStairs)
    IL_0077:  nop
    IL_0078:  nop
    IL_0079:  ldsfld     bool AsAboveSoBelow.ABPipeCompat::RimefellerActive
    IL_007e:  brfalse.s  IL_0088

    IL_0080:  ldloc.0
    IL_0081:  ldfld      bool AsAboveSoBelow.ABStairsExtension::bridgeChem
    IL_0086:  br.s       IL_0089

    IL_0088:  ldc.i4.0
    IL_0089:  stloc.s    V_5
    IL_008b:  ldloc.s    V_5
    IL_008d:  brfalse.s  IL_0099

    IL_008f:  nop
    IL_0090:  ldarg.0
    IL_0091:  ldarg.1
    IL_0092:  call       void AsAboveSoBelow.RimefellerBridge::BridgePair(class AsAboveSoBelow.Building_ABStairs,
                                                                          class AsAboveSoBelow.Building_ABStairs)
    IL_0097:  nop
    IL_0098:  nop
    IL_0099:  ret
  } // end of method ABPipeCompat::BridgeLink

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
// WARNING: Created Win32 resource file F:\Repositories\rimworld_mods\.tmp\ABPipeCompat.res
