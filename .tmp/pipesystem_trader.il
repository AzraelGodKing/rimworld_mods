
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.8.3928.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.


// warning : THIS IS A PARTIAL DISASSEMBLY, NOT SUITABLE FOR RE-ASSEMBLING



.class public auto ansi beforefieldinit PipeSystem.CompResourceTrader
       extends PipeSystem.CompResource
{
  .field private bool resourceOnInt
  .field private bool lowPowerModeOnInt
  .field private class ['Assembly-CSharp']Verse.Sound.Sustainer sustainerResourceOn
  .field family class ['Assembly-CSharp']RimWorld.CompFlickable compFlickable
  .field family class ['Assembly-CSharp']RimWorld.CompSchedule compSchedule
  .field family class ['Assembly-CSharp']RimWorld.CompBreakdownable compBreakdownable
  .field family class ['Assembly-CSharp']RimWorld.CompRefuelable compRefuelable
  .field family class ['Assembly-CSharp']RimWorld.CompPowerTrader compPowerTrader
  .field family class PipeSystem.PipeNetOverlayDrawer pipeNetOverlayDrawer
  .field private float32 '<BaseConsumption>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .field private bool '<UsedLastTick>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig specialname instance class PipeSystem.CompProperties_ResourceTrader 
          get_Props() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class ['Assembly-CSharp']Verse.CompProperties ['Assembly-CSharp']Verse.ThingComp::props
    IL_0006:  castclass  PipeSystem.CompProperties_ResourceTrader
    IL_000b:  ret
  } // end of method CompResourceTrader::get_Props

  .method public hidebysig specialname instance string 
          get_OnSignal() cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  8
    IL_0000:  ldstr      "Resource"
    IL_0005:  ldarg.0
    IL_0006:  call       instance class PipeSystem.Resource PipeSystem.CompResource::get_Resource()
    IL_000b:  ldfld      string PipeSystem.Resource::name
    IL_0010:  ldstr      "TurnedOn"
    IL_0015:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_001a:  ret
  } // end of method CompResourceTrader::get_OnSignal

  .method public hidebysig specialname instance string 
          get_OffSignal() cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  8
    IL_0000:  ldstr      "Resource"
    IL_0005:  ldarg.0
    IL_0006:  call       instance class PipeSystem.Resource PipeSystem.CompResource::get_Resource()
    IL_000b:  ldfld      string PipeSystem.Resource::name
    IL_0010:  ldstr      "TurnedOff"
    IL_0015:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_001a:  ret
  } // end of method CompResourceTrader::get_OffSignal

  .method public hidebysig specialname instance float32 
          get_BaseConsumption() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float32 PipeSystem.CompResourceTrader::'<BaseConsumption>k__BackingField'
    IL_0006:  ret
  } // end of method CompResourceTrader::get_BaseConsumption

  .method public hidebysig specialname instance void 
          set_BaseConsumption(float32 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      float32 PipeSystem.CompResourceTrader::'<BaseConsumption>k__BackingField'
    IL_0007:  ret
  } // end of method CompResourceTrader::set_BaseConsumption

  .method public hidebysig specialname instance bool 
          get_UsedLastTick() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool PipeSystem.CompResourceTrader::'<UsedLastTick>k__BackingField'
    IL_0006:  ret
  } // end of method CompResourceTrader::get_UsedLastTick

  .method public hidebysig specialname instance void 
          set_UsedLastTick(bool 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      bool PipeSystem.CompResourceTrader::'<UsedLastTick>k__BackingField'
    IL_0007:  ret
  } // end of method CompResourceTrader::set_UsedLastTick

  .method public hidebysig specialname instance float32 
          get_Consumption() cil managed
  {
    // Code size       52 (0x34)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_0006:  ldfld      float32 PipeSystem.CompProperties_ResourceTrader::idleConsumptionPerTick
    IL_000b:  ldc.r4     0.0
    IL_0010:  bge.s      IL_0019

    IL_0012:  ldarg.0
    IL_0013:  call       instance float32 PipeSystem.CompResourceTrader::get_BaseConsumption()
    IL_0018:  ret

    IL_0019:  ldarg.0
    IL_001a:  call       instance bool PipeSystem.CompResourceTrader::get_UsedLastTick()
    IL_001f:  brtrue.s   IL_002d

    IL_0021:  ldarg.0
    IL_0022:  call       instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_0027:  ldfld      float32 PipeSystem.CompProperties_ResourceTrader::idleConsumptionPerTick
    IL_002c:  ret

    IL_002d:  ldarg.0
    IL_002e:  call       instance float32 PipeSystem.CompResourceTrader::get_BaseConsumption()
    IL_0033:  ret
  } // end of method CompResourceTrader::get_Consumption

  .method public hidebysig specialname instance bool 
          get_ResourceOn() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool PipeSystem.CompResourceTrader::resourceOnInt
    IL_0006:  ret
  } // end of method CompResourceTrader::get_ResourceOn

  .method public hidebysig specialname instance void 
          set_ResourceOn(bool 'value') cil managed
  {
    // Code size       214 (0xd6)
    .maxstack  5
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool PipeSystem.CompResourceTrader::resourceOnInt
    IL_0006:  ldarg.1
    IL_0007:  bne.un.s   IL_000a

    IL_0009:  ret

    IL_000a:  ldarg.0
    IL_000b:  ldarg.1
    IL_000c:  stfld      bool PipeSystem.CompResourceTrader::resourceOnInt
    IL_0011:  ldarg.0
    IL_0012:  ldfld      bool PipeSystem.CompResourceTrader::resourceOnInt
    IL_0017:  brfalse.s  IL_0082

    IL_0019:  ldarg.0
    IL_001a:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_001f:  call       bool ['Assembly-CSharp']RimWorld.FlickUtility::WantsToBeOn(class ['Assembly-CSharp']Verse.Thing)
    IL_0024:  brtrue.s   IL_0041

    IL_0026:  ldstr      "Tried to turn resource on "
    IL_002b:  ldarg.0
    IL_002c:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0031:  ldstr      " which did not desire it."
    IL_0036:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_003b:  call       void ['Assembly-CSharp']Verse.Log::Warning(string)
    IL_0040:  ret

    IL_0041:  ldarg.0
    IL_0042:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0047:  call       bool ['Assembly-CSharp']RimWorld.BreakdownableUtility::IsBrokenDown(class ['Assembly-CSharp']Verse.Thing)
    IL_004c:  brfalse.s  IL_0069

    IL_004e:  ldstr      "Tried to turn resource on "
    IL_0053:  ldarg.0
    IL_0054:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0059:  ldstr      " which is broken down."
    IL_005e:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0063:  call       void ['Assembly-CSharp']Verse.Log::Warning(string)
    IL_0068:  ret

    IL_0069:  ldarg.0
    IL_006a:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_006f:  ldarg.0
    IL_0070:  call       instance string PipeSystem.CompResourceTrader::get_OnSignal()
    IL_0075:  callvirt   instance void ['Assembly-CSharp']Verse.ThingWithComps::BroadcastCompSignal(string)
    IL_007a:  ldarg.0
    IL_007b:  call       instance void PipeSystem.CompResourceTrader::StartSustainerIfInactive()
    IL_0080:  br.s       IL_0099

    IL_0082:  ldarg.0
    IL_0083:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0088:  ldarg.0
    IL_0089:  call       instance string PipeSystem.CompResourceTrader::get_OffSignal()
    IL_008e:  callvirt   instance void ['Assembly-CSharp']Verse.ThingWithComps::BroadcastCompSignal(string)
    IL_0093:  ldarg.0
    IL_0094:  call       instance void PipeSystem.CompResourceTrader::EndSustainerIfActive()
    IL_0099:  ldarg.0
    IL_009a:  call       instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_009f:  ldfld      bool PipeSystem.CompProperties_ResourceTrader::resourceOffOverlay
    IL_00a4:  brfalse.s  IL_00d5

    IL_00a6:  ldarg.0
    IL_00a7:  ldfld      class PipeSystem.PipeNetOverlayDrawer PipeSystem.CompResourceTrader::pipeNetOverlayDrawer
    IL_00ac:  dup
    IL_00ad:  brtrue.s   IL_00b1

    IL_00af:  pop
    IL_00b0:  ret

    IL_00b1:  ldarg.0
    IL_00b2:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_00b7:  ldarg.0
    IL_00b8:  call       instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_00bd:  ldfld      class PipeSystem.PipeNetDef PipeSystem.CompProperties_Resource::pipeNet
    IL_00c2:  ldfld      class [UnityEngine.CoreModule]UnityEngine.Material PipeSystem.PipeNetDef::offMat
    IL_00c7:  ldarg.0
    IL_00c8:  ldfld      bool PipeSystem.CompResourceTrader::resourceOnInt
    IL_00cd:  ldc.i4.0
    IL_00ce:  ceq
    IL_00d0:  call       instance void PipeSystem.PipeNetOverlayDrawer::TogglePulsing(class ['Assembly-CSharp']Verse.Thing,
                                                                                      class [UnityEngine.CoreModule]UnityEngine.Material,
                                                                                      bool)
    IL_00d5:  ret
  } // end of method CompResourceTrader::set_ResourceOn

  .method public hidebysig specialname instance bool 
          get_LowPowerModeOn() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool PipeSystem.CompResourceTrader::lowPowerModeOnInt
    IL_0006:  ret
  } // end of method CompResourceTrader::get_LowPowerModeOn

  .method public hidebysig specialname instance void 
          set_LowPowerModeOn(bool 'value') cil managed
  {
    // Code size       93 (0x5d)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  call       instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_0006:  callvirt   instance bool PipeSystem.CompProperties_ResourceTrader::get_EverHasLowPowerMode()
    IL_000b:  brfalse.s  IL_0016

    IL_000d:  ldarg.0
    IL_000e:  ldfld      bool PipeSystem.CompResourceTrader::lowPowerModeOnInt
    IL_0013:  ldarg.1
    IL_0014:  bne.un.s   IL_0017

    IL_0016:  ret

    IL_0017:  ldarg.0
    IL_0018:  ldfld      class ['Assembly-CSharp']RimWorld.CompPowerTrader PipeSystem.CompResource::powerComp
    IL_001d:  ldarg.1
    IL_001e:  brtrue.s   IL_0033

    IL_0020:  ldarg.0
    IL_0021:  ldfld      class ['Assembly-CSharp']RimWorld.CompPowerTrader PipeSystem.CompResource::powerComp
    IL_0026:  callvirt   instance class ['Assembly-CSharp']RimWorld.CompProperties_Power ['Assembly-CSharp']RimWorld.CompPower::get_Props()
    IL_002b:  callvirt   instance float32 ['Assembly-CSharp']RimWorld.CompProperties_Power::get_PowerConsumption()
    IL_0030:  neg
    IL_0031:  br.s       IL_0044

    IL_0033:  ldarg.0
    IL_0034:  ldfld      class ['Assembly-CSharp']RimWorld.CompPowerTrader PipeSystem.CompResource::powerComp
    IL_0039:  callvirt   instance class ['Assembly-CSharp']RimWorld.CompProperties_Power ['Assembly-CSharp']RimWorld.CompPower::get_Props()
    IL_003e:  ldfld      float32 ['Assembly-CSharp']RimWorld.CompProperties_Power::idlePowerDraw
    IL_0043:  neg
    IL_0044:  callvirt   instance void ['Assembly-CSharp']RimWorld.CompPowerTrader::set_PowerOutput(float32)
    IL_0049:  ldarg.0
    IL_004a:  ldarg.1
    IL_004b:  stfld      bool PipeSystem.CompResourceTrader::lowPowerModeOnInt
    IL_0050:  ldarg.0
    IL_0051:  callvirt   instance class PipeSystem.PipeNet PipeSystem.CompResource::get_PipeNet()
    IL_0056:  ldarg.0
    IL_0057:  callvirt   instance void PipeSystem.PipeNet::UpdateLowPowerModeTrader(class PipeSystem.CompResourceTrader)
    IL_005c:  ret
  } // end of method CompResourceTrader::set_LowPowerModeOn

  .method public hidebysig newslot specialname virtual 
          instance bool  get_ShouldBeLowPowerMode() cil managed
  {
    // Code size       104 (0x68)
    .maxstack  2
    .locals init (class ['Assembly-CSharp']RimWorld.CompRefuelable V_0)
    IL_0000:  ldarg.0
    IL_0001:  call       instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_0006:  ldc.r4     0.0
    IL_000b:  bge.un.s   IL_0040

    IL_000d:  ldarg.0
    IL_000e:  call       instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_0013:  ldfld      bool PipeSystem.CompProperties_ResourceTrader::producerLowPowerWhenStorageFull
    IL_0018:  brfalse.s  IL_0040

    IL_001a:  ldarg.0
    IL_001b:  callvirt   instance class PipeSystem.PipeNet PipeSystem.CompResource::get_PipeNet()
    IL_0020:  callvirt   instance float32 PipeSystem.PipeNet::get_AvailableCapacityLastTick()
    IL_0025:  ldc.r4     0.0
    IL_002a:  bgt.un.s   IL_0040

    IL_002c:  ldarg.0
    IL_002d:  callvirt   instance class PipeSystem.PipeNet PipeSystem.CompResource::get_PipeNet()
    IL_0032:  callvirt   instance float32 PipeSystem.PipeNet::get_OverflowAmount()
    IL_0037:  ldc.r4     0.0
    IL_003c:  ble.un.s   IL_0040

    IL_003e:  ldc.i4.1
    IL_003f:  ret

    IL_0040:  ldarg.0
    IL_0041:  call       instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_0046:  ldfld      bool PipeSystem.CompProperties_ResourceTrader::lowPowerWhenRefuelableEmpty
    IL_004b:  brfalse.s  IL_0066

    IL_004d:  ldarg.0
    IL_004e:  ldfld      class ['Assembly-CSharp']RimWorld.CompRefuelable PipeSystem.CompResourceTrader::compRefuelable
    IL_0053:  stloc.0
    IL_0054:  ldloc.0
    IL_0055:  brfalse.s  IL_0066

    IL_0057:  ldloc.0
    IL_0058:  callvirt   instance float32 ['Assembly-CSharp']RimWorld.CompRefuelable::get_Fuel()
    IL_005d:  ldc.r4     0.0
    IL_0062:  bgt.un.s   IL_0066

    IL_0064:  ldc.i4.1
    IL_0065:  ret

    IL_0066:  ldc.i4.0
    IL_0067:  ret
  } // end of method CompResourceTrader::get_ShouldBeLowPowerMode

  .method public hidebysig virtual instance void 
          PostSpawnSetup(bool respawningAfterLoad) cil managed
  {
    // Code size       116 (0x74)
    .maxstack  5
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0007:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_000c:  callvirt   instance !!0 ['Assembly-CSharp']Verse.Map::GetComponent<class PipeSystem.PipeNetOverlayDrawer>()
    IL_0011:  stfld      class PipeSystem.PipeNetOverlayDrawer PipeSystem.CompResourceTrader::pipeNetOverlayDrawer
    IL_0016:  ldarg.0
    IL_0017:  call       instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_001c:  ldfld      bool PipeSystem.CompProperties_ResourceTrader::resourceOffOverlay
    IL_0021:  brfalse.s  IL_0053

    IL_0023:  ldarg.0
    IL_0024:  ldfld      class PipeSystem.PipeNetOverlayDrawer PipeSystem.CompResourceTrader::pipeNetOverlayDrawer
    IL_0029:  dup
    IL_002a:  brtrue.s   IL_002f

    IL_002c:  pop
    IL_002d:  br.s       IL_0053

    IL_002f:  ldarg.0
    IL_0030:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0035:  ldarg.0
    IL_0036:  call       instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_003b:  ldfld      class PipeSystem.PipeNetDef PipeSystem.CompProperties_Resource::pipeNet
    IL_0040:  ldfld      class [UnityEngine.CoreModule]UnityEngine.Material PipeSystem.PipeNetDef::offMat
    IL_0045:  ldarg.0
    IL_0046:  ldfld      bool PipeSystem.CompResourceTrader::resourceOnInt
    IL_004b:  ldc.i4.0
    IL_004c:  ceq
    IL_004e:  call       instance void PipeSystem.PipeNetOverlayDrawer::TogglePulsing(class ['Assembly-CSharp']Verse.Thing,
                                                                                      class [UnityEngine.CoreModule]UnityEngine.Material,
                                                                                      bool)
    IL_0053:  ldarg.0
    IL_0054:  call       instance bool PipeSystem.CompResourceTrader::get_ResourceOn()
    IL_0059:  brfalse.s  IL_006c

    IL_005b:  ldarg.0
    IL_005c:  ldftn      instance void PipeSystem.CompResourceTrader::'<PostSpawnSetup>b__33_0'()
    IL_0062:  newobj     instance void [mscorlib]System.Action::.ctor(object,
                                                                      native int)
    IL_0067:  call       void ['Assembly-CSharp']Verse.LongEventHandler::ExecuteWhenFinished(class [mscorlib]System.Action)
    IL_006c:  ldarg.0
    IL_006d:  ldarg.1
    IL_006e:  call       instance void PipeSystem.CompResource::PostSpawnSetup(bool)
    IL_0073:  ret
  } // end of method CompResourceTrader::PostSpawnSetup

  .method public hidebysig virtual instance void 
          PostPostMake() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void PipeSystem.CompResourceTrader::InitializeComps()
    IL_0006:  ldarg.0
    IL_0007:  call       instance void ['Assembly-CSharp']Verse.ThingComp::PostPostMake()
    IL_000c:  ret
  } // end of method CompResourceTrader::PostPostMake

  .method public hidebysig virtual instance void 
          PostDeSpawn(class ['Assembly-CSharp']Verse.Map map,
                      [opt] valuetype ['Assembly-CSharp']Verse.DestroyMode mode) cil managed
  {
    .param [2] = uint8(0x00)
    // Code size       68 (0x44)
    .maxstack  4
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  call       instance void PipeSystem.CompResource::PostDeSpawn(class ['Assembly-CSharp']Verse.Map,
                                                                            valuetype ['Assembly-CSharp']Verse.DestroyMode)
    IL_0008:  ldarg.0
    IL_0009:  call       instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_000e:  ldfld      bool PipeSystem.CompProperties_ResourceTrader::resourceOffOverlay
    IL_0013:  brfalse.s  IL_003d

    IL_0015:  ldarg.0
    IL_0016:  ldfld      class PipeSystem.PipeNetOverlayDrawer PipeSystem.CompResourceTrader::pipeNetOverlayDrawer
    IL_001b:  dup
    IL_001c:  brtrue.s   IL_0021

    IL_001e:  pop
    IL_001f:  br.s       IL_003d

    IL_0021:  ldarg.0
    IL_0022:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0027:  ldarg.0
    IL_0028:  call       instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_002d:  ldfld      class PipeSystem.PipeNetDef PipeSystem.CompProperties_Resource::pipeNet
    IL_0032:  ldfld      class [UnityEngine.CoreModule]UnityEngine.Material PipeSystem.PipeNetDef::offMat
    IL_0037:  ldc.i4.0
    IL_0038:  call       instance void PipeSystem.PipeNetOverlayDrawer::TogglePulsing(class ['Assembly-CSharp']Verse.Thing,
                                                                                      class [UnityEngine.CoreModule]UnityEngine.Material,
                                                                                      bool)
    IL_003d:  ldarg.0
    IL_003e:  call       instance void PipeSystem.CompResourceTrader::EndSustainerIfActive()
    IL_0043:  ret
  } // end of method CompResourceTrader::PostDeSpawn

  .method public hidebysig virtual instance void 
          PostExposeData() cil managed
  {
    // Code size       39 (0x27)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void ['Assembly-CSharp']Verse.ThingComp::PostExposeData()
    IL_0006:  ldarg.0
    IL_0007:  ldflda     bool PipeSystem.CompResourceTrader::resourceOnInt
    IL_000c:  ldstr      "resourceOn"
    IL_0011:  ldc.i4.1
    IL_0012:  ldc.i4.0
    IL_0013:  call       void ['Assembly-CSharp']Verse.Scribe_Values::Look<bool>(!!0&,
                                                                                 string,
                                                                                 !!0,
                                                                                 bool)
    IL_0018:  ldsfld     valuetype ['Assembly-CSharp']Verse.LoadSaveMode ['Assembly-CSharp']Verse.Scribe::mode
    IL_001d:  ldc.i4.2
    IL_001e:  bne.un.s   IL_0026

    IL_0020:  ldarg.0
    IL_0021:  call       instance void PipeSystem.CompResourceTrader::InitializeComps()
    IL_0026:  ret
  } // end of method CompResourceTrader::PostExposeData

  .method public hidebysig virtual instance string 
          CompInspectStringExtra() cil managed
  {
    // Code size       246 (0xf6)
    .maxstack  5
    .locals init (class [mscorlib]System.Text.StringBuilder V_0)
    IL_0000:  newobj     instance void [mscorlib]System.Text.StringBuilder::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldarg.0
    IL_0007:  call       instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_000c:  ldc.r4     0.0
    IL_0011:  blt.un.s   IL_0067

    IL_0013:  ldloc.0
    IL_0014:  ldstr      "{0} {1:##0} {2}/d"
    IL_0019:  ldstr      "PipeSystem_ResourceNeeded"
    IL_001e:  ldarg.0
    IL_001f:  call       instance class PipeSystem.Resource PipeSystem.CompResource::get_Resource()
    IL_0024:  ldfld      string PipeSystem.Resource::name
    IL_0029:  call       valuetype ['Assembly-CSharp']Verse.NamedArgument ['Assembly-CSharp']Verse.NamedArgument::op_Implicit(string)
    IL_002e:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.TranslatorFormattedStringExtensions::Translate(string,
                                                                                                                                                 valuetype ['Assembly-CSharp']Verse.NamedArgument)
    IL_0033:  box        ['Assembly-CSharp']Verse.TaggedString
    IL_0038:  ldarg.0
    IL_0039:  call       instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_003e:  ldc.r4     100.
    IL_0043:  div
    IL_0044:  ldc.r4     60000.
    IL_0049:  mul
    IL_004a:  box        [mscorlib]System.Single
    IL_004f:  ldarg.0
    IL_0050:  call       instance class PipeSystem.Resource PipeSystem.CompResource::get_Resource()
    IL_0055:  ldfld      string PipeSystem.Resource::unit
    IL_005a:  call       string [mscorlib]System.String::Format(string,
                                                                object,
                                                                object,
                                                                object)
    IL_005f:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::Append(string)
    IL_0064:  pop
    IL_0065:  br.s       IL_00c9

    IL_0067:  ldloc.0
    IL_0068:  ldstr      "{0} {1:##0} {2}/d"
    IL_006d:  ldstr      "PipeSystem_ResourceOutput"
    IL_0072:  ldarg.0
    IL_0073:  call       instance class PipeSystem.Resource PipeSystem.CompResource::get_Resource()
    IL_0078:  ldfld      string PipeSystem.Resource::name
    IL_007d:  call       valuetype ['Assembly-CSharp']Verse.NamedArgument ['Assembly-CSharp']Verse.NamedArgument::op_Implicit(string)
    IL_0082:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.TranslatorFormattedStringExtensions::Translate(string,
                                                                                                                                                 valuetype ['Assembly-CSharp']Verse.NamedArgument)
    IL_0087:  box        ['Assembly-CSharp']Verse.TaggedString
    IL_008c:  ldarg.0
    IL_008d:  call       instance bool PipeSystem.CompResourceTrader::get_ResourceOn()
    IL_0092:  brtrue.s   IL_009b

    IL_0094:  ldc.r4     0.0
    IL_0099:  br.s       IL_00ae

    IL_009b:  ldarg.0
    IL_009c:  call       instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_00a1:  neg
    IL_00a2:  ldc.r4     100.
    IL_00a7:  div
    IL_00a8:  ldc.r4     60000.
    IL_00ad:  mul
    IL_00ae:  box        [mscorlib]System.Single
    IL_00b3:  ldarg.0
    IL_00b4:  call       instance class PipeSystem.Resource PipeSystem.CompResource::get_Resource()
    IL_00b9:  ldfld      string PipeSystem.Resource::unit
    IL_00be:  call       string [mscorlib]System.String::Format(string,
                                                                object,
                                                                object,
                                                                object)
    IL_00c3:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::Append(string)
    IL_00c8:  pop
    IL_00c9:  ldloc.0
    IL_00ca:  ldarg.0
    IL_00cb:  call       instance string PipeSystem.CompResource::CompInspectStringExtra()
    IL_00d0:  call       class [mscorlib]System.Text.StringBuilder ['Assembly-CSharp']Verse.GenText::AppendInNewLine(class [mscorlib]System.Text.StringBuilder,
                                                                                                                     string)
    IL_00d5:  pop
    IL_00d6:  ldsfld     bool ['Assembly-CSharp']Verse.DebugSettings::godMode
    IL_00db:  brfalse.s  IL_00ea

    IL_00dd:  ldloc.0
    IL_00de:  ldarg.0
    IL_00df:  call       instance string PipeSystem.CompResourceTrader::get_DebugString()
    IL_00e4:  call       class [mscorlib]System.Text.StringBuilder ['Assembly-CSharp']Verse.GenText::AppendInNewLine(class [mscorlib]System.Text.StringBuilder,
                                                                                                                     string)
    IL_00e9:  pop
    IL_00ea:  ldloc.0
    IL_00eb:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_00f0:  callvirt   instance string [mscorlib]System.String::Trim()
    IL_00f5:  ret
  } // end of method CompResourceTrader::CompInspectStringExtra

  .method private hidebysig instance void 
          StartSustainerIfInactive() cil managed
  {
    // Code size       67 (0x43)
    .maxstack  4
    IL_0000:  ldarg.0
    IL_0001:  call       instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_0006:  ldfld      class ['Assembly-CSharp']Verse.SoundDef PipeSystem.CompProperties_ResourceTrader::soundAmbientReceivingResource
    IL_000b:  call       bool ['Assembly-CSharp']Verse.SoundDefHelper::NullOrUndefined(class ['Assembly-CSharp']Verse.SoundDef)
    IL_0010:  brtrue.s   IL_001a

    IL_0012:  ldarg.0
    IL_0013:  ldfld      class ['Assembly-CSharp']Verse.Sound.Sustainer PipeSystem.CompResourceTrader::sustainerResourceOn
    IL_0018:  brfalse.s  IL_001b

    IL_001a:  ret

    IL_001b:  ldarg.0
    IL_001c:  ldarg.0
    IL_001d:  call       instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_0022:  ldfld      class ['Assembly-CSharp']Verse.SoundDef PipeSystem.CompProperties_ResourceTrader::soundAmbientReceivingResource
    IL_0027:  ldarg.0
    IL_0028:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_002d:  call       valuetype ['Assembly-CSharp']Verse.TargetInfo ['Assembly-CSharp']Verse.TargetInfo::op_Implicit(class ['Assembly-CSharp']Verse.Thing)
    IL_0032:  ldc.i4.0
    IL_0033:  call       valuetype ['Assembly-CSharp']Verse.Sound.SoundInfo ['Assembly-CSharp']Verse.Sound.SoundInfo::InMap(valuetype ['Assembly-CSharp']Verse.TargetInfo,
                                                                                                                            valuetype ['Assembly-CSharp']Verse.Sound.MaintenanceType)
    IL_0038:  call       class ['Assembly-CSharp']Verse.Sound.Sustainer ['Assembly-CSharp']Verse.Sound.SoundStarter::TrySpawnSustainer(class ['Assembly-CSharp']Verse.SoundDef,
                                                                                                                                       valuetype ['Assembly-CSharp']Verse.Sound.SoundInfo)
    IL_003d:  stfld      class ['Assembly-CSharp']Verse.Sound.Sustainer PipeSystem.CompResourceTrader::sustainerResourceOn
    IL_0042:  ret
  } // end of method CompResourceTrader::StartSustainerIfInactive

  .method private hidebysig instance void 
          EndSustainerIfActive() cil managed
  {
    // Code size       28 (0x1c)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class ['Assembly-CSharp']Verse.Sound.Sustainer PipeSystem.CompResourceTrader::sustainerResourceOn
    IL_0006:  brtrue.s   IL_0009

    IL_0008:  ret

    IL_0009:  ldarg.0
    IL_000a:  ldfld      class ['Assembly-CSharp']Verse.Sound.Sustainer PipeSystem.CompResourceTrader::sustainerResourceOn
    IL_000f:  callvirt   instance void ['Assembly-CSharp']Verse.Sound.Sustainer::End()
    IL_0014:  ldarg.0
    IL_0015:  ldnull
    IL_0016:  stfld      class ['Assembly-CSharp']Verse.Sound.Sustainer PipeSystem.CompResourceTrader::sustainerResourceOn
    IL_001b:  ret
  } // end of method CompResourceTrader::EndSustainerIfActive

  .method public hidebysig virtual instance void 
          CompTick() cil managed
  {
    // Code size       297 (0x129)
    .maxstack  4
    IL_0000:  ldarg.0
    IL_0001:  call       instance void ['Assembly-CSharp']Verse.ThingComp::CompTick()
    IL_0006:  ldarg.0
    IL_0007:  call       instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_000c:  ldfld      bool PipeSystem.CompProperties_ResourceTrader::handleCompRefuelableTicking
    IL_0011:  brfalse    IL_00af

    IL_0016:  ldarg.0
    IL_0017:  ldarg.0
    IL_0018:  ldfld      class ['Assembly-CSharp']RimWorld.CompRefuelable PipeSystem.CompResourceTrader::compRefuelable
    IL_001d:  callvirt   instance class ['Assembly-CSharp']RimWorld.CompProperties_Refuelable ['Assembly-CSharp']RimWorld.CompRefuelable::get_Props()
    IL_0022:  ldfld      float32 ['Assembly-CSharp']RimWorld.CompProperties_Refuelable::fuelConsumptionRate
    IL_0027:  ldc.r4     60000.
    IL_002c:  div
    IL_002d:  callvirt   instance void PipeSystem.CompResourceTrader::HandleRefuelableTicking(float32)
    IL_0032:  ldarg.0
    IL_0033:  ldfld      class ['Assembly-CSharp']RimWorld.CompRefuelable PipeSystem.CompResourceTrader::compRefuelable
    IL_0038:  callvirt   instance class ['Assembly-CSharp']RimWorld.CompProperties_Refuelable ['Assembly-CSharp']RimWorld.CompRefuelable::get_Props()
    IL_003d:  ldfld      float32 ['Assembly-CSharp']RimWorld.CompProperties_Refuelable::fuelConsumptionPerTickInRain
    IL_0042:  ldc.r4     0.0
    IL_0047:  ble.un.s   IL_00af

    IL_0049:  ldarg.0
    IL_004a:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_004f:  callvirt   instance bool ['Assembly-CSharp']Verse.Thing::get_Spawned()
    IL_0054:  brfalse.s  IL_00af

    IL_0056:  ldarg.0
    IL_0057:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_005c:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_0061:  ldfld      class ['Assembly-CSharp']RimWorld.WeatherManager ['Assembly-CSharp']Verse.Map::weatherManager
    IL_0066:  callvirt   instance float32 ['Assembly-CSharp']RimWorld.WeatherManager::get_RainRate()
    IL_006b:  ldc.r4     0.40000001
    IL_0070:  ble.un.s   IL_00af

    IL_0072:  ldarg.0
    IL_0073:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0078:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_007d:  ldfld      class ['Assembly-CSharp']Verse.RoofGrid ['Assembly-CSharp']Verse.Map::roofGrid
    IL_0082:  ldarg.0
    IL_0083:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0088:  callvirt   instance valuetype ['Assembly-CSharp']Verse.IntVec3 ['Assembly-CSharp']Verse.Thing::get_Position()
    IL_008d:  callvirt   instance bool ['Assembly-CSharp']Verse.RoofGrid::Roofed(valuetype ['Assembly-CSharp']Verse.IntVec3)
    IL_0092:  brtrue.s   IL_00af

    IL_0094:  ldarg.0
    IL_0095:  ldfld      class ['Assembly-CSharp']RimWorld.CompRefuelable PipeSystem.CompResourceTrader::compRefuelable
    IL_009a:  ldarg.0
    IL_009b:  ldfld      class ['Assembly-CSharp']RimWorld.CompRefuelable PipeSystem.CompResourceTrader::compRefuelable
    IL_00a0:  callvirt   instance class ['Assembly-CSharp']RimWorld.CompProperties_Refuelable ['Assembly-CSharp']RimWorld.CompRefuelable::get_Props()
    IL_00a5:  ldfld      float32 ['Assembly-CSharp']RimWorld.CompProperties_Refuelable::fuelConsumptionPerTickInRain
    IL_00aa:  callvirt   instance void ['Assembly-CSharp']RimWorld.CompRefuelable::ConsumeFuel(float32)
    IL_00af:  ldarg.0
    IL_00b0:  call       instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_00b5:  ldfld      class ['Assembly-CSharp']Verse.SoundDef PipeSystem.CompProperties_ResourceTrader::soundAmbientReceivingResource
    IL_00ba:  brtrue.s   IL_00bd

    IL_00bc:  ret

    IL_00bd:  ldarg.0
    IL_00be:  call       instance bool PipeSystem.CompResourceTrader::get_ResourceOn()
    IL_00c3:  brfalse.s  IL_010d

    IL_00c5:  ldarg.0
    IL_00c6:  ldfld      class ['Assembly-CSharp']Verse.Sound.Sustainer PipeSystem.CompResourceTrader::sustainerResourceOn
    IL_00cb:  brfalse.s  IL_00da

    IL_00cd:  ldarg.0
    IL_00ce:  ldfld      class ['Assembly-CSharp']Verse.Sound.Sustainer PipeSystem.CompResourceTrader::sustainerResourceOn
    IL_00d3:  callvirt   instance bool ['Assembly-CSharp']Verse.Sound.Sustainer::get_Ended()
    IL_00d8:  brfalse.s  IL_0101

    IL_00da:  ldarg.0
    IL_00db:  ldarg.0
    IL_00dc:  call       instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_00e1:  ldfld      class ['Assembly-CSharp']Verse.SoundDef PipeSystem.CompProperties_ResourceTrader::soundAmbientReceivingResource
    IL_00e6:  ldarg.0
    IL_00e7:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_00ec:  call       valuetype ['Assembly-CSharp']Verse.TargetInfo ['Assembly-CSharp']Verse.TargetInfo::op_Implicit(class ['Assembly-CSharp']Verse.Thing)
    IL_00f1:  ldc.i4.0
    IL_00f2:  call       valuetype ['Assembly-CSharp']Verse.Sound.SoundInfo ['Assembly-CSharp']Verse.Sound.SoundInfo::InMap(valuetype ['Assembly-CSharp']Verse.TargetInfo,
                                                                                                                            valuetype ['Assembly-CSharp']Verse.Sound.MaintenanceType)
    IL_00f7:  call       class ['Assembly-CSharp']Verse.Sound.Sustainer ['Assembly-CSharp']Verse.Sound.SoundStarter::TrySpawnSustainer(class ['Assembly-CSharp']Verse.SoundDef,
                                                                                                                                       valuetype ['Assembly-CSharp']Verse.Sound.SoundInfo)
    IL_00fc:  stfld      class ['Assembly-CSharp']Verse.Sound.Sustainer PipeSystem.CompResourceTrader::sustainerResourceOn
    IL_0101:  ldarg.0
    IL_0102:  ldfld      class ['Assembly-CSharp']Verse.Sound.Sustainer PipeSystem.CompResourceTrader::sustainerResourceOn
    IL_0107:  callvirt   instance void ['Assembly-CSharp']Verse.Sound.Sustainer::Maintain()
    IL_010c:  ret

    IL_010d:  ldarg.0
    IL_010e:  ldfld      class ['Assembly-CSharp']Verse.Sound.Sustainer PipeSystem.CompResourceTrader::sustainerResourceOn
    IL_0113:  brtrue.s   IL_0116

    IL_0115:  ret

    IL_0116:  ldarg.0
    IL_0117:  ldfld      class ['Assembly-CSharp']Verse.Sound.Sustainer PipeSystem.CompResourceTrader::sustainerResourceOn
    IL_011c:  callvirt   instance void ['Assembly-CSharp']Verse.Sound.Sustainer::End()
    IL_0121:  ldarg.0
    IL_0122:  ldnull
    IL_0123:  stfld      class ['Assembly-CSharp']Verse.Sound.Sustainer PipeSystem.CompResourceTrader::sustainerResourceOn
    IL_0128:  ret
  } // end of method CompResourceTrader::CompTick

  .method family hidebysig newslot virtual 
          instance void  HandleRefuelableTicking(float32 baseFuelConsumptionRate) cil managed
  {
    // Code size       119 (0x77)
    .maxstack  2
    .locals init (class ['Assembly-CSharp']RimWorld.CompFlickable V_0,
             class ['Assembly-CSharp']RimWorld.CompPowerTrader V_1)
    IL_0000:  ldarg.0
    IL_0001:  call       instance bool PipeSystem.CompResourceTrader::get_ResourceOn()
    IL_0006:  brtrue.s   IL_0009

    IL_0008:  ret

    IL_0009:  ldarg.0
    IL_000a:  call       instance bool PipeSystem.CompResourceTrader::get_LowPowerModeOn()
    IL_000f:  brfalse.s  IL_001f

    IL_0011:  ldarg.0
    IL_0012:  call       instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_0017:  ldfld      bool PipeSystem.CompProperties_ResourceTrader::disableRefuelableConsumptionInLowPowerMode
    IL_001c:  brfalse.s  IL_001f

    IL_001e:  ret

    IL_001f:  ldarg.0
    IL_0020:  ldfld      class ['Assembly-CSharp']RimWorld.CompRefuelable PipeSystem.CompResourceTrader::compRefuelable
    IL_0025:  callvirt   instance class ['Assembly-CSharp']RimWorld.CompProperties_Refuelable ['Assembly-CSharp']RimWorld.CompRefuelable::get_Props()
    IL_002a:  ldfld      bool ['Assembly-CSharp']RimWorld.CompProperties_Refuelable::consumeFuelOnlyWhenUsed
    IL_002f:  brfalse.s  IL_0032

    IL_0031:  ret

    IL_0032:  ldarg.0
    IL_0033:  ldfld      class ['Assembly-CSharp']RimWorld.CompFlickable PipeSystem.CompResourceTrader::compFlickable
    IL_0038:  stloc.0
    IL_0039:  ldloc.0
    IL_003a:  brfalse.s  IL_0045

    IL_003c:  ldloc.0
    IL_003d:  callvirt   instance bool ['Assembly-CSharp']RimWorld.CompFlickable::get_SwitchIsOn()
    IL_0042:  brtrue.s   IL_0045

    IL_0044:  ret

    IL_0045:  ldarg.0
    IL_0046:  ldfld      class ['Assembly-CSharp']RimWorld.CompRefuelable PipeSystem.CompResourceTrader::compRefuelable
    IL_004b:  callvirt   instance class ['Assembly-CSharp']RimWorld.CompProperties_Refuelable ['Assembly-CSharp']RimWorld.CompRefuelable::get_Props()
    IL_0050:  ldfld      bool ['Assembly-CSharp']RimWorld.CompProperties_Refuelable::consumeFuelOnlyWhenPowered
    IL_0055:  brfalse.s  IL_006a

    IL_0057:  ldarg.0
    IL_0058:  ldfld      class ['Assembly-CSharp']RimWorld.CompPowerTrader PipeSystem.CompResource::powerComp
    IL_005d:  stloc.1
    IL_005e:  ldloc.1
    IL_005f:  brfalse.s  IL_0069

    IL_0061:  ldloc.1
    IL_0062:  callvirt   instance bool ['Assembly-CSharp']RimWorld.CompPowerTrader::get_PowerOn()
    IL_0067:  brtrue.s   IL_006a

    IL_0069:  ret

    IL_006a:  ldarg.0
    IL_006b:  ldfld      class ['Assembly-CSharp']RimWorld.CompRefuelable PipeSystem.CompResourceTrader::compRefuelable
    IL_0070:  ldarg.1
    IL_0071:  callvirt   instance void ['Assembly-CSharp']RimWorld.CompRefuelable::ConsumeFuel(float32)
    IL_0076:  ret
  } // end of method CompResourceTrader::HandleRefuelableTicking

  .method public hidebysig instance void 
          Notify_UsedThisTick() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance bool PipeSystem.CompResourceTrader::get_UsedLastTick()
    IL_0006:  brfalse.s  IL_0009

    IL_0008:  ret

    IL_0009:  ldarg.0
    IL_000a:  ldc.i4.1
    IL_000b:  call       instance void PipeSystem.CompResourceTrader::set_UsedLastTick(bool)
    IL_0010:  ldarg.0
    IL_0011:  callvirt   instance class PipeSystem.PipeNet PipeSystem.CompResource::get_PipeNet()
    IL_0016:  ldc.i4.1
    IL_0017:  stfld      bool PipeSystem.PipeNet::receiversDirty
    IL_001c:  ret
  } // end of method CompResourceTrader::Notify_UsedThisTick

  .method public hidebysig virtual instance void 
          ReceiveCompSignal(string signal) cil managed
  {
    // Code size       226 (0xe2)
    .maxstack  2
    .locals init (class ['Assembly-CSharp']RimWorld.CompRefuelable V_0)
    IL_0000:  ldstr      "Received comp signal: "
    IL_0005:  ldarg.1
    IL_0006:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_000b:  call       void PipeSystem.PipeSystemDebug::Message(string)
    IL_0010:  ldarg.1
    IL_0011:  ldstr      "FlickedOff"
    IL_0016:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_001b:  brtrue.s   IL_0060

    IL_001d:  ldarg.1
    IL_001e:  ldstr      "ScheduledOff"
    IL_0023:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0028:  brtrue.s   IL_0060

    IL_002a:  ldarg.1
    IL_002b:  ldstr      "Breakdown"
    IL_0030:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0035:  brtrue.s   IL_0060

    IL_0037:  ldarg.1
    IL_0038:  ldstr      "RanOutOfFuel"
    IL_003d:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0042:  brtrue.s   IL_0060

    IL_0044:  ldarg.1
    IL_0045:  ldstr      "PowerTurnedOff"
    IL_004a:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_004f:  brtrue.s   IL_0060

    IL_0051:  ldarg.1
    IL_0052:  ldstr      "Refueled"
    IL_0057:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_005c:  brtrue.s   IL_0068

    IL_005e:  br.s       IL_0087

    IL_0060:  ldarg.0
    IL_0061:  ldc.i4.0
    IL_0062:  call       instance void PipeSystem.CompResourceTrader::set_ResourceOn(bool)
    IL_0067:  ret

    IL_0068:  ldarg.0
    IL_0069:  ldfld      class ['Assembly-CSharp']RimWorld.CompRefuelable PipeSystem.CompResourceTrader::compRefuelable
    IL_006e:  stloc.0
    IL_006f:  ldloc.0
    IL_0070:  brfalse.s  IL_0086

    IL_0072:  ldloc.0
    IL_0073:  callvirt   instance float32 ['Assembly-CSharp']RimWorld.CompRefuelable::get_Fuel()
    IL_0078:  ldc.r4     0.0
    IL_007d:  bgt.un.s   IL_0086

    IL_007f:  ldarg.0
    IL_0080:  ldc.i4.0
    IL_0081:  call       instance void PipeSystem.CompResourceTrader::set_ResourceOn(bool)
    IL_0086:  ret

    IL_0087:  ldarg.1
    IL_0088:  ldarg.0
    IL_0089:  call       instance string PipeSystem.CompResourceTrader::get_OffSignal()
    IL_008e:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0093:  brfalse.s  IL_009c

    IL_0095:  ldarg.0
    IL_0096:  ldc.i4.0
    IL_0097:  call       instance void PipeSystem.CompResourceTrader::set_ResourceOn(bool)
    IL_009c:  ldarg.0
    IL_009d:  callvirt   instance class PipeSystem.PipeNet PipeSystem.CompResource::get_PipeNet()
    IL_00a2:  brfalse.s  IL_00e1

    IL_00a4:  ldarg.0
    IL_00a5:  call       instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_00aa:  ldc.r4     0.0
    IL_00af:  beq.s      IL_00c9

    IL_00b1:  ldarg.0
    IL_00b2:  callvirt   instance class PipeSystem.PipeNet PipeSystem.CompResource::get_PipeNet()
    IL_00b7:  ldc.i4.1
    IL_00b8:  stfld      bool PipeSystem.PipeNet::receiversDirty
    IL_00bd:  ldarg.0
    IL_00be:  callvirt   instance class PipeSystem.PipeNet PipeSystem.CompResource::get_PipeNet()
    IL_00c3:  ldc.i4.1
    IL_00c4:  stfld      bool PipeSystem.PipeNet::producersDirty
    IL_00c9:  ldarg.0
    IL_00ca:  callvirt   instance class PipeSystem.PipeNet PipeSystem.CompResource::get_PipeNet()
    IL_00cf:  ldarg.0
    IL_00d0:  callvirt   instance void PipeSystem.PipeNet::UpdateLowPowerModeTrader(class PipeSystem.CompResourceTrader)
    IL_00d5:  ldarg.0
    IL_00d6:  ldarg.0
    IL_00d7:  callvirt   instance bool PipeSystem.CompResourceTrader::get_ShouldBeLowPowerMode()
    IL_00dc:  call       instance void PipeSystem.CompResourceTrader::set_LowPowerModeOn(bool)
    IL_00e1:  ret
  } // end of method CompResourceTrader::ReceiveCompSignal

  .method public hidebysig newslot virtual 
          instance bool  CanBeOn() cil managed
  {
    // Code size       108 (0x6c)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class ['Assembly-CSharp']RimWorld.CompFlickable PipeSystem.CompResourceTrader::compFlickable
    IL_0006:  brfalse.s  IL_0015

    IL_0008:  ldarg.0
    IL_0009:  ldfld      class ['Assembly-CSharp']RimWorld.CompFlickable PipeSystem.CompResourceTrader::compFlickable
    IL_000e:  callvirt   instance bool ['Assembly-CSharp']RimWorld.CompFlickable::get_SwitchIsOn()
    IL_0013:  brfalse.s  IL_006a

    IL_0015:  ldarg.0
    IL_0016:  ldfld      class ['Assembly-CSharp']RimWorld.CompSchedule PipeSystem.CompResourceTrader::compSchedule
    IL_001b:  brfalse.s  IL_002a

    IL_001d:  ldarg.0
    IL_001e:  ldfld      class ['Assembly-CSharp']RimWorld.CompSchedule PipeSystem.CompResourceTrader::compSchedule
    IL_0023:  callvirt   instance bool ['Assembly-CSharp']RimWorld.CompSchedule::get_Allowed()
    IL_0028:  brfalse.s  IL_006a

    IL_002a:  ldarg.0
    IL_002b:  ldfld      class ['Assembly-CSharp']RimWorld.CompBreakdownable PipeSystem.CompResourceTrader::compBreakdownable
    IL_0030:  brfalse.s  IL_003f

    IL_0032:  ldarg.0
    IL_0033:  ldfld      class ['Assembly-CSharp']RimWorld.CompBreakdownable PipeSystem.CompResourceTrader::compBreakdownable
    IL_0038:  callvirt   instance bool ['Assembly-CSharp']RimWorld.CompBreakdownable::get_BrokenDown()
    IL_003d:  brtrue.s   IL_006a

    IL_003f:  ldarg.0
    IL_0040:  ldfld      class ['Assembly-CSharp']RimWorld.CompRefuelable PipeSystem.CompResourceTrader::compRefuelable
    IL_0045:  brfalse.s  IL_0054

    IL_0047:  ldarg.0
    IL_0048:  ldfld      class ['Assembly-CSharp']RimWorld.CompRefuelable PipeSystem.CompResourceTrader::compRefuelable
    IL_004d:  callvirt   instance bool ['Assembly-CSharp']RimWorld.CompRefuelable::get_HasFuel()
    IL_0052:  brfalse.s  IL_006a

    IL_0054:  ldarg.0
    IL_0055:  ldfld      class ['Assembly-CSharp']RimWorld.CompPowerTrader PipeSystem.CompResourceTrader::compPowerTrader
    IL_005a:  brfalse.s  IL_0068

    IL_005c:  ldarg.0
    IL_005d:  ldfld      class ['Assembly-CSharp']RimWorld.CompPowerTrader PipeSystem.CompResourceTrader::compPowerTrader
    IL_0062:  callvirt   instance bool ['Assembly-CSharp']RimWorld.CompPowerTrader::get_PowerOn()
    IL_0067:  ret

    IL_0068:  ldc.i4.1
    IL_0069:  ret

    IL_006a:  ldc.i4.0
    IL_006b:  ret
  } // end of method CompResourceTrader::CanBeOn

  .method public hidebysig specialname instance string 
          get_DebugString() cil managed
  {
    // Code size       106 (0x6a)
    .maxstack  4
    .locals init (bool V_0,
             float32 V_1)
    IL_0000:  newobj     instance void [mscorlib]System.Text.StringBuilder::.ctor()
    IL_0005:  dup
    IL_0006:  ldarg.0
    IL_0007:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_000c:  callvirt   instance string ['Assembly-CSharp']Verse.Entity::get_LabelCap()
    IL_0011:  ldstr      " CompResourceTrader:"
    IL_0016:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_001b:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
    IL_0020:  pop
    IL_0021:  dup
    IL_0022:  ldstr      "   ResourceOn: "
    IL_0027:  ldarg.0
    IL_0028:  call       instance bool PipeSystem.CompResourceTrader::get_ResourceOn()
    IL_002d:  stloc.0
    IL_002e:  ldloca.s   V_0
    IL_0030:  call       instance string [mscorlib]System.Boolean::ToString()
    IL_0035:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_003a:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
    IL_003f:  pop
    IL_0040:  dup
    IL_0041:  ldstr      "   consumption: "
    IL_0046:  ldarg.0
    IL_0047:  call       instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_004c:  stloc.1
    IL_004d:  ldloca.s   V_1
    IL_004f:  call       instance string [mscorlib]System.Single::ToString()
    IL_0054:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0059:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
    IL_005e:  pop
    IL_005f:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0064:  callvirt   instance string [mscorlib]System.String::Trim()
    IL_0069:  ret
  } // end of method CompResourceTrader::get_DebugString

  .method private hidebysig instance void 
          InitializeComps() cil managed
  {
    // Code size       103 (0x67)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0007:  call       !!0 ['Assembly-CSharp']Verse.ThingCompUtility::TryGetComp<class ['Assembly-CSharp']RimWorld.CompFlickable>(class ['Assembly-CSharp']Verse.Thing)
    IL_000c:  stfld      class ['Assembly-CSharp']RimWorld.CompFlickable PipeSystem.CompResourceTrader::compFlickable
    IL_0011:  ldarg.0
    IL_0012:  ldarg.0
    IL_0013:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0018:  call       !!0 ['Assembly-CSharp']Verse.ThingCompUtility::TryGetComp<class ['Assembly-CSharp']RimWorld.CompSchedule>(class ['Assembly-CSharp']Verse.Thing)
    IL_001d:  stfld      class ['Assembly-CSharp']RimWorld.CompSchedule PipeSystem.CompResourceTrader::compSchedule
    IL_0022:  ldarg.0
    IL_0023:  ldarg.0
    IL_0024:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0029:  call       !!0 ['Assembly-CSharp']Verse.ThingCompUtility::TryGetComp<class ['Assembly-CSharp']RimWorld.CompBreakdownable>(class ['Assembly-CSharp']Verse.Thing)
    IL_002e:  stfld      class ['Assembly-CSharp']RimWorld.CompBreakdownable PipeSystem.CompResourceTrader::compBreakdownable
    IL_0033:  ldarg.0
    IL_0034:  ldarg.0
    IL_0035:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_003a:  call       !!0 ['Assembly-CSharp']Verse.ThingCompUtility::TryGetComp<class ['Assembly-CSharp']RimWorld.CompRefuelable>(class ['Assembly-CSharp']Verse.Thing)
    IL_003f:  stfld      class ['Assembly-CSharp']RimWorld.CompRefuelable PipeSystem.CompResourceTrader::compRefuelable
    IL_0044:  ldarg.0
    IL_0045:  ldarg.0
    IL_0046:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_004b:  call       !!0 ['Assembly-CSharp']Verse.ThingCompUtility::TryGetComp<class ['Assembly-CSharp']RimWorld.CompPowerTrader>(class ['Assembly-CSharp']Verse.Thing)
    IL_0050:  stfld      class ['Assembly-CSharp']RimWorld.CompPowerTrader PipeSystem.CompResourceTrader::compPowerTrader
    IL_0055:  ldarg.0
    IL_0056:  ldarg.0
    IL_0057:  call       instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_005c:  ldfld      float32 PipeSystem.CompProperties_ResourceTrader::consumptionPerTick
    IL_0061:  call       instance void PipeSystem.CompResourceTrader::set_BaseConsumption(float32)
    IL_0066:  ret
  } // end of method CompResourceTrader::InitializeComps

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void PipeSystem.CompResource::.ctor()
    IL_0006:  ret
  } // end of method CompResourceTrader::.ctor

  .method private hidebysig instance void 
          '<PostSpawnSetup>b__33_0'() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void PipeSystem.CompResourceTrader::StartSustainerIfInactive()
    IL_0006:  ret
  } // end of method CompResourceTrader::'<PostSpawnSetup>b__33_0'

  .property instance class PipeSystem.CompProperties_ResourceTrader
          Props()
  {
    .get instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
  } // end of property CompResourceTrader::Props
  .property instance string OnSignal()
  {
    .get instance string PipeSystem.CompResourceTrader::get_OnSignal()
  } // end of property CompResourceTrader::OnSignal
  .property instance string OffSignal()
  {
    .get instance string PipeSystem.CompResourceTrader::get_OffSignal()
  } // end of property CompResourceTrader::OffSignal
  .property instance float32 BaseConsumption()
  {
    .get instance float32 PipeSystem.CompResourceTrader::get_BaseConsumption()
    .set instance void PipeSystem.CompResourceTrader::set_BaseConsumption(float32)
  } // end of property CompResourceTrader::BaseConsumption
  .property instance bool UsedLastTick()
  {
    .get instance bool PipeSystem.CompResourceTrader::get_UsedLastTick()
    .set instance void PipeSystem.CompResourceTrader::set_UsedLastTick(bool)
  } // end of property CompResourceTrader::UsedLastTick
  .property instance float32 Consumption()
  {
    .get instance float32 PipeSystem.CompResourceTrader::get_Consumption()
  } // end of property CompResourceTrader::Consumption
  .property instance bool ResourceOn()
  {
    .get instance bool PipeSystem.CompResourceTrader::get_ResourceOn()
    .set instance void PipeSystem.CompResourceTrader::set_ResourceOn(bool)
  } // end of property CompResourceTrader::ResourceOn
  .property instance bool LowPowerModeOn()
  {
    .get instance bool PipeSystem.CompResourceTrader::get_LowPowerModeOn()
    .set instance void PipeSystem.CompResourceTrader::set_LowPowerModeOn(bool)
  } // end of property CompResourceTrader::LowPowerModeOn
  .property instance bool ShouldBeLowPowerMode()
  {
    .get instance bool PipeSystem.CompResourceTrader::get_ShouldBeLowPowerMode()
  } // end of property CompResourceTrader::ShouldBeLowPowerMode
  .property instance string DebugString()
  {
    .get instance string PipeSystem.CompResourceTrader::get_DebugString()
  } // end of property CompResourceTrader::DebugString
} // end of class PipeSystem.CompResourceTrader

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file F:\Repositories\rimworld_mods\.tmp\pipesystem_trader.res
