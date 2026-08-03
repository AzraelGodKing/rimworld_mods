
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.8.3928.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.


// warning : THIS IS A PARTIAL DISASSEMBLY, NOT SUITABLE FOR RE-ASSEMBLING



.class public auto ansi beforefieldinit DubsBadHygiene.CompWaterStorage
       extends ['Assembly-CSharp']Verse.ThingComp
{
  .custom instance void ['Assembly-CSharp']Verse.StaticConstructorOnStartup::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig virtual instance void 
          CompTick() cil managed
  {
    // Code size       569 (0x239)
    .maxstack  4
    .locals init (float32 V_0,
             float32 V_1)
    IL_0000:  ldarg.0
    IL_0001:  call       instance void ['Assembly-CSharp']Verse.ThingComp::CompTick()
    IL_0006:  ldarg.0
    IL_0007:  call       instance class ['Assembly-CSharp']Verse.IThingHolder ['Assembly-CSharp']Verse.ThingComp::get_ParentHolder()
    IL_000c:  isinst     ['Assembly-CSharp']RimWorld.MinifiedThing
    IL_0011:  brfalse.s  IL_0014

    IL_0013:  ret

    IL_0014:  ldarg.0
    IL_0015:  ldfld      bool DubsBadHygiene.CompWaterStorage::DrainTank
    IL_001a:  brfalse.s  IL_0072

    IL_001c:  ldarg.0
    IL_001d:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0022:  ldc.i4.s   10
    IL_0024:  call       bool ['Assembly-CSharp']Verse.Gen::IsHashIntervalTick(class ['Assembly-CSharp']Verse.Thing,
                                                                               int32)
    IL_0029:  brfalse.s  IL_0071

    IL_002b:  ldarg.0
    IL_002c:  ldfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_0031:  ldc.r4     1.
    IL_0036:  call       float32 [UnityEngine.CoreModule]UnityEngine.Mathf::Min(float32,
                                                                                float32)
    IL_003b:  conv.r4
    IL_003c:  stloc.0
    IL_003d:  ldloc.0
    IL_003e:  ldc.r4     0.0
    IL_0043:  ble.un.s   IL_0071

    IL_0045:  ldarg.0
    IL_0046:  ldarg.0
    IL_0047:  ldfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_004c:  ldloc.0
    IL_004d:  sub
    IL_004e:  stfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_0053:  ldarg.0
    IL_0054:  ldarg.0
    IL_0055:  ldfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_005a:  ldarg.0
    IL_005b:  call       instance class DubsBadHygiene.CompPipe DubsBadHygiene.CompWaterStorage::get_PipeComp()
    IL_0060:  callvirt   instance class DubsBadHygiene.PlumbingNet DubsBadHygiene.CompPipe::get_pipeNet()
    IL_0065:  ldloc.0
    IL_0066:  callvirt   instance float32 DubsBadHygiene.PlumbingNet::PushWater(float32)
    IL_006b:  add
    IL_006c:  stfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_0071:  ret

    IL_0072:  ldarg.0
    IL_0073:  call       instance class DubsBadHygiene.CompProperties_WaterStorage DubsBadHygiene.CompWaterStorage::get_Props()
    IL_0078:  ldfld      bool DubsBadHygiene.CompProperties_WaterStorage::Auto
    IL_007d:  brtrue.s   IL_00ae

    IL_007f:  ldarg.0
    IL_0080:  call       instance class DubsBadHygiene.CompProperties_WaterStorage DubsBadHygiene.CompWaterStorage::get_Props()
    IL_0085:  ldfld      bool DubsBadHygiene.CompProperties_WaterStorage::AutoOnRain
    IL_008a:  brfalse    IL_0159

    IL_008f:  ldarg.0
    IL_0090:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0095:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_009a:  ldfld      class ['Assembly-CSharp']RimWorld.WeatherManager ['Assembly-CSharp']Verse.Map::weatherManager
    IL_009f:  callvirt   instance float32 ['Assembly-CSharp']RimWorld.WeatherManager::get_RainRate()
    IL_00a4:  ldc.r4     9.9999998e-03
    IL_00a9:  ble.un     IL_0159

    IL_00ae:  ldarg.0
    IL_00af:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_00b4:  ldarg.0
    IL_00b5:  call       instance class DubsBadHygiene.CompProperties_WaterStorage DubsBadHygiene.CompWaterStorage::get_Props()
    IL_00ba:  ldfld      int32 DubsBadHygiene.CompProperties_WaterStorage::Tickrate
    IL_00bf:  call       bool ['Assembly-CSharp']Verse.Gen::IsHashIntervalTick(class ['Assembly-CSharp']Verse.Thing,
                                                                               int32)
    IL_00c4:  brfalse    IL_0159

    IL_00c9:  ldarg.0
    IL_00ca:  call       instance bool DubsBadHygiene.CompWaterStorage::get_WorkingNow()
    IL_00cf:  brfalse    IL_0159

    IL_00d4:  ldarg.0
    IL_00d5:  call       instance class DubsBadHygiene.CompProperties_WaterStorage DubsBadHygiene.CompWaterStorage::get_Props()
    IL_00da:  ldfld      float32 DubsBadHygiene.CompProperties_WaterStorage::AutoGenRate
    IL_00df:  stloc.1
    IL_00e0:  ldarg.0
    IL_00e1:  call       instance class DubsBadHygiene.CompProperties_WaterStorage DubsBadHygiene.CompWaterStorage::get_Props()
    IL_00e6:  ldfld      bool DubsBadHygiene.CompProperties_WaterStorage::AutoOnRain
    IL_00eb:  brfalse.s  IL_0105

    IL_00ed:  ldloc.1
    IL_00ee:  ldarg.0
    IL_00ef:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_00f4:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_00f9:  ldfld      class ['Assembly-CSharp']RimWorld.WeatherManager ['Assembly-CSharp']Verse.Map::weatherManager
    IL_00fe:  callvirt   instance float32 ['Assembly-CSharp']RimWorld.WeatherManager::get_RainRate()
    IL_0103:  mul
    IL_0104:  stloc.1
    IL_0105:  ldarg.0
    IL_0106:  ldarg.0
    IL_0107:  ldfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_010c:  ldloc.1
    IL_010d:  add
    IL_010e:  stfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_0113:  ldarg.0
    IL_0114:  ldfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_0119:  ldarg.0
    IL_011a:  call       instance class DubsBadHygiene.CompProperties_WaterStorage DubsBadHygiene.CompWaterStorage::get_Props()
    IL_011f:  ldfld      float32 DubsBadHygiene.CompProperties_WaterStorage::WaterStorageCap
    IL_0124:  ble.un.s   IL_0137

    IL_0126:  ldarg.0
    IL_0127:  ldarg.0
    IL_0128:  call       instance class DubsBadHygiene.CompProperties_WaterStorage DubsBadHygiene.CompWaterStorage::get_Props()
    IL_012d:  ldfld      float32 DubsBadHygiene.CompProperties_WaterStorage::WaterStorageCap
    IL_0132:  stfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_0137:  ldarg.0
    IL_0138:  ldfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_013d:  call       bool [mscorlib]System.Single::IsNaN(float32)
    IL_0142:  brfalse.s  IL_0159

    IL_0144:  ldarg.0
    IL_0145:  ldc.r4     0.0
    IL_014a:  stfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_014f:  ldstr      "NaN on WaterStorage in CompTick1"
    IL_0154:  call       void ['Assembly-CSharp']Verse.Log::Warning(string)
    IL_0159:  ldarg.0
    IL_015a:  call       instance class DubsBadHygiene.CompPipe DubsBadHygiene.CompWaterStorage::get_PipeComp()
    IL_015f:  dup
    IL_0160:  brtrue.s   IL_0166

    IL_0162:  pop
    IL_0163:  ldnull
    IL_0164:  br.s       IL_016b

    IL_0166:  call       instance class DubsBadHygiene.PlumbingNet DubsBadHygiene.CompPipe::get_pipeNet()
    IL_016b:  brfalse    IL_0238

    IL_0170:  ldarg.0
    IL_0171:  ldarg.0
    IL_0172:  ldfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_0177:  ldarg.0
    IL_0178:  call       instance class DubsBadHygiene.CompPipe DubsBadHygiene.CompWaterStorage::get_PipeComp()
    IL_017d:  callvirt   instance class DubsBadHygiene.PlumbingNet DubsBadHygiene.CompPipe::get_pipeNet()
    IL_0182:  ldfld      float32 DubsBadHygiene.PlumbingNet::WaterPumpedPerTick
    IL_0187:  add
    IL_0188:  stfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_018d:  ldsfld     class DubsBadHygiene.DubsModOptions DubsBadHygiene.ModOption::WaterPumpCapacity
    IL_0192:  ldfld      float32 DubsBadHygiene.DubsModOptions::Val
    IL_0197:  ldc.r4     0.0
    IL_019c:  beq.s      IL_01b1

    IL_019e:  ldarg.0
    IL_019f:  ldfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_01a4:  ldarg.0
    IL_01a5:  call       instance class DubsBadHygiene.CompProperties_WaterStorage DubsBadHygiene.CompWaterStorage::get_Props()
    IL_01aa:  ldfld      float32 DubsBadHygiene.CompProperties_WaterStorage::WaterStorageCap
    IL_01af:  ble.un.s   IL_01c2

    IL_01b1:  ldarg.0
    IL_01b2:  ldarg.0
    IL_01b3:  call       instance class DubsBadHygiene.CompProperties_WaterStorage DubsBadHygiene.CompWaterStorage::get_Props()
    IL_01b8:  ldfld      float32 DubsBadHygiene.CompProperties_WaterStorage::WaterStorageCap
    IL_01bd:  stfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_01c2:  ldarg.0
    IL_01c3:  ldfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_01c8:  ldc.r4     0.0
    IL_01cd:  bge.un.s   IL_01da

    IL_01cf:  ldarg.0
    IL_01d0:  ldc.r4     0.0
    IL_01d5:  stfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_01da:  ldarg.0
    IL_01db:  ldfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_01e0:  call       bool [mscorlib]System.Single::IsNaN(float32)
    IL_01e5:  brfalse.s  IL_01fc

    IL_01e7:  ldarg.0
    IL_01e8:  ldc.r4     0.0
    IL_01ed:  stfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_01f2:  ldstr      "NaN on WaterStorage in CompTick2"
    IL_01f7:  call       void ['Assembly-CSharp']Verse.Log::Warning(string)
    IL_01fc:  ldarg.0
    IL_01fd:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0202:  ldc.i4     0x1388
    IL_0207:  call       bool ['Assembly-CSharp']Verse.Gen::IsHashIntervalTick(class ['Assembly-CSharp']Verse.Thing,
                                                                               int32)
    IL_020c:  brfalse.s  IL_0238

    IL_020e:  ldarg.0
    IL_020f:  call       instance class DubsBadHygiene.CompPipe DubsBadHygiene.CompWaterStorage::get_PipeComp()
    IL_0214:  callvirt   instance class DubsBadHygiene.PlumbingNet DubsBadHygiene.CompPipe::get_pipeNet()
    IL_0219:  callvirt   instance bool DubsBadHygiene.PlumbingNet::get_HasFilter()
    IL_021e:  brfalse.s  IL_0228

    IL_0220:  ldarg.0
    IL_0221:  ldc.i4.0
    IL_0222:  stfld      valuetype DubsBadHygiene.ContaminationLevel DubsBadHygiene.CompWaterStorage::WaterQuality
    IL_0227:  ret

    IL_0228:  ldarg.0
    IL_0229:  ldfld      valuetype DubsBadHygiene.ContaminationLevel DubsBadHygiene.CompWaterStorage::WaterQuality
    IL_022e:  ldc.i4.2
    IL_022f:  beq.s      IL_0238

    IL_0231:  ldarg.0
    IL_0232:  ldc.i4.1
    IL_0233:  stfld      valuetype DubsBadHygiene.ContaminationLevel DubsBadHygiene.CompWaterStorage::WaterQuality
    IL_0238:  ret
  } // end of method CompWaterStorage::CompTick

} // end of class DubsBadHygiene.CompWaterStorage

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file F:\Repositories\rimworld_mods\.tmp\dbh_wstor_tick.res
