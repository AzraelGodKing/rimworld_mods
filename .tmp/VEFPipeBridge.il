
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.8.3928.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.


// warning : THIS IS A PARTIAL DISASSEMBLY, NOT SUITABLE FOR RE-ASSEMBLING



.class public abstract auto ansi sealed beforefieldinit AsAboveSoBelow.VEFPipeBridge
       extends [mscorlib]System.Object
{
  .field private static literal float32 Damping = float32(0.5)
  .field private static literal float32 MinTransfer = float32(0.5)
  .field private static literal float32 MinDirect = float32(9.9999998e-03)
  .method public hidebysig static void  BridgePair(class AsAboveSoBelow.Building_ABStairs a,
                                                   class AsAboveSoBelow.Building_ABStairs b) cil managed
  {
    // Code size       321 (0x141)
    .maxstack  3
    .locals init (class [PipeSystem]PipeSystem.PipeNetManager V_0,
             class [PipeSystem]PipeSystem.PipeNetManager V_1,
             class [mscorlib]System.Collections.Generic.List`1<class [PipeSystem]PipeSystem.PipeNet> V_2,
             class [mscorlib]System.Collections.Generic.List`1<class [PipeSystem]PipeSystem.PipeNet> V_3,
             bool V_4,
             int32 V_5,
             class [PipeSystem]PipeSystem.PipeNet V_6,
             class [PipeSystem]PipeSystem.PipeNet V_7,
             bool V_8,
             int32 V_9,
             class [PipeSystem]PipeSystem.PipeNet V_10,
             bool V_11,
             bool V_12,
             bool V_13,
             bool V_14)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_0007:  callvirt   instance !!0 ['Assembly-CSharp']Verse.Map::GetComponent<class [PipeSystem]PipeSystem.PipeNetManager>()
    IL_000c:  stloc.0
    IL_000d:  ldarg.1
    IL_000e:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_0013:  callvirt   instance !!0 ['Assembly-CSharp']Verse.Map::GetComponent<class [PipeSystem]PipeSystem.PipeNetManager>()
    IL_0018:  stloc.1
    IL_0019:  ldloc.0
    IL_001a:  brfalse.s  IL_0022

    IL_001c:  ldloc.1
    IL_001d:  ldnull
    IL_001e:  ceq
    IL_0020:  br.s       IL_0023

    IL_0022:  ldc.i4.1
    IL_0023:  stloc.s    V_4
    IL_0025:  ldloc.s    V_4
    IL_0027:  brfalse.s  IL_002f

    IL_0029:  nop
    IL_002a:  br         IL_0140

    IL_002f:  ldloc.0
    IL_0030:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class [PipeSystem]PipeSystem.PipeNet> [PipeSystem]PipeSystem.PipeNetManager::pipeNets
    IL_0035:  stloc.2
    IL_0036:  ldloc.1
    IL_0037:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class [PipeSystem]PipeSystem.PipeNet> [PipeSystem]PipeSystem.PipeNetManager::pipeNets
    IL_003c:  stloc.3
    IL_003d:  ldc.i4.0
    IL_003e:  stloc.s    V_5
    IL_0040:  br         IL_012d

    IL_0045:  nop
    IL_0046:  ldloc.2
    IL_0047:  ldloc.s    V_5
    IL_0049:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class [PipeSystem]PipeSystem.PipeNet>::get_Item(int32)
    IL_004e:  stloc.s    V_6
    IL_0050:  ldloc.s    V_6
    IL_0052:  brtrue.s   IL_0057

    IL_0054:  ldnull
    IL_0055:  br.s       IL_005e

    IL_0057:  ldloc.s    V_6
    IL_0059:  ldfld      class ['Assembly-CSharp']Verse.BoolGrid [PipeSystem]PipeSystem.PipeNet::networkGrid
    IL_005e:  brfalse.s  IL_0078

    IL_0060:  ldloc.s    V_6
    IL_0062:  ldarg.0
    IL_0063:  callvirt   instance valuetype ['Assembly-CSharp']Verse.IntVec3 ['Assembly-CSharp']Verse.Thing::get_Position()
    IL_0068:  ldarg.0
    IL_0069:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_006e:  call       bool AsAboveSoBelow.VEFPipeBridge::Touches(object,
                                                                    valuetype ['Assembly-CSharp']Verse.IntVec3,
                                                                    class ['Assembly-CSharp']Verse.Map)
    IL_0073:  ldc.i4.0
    IL_0074:  ceq
    IL_0076:  br.s       IL_0079

    IL_0078:  ldc.i4.1
    IL_0079:  stloc.s    V_8
    IL_007b:  ldloc.s    V_8
    IL_007d:  brfalse.s  IL_0085

    IL_007f:  nop
    IL_0080:  br         IL_0127

    IL_0085:  ldnull
    IL_0086:  stloc.s    V_7
    IL_0088:  ldc.i4.0
    IL_0089:  stloc.s    V_9
    IL_008b:  br.s       IL_00e2

    IL_008d:  nop
    IL_008e:  ldloc.3
    IL_008f:  ldloc.s    V_9
    IL_0091:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class [PipeSystem]PipeSystem.PipeNet>::get_Item(int32)
    IL_0096:  stloc.s    V_10
    IL_0098:  ldloc.s    V_10
    IL_009a:  brtrue.s   IL_009f

    IL_009c:  ldnull
    IL_009d:  br.s       IL_00a6

    IL_009f:  ldloc.s    V_10
    IL_00a1:  ldfld      class ['Assembly-CSharp']Verse.BoolGrid [PipeSystem]PipeSystem.PipeNet::networkGrid
    IL_00a6:  brfalse.s  IL_00cd

    IL_00a8:  ldloc.s    V_10
    IL_00aa:  ldfld      class [PipeSystem]PipeSystem.PipeNetDef [PipeSystem]PipeSystem.PipeNet::def
    IL_00af:  ldloc.s    V_6
    IL_00b1:  ldfld      class [PipeSystem]PipeSystem.PipeNetDef [PipeSystem]PipeSystem.PipeNet::def
    IL_00b6:  bne.un.s   IL_00cd

    IL_00b8:  ldloc.s    V_10
    IL_00ba:  ldarg.1
    IL_00bb:  callvirt   instance valuetype ['Assembly-CSharp']Verse.IntVec3 ['Assembly-CSharp']Verse.Thing::get_Position()
    IL_00c0:  ldarg.1
    IL_00c1:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_00c6:  call       bool AsAboveSoBelow.VEFPipeBridge::Touches(object,
                                                                    valuetype ['Assembly-CSharp']Verse.IntVec3,
                                                                    class ['Assembly-CSharp']Verse.Map)
    IL_00cb:  br.s       IL_00ce

    IL_00cd:  ldc.i4.0
    IL_00ce:  stloc.s    V_11
    IL_00d0:  ldloc.s    V_11
    IL_00d2:  brfalse.s  IL_00db

    IL_00d4:  nop
    IL_00d5:  ldloc.s    V_10
    IL_00d7:  stloc.s    V_7
    IL_00d9:  br.s       IL_00f2

    IL_00db:  nop
    IL_00dc:  ldloc.s    V_9
    IL_00de:  ldc.i4.1
    IL_00df:  add
    IL_00e0:  stloc.s    V_9
    IL_00e2:  ldloc.s    V_9
    IL_00e4:  ldloc.3
    IL_00e5:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class [PipeSystem]PipeSystem.PipeNet>::get_Count()
    IL_00ea:  clt
    IL_00ec:  stloc.s    V_12
    IL_00ee:  ldloc.s    V_12
    IL_00f0:  brtrue.s   IL_008d

    IL_00f2:  ldloc.s    V_7
    IL_00f4:  brfalse.s  IL_00fe

    IL_00f6:  ldloc.s    V_7
    IL_00f8:  ldloc.s    V_6
    IL_00fa:  ceq
    IL_00fc:  br.s       IL_00ff

    IL_00fe:  ldc.i4.1
    IL_00ff:  stloc.s    V_13
    IL_0101:  ldloc.s    V_13
    IL_0103:  brfalse.s  IL_0108

    IL_0105:  nop
    IL_0106:  br.s       IL_0127

    IL_0108:  ldloc.s    V_6
    IL_010a:  ldloc.s    V_7
    IL_010c:  call       void AsAboveSoBelow.VEFPipeBridge::DirectFeed(object,
                                                                       object)
    IL_0111:  nop
    IL_0112:  ldloc.s    V_7
    IL_0114:  ldloc.s    V_6
    IL_0116:  call       void AsAboveSoBelow.VEFPipeBridge::DirectFeed(object,
                                                                       object)
    IL_011b:  nop
    IL_011c:  ldloc.s    V_6
    IL_011e:  ldloc.s    V_7
    IL_0120:  call       void AsAboveSoBelow.VEFPipeBridge::EqualizeStorages(object,
                                                                             object)
    IL_0125:  nop
    IL_0126:  nop
    IL_0127:  ldloc.s    V_5
    IL_0129:  ldc.i4.1
    IL_012a:  add
    IL_012b:  stloc.s    V_5
    IL_012d:  ldloc.s    V_5
    IL_012f:  ldloc.2
    IL_0130:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class [PipeSystem]PipeSystem.PipeNet>::get_Count()
    IL_0135:  clt
    IL_0137:  stloc.s    V_14
    IL_0139:  ldloc.s    V_14
    IL_013b:  brtrue     IL_0045

    IL_0140:  ret
  } // end of method VEFPipeBridge::BridgePair

  .method private hidebysig static bool  Touches(object netObj,
                                                 valuetype ['Assembly-CSharp']Verse.IntVec3 c,
                                                 class ['Assembly-CSharp']Verse.Map map) cil managed
  {
    // Code size       120 (0x78)
    .maxstack  3
    .locals init (class [PipeSystem]PipeSystem.PipeNet V_0,
             valuetype ['Assembly-CSharp']Verse.IntVec3[] V_1,
             bool V_2,
             bool V_3,
             int32 V_4,
             valuetype ['Assembly-CSharp']Verse.IntVec3 V_5,
             bool V_6,
             bool V_7)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  castclass  [PipeSystem]PipeSystem.PipeNet
    IL_0007:  stloc.0
    IL_0008:  ldloc.0
    IL_0009:  ldfld      class ['Assembly-CSharp']Verse.BoolGrid [PipeSystem]PipeSystem.PipeNet::networkGrid
    IL_000e:  ldarg.1
    IL_000f:  callvirt   instance bool ['Assembly-CSharp']Verse.BoolGrid::get_Item(valuetype ['Assembly-CSharp']Verse.IntVec3)
    IL_0014:  stloc.2
    IL_0015:  ldloc.2
    IL_0016:  brfalse.s  IL_001d

    IL_0018:  nop
    IL_0019:  ldc.i4.1
    IL_001a:  stloc.3
    IL_001b:  br.s       IL_0076

    IL_001d:  ldsfld     valuetype ['Assembly-CSharp']Verse.IntVec3[] ['Assembly-CSharp']Verse.GenAdj::CardinalDirections
    IL_0022:  stloc.1
    IL_0023:  ldc.i4.0
    IL_0024:  stloc.s    V_4
    IL_0026:  br.s       IL_0065

    IL_0028:  nop
    IL_0029:  ldarg.1
    IL_002a:  ldloc.1
    IL_002b:  ldloc.s    V_4
    IL_002d:  ldelem     ['Assembly-CSharp']Verse.IntVec3
    IL_0032:  call       valuetype ['Assembly-CSharp']Verse.IntVec3 ['Assembly-CSharp']Verse.IntVec3::op_Addition(valuetype ['Assembly-CSharp']Verse.IntVec3,
                                                                                                                  valuetype ['Assembly-CSharp']Verse.IntVec3)
    IL_0037:  stloc.s    V_5
    IL_0039:  ldloc.s    V_5
    IL_003b:  ldarg.2
    IL_003c:  call       bool ['Assembly-CSharp']Verse.GenGrid::InBounds(valuetype ['Assembly-CSharp']Verse.IntVec3,
                                                                         class ['Assembly-CSharp']Verse.Map)
    IL_0041:  brfalse.s  IL_0052

    IL_0043:  ldloc.0
    IL_0044:  ldfld      class ['Assembly-CSharp']Verse.BoolGrid [PipeSystem]PipeSystem.PipeNet::networkGrid
    IL_0049:  ldloc.s    V_5
    IL_004b:  callvirt   instance bool ['Assembly-CSharp']Verse.BoolGrid::get_Item(valuetype ['Assembly-CSharp']Verse.IntVec3)
    IL_0050:  br.s       IL_0053

    IL_0052:  ldc.i4.0
    IL_0053:  stloc.s    V_6
    IL_0055:  ldloc.s    V_6
    IL_0057:  brfalse.s  IL_005e

    IL_0059:  nop
    IL_005a:  ldc.i4.1
    IL_005b:  stloc.3
    IL_005c:  br.s       IL_0076

    IL_005e:  nop
    IL_005f:  ldloc.s    V_4
    IL_0061:  ldc.i4.1
    IL_0062:  add
    IL_0063:  stloc.s    V_4
    IL_0065:  ldloc.s    V_4
    IL_0067:  ldloc.1
    IL_0068:  ldlen
    IL_0069:  conv.i4
    IL_006a:  clt
    IL_006c:  stloc.s    V_7
    IL_006e:  ldloc.s    V_7
    IL_0070:  brtrue.s   IL_0028

    IL_0072:  ldc.i4.0
    IL_0073:  stloc.3
    IL_0074:  br.s       IL_0076

    IL_0076:  ldloc.3
    IL_0077:  ret
  } // end of method VEFPipeBridge::Touches

  .method private hidebysig static void  DirectFeed(object fromObj,
                                                    object toObj) cil managed
  {
    // Code size       253 (0xfd)
    .maxstack  3
    .locals init (class [PipeSystem]PipeSystem.PipeNet V_0,
             class [PipeSystem]PipeSystem.PipeNet V_1,
             float32 V_2,
             class [mscorlib]System.Collections.Generic.List`1<class [PipeSystem]PipeSystem.CompResourceTrader> V_3,
             float32 V_4,
             float32 V_5,
             float32 V_6,
             int32 V_7,
             class [PipeSystem]PipeSystem.CompResourceTrader V_8,
             bool V_9,
             bool V_10,
             bool V_11,
             bool V_12)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  castclass  [PipeSystem]PipeSystem.PipeNet
    IL_0007:  stloc.0
    IL_0008:  ldarg.1
    IL_0009:  castclass  [PipeSystem]PipeSystem.PipeNet
    IL_000e:  stloc.1
    IL_000f:  ldc.r4     0.0
    IL_0014:  stloc.2
    IL_0015:  ldloc.1
    IL_0016:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class [PipeSystem]PipeSystem.CompResourceTrader> [PipeSystem]PipeSystem.PipeNet::receivers
    IL_001b:  stloc.3
    IL_001c:  ldc.i4.0
    IL_001d:  stloc.s    V_7
    IL_001f:  br.s       IL_0058

    IL_0021:  nop
    IL_0022:  ldloc.3
    IL_0023:  ldloc.s    V_7
    IL_0025:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class [PipeSystem]PipeSystem.CompResourceTrader>::get_Item(int32)
    IL_002a:  stloc.s    V_8
    IL_002c:  ldloc.s    V_8
    IL_002e:  callvirt   instance bool [PipeSystem]PipeSystem.CompResourceTrader::get_ResourceOn()
    IL_0033:  brtrue.s   IL_003e

    IL_0035:  ldloc.s    V_8
    IL_0037:  callvirt   instance bool [PipeSystem]PipeSystem.CompResourceTrader::CanBeOn()
    IL_003c:  br.s       IL_003f

    IL_003e:  ldc.i4.0
    IL_003f:  stloc.s    V_9
    IL_0041:  ldloc.s    V_9
    IL_0043:  brfalse.s  IL_0051

    IL_0045:  nop
    IL_0046:  ldloc.2
    IL_0047:  ldloc.s    V_8
    IL_0049:  callvirt   instance float32 [PipeSystem]PipeSystem.CompResourceTrader::get_Consumption()
    IL_004e:  add
    IL_004f:  stloc.2
    IL_0050:  nop
    IL_0051:  nop
    IL_0052:  ldloc.s    V_7
    IL_0054:  ldc.i4.1
    IL_0055:  add
    IL_0056:  stloc.s    V_7
    IL_0058:  ldloc.s    V_7
    IL_005a:  ldloc.3
    IL_005b:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class [PipeSystem]PipeSystem.CompResourceTrader>::get_Count()
    IL_0060:  clt
    IL_0062:  stloc.s    V_10
    IL_0064:  ldloc.s    V_10
    IL_0066:  brtrue.s   IL_0021

    IL_0068:  ldloc.1
    IL_0069:  callvirt   instance float32 [PipeSystem]PipeSystem.PipeNet::get_Consumption()
    IL_006e:  ldloc.2
    IL_006f:  add
    IL_0070:  ldloc.1
    IL_0071:  callvirt   instance float32 [PipeSystem]PipeSystem.PipeNet::get_Production()
    IL_0076:  sub
    IL_0077:  ldloc.1
    IL_0078:  callvirt   instance float32 [PipeSystem]PipeSystem.PipeNet::get_ExtraProductionThisTick()
    IL_007d:  sub
    IL_007e:  stloc.s    V_4
    IL_0080:  ldloc.s    V_4
    IL_0082:  ldc.r4     9.9999998e-03
    IL_0087:  cgt.un
    IL_0089:  ldc.i4.0
    IL_008a:  ceq
    IL_008c:  stloc.s    V_11
    IL_008e:  ldloc.s    V_11
    IL_0090:  brfalse.s  IL_0095

    IL_0092:  nop
    IL_0093:  br.s       IL_00fc

    IL_0095:  ldc.r4     0.0
    IL_009a:  ldloc.0
    IL_009b:  callvirt   instance float32 [PipeSystem]PipeSystem.PipeNet::get_Production()
    IL_00a0:  ldloc.0
    IL_00a1:  callvirt   instance float32 [PipeSystem]PipeSystem.PipeNet::get_Consumption()
    IL_00a6:  sub
    IL_00a7:  ldloc.0
    IL_00a8:  callvirt   instance float32 [PipeSystem]PipeSystem.PipeNet::get_ExtraConsumptionThisTick()
    IL_00ad:  sub
    IL_00ae:  call       float32 [UnityEngine.CoreModule]UnityEngine.Mathf::Max(float32,
                                                                                float32)
    IL_00b3:  ldloc.0
    IL_00b4:  callvirt   instance float32 [PipeSystem]PipeSystem.PipeNet::get_Stored()
    IL_00b9:  add
    IL_00ba:  stloc.s    V_5
    IL_00bc:  ldloc.s    V_4
    IL_00be:  ldloc.s    V_5
    IL_00c0:  call       float32 [UnityEngine.CoreModule]UnityEngine.Mathf::Min(float32,
                                                                                float32)
    IL_00c5:  stloc.s    V_6
    IL_00c7:  ldloc.s    V_6
    IL_00c9:  ldc.r4     9.9999998e-03
    IL_00ce:  cgt.un
    IL_00d0:  ldc.i4.0
    IL_00d1:  ceq
    IL_00d3:  stloc.s    V_12
    IL_00d5:  ldloc.s    V_12
    IL_00d7:  brfalse.s  IL_00dc

    IL_00d9:  nop
    IL_00da:  br.s       IL_00fc

    IL_00dc:  ldloc.0
    IL_00dd:  dup
    IL_00de:  callvirt   instance float32 [PipeSystem]PipeSystem.PipeNet::get_ExtraConsumptionThisTick()
    IL_00e3:  ldloc.s    V_6
    IL_00e5:  add
    IL_00e6:  callvirt   instance void [PipeSystem]PipeSystem.PipeNet::set_ExtraConsumptionThisTick(float32)
    IL_00eb:  nop
    IL_00ec:  ldloc.1
    IL_00ed:  dup
    IL_00ee:  callvirt   instance float32 [PipeSystem]PipeSystem.PipeNet::get_ExtraProductionThisTick()
    IL_00f3:  ldloc.s    V_6
    IL_00f5:  add
    IL_00f6:  callvirt   instance void [PipeSystem]PipeSystem.PipeNet::set_ExtraProductionThisTick(float32)
    IL_00fb:  nop
    IL_00fc:  ret
  } // end of method VEFPipeBridge::DirectFeed

  .method private hidebysig static void  EqualizeStorages(object netAObj,
                                                          object netBObj) cil managed
  {
    // Code size       258 (0x102)
    .maxstack  5
    .locals init (class [PipeSystem]PipeSystem.PipeNet V_0,
             class [PipeSystem]PipeSystem.PipeNet V_1,
             float32 V_2,
             float32 V_3,
             float32 V_4,
             float32 V_5,
             float32 V_6,
             class [PipeSystem]PipeSystem.PipeNet V_7,
             class [PipeSystem]PipeSystem.PipeNet V_8,
             float32 V_9,
             float32 V_10,
             float32 V_11,
             float32 V_12,
             bool V_13,
             bool V_14,
             bool V_15,
             bool V_16,
             bool V_17,
             float32 V_18)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  castclass  [PipeSystem]PipeSystem.PipeNet
    IL_0007:  stloc.0
    IL_0008:  ldarg.1
    IL_0009:  castclass  [PipeSystem]PipeSystem.PipeNet
    IL_000e:  stloc.1
    IL_000f:  ldloc.0
    IL_0010:  callvirt   instance float32 [PipeSystem]PipeSystem.PipeNet::CurrentStored()
    IL_0015:  stloc.2
    IL_0016:  ldloc.1
    IL_0017:  callvirt   instance float32 [PipeSystem]PipeSystem.PipeNet::CurrentStored()
    IL_001c:  stloc.3
    IL_001d:  ldloc.2
    IL_001e:  ldloc.0
    IL_001f:  callvirt   instance float32 [PipeSystem]PipeSystem.PipeNet::get_AvailableCapacity()
    IL_0024:  add
    IL_0025:  stloc.s    V_4
    IL_0027:  ldloc.3
    IL_0028:  ldloc.1
    IL_0029:  callvirt   instance float32 [PipeSystem]PipeSystem.PipeNet::get_AvailableCapacity()
    IL_002e:  add
    IL_002f:  stloc.s    V_5
    IL_0031:  ldloc.s    V_4
    IL_0033:  ldc.r4     0.0
    IL_0038:  ble.s      IL_0048

    IL_003a:  ldloc.s    V_5
    IL_003c:  ldc.r4     0.0
    IL_0041:  cgt.un
    IL_0043:  ldc.i4.0
    IL_0044:  ceq
    IL_0046:  br.s       IL_0049

    IL_0048:  ldc.i4.1
    IL_0049:  stloc.s    V_13
    IL_004b:  ldloc.s    V_13
    IL_004d:  brfalse.s  IL_0055

    IL_004f:  nop
    IL_0050:  br         IL_0101

    IL_0055:  ldloc.2
    IL_0056:  ldloc.s    V_5
    IL_0058:  mul
    IL_0059:  ldloc.3
    IL_005a:  ldloc.s    V_4
    IL_005c:  mul
    IL_005d:  sub
    IL_005e:  ldloc.s    V_4
    IL_0060:  ldloc.s    V_5
    IL_0062:  add
    IL_0063:  div
    IL_0064:  ldc.r4     0.5
    IL_0069:  mul
    IL_006a:  stloc.s    V_6
    IL_006c:  ldloc.s    V_6
    IL_006e:  ldc.r4     0.5
    IL_0073:  cgt
    IL_0075:  stloc.s    V_14
    IL_0077:  ldloc.s    V_14
    IL_0079:  brfalse.s  IL_0089

    IL_007b:  nop
    IL_007c:  ldloc.0
    IL_007d:  stloc.s    V_7
    IL_007f:  ldloc.1
    IL_0080:  stloc.s    V_8
    IL_0082:  ldloc.s    V_6
    IL_0084:  stloc.s    V_9
    IL_0086:  nop
    IL_0087:  br.s       IL_00aa

    IL_0089:  ldloc.s    V_6
    IL_008b:  ldc.r4     -0.5
    IL_0090:  clt
    IL_0092:  stloc.s    V_15
    IL_0094:  ldloc.s    V_15
    IL_0096:  brfalse.s  IL_00a7

    IL_0098:  nop
    IL_0099:  ldloc.1
    IL_009a:  stloc.s    V_7
    IL_009c:  ldloc.0
    IL_009d:  stloc.s    V_8
    IL_009f:  ldloc.s    V_6
    IL_00a1:  neg
    IL_00a2:  stloc.s    V_9
    IL_00a4:  nop
    IL_00a5:  br.s       IL_00aa

    IL_00a7:  nop
    IL_00a8:  br.s       IL_0101

    IL_00aa:  ldloc.s    V_7
    IL_00ac:  ldloc.s    V_9
    IL_00ae:  ldloca.s   V_10
    IL_00b0:  ldnull
    IL_00b1:  ldc.i4.0
    IL_00b2:  callvirt   instance void [PipeSystem]PipeSystem.PipeNet::DrawAmongStorage(float32,
                                                                                        float32&,
                                                                                        class [mscorlib]System.Collections.Generic.List`1<class [PipeSystem]PipeSystem.CompResourceStorage>,
                                                                                        bool)
    IL_00b7:  nop
    IL_00b8:  ldloc.s    V_10
    IL_00ba:  ldc.r4     0.0
    IL_00bf:  cgt.un
    IL_00c1:  ldc.i4.0
    IL_00c2:  ceq
    IL_00c4:  stloc.s    V_16
    IL_00c6:  ldloc.s    V_16
    IL_00c8:  brfalse.s  IL_00cd

    IL_00ca:  nop
    IL_00cb:  br.s       IL_0101

    IL_00cd:  ldloc.s    V_8
    IL_00cf:  ldloc.s    V_10
    IL_00d1:  ldloca.s   V_11
    IL_00d3:  ldnull
    IL_00d4:  ldc.i4.0
    IL_00d5:  callvirt   instance void [PipeSystem]PipeSystem.PipeNet::DistributeAmongStorage(float32,
                                                                                              float32&,
                                                                                              class [mscorlib]System.Collections.Generic.List`1<class [PipeSystem]PipeSystem.CompResourceStorage>,
                                                                                              bool)
    IL_00da:  nop
    IL_00db:  ldloc.s    V_10
    IL_00dd:  ldloc.s    V_11
    IL_00df:  sub
    IL_00e0:  stloc.s    V_12
    IL_00e2:  ldloc.s    V_12
    IL_00e4:  ldc.r4     1.e-03
    IL_00e9:  cgt
    IL_00eb:  stloc.s    V_17
    IL_00ed:  ldloc.s    V_17
    IL_00ef:  brfalse.s  IL_0101

    IL_00f1:  nop
    IL_00f2:  ldloc.s    V_7
    IL_00f4:  ldloc.s    V_12
    IL_00f6:  ldloca.s   V_18
    IL_00f8:  ldnull
    IL_00f9:  ldc.i4.0
    IL_00fa:  callvirt   instance void [PipeSystem]PipeSystem.PipeNet::DistributeAmongStorage(float32,
                                                                                              float32&,
                                                                                              class [mscorlib]System.Collections.Generic.List`1<class [PipeSystem]PipeSystem.CompResourceStorage>,
                                                                                              bool)
    IL_00ff:  nop
    IL_0100:  nop
    IL_0101:  ret
  } // end of method VEFPipeBridge::EqualizeStorages

} // end of class AsAboveSoBelow.VEFPipeBridge

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file F:\Repositories\rimworld_mods\.tmp\VEFPipeBridge.res
