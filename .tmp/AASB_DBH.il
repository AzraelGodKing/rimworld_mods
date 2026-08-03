
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.8.3928.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.


// warning : THIS IS A PARTIAL DISASSEMBLY, NOT SUITABLE FOR RE-ASSEMBLING



.class public abstract auto ansi sealed beforefieldinit AsAboveSoBelow.DBHWaterBridge
       extends [mscorlib]System.Object
{
  .field private static literal float32 Damping = float32(0.5)
  .field private static literal float32 MinTransfer = float32(5.0000001e-02)
  .method public hidebysig static void  BridgePair(class AsAboveSoBelow.Building_ABStairs a,
                                                   class AsAboveSoBelow.Building_ABStairs b) cil managed
  {
    // Code size       601 (0x259)
    .maxstack  4
    .locals init (class [BadHygiene]DubsBadHygiene.HygienePipeMapComp V_0,
             class [BadHygiene]DubsBadHygiene.HygienePipeMapComp V_1,
             class [BadHygiene]DubsBadHygiene.PlumbingNet[] V_2,
             class [BadHygiene]DubsBadHygiene.PlumbingNet[] V_3,
             bool V_4,
             int32 V_5,
             class [BadHygiene]DubsBadHygiene.PlumbingNet V_6,
             class [BadHygiene]DubsBadHygiene.PlumbingNet V_7,
             float32 V_8,
             class [mscorlib]System.Collections.Generic.List`1<class [BadHygiene]DubsBadHygiene.CompWaterStorage> V_9,
             float32 V_10,
             class [mscorlib]System.Collections.Generic.List`1<class [BadHygiene]DubsBadHygiene.CompWaterStorage> V_11,
             float32 V_12,
             class [BadHygiene]DubsBadHygiene.PlumbingNet V_13,
             class [BadHygiene]DubsBadHygiene.PlumbingNet V_14,
             float32 V_15,
             float32 V_16,
             bool V_17,
             int32 V_18,
             bool V_19,
             bool V_20,
             bool V_21,
             int32 V_22,
             bool V_23,
             int32 V_24,
             bool V_25,
             bool V_26,
             bool V_27,
             bool V_28,
             bool V_29,
             valuetype [BadHygiene]DubsBadHygiene.ContaminationLevel V_30,
             bool V_31,
             bool V_32)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_0007:  callvirt   instance !!0 ['Assembly-CSharp']Verse.Map::GetComponent<class [BadHygiene]DubsBadHygiene.HygienePipeMapComp>()
    IL_000c:  stloc.0
    IL_000d:  ldarg.1
    IL_000e:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_0013:  callvirt   instance !!0 ['Assembly-CSharp']Verse.Map::GetComponent<class [BadHygiene]DubsBadHygiene.HygienePipeMapComp>()
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
    IL_002a:  br         IL_0258

    IL_002f:  ldloc.0
    IL_0030:  ldfld      class [BadHygiene]DubsBadHygiene.PlumbingNet[] [BadHygiene]DubsBadHygiene.HygienePipeMapComp::PipeNets
    IL_0035:  stloc.2
    IL_0036:  ldloc.1
    IL_0037:  ldfld      class [BadHygiene]DubsBadHygiene.PlumbingNet[] [BadHygiene]DubsBadHygiene.HygienePipeMapComp::PipeNets
    IL_003c:  stloc.3
    IL_003d:  ldc.i4.0
    IL_003e:  stloc.s    V_5
    IL_0040:  br         IL_0248

    IL_0045:  nop
    IL_0046:  ldloc.2
    IL_0047:  ldloc.s    V_5
    IL_0049:  ldelem.ref
    IL_004a:  stloc.s    V_6
    IL_004c:  ldloc.s    V_6
    IL_004e:  ldfld      class [System.Core]System.Collections.Generic.HashSet`1<valuetype ['Assembly-CSharp']Verse.IntVec3> [BadHygiene]DubsBadHygiene.PlumbingNet::cells
    IL_0053:  ldarg.0
    IL_0054:  callvirt   instance valuetype ['Assembly-CSharp']Verse.IntVec3 ['Assembly-CSharp']Verse.Thing::get_Position()
    IL_0059:  callvirt   instance bool class [System.Core]System.Collections.Generic.HashSet`1<valuetype ['Assembly-CSharp']Verse.IntVec3>::Contains(!0)
    IL_005e:  ldc.i4.0
    IL_005f:  ceq
    IL_0061:  stloc.s    V_17
    IL_0063:  ldloc.s    V_17
    IL_0065:  brfalse.s  IL_006d

    IL_0067:  nop
    IL_0068:  br         IL_0242

    IL_006d:  ldnull
    IL_006e:  stloc.s    V_7
    IL_0070:  ldc.i4.0
    IL_0071:  stloc.s    V_18
    IL_0073:  br.s       IL_00b5

    IL_0075:  nop
    IL_0076:  ldloc.3
    IL_0077:  ldloc.s    V_18
    IL_0079:  ldelem.ref
    IL_007a:  ldfld      int32 [BadHygiene]DubsBadHygiene.PlumbingNet::NetType
    IL_007f:  ldloc.s    V_6
    IL_0081:  ldfld      int32 [BadHygiene]DubsBadHygiene.PlumbingNet::NetType
    IL_0086:  bne.un.s   IL_009e

    IL_0088:  ldloc.3
    IL_0089:  ldloc.s    V_18
    IL_008b:  ldelem.ref
    IL_008c:  ldfld      class [System.Core]System.Collections.Generic.HashSet`1<valuetype ['Assembly-CSharp']Verse.IntVec3> [BadHygiene]DubsBadHygiene.PlumbingNet::cells
    IL_0091:  ldarg.1
    IL_0092:  callvirt   instance valuetype ['Assembly-CSharp']Verse.IntVec3 ['Assembly-CSharp']Verse.Thing::get_Position()
    IL_0097:  callvirt   instance bool class [System.Core]System.Collections.Generic.HashSet`1<valuetype ['Assembly-CSharp']Verse.IntVec3>::Contains(!0)
    IL_009c:  br.s       IL_009f

    IL_009e:  ldc.i4.0
    IL_009f:  stloc.s    V_19
    IL_00a1:  ldloc.s    V_19
    IL_00a3:  brfalse.s  IL_00ae

    IL_00a5:  nop
    IL_00a6:  ldloc.3
    IL_00a7:  ldloc.s    V_18
    IL_00a9:  ldelem.ref
    IL_00aa:  stloc.s    V_7
    IL_00ac:  br.s       IL_00c2

    IL_00ae:  nop
    IL_00af:  ldloc.s    V_18
    IL_00b1:  ldc.i4.1
    IL_00b2:  add
    IL_00b3:  stloc.s    V_18
    IL_00b5:  ldloc.s    V_18
    IL_00b7:  ldloc.3
    IL_00b8:  ldlen
    IL_00b9:  conv.i4
    IL_00ba:  clt
    IL_00bc:  stloc.s    V_20
    IL_00be:  ldloc.s    V_20
    IL_00c0:  brtrue.s   IL_0075

    IL_00c2:  ldloc.s    V_7
    IL_00c4:  brfalse.s  IL_00ce

    IL_00c6:  ldloc.s    V_7
    IL_00c8:  ldloc.s    V_6
    IL_00ca:  ceq
    IL_00cc:  br.s       IL_00cf

    IL_00ce:  ldc.i4.1
    IL_00cf:  stloc.s    V_21
    IL_00d1:  ldloc.s    V_21
    IL_00d3:  brfalse.s  IL_00db

    IL_00d5:  nop
    IL_00d6:  br         IL_0242

    IL_00db:  ldc.r4     0.0
    IL_00e0:  stloc.s    V_8
    IL_00e2:  ldloc.s    V_6
    IL_00e4:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class [BadHygiene]DubsBadHygiene.CompWaterStorage> [BadHygiene]DubsBadHygiene.PlumbingNet::WaterTowers
    IL_00e9:  stloc.s    V_9
    IL_00eb:  ldc.i4.0
    IL_00ec:  stloc.s    V_22
    IL_00ee:  br.s       IL_011a

    IL_00f0:  nop
    IL_00f1:  ldloc.s    V_8
    IL_00f3:  ldloc.s    V_9
    IL_00f5:  ldloc.s    V_22
    IL_00f7:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class [BadHygiene]DubsBadHygiene.CompWaterStorage>::get_Item(int32)
    IL_00fc:  ldfld      float32 [BadHygiene]DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_0101:  ldloc.s    V_9
    IL_0103:  ldloc.s    V_22
    IL_0105:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class [BadHygiene]DubsBadHygiene.CompWaterStorage>::get_Item(int32)
    IL_010a:  callvirt   instance float32 [BadHygiene]DubsBadHygiene.CompWaterStorage::get_space()
    IL_010f:  add
    IL_0110:  add
    IL_0111:  stloc.s    V_8
    IL_0113:  nop
    IL_0114:  ldloc.s    V_22
    IL_0116:  ldc.i4.1
    IL_0117:  add
    IL_0118:  stloc.s    V_22
    IL_011a:  ldloc.s    V_22
    IL_011c:  ldloc.s    V_9
    IL_011e:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class [BadHygiene]DubsBadHygiene.CompWaterStorage>::get_Count()
    IL_0123:  clt
    IL_0125:  stloc.s    V_23
    IL_0127:  ldloc.s    V_23
    IL_0129:  brtrue.s   IL_00f0

    IL_012b:  ldc.r4     0.0
    IL_0130:  stloc.s    V_10
    IL_0132:  ldloc.s    V_7
    IL_0134:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class [BadHygiene]DubsBadHygiene.CompWaterStorage> [BadHygiene]DubsBadHygiene.PlumbingNet::WaterTowers
    IL_0139:  stloc.s    V_11
    IL_013b:  ldc.i4.0
    IL_013c:  stloc.s    V_24
    IL_013e:  br.s       IL_016a

    IL_0140:  nop
    IL_0141:  ldloc.s    V_10
    IL_0143:  ldloc.s    V_11
    IL_0145:  ldloc.s    V_24
    IL_0147:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class [BadHygiene]DubsBadHygiene.CompWaterStorage>::get_Item(int32)
    IL_014c:  ldfld      float32 [BadHygiene]DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_0151:  ldloc.s    V_11
    IL_0153:  ldloc.s    V_24
    IL_0155:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class [BadHygiene]DubsBadHygiene.CompWaterStorage>::get_Item(int32)
    IL_015a:  callvirt   instance float32 [BadHygiene]DubsBadHygiene.CompWaterStorage::get_space()
    IL_015f:  add
    IL_0160:  add
    IL_0161:  stloc.s    V_10
    IL_0163:  nop
    IL_0164:  ldloc.s    V_24
    IL_0166:  ldc.i4.1
    IL_0167:  add
    IL_0168:  stloc.s    V_24
    IL_016a:  ldloc.s    V_24
    IL_016c:  ldloc.s    V_11
    IL_016e:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class [BadHygiene]DubsBadHygiene.CompWaterStorage>::get_Count()
    IL_0173:  clt
    IL_0175:  stloc.s    V_25
    IL_0177:  ldloc.s    V_25
    IL_0179:  brtrue.s   IL_0140

    IL_017b:  ldloc.s    V_8
    IL_017d:  ldc.r4     0.0
    IL_0182:  ble.s      IL_0192

    IL_0184:  ldloc.s    V_10
    IL_0186:  ldc.r4     0.0
    IL_018b:  cgt.un
    IL_018d:  ldc.i4.0
    IL_018e:  ceq
    IL_0190:  br.s       IL_0193

    IL_0192:  ldc.i4.1
    IL_0193:  stloc.s    V_26
    IL_0195:  ldloc.s    V_26
    IL_0197:  brfalse.s  IL_019f

    IL_0199:  nop
    IL_019a:  br         IL_0242

    IL_019f:  ldloc.s    V_6
    IL_01a1:  callvirt   instance float32 [BadHygiene]DubsBadHygiene.PlumbingNet::get_WaterStorage()
    IL_01a6:  ldloc.s    V_10
    IL_01a8:  mul
    IL_01a9:  ldloc.s    V_7
    IL_01ab:  callvirt   instance float32 [BadHygiene]DubsBadHygiene.PlumbingNet::get_WaterStorage()
    IL_01b0:  ldloc.s    V_8
    IL_01b2:  mul
    IL_01b3:  sub
    IL_01b4:  ldloc.s    V_8
    IL_01b6:  ldloc.s    V_10
    IL_01b8:  add
    IL_01b9:  div
    IL_01ba:  ldc.r4     0.5
    IL_01bf:  mul
    IL_01c0:  stloc.s    V_12
    IL_01c2:  ldloc.s    V_12
    IL_01c4:  ldc.r4     5.0000001e-02
    IL_01c9:  cgt
    IL_01cb:  stloc.s    V_27
    IL_01cd:  ldloc.s    V_27
    IL_01cf:  brfalse.s  IL_01e1

    IL_01d1:  nop
    IL_01d2:  ldloc.s    V_6
    IL_01d4:  stloc.s    V_13
    IL_01d6:  ldloc.s    V_7
    IL_01d8:  stloc.s    V_14
    IL_01da:  ldloc.s    V_12
    IL_01dc:  stloc.s    V_15
    IL_01de:  nop
    IL_01df:  br.s       IL_0204

    IL_01e1:  ldloc.s    V_12
    IL_01e3:  ldc.r4     -5.0000001e-02
    IL_01e8:  clt
    IL_01ea:  stloc.s    V_28
    IL_01ec:  ldloc.s    V_28
    IL_01ee:  brfalse.s  IL_0201

    IL_01f0:  nop
    IL_01f1:  ldloc.s    V_7
    IL_01f3:  stloc.s    V_13
    IL_01f5:  ldloc.s    V_6
    IL_01f7:  stloc.s    V_14
    IL_01f9:  ldloc.s    V_12
    IL_01fb:  neg
    IL_01fc:  stloc.s    V_15
    IL_01fe:  nop
    IL_01ff:  br.s       IL_0204

    IL_0201:  nop
    IL_0202:  br.s       IL_0242

    IL_0204:  ldloc.s    V_13
    IL_0206:  ldloc.s    V_15
    IL_0208:  ldloca.s   V_30
    IL_020a:  callvirt   instance bool [BadHygiene]DubsBadHygiene.PlumbingNet::PullWater(float32,
                                                                                         valuetype [BadHygiene]DubsBadHygiene.ContaminationLevel&)
    IL_020f:  ldc.i4.0
    IL_0210:  ceq
    IL_0212:  stloc.s    V_29
    IL_0214:  ldloc.s    V_29
    IL_0216:  brfalse.s  IL_021b

    IL_0218:  nop
    IL_0219:  br.s       IL_0242

    IL_021b:  ldloc.s    V_14
    IL_021d:  ldloc.s    V_15
    IL_021f:  callvirt   instance float32 [BadHygiene]DubsBadHygiene.PlumbingNet::PushWater(float32)
    IL_0224:  stloc.s    V_16
    IL_0226:  ldloc.s    V_16
    IL_0228:  ldc.r4     0.0
    IL_022d:  cgt
    IL_022f:  stloc.s    V_31
    IL_0231:  ldloc.s    V_31
    IL_0233:  brfalse.s  IL_0241

    IL_0235:  nop
    IL_0236:  ldloc.s    V_13
    IL_0238:  ldloc.s    V_16
    IL_023a:  callvirt   instance float32 [BadHygiene]DubsBadHygiene.PlumbingNet::PushWater(float32)
    IL_023f:  pop
    IL_0240:  nop
    IL_0241:  nop
    IL_0242:  ldloc.s    V_5
    IL_0244:  ldc.i4.1
    IL_0245:  add
    IL_0246:  stloc.s    V_5
    IL_0248:  ldloc.s    V_5
    IL_024a:  ldloc.2
    IL_024b:  ldlen
    IL_024c:  conv.i4
    IL_024d:  clt
    IL_024f:  stloc.s    V_32
    IL_0251:  ldloc.s    V_32
    IL_0253:  brtrue     IL_0045

    IL_0258:  ret
  } // end of method DBHWaterBridge::BridgePair

} // end of class AsAboveSoBelow.DBHWaterBridge

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file F:\Repositories\rimworld_mods\.tmp\AASB_DBH.res
