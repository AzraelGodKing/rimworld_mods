
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.8.3928.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.


// warning : THIS IS A PARTIAL DISASSEMBLY, NOT SUITABLE FOR RE-ASSEMBLING



.class public auto ansi beforefieldinit PipeSystem.PipeNet
       extends [mscorlib]System.Object
{
  .field public class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResource> connectors
  .field public class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> producers
  .field public class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> receivers
  .field public class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> storages
  .field public class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing> thingConverters
  .field public class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToResource> resourceConverters
  .field public class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompRefillWithPipes> refillables
  .field public class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceProcessor> processors
  .field public class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompDeepExtractor> extractors
  .field public class ['Assembly-CSharp']Verse.Map map
  .field public class ['Assembly-CSharp']Verse.BoolGrid networkGrid
  .field public class PipeSystem.PipeNetDef def
  .field public bool receiversDirty
  .field public bool nextTickRDirty
  .field public bool producersDirty
  .field public bool resourceTradersWithLowPowerModeDirty
  .field private float32 overflowAmount
  .field assembly class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> markedForTransfer
  .field family initonly class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> receiversOn
  .field family initonly class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> receiversOff
  .field family initonly class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> producersOn
  .field family initonly class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> producersOff
  .field family initonly class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> resourceTradersWithLowPowerMode
  .field private int32 '<NextTick>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .field private float32 '<Consumption>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .field private float32 '<VisualConsumption>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .field private float32 '<ExtraConsumptionThisTick>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .field private float32 '<Production>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .field private float32 '<VisualProduction>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .field private float32 '<ExtraProductionThisTick>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .field private float32 '<Stored>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .field private float32 '<AvailableCapacityLastTick>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig specialname instance int32 
          get_NextTick() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      int32 PipeSystem.PipeNet::'<NextTick>k__BackingField'
    IL_0006:  ret
  } // end of method PipeNet::get_NextTick

  .method public hidebysig specialname instance void 
          set_NextTick(int32 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      int32 PipeSystem.PipeNet::'<NextTick>k__BackingField'
    IL_0007:  ret
  } // end of method PipeNet::set_NextTick

  .method public hidebysig specialname instance float32 
          get_Consumption() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float32 PipeSystem.PipeNet::'<Consumption>k__BackingField'
    IL_0006:  ret
  } // end of method PipeNet::get_Consumption

  .method private hidebysig specialname instance void 
          set_Consumption(float32 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      float32 PipeSystem.PipeNet::'<Consumption>k__BackingField'
    IL_0007:  ret
  } // end of method PipeNet::set_Consumption

  .method public hidebysig specialname instance float32 
          get_VisualConsumption() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float32 PipeSystem.PipeNet::'<VisualConsumption>k__BackingField'
    IL_0006:  ret
  } // end of method PipeNet::get_VisualConsumption

  .method private hidebysig specialname instance void 
          set_VisualConsumption(float32 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      float32 PipeSystem.PipeNet::'<VisualConsumption>k__BackingField'
    IL_0007:  ret
  } // end of method PipeNet::set_VisualConsumption

  .method public hidebysig specialname instance float32 
          get_ExtraConsumptionThisTick() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float32 PipeSystem.PipeNet::'<ExtraConsumptionThisTick>k__BackingField'
    IL_0006:  ret
  } // end of method PipeNet::get_ExtraConsumptionThisTick

  .method public hidebysig specialname instance void 
          set_ExtraConsumptionThisTick(float32 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      float32 PipeSystem.PipeNet::'<ExtraConsumptionThisTick>k__BackingField'
    IL_0007:  ret
  } // end of method PipeNet::set_ExtraConsumptionThisTick

  .method public hidebysig specialname instance float32 
          get_TotalConsumptionThisTick() cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance float32 PipeSystem.PipeNet::get_Consumption()
    IL_0006:  ldarg.0
    IL_0007:  call       instance float32 PipeSystem.PipeNet::get_ExtraConsumptionThisTick()
    IL_000c:  add
    IL_000d:  ret
  } // end of method PipeNet::get_TotalConsumptionThisTick

  .method public hidebysig specialname instance float32 
          get_Production() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float32 PipeSystem.PipeNet::'<Production>k__BackingField'
    IL_0006:  ret
  } // end of method PipeNet::get_Production

  .method private hidebysig specialname instance void 
          set_Production(float32 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      float32 PipeSystem.PipeNet::'<Production>k__BackingField'
    IL_0007:  ret
  } // end of method PipeNet::set_Production

  .method public hidebysig specialname instance float32 
          get_VisualProduction() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float32 PipeSystem.PipeNet::'<VisualProduction>k__BackingField'
    IL_0006:  ret
  } // end of method PipeNet::get_VisualProduction

  .method private hidebysig specialname instance void 
          set_VisualProduction(float32 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      float32 PipeSystem.PipeNet::'<VisualProduction>k__BackingField'
    IL_0007:  ret
  } // end of method PipeNet::set_VisualProduction

  .method public hidebysig specialname instance float32 
          get_ExtraProductionThisTick() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float32 PipeSystem.PipeNet::'<ExtraProductionThisTick>k__BackingField'
    IL_0006:  ret
  } // end of method PipeNet::get_ExtraProductionThisTick

  .method public hidebysig specialname instance void 
          set_ExtraProductionThisTick(float32 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      float32 PipeSystem.PipeNet::'<ExtraProductionThisTick>k__BackingField'
    IL_0007:  ret
  } // end of method PipeNet::set_ExtraProductionThisTick

  .method public hidebysig specialname instance float32 
          get_TotalProductionThisTick() cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance float32 PipeSystem.PipeNet::get_Production()
    IL_0006:  ldarg.0
    IL_0007:  call       instance float32 PipeSystem.PipeNet::get_ExtraProductionThisTick()
    IL_000c:  add
    IL_000d:  ret
  } // end of method PipeNet::get_TotalProductionThisTick

  .method public hidebysig specialname instance float32 
          get_ExtractorRawProduction() cil managed
  {
    // Code size       50 (0x32)
    .maxstack  3
    .locals init (float32 V_0,
             int32 V_1)
    IL_0000:  ldc.r4     0.0
    IL_0005:  stloc.0
    IL_0006:  ldc.i4.0
    IL_0007:  stloc.1
    IL_0008:  br.s       IL_0022

    IL_000a:  ldloc.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompDeepExtractor> PipeSystem.PipeNet::extractors
    IL_0011:  ldloc.1
    IL_0012:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompDeepExtractor>::get_Item(int32)
    IL_0017:  callvirt   instance float32 PipeSystem.CompDeepExtractor::get_RawProduction()
    IL_001c:  add
    IL_001d:  stloc.0
    IL_001e:  ldloc.1
    IL_001f:  ldc.i4.1
    IL_0020:  add
    IL_0021:  stloc.1
    IL_0022:  ldloc.1
    IL_0023:  ldarg.0
    IL_0024:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompDeepExtractor> PipeSystem.PipeNet::extractors
    IL_0029:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompDeepExtractor>::get_Count()
    IL_002e:  blt.s      IL_000a

    IL_0030:  ldloc.0
    IL_0031:  ret
  } // end of method PipeNet::get_ExtractorRawProduction

  .method public hidebysig specialname instance float32 
          get_Stored() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float32 PipeSystem.PipeNet::'<Stored>k__BackingField'
    IL_0006:  ret
  } // end of method PipeNet::get_Stored

  .method private hidebysig specialname instance void 
          set_Stored(float32 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      float32 PipeSystem.PipeNet::'<Stored>k__BackingField'
    IL_0007:  ret
  } // end of method PipeNet::set_Stored

  .method public hidebysig specialname instance float32 
          get_AvailableCapacity() cil managed
  {
    // Code size       50 (0x32)
    .maxstack  3
    .locals init (float32 V_0,
             int32 V_1)
    IL_0000:  ldc.r4     0.0
    IL_0005:  stloc.0
    IL_0006:  ldc.i4.0
    IL_0007:  stloc.1
    IL_0008:  br.s       IL_0022

    IL_000a:  ldloc.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::storages
    IL_0011:  ldloc.1
    IL_0012:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Item(int32)
    IL_0017:  callvirt   instance float32 PipeSystem.CompResourceStorage::get_AmountCanAccept()
    IL_001c:  add
    IL_001d:  stloc.0
    IL_001e:  ldloc.1
    IL_001f:  ldc.i4.1
    IL_0020:  add
    IL_0021:  stloc.1
    IL_0022:  ldloc.1
    IL_0023:  ldarg.0
    IL_0024:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::storages
    IL_0029:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Count()
    IL_002e:  blt.s      IL_000a

    IL_0030:  ldloc.0
    IL_0031:  ret
  } // end of method PipeNet::get_AvailableCapacity

  .method public hidebysig specialname instance float32 
          get_AvailableCapacityLastTick() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float32 PipeSystem.PipeNet::'<AvailableCapacityLastTick>k__BackingField'
    IL_0006:  ret
  } // end of method PipeNet::get_AvailableCapacityLastTick

  .method private hidebysig specialname instance void 
          set_AvailableCapacityLastTick(float32 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      float32 PipeSystem.PipeNet::'<AvailableCapacityLastTick>k__BackingField'
    IL_0007:  ret
  } // end of method PipeNet::set_AvailableCapacityLastTick

  .method public hidebysig specialname instance float32 
          get_ThingConvertersMaxOutput() cil managed
  {
    // Code size       57 (0x39)
    .maxstack  2
    .locals init (int32 V_0,
             int32 V_1,
             class PipeSystem.CompConvertToThing V_2)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  ldc.i4.0
    IL_0003:  stloc.1
    IL_0004:  br.s       IL_0028

    IL_0006:  ldarg.0
    IL_0007:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing> PipeSystem.PipeNet::thingConverters
    IL_000c:  ldloc.1
    IL_000d:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing>::get_Item(int32)
    IL_0012:  stloc.2
    IL_0013:  ldloc.2
    IL_0014:  callvirt   instance bool PipeSystem.CompConvertToThing::get_CanOutputNow()
    IL_0019:  brfalse.s  IL_0024

    IL_001b:  ldloc.0
    IL_001c:  ldloc.2
    IL_001d:  callvirt   instance int32 PipeSystem.CompConvertToThing::get_MaxCanOutput()
    IL_0022:  add
    IL_0023:  stloc.0
    IL_0024:  ldloc.1
    IL_0025:  ldc.i4.1
    IL_0026:  add
    IL_0027:  stloc.1
    IL_0028:  ldloc.1
    IL_0029:  ldarg.0
    IL_002a:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing> PipeSystem.PipeNet::thingConverters
    IL_002f:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing>::get_Count()
    IL_0034:  blt.s      IL_0006

    IL_0036:  ldloc.0
    IL_0037:  conv.r4
    IL_0038:  ret
  } // end of method PipeNet::get_ThingConvertersMaxOutput

  .method public hidebysig specialname instance float32 
          get_RefillableAmount() cil managed
  {
    // Code size       99 (0x63)
    .maxstack  3
    .locals init (float32 V_0,
             int32 V_1,
             class PipeSystem.CompRefillWithPipes V_2)
    IL_0000:  ldc.r4     0.0
    IL_0005:  stloc.0
    IL_0006:  ldc.i4.0
    IL_0007:  stloc.1
    IL_0008:  br.s       IL_0053

    IL_000a:  ldarg.0
    IL_000b:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompRefillWithPipes> PipeSystem.PipeNet::refillables
    IL_0010:  ldloc.1
    IL_0011:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompRefillWithPipes>::get_Item(int32)
    IL_0016:  stloc.2
    IL_0017:  ldloc.0
    IL_0018:  ldloc.2
    IL_0019:  ldfld      class ['Assembly-CSharp']RimWorld.CompRefuelable PipeSystem.CompRefillWithPipes::compRefuelable
    IL_001e:  callvirt   instance float32 ['Assembly-CSharp']RimWorld.CompRefuelable::get_TargetFuelLevel()
    IL_0023:  ldloc.2
    IL_0024:  ldfld      class ['Assembly-CSharp']RimWorld.CompRefuelable PipeSystem.CompRefillWithPipes::compRefuelable
    IL_0029:  callvirt   instance float32 ['Assembly-CSharp']RimWorld.CompRefuelable::get_Fuel()
    IL_002e:  sub
    IL_002f:  ldloc.2
    IL_0030:  ldfld      class ['Assembly-CSharp']RimWorld.CompRefuelable PipeSystem.CompRefillWithPipes::compRefuelable
    IL_0035:  callvirt   instance class ['Assembly-CSharp']RimWorld.CompProperties_Refuelable ['Assembly-CSharp']RimWorld.CompRefuelable::get_Props()
    IL_003a:  callvirt   instance float32 ['Assembly-CSharp']RimWorld.CompProperties_Refuelable::get_FuelMultiplierCurrentDifficulty()
    IL_003f:  div
    IL_0040:  ldloc.2
    IL_0041:  callvirt   instance class PipeSystem.CompProperties_RefillWithPipes PipeSystem.CompRefillWithPipes::get_Props()
    IL_0046:  ldfld      int32 PipeSystem.CompProperties_RefillWithPipes::ratio
    IL_004b:  conv.r4
    IL_004c:  mul
    IL_004d:  add
    IL_004e:  stloc.0
    IL_004f:  ldloc.1
    IL_0050:  ldc.i4.1
    IL_0051:  add
    IL_0052:  stloc.1
    IL_0053:  ldloc.1
    IL_0054:  ldarg.0
    IL_0055:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompRefillWithPipes> PipeSystem.PipeNet::refillables
    IL_005a:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompRefillWithPipes>::get_Count()
    IL_005f:  blt.s      IL_000a

    IL_0061:  ldloc.0
    IL_0062:  ret
  } // end of method PipeNet::get_RefillableAmount

  .method public hidebysig specialname instance float32 
          get_OverflowAmount() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float32 PipeSystem.PipeNet::overflowAmount
    IL_0006:  ret
  } // end of method PipeNet::get_OverflowAmount

  .method famorassem hidebysig specialname 
          instance void  set_OverflowAmount(float32 'value') cil managed
  {
    // Code size       45 (0x2d)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float32 PipeSystem.PipeNet::overflowAmount
    IL_0006:  ldc.r4     0.0
    IL_000b:  cgt.un
    IL_000d:  ldc.i4.0
    IL_000e:  ceq
    IL_0010:  ldarg.1
    IL_0011:  ldc.r4     0.0
    IL_0016:  cgt.un
    IL_0018:  ldc.i4.0
    IL_0019:  ceq
    IL_001b:  xor
    IL_001c:  brfalse.s  IL_0025

    IL_001e:  ldarg.0
    IL_001f:  ldc.i4.1
    IL_0020:  stfld      bool PipeSystem.PipeNet::resourceTradersWithLowPowerModeDirty
    IL_0025:  ldarg.0
    IL_0026:  ldarg.1
    IL_0027:  stfld      float32 PipeSystem.PipeNet::overflowAmount
    IL_002c:  ret
  } // end of method PipeNet::set_OverflowAmount

  .method family hidebysig instance bool 
          ReceiversDirty() cil managed
  {
    // Code size       259 (0x103)
    .maxstack  3
    .locals init (bool V_0,
             float32 V_1,
             float32 V_2,
             int32 V_3,
             class PipeSystem.CompResourceTrader V_4)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  ldstr      "Receivers dirty"
    IL_0007:  call       void PipeSystem.PipeSystemDebug::Message(string)
    IL_000c:  ldarg.0
    IL_000d:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::receiversOn
    IL_0012:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::Clear()
    IL_0017:  ldarg.0
    IL_0018:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::receiversOff
    IL_001d:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::Clear()
    IL_0022:  ldc.r4     0.0
    IL_0027:  stloc.1
    IL_0028:  ldc.r4     0.0
    IL_002d:  stloc.2
    IL_002e:  ldc.i4.0
    IL_002f:  stloc.3
    IL_0030:  br         IL_00d9

    IL_0035:  ldarg.0
    IL_0036:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::receivers
    IL_003b:  ldloc.3
    IL_003c:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::get_Item(int32)
    IL_0041:  stloc.s    V_4
    IL_0043:  ldloc.s    V_4
    IL_0045:  callvirt   instance bool PipeSystem.CompResourceTrader::get_ResourceOn()
    IL_004a:  brfalse.s  IL_00c8

    IL_004c:  ldarg.0
    IL_004d:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::receiversOn
    IL_0052:  ldloc.s    V_4
    IL_0054:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::Add(!0)
    IL_0059:  ldloc.s    V_4
    IL_005b:  callvirt   instance bool PipeSystem.CompResourceTrader::get_LowPowerModeOn()
    IL_0060:  brtrue.s   IL_0086

    IL_0062:  ldloc.s    V_4
    IL_0064:  callvirt   instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_0069:  ldfld      bool PipeSystem.CompProperties_ResourceTrader::visualOnlyConsumption
    IL_006e:  brfalse.s  IL_007c

    IL_0070:  ldloc.2
    IL_0071:  ldloc.s    V_4
    IL_0073:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_0078:  add
    IL_0079:  stloc.2
    IL_007a:  br.s       IL_0086

    IL_007c:  ldloc.1
    IL_007d:  ldloc.s    V_4
    IL_007f:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_0084:  add
    IL_0085:  stloc.1
    IL_0086:  ldloc.s    V_4
    IL_0088:  callvirt   instance bool PipeSystem.CompResourceTrader::get_UsedLastTick()
    IL_008d:  brfalse.s  IL_00d5

    IL_008f:  ldarg.0
    IL_0090:  ldfld      class ['Assembly-CSharp']Verse.Map PipeSystem.PipeNet::map
    IL_0095:  ldfld      class ['Assembly-CSharp']Verse.AI.ReservationManager ['Assembly-CSharp']Verse.Map::reservationManager
    IL_009a:  ldloc.s    V_4
    IL_009c:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_00a1:  call       valuetype ['Assembly-CSharp']Verse.LocalTargetInfo ['Assembly-CSharp']Verse.LocalTargetInfo::op_Implicit(class ['Assembly-CSharp']Verse.Thing)
    IL_00a6:  call       class ['Assembly-CSharp']RimWorld.Faction ['Assembly-CSharp']RimWorld.Faction::get_OfPlayer()
    IL_00ab:  callvirt   instance bool ['Assembly-CSharp']Verse.AI.ReservationManager::IsReservedByAnyoneOf(valuetype ['Assembly-CSharp']Verse.LocalTargetInfo,
                                                                                                            class ['Assembly-CSharp']RimWorld.Faction)
    IL_00b0:  brtrue.s   IL_00c4

    IL_00b2:  ldloc.s    V_4
    IL_00b4:  ldc.i4.0
    IL_00b5:  callvirt   instance void PipeSystem.CompResourceTrader::set_UsedLastTick(bool)
    IL_00ba:  ldstr      "setting UsedLastTick to false"
    IL_00bf:  call       void PipeSystem.PipeSystemDebug::Message(string)
    IL_00c4:  ldc.i4.1
    IL_00c5:  stloc.0
    IL_00c6:  br.s       IL_00d5

    IL_00c8:  ldarg.0
    IL_00c9:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::receiversOff
    IL_00ce:  ldloc.s    V_4
    IL_00d0:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::Add(!0)
    IL_00d5:  ldloc.3
    IL_00d6:  ldc.i4.1
    IL_00d7:  add
    IL_00d8:  stloc.3
    IL_00d9:  ldloc.3
    IL_00da:  ldarg.0
    IL_00db:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::receivers
    IL_00e0:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::get_Count()
    IL_00e5:  blt        IL_0035

    IL_00ea:  ldarg.0
    IL_00eb:  ldloc.1
    IL_00ec:  call       instance void PipeSystem.PipeNet::set_Consumption(float32)
    IL_00f1:  ldarg.0
    IL_00f2:  ldloc.1
    IL_00f3:  ldloc.2
    IL_00f4:  add
    IL_00f5:  call       instance void PipeSystem.PipeNet::set_VisualConsumption(float32)
    IL_00fa:  ldarg.0
    IL_00fb:  ldc.i4.0
    IL_00fc:  stfld      bool PipeSystem.PipeNet::receiversDirty
    IL_0101:  ldloc.0
    IL_0102:  ret
  } // end of method PipeNet::ReceiversDirty

  .method family hidebysig instance void 
          ProducersDirty() cil managed
  {
    // Code size       180 (0xb4)
    .maxstack  3
    .locals init (float32 V_0,
             float32 V_1,
             int32 V_2,
             class PipeSystem.CompResourceTrader V_3)
    IL_0000:  ldstr      "Producers dirty"
    IL_0005:  call       void PipeSystem.PipeSystemDebug::Message(string)
    IL_000a:  ldarg.0
    IL_000b:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::producersOn
    IL_0010:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::Clear()
    IL_0015:  ldarg.0
    IL_0016:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::producersOff
    IL_001b:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::Clear()
    IL_0020:  ldc.r4     0.0
    IL_0025:  stloc.0
    IL_0026:  ldc.r4     0.0
    IL_002b:  stloc.1
    IL_002c:  ldc.i4.0
    IL_002d:  stloc.2
    IL_002e:  br.s       IL_008e

    IL_0030:  ldarg.0
    IL_0031:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::producers
    IL_0036:  ldloc.2
    IL_0037:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::get_Item(int32)
    IL_003c:  stloc.3
    IL_003d:  ldloc.3
    IL_003e:  callvirt   instance bool PipeSystem.CompResourceTrader::get_ResourceOn()
    IL_0043:  brfalse.s  IL_007e

    IL_0045:  ldarg.0
    IL_0046:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::producersOn
    IL_004b:  ldloc.3
    IL_004c:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::Add(!0)
    IL_0051:  ldloc.3
    IL_0052:  callvirt   instance bool PipeSystem.CompResourceTrader::get_LowPowerModeOn()
    IL_0057:  brtrue.s   IL_008a

    IL_0059:  ldloc.3
    IL_005a:  callvirt   instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_005f:  ldfld      bool PipeSystem.CompProperties_ResourceTrader::visualOnlyConsumption
    IL_0064:  brfalse.s  IL_0072

    IL_0066:  ldloc.1
    IL_0067:  ldloc.3
    IL_0068:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_006d:  neg
    IL_006e:  add
    IL_006f:  stloc.1
    IL_0070:  br.s       IL_008a

    IL_0072:  ldloc.0
    IL_0073:  ldloc.3
    IL_0074:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_0079:  neg
    IL_007a:  add
    IL_007b:  stloc.0
    IL_007c:  br.s       IL_008a

    IL_007e:  ldarg.0
    IL_007f:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::producersOff
    IL_0084:  ldloc.3
    IL_0085:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::Add(!0)
    IL_008a:  ldloc.2
    IL_008b:  ldc.i4.1
    IL_008c:  add
    IL_008d:  stloc.2
    IL_008e:  ldloc.2
    IL_008f:  ldarg.0
    IL_0090:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::producers
    IL_0095:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::get_Count()
    IL_009a:  blt.s      IL_0030

    IL_009c:  ldarg.0
    IL_009d:  ldloc.0
    IL_009e:  call       instance void PipeSystem.PipeNet::set_Production(float32)
    IL_00a3:  ldarg.0
    IL_00a4:  ldloc.0
    IL_00a5:  ldloc.1
    IL_00a6:  add
    IL_00a7:  call       instance void PipeSystem.PipeNet::set_VisualProduction(float32)
    IL_00ac:  ldarg.0
    IL_00ad:  ldc.i4.0
    IL_00ae:  stfld      bool PipeSystem.PipeNet::producersDirty
    IL_00b3:  ret
  } // end of method PipeNet::ProducersDirty

  .method public hidebysig newslot virtual 
          instance void  Destroy() cil managed
  {
    // Code size       74 (0x4a)
    .maxstack  3
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Removing pipe net"
    IL_0005:  call       void PipeSystem.PipeSystemDebug::Message(string)
    IL_000a:  ldarg.0
    IL_000b:  ldfld      class ['Assembly-CSharp']Verse.Map PipeSystem.PipeNet::map
    IL_0010:  callvirt   instance !!0 ['Assembly-CSharp']Verse.Map::GetComponent<class PipeSystem.PipeNetManager>()
    IL_0015:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.PipeNet> PipeSystem.PipeNetManager::pipeNets
    IL_001a:  ldarg.0
    IL_001b:  callvirt   instance bool class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.PipeNet>::Remove(!0)
    IL_0020:  pop
    IL_0021:  ldc.i4.0
    IL_0022:  stloc.0
    IL_0023:  br.s       IL_003b

    IL_0025:  ldarg.0
    IL_0026:  ldarg.0
    IL_0027:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResource> PipeSystem.PipeNet::connectors
    IL_002c:  ldloc.0
    IL_002d:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResource>::get_Item(int32)
    IL_0032:  callvirt   instance void PipeSystem.PipeNet::UnregisterComp(class PipeSystem.CompResource)
    IL_0037:  ldloc.0
    IL_0038:  ldc.i4.1
    IL_0039:  add
    IL_003a:  stloc.0
    IL_003b:  ldloc.0
    IL_003c:  ldarg.0
    IL_003d:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResource> PipeSystem.PipeNet::connectors
    IL_0042:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResource>::get_Count()
    IL_0047:  blt.s      IL_0025

    IL_0049:  ret
  } // end of method PipeNet::Destroy

  .method public hidebysig newslot virtual 
          instance void  Merge(class PipeSystem.PipeNet otherNet) cil managed
  {
    // Code size       89 (0x59)
    .maxstack  3
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_001a

    IL_0004:  ldarg.0
    IL_0005:  ldarg.1
    IL_0006:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResource> PipeSystem.PipeNet::connectors
    IL_000b:  ldloc.0
    IL_000c:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResource>::get_Item(int32)
    IL_0011:  callvirt   instance void PipeSystem.PipeNet::RegisterComp(class PipeSystem.CompResource)
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldarg.1
    IL_001c:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResource> PipeSystem.PipeNet::connectors
    IL_0021:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResource>::get_Count()
    IL_0026:  blt.s      IL_0004

    IL_0028:  ldarg.0
    IL_0029:  ldfld      class ['Assembly-CSharp']Verse.Map PipeSystem.PipeNet::map
    IL_002e:  callvirt   instance !!0 ['Assembly-CSharp']Verse.Map::GetComponent<class PipeSystem.PipeNetManager>()
    IL_0033:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.PipeNet> PipeSystem.PipeNetManager::pipeNets
    IL_0038:  ldarg.1
    IL_0039:  callvirt   instance bool class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.PipeNet>::Remove(!0)
    IL_003e:  pop
    IL_003f:  ldarg.0
    IL_0040:  ldarg.1
    IL_0041:  callvirt   instance float32 PipeSystem.PipeNet::get_OverflowAmount()
    IL_0046:  ldc.i4.1
    IL_0047:  call       instance float32 PipeSystem.PipeNet::DistributeToOverflow(float32,
                                                                                   bool)
    IL_004c:  pop
    IL_004d:  ldarg.1
    IL_004e:  ldc.r4     0.0
    IL_0053:  callvirt   instance void PipeSystem.PipeNet::set_OverflowAmount(float32)
    IL_0058:  ret
  } // end of method PipeNet::Merge

  .method public hidebysig newslot virtual 
          instance void  RegisterComp(class PipeSystem.CompResource comp) cil managed
  {
    // Code size       419 (0x1a3)
    .maxstack  3
    .locals init (class PipeSystem.CompResourceTrader V_0,
             class PipeSystem.CompResourceStorage V_1,
             class PipeSystem.CompConvertToThing V_2,
             class PipeSystem.CompConvertToResource V_3,
             class PipeSystem.CompResourceProcessor V_4,
             class PipeSystem.CompRefillWithPipes V_5,
             class PipeSystem.CompDeepExtractor V_6,
             valuetype ['Assembly-CSharp']Verse.CellRect/Enumerator V_7,
             valuetype ['Assembly-CSharp']Verse.CellRect V_8,
             valuetype ['Assembly-CSharp']Verse.IntVec3 V_9)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResource> PipeSystem.PipeNet::connectors
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance bool class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResource>::Contains(!0)
    IL_000c:  brfalse.s  IL_000f

    IL_000e:  ret

    IL_000f:  ldarg.0
    IL_0010:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResource> PipeSystem.PipeNet::connectors
    IL_0015:  ldarg.1
    IL_0016:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResource>::Add(!0)
    IL_001b:  ldarg.1
    IL_001c:  isinst     PipeSystem.CompResourceTrader
    IL_0021:  stloc.0
    IL_0022:  ldloc.0
    IL_0023:  brfalse.s  IL_008f

    IL_0025:  ldloc.0
    IL_0026:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_002b:  ldc.r4     0.0
    IL_0030:  bge.un.s   IL_0047

    IL_0032:  ldarg.0
    IL_0033:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::producers
    IL_0038:  ldloc.0
    IL_0039:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::Add(!0)
    IL_003e:  ldarg.0
    IL_003f:  ldc.i4.1
    IL_0040:  stfld      bool PipeSystem.PipeNet::producersDirty
    IL_0045:  br.s       IL_0067

    IL_0047:  ldloc.0
    IL_0048:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_004d:  ldc.r4     0.0
    IL_0052:  blt.un.s   IL_0067

    IL_0054:  ldarg.0
    IL_0055:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::receivers
    IL_005a:  ldloc.0
    IL_005b:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::Add(!0)
    IL_0060:  ldarg.0
    IL_0061:  ldc.i4.1
    IL_0062:  stfld      bool PipeSystem.PipeNet::receiversDirty
    IL_0067:  ldloc.0
    IL_0068:  callvirt   instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_006d:  callvirt   instance bool PipeSystem.CompProperties_ResourceTrader::get_EverHasLowPowerMode()
    IL_0072:  brfalse    IL_0153

    IL_0077:  ldarg.0
    IL_0078:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::resourceTradersWithLowPowerMode
    IL_007d:  ldloc.0
    IL_007e:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::Add(!0)
    IL_0083:  ldarg.0
    IL_0084:  ldc.i4.1
    IL_0085:  stfld      bool PipeSystem.PipeNet::resourceTradersWithLowPowerModeDirty
    IL_008a:  br         IL_0153

    IL_008f:  ldarg.1
    IL_0090:  isinst     PipeSystem.CompResourceStorage
    IL_0095:  stloc.1
    IL_0096:  ldloc.1
    IL_0097:  brfalse.s  IL_00d4

    IL_0099:  ldloc.1
    IL_009a:  ldfld      bool PipeSystem.CompResourceStorage::markedForTransfer
    IL_009f:  brfalse.s  IL_00b2

    IL_00a1:  ldarg.0
    IL_00a2:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::markedForTransfer
    IL_00a7:  ldloc.1
    IL_00a8:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::Add(!0)
    IL_00ad:  br         IL_0153

    IL_00b2:  ldarg.0
    IL_00b3:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::storages
    IL_00b8:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Count()
    IL_00bd:  brtrue.s   IL_00c6

    IL_00bf:  ldarg.0
    IL_00c0:  ldc.i4.1
    IL_00c1:  stfld      bool PipeSystem.PipeNet::resourceTradersWithLowPowerModeDirty
    IL_00c6:  ldarg.0
    IL_00c7:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::storages
    IL_00cc:  ldloc.1
    IL_00cd:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::Add(!0)
    IL_00d2:  br.s       IL_0153

    IL_00d4:  ldarg.1
    IL_00d5:  isinst     PipeSystem.CompConvertToThing
    IL_00da:  stloc.2
    IL_00db:  ldloc.2
    IL_00dc:  brfalse.s  IL_00ec

    IL_00de:  ldarg.0
    IL_00df:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing> PipeSystem.PipeNet::thingConverters
    IL_00e4:  ldloc.2
    IL_00e5:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing>::Add(!0)
    IL_00ea:  br.s       IL_0153

    IL_00ec:  ldarg.1
    IL_00ed:  isinst     PipeSystem.CompConvertToResource
    IL_00f2:  stloc.3
    IL_00f3:  ldloc.3
    IL_00f4:  brfalse.s  IL_0104

    IL_00f6:  ldarg.0
    IL_00f7:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToResource> PipeSystem.PipeNet::resourceConverters
    IL_00fc:  ldloc.3
    IL_00fd:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToResource>::Add(!0)
    IL_0102:  br.s       IL_0153

    IL_0104:  ldarg.1
    IL_0105:  isinst     PipeSystem.CompResourceProcessor
    IL_010a:  stloc.s    V_4
    IL_010c:  ldloc.s    V_4
    IL_010e:  brfalse.s  IL_011f

    IL_0110:  ldarg.0
    IL_0111:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceProcessor> PipeSystem.PipeNet::processors
    IL_0116:  ldloc.s    V_4
    IL_0118:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceProcessor>::Add(!0)
    IL_011d:  br.s       IL_0153

    IL_011f:  ldarg.1
    IL_0120:  isinst     PipeSystem.CompRefillWithPipes
    IL_0125:  stloc.s    V_5
    IL_0127:  ldloc.s    V_5
    IL_0129:  brfalse.s  IL_013a

    IL_012b:  ldarg.0
    IL_012c:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompRefillWithPipes> PipeSystem.PipeNet::refillables
    IL_0131:  ldloc.s    V_5
    IL_0133:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompRefillWithPipes>::Add(!0)
    IL_0138:  br.s       IL_0153

    IL_013a:  ldarg.1
    IL_013b:  isinst     PipeSystem.CompDeepExtractor
    IL_0140:  stloc.s    V_6
    IL_0142:  ldloc.s    V_6
    IL_0144:  brfalse.s  IL_0153

    IL_0146:  ldarg.0
    IL_0147:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompDeepExtractor> PipeSystem.PipeNet::extractors
    IL_014c:  ldloc.s    V_6
    IL_014e:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompDeepExtractor>::Add(!0)
    IL_0153:  ldarg.1
    IL_0154:  ldarg.0
    IL_0155:  callvirt   instance void PipeSystem.CompResource::set_PipeNet(class PipeSystem.PipeNet)
    IL_015a:  ldarg.1
    IL_015b:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0160:  call       valuetype ['Assembly-CSharp']Verse.CellRect ['Assembly-CSharp']Verse.GenAdj::OccupiedRect(class ['Assembly-CSharp']Verse.Thing)
    IL_0165:  stloc.s    V_8
    IL_0167:  ldloca.s   V_8
    IL_0169:  call       instance valuetype ['Assembly-CSharp']Verse.CellRect/Enumerator ['Assembly-CSharp']Verse.CellRect::GetEnumerator()
    IL_016e:  stloc.s    V_7
    .try
    {
      IL_0170:  br.s       IL_0189

      IL_0172:  ldloca.s   V_7
      IL_0174:  call       instance valuetype ['Assembly-CSharp']Verse.IntVec3 ['Assembly-CSharp']Verse.CellRect/Enumerator::get_Current()
      IL_0179:  stloc.s    V_9
      IL_017b:  ldarg.0
      IL_017c:  ldfld      class ['Assembly-CSharp']Verse.BoolGrid PipeSystem.PipeNet::networkGrid
      IL_0181:  ldloc.s    V_9
      IL_0183:  ldc.i4.1
      IL_0184:  callvirt   instance void ['Assembly-CSharp']Verse.BoolGrid::Set(valuetype ['Assembly-CSharp']Verse.IntVec3,
                                                                                bool)
      IL_0189:  ldloca.s   V_7
      IL_018b:  call       instance bool ['Assembly-CSharp']Verse.CellRect/Enumerator::MoveNext()
      IL_0190:  brtrue.s   IL_0172

      IL_0192:  leave.s    IL_01a2

    }  // end .try
    finally
    {
      IL_0194:  ldloca.s   V_7
      IL_0196:  constrained. ['Assembly-CSharp']Verse.CellRect/Enumerator
      IL_019c:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
      IL_01a1:  endfinally
    }  // end handler
    IL_01a2:  ret
  } // end of method PipeNet::RegisterComp

  .method public hidebysig newslot virtual 
          instance void  UnregisterComp(class PipeSystem.CompResource comp) cil managed
  {
    // Code size       475 (0x1db)
    .maxstack  3
    .locals init (class PipeSystem.CompResourceTrader V_0,
             class PipeSystem.CompResourceStorage V_1,
             class PipeSystem.CompConvertToThing V_2,
             class PipeSystem.CompConvertToResource V_3,
             class PipeSystem.CompResourceProcessor V_4,
             class PipeSystem.CompRefillWithPipes V_5,
             class PipeSystem.CompDeepExtractor V_6,
             valuetype ['Assembly-CSharp']Verse.CellRect/Enumerator V_7,
             valuetype ['Assembly-CSharp']Verse.CellRect V_8,
             valuetype ['Assembly-CSharp']Verse.IntVec3 V_9,
             float32 V_10)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResource> PipeSystem.PipeNet::connectors
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance bool class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResource>::Remove(!0)
    IL_000c:  pop
    IL_000d:  ldarg.1
    IL_000e:  isinst     PipeSystem.CompResourceTrader
    IL_0013:  stloc.0
    IL_0014:  ldloc.0
    IL_0015:  brfalse.s  IL_007d

    IL_0017:  ldloc.0
    IL_0018:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_001d:  ldc.r4     0.0
    IL_0022:  bge.un.s   IL_003a

    IL_0024:  ldarg.0
    IL_0025:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::producers
    IL_002a:  ldloc.0
    IL_002b:  callvirt   instance bool class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::Remove(!0)
    IL_0030:  pop
    IL_0031:  ldarg.0
    IL_0032:  ldc.i4.1
    IL_0033:  stfld      bool PipeSystem.PipeNet::producersDirty
    IL_0038:  br.s       IL_005b

    IL_003a:  ldloc.0
    IL_003b:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_0040:  ldc.r4     0.0
    IL_0045:  ble.un.s   IL_005b

    IL_0047:  ldarg.0
    IL_0048:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::receivers
    IL_004d:  ldloc.0
    IL_004e:  callvirt   instance bool class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::Remove(!0)
    IL_0053:  pop
    IL_0054:  ldarg.0
    IL_0055:  ldc.i4.1
    IL_0056:  stfld      bool PipeSystem.PipeNet::receiversDirty
    IL_005b:  ldloc.0
    IL_005c:  callvirt   instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_0061:  callvirt   instance bool PipeSystem.CompProperties_ResourceTrader::get_EverHasLowPowerMode()
    IL_0066:  brfalse    IL_0134

    IL_006b:  ldarg.0
    IL_006c:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::resourceTradersWithLowPowerMode
    IL_0071:  ldloc.0
    IL_0072:  callvirt   instance bool class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::Remove(!0)
    IL_0077:  pop
    IL_0078:  br         IL_0134

    IL_007d:  ldarg.1
    IL_007e:  isinst     PipeSystem.CompResourceStorage
    IL_0083:  stloc.1
    IL_0084:  ldloc.1
    IL_0085:  brfalse.s  IL_00b0

    IL_0087:  ldarg.0
    IL_0088:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::storages
    IL_008d:  ldloc.1
    IL_008e:  callvirt   instance bool class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::Remove(!0)
    IL_0093:  pop
    IL_0094:  ldarg.0
    IL_0095:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::storages
    IL_009a:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Count()
    IL_009f:  brtrue     IL_0134

    IL_00a4:  ldarg.0
    IL_00a5:  ldc.i4.1
    IL_00a6:  stfld      bool PipeSystem.PipeNet::resourceTradersWithLowPowerModeDirty
    IL_00ab:  br         IL_0134

    IL_00b0:  ldarg.1
    IL_00b1:  isinst     PipeSystem.CompConvertToThing
    IL_00b6:  stloc.2
    IL_00b7:  ldloc.2
    IL_00b8:  brfalse.s  IL_00c9

    IL_00ba:  ldarg.0
    IL_00bb:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing> PipeSystem.PipeNet::thingConverters
    IL_00c0:  ldloc.2
    IL_00c1:  callvirt   instance bool class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing>::Remove(!0)
    IL_00c6:  pop
    IL_00c7:  br.s       IL_0134

    IL_00c9:  ldarg.1
    IL_00ca:  isinst     PipeSystem.CompConvertToResource
    IL_00cf:  stloc.3
    IL_00d0:  ldloc.3
    IL_00d1:  brfalse.s  IL_00e2

    IL_00d3:  ldarg.0
    IL_00d4:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToResource> PipeSystem.PipeNet::resourceConverters
    IL_00d9:  ldloc.3
    IL_00da:  callvirt   instance bool class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToResource>::Remove(!0)
    IL_00df:  pop
    IL_00e0:  br.s       IL_0134

    IL_00e2:  ldarg.1
    IL_00e3:  isinst     PipeSystem.CompResourceProcessor
    IL_00e8:  stloc.s    V_4
    IL_00ea:  ldloc.s    V_4
    IL_00ec:  brfalse.s  IL_00fe

    IL_00ee:  ldarg.0
    IL_00ef:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceProcessor> PipeSystem.PipeNet::processors
    IL_00f4:  ldloc.s    V_4
    IL_00f6:  callvirt   instance bool class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceProcessor>::Remove(!0)
    IL_00fb:  pop
    IL_00fc:  br.s       IL_0134

    IL_00fe:  ldarg.1
    IL_00ff:  isinst     PipeSystem.CompRefillWithPipes
    IL_0104:  stloc.s    V_5
    IL_0106:  ldloc.s    V_5
    IL_0108:  brfalse.s  IL_011a

    IL_010a:  ldarg.0
    IL_010b:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompRefillWithPipes> PipeSystem.PipeNet::refillables
    IL_0110:  ldloc.s    V_5
    IL_0112:  callvirt   instance bool class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompRefillWithPipes>::Remove(!0)
    IL_0117:  pop
    IL_0118:  br.s       IL_0134

    IL_011a:  ldarg.1
    IL_011b:  isinst     PipeSystem.CompDeepExtractor
    IL_0120:  stloc.s    V_6
    IL_0122:  ldloc.s    V_6
    IL_0124:  brfalse.s  IL_0134

    IL_0126:  ldarg.0
    IL_0127:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompDeepExtractor> PipeSystem.PipeNet::extractors
    IL_012c:  ldloc.s    V_6
    IL_012e:  callvirt   instance bool class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompDeepExtractor>::Remove(!0)
    IL_0133:  pop
    IL_0134:  ldarg.1
    IL_0135:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_013a:  call       valuetype ['Assembly-CSharp']Verse.CellRect ['Assembly-CSharp']Verse.GenAdj::OccupiedRect(class ['Assembly-CSharp']Verse.Thing)
    IL_013f:  stloc.s    V_8
    IL_0141:  ldloca.s   V_8
    IL_0143:  call       instance valuetype ['Assembly-CSharp']Verse.CellRect/Enumerator ['Assembly-CSharp']Verse.CellRect::GetEnumerator()
    IL_0148:  stloc.s    V_7
    .try
    {
      IL_014a:  br.s       IL_0163

      IL_014c:  ldloca.s   V_7
      IL_014e:  call       instance valuetype ['Assembly-CSharp']Verse.IntVec3 ['Assembly-CSharp']Verse.CellRect/Enumerator::get_Current()
      IL_0153:  stloc.s    V_9
      IL_0155:  ldarg.0
      IL_0156:  ldfld      class ['Assembly-CSharp']Verse.BoolGrid PipeSystem.PipeNet::networkGrid
      IL_015b:  ldloc.s    V_9
      IL_015d:  ldc.i4.0
      IL_015e:  callvirt   instance void ['Assembly-CSharp']Verse.BoolGrid::Set(valuetype ['Assembly-CSharp']Verse.IntVec3,
                                                                                bool)
      IL_0163:  ldloca.s   V_7
      IL_0165:  call       instance bool ['Assembly-CSharp']Verse.CellRect/Enumerator::MoveNext()
      IL_016a:  brtrue.s   IL_014c

      IL_016c:  leave.s    IL_017c

    }  // end .try
    finally
    {
      IL_016e:  ldloca.s   V_7
      IL_0170:  constrained. ['Assembly-CSharp']Verse.CellRect/Enumerator
      IL_0176:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
      IL_017b:  endfinally
    }  // end handler
    IL_017c:  ldarg.0
    IL_017d:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResource> PipeSystem.PipeNet::connectors
    IL_0182:  call       bool ['Assembly-CSharp']Verse.GenList::NullOrEmpty<class PipeSystem.CompResource>(class [mscorlib]System.Collections.Generic.IList`1<!!0>)
    IL_0187:  brfalse.s  IL_01da

    IL_0189:  ldarg.0
    IL_018a:  call       instance float32 PipeSystem.PipeNet::get_OverflowAmount()
    IL_018f:  stloc.s    V_10
    IL_0191:  ldarg.0
    IL_0192:  ldc.r4     0.0
    IL_0197:  call       instance void PipeSystem.PipeNet::set_OverflowAmount(float32)
    IL_019c:  ldarg.0
    IL_019d:  callvirt   instance void PipeSystem.PipeNet::Destroy()
    IL_01a2:  ldloc.s    V_10
    IL_01a4:  ldc.r4     0.0
    IL_01a9:  ble.un.s   IL_01da

    IL_01ab:  ldarg.0
    IL_01ac:  ldfld      class ['Assembly-CSharp']Verse.Map PipeSystem.PipeNet::map
    IL_01b1:  callvirt   instance !!0 ['Assembly-CSharp']Verse.Map::GetComponent<class PipeSystem.PipeNetManager>()
    IL_01b6:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.PipeNet> PipeSystem.PipeNetManager::pipeNets
    IL_01bb:  ldarg.0
    IL_01bc:  ldftn      instance bool PipeSystem.PipeNet::'<UnregisterComp>b__79_0'(class PipeSystem.PipeNet)
    IL_01c2:  newobj     instance void class [mscorlib]System.Predicate`1<class PipeSystem.PipeNet>::.ctor(object,
                                                                                                           native int)
    IL_01c7:  call       !!0 ['Assembly-CSharp']Verse.GenCollection::FirstOrDefault<class PipeSystem.PipeNet>(class [mscorlib]System.Collections.Generic.List`1<!!0>,
                                                                                                              class [mscorlib]System.Predicate`1<!!0>)
    IL_01cc:  dup
    IL_01cd:  brtrue.s   IL_01d1

    IL_01cf:  pop
    IL_01d0:  ret

    IL_01d1:  ldloc.s    V_10
    IL_01d3:  ldc.i4.1
    IL_01d4:  call       instance float32 PipeSystem.PipeNet::DistributeToOverflow(float32,
                                                                                   bool)
    IL_01d9:  pop
    IL_01da:  ret
  } // end of method PipeNet::UnregisterComp

  .method public hidebysig instance float32 
          CurrentStored() cil managed
  {
    // Code size       50 (0x32)
    .maxstack  3
    .locals init (float32 V_0,
             int32 V_1)
    IL_0000:  ldc.r4     0.0
    IL_0005:  stloc.0
    IL_0006:  ldc.i4.0
    IL_0007:  stloc.1
    IL_0008:  br.s       IL_0022

    IL_000a:  ldloc.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::storages
    IL_0011:  ldloc.1
    IL_0012:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Item(int32)
    IL_0017:  callvirt   instance float32 PipeSystem.CompResourceStorage::get_AmountStored()
    IL_001c:  add
    IL_001d:  stloc.0
    IL_001e:  ldloc.1
    IL_001f:  ldc.i4.1
    IL_0020:  add
    IL_0021:  stloc.1
    IL_0022:  ldloc.1
    IL_0023:  ldarg.0
    IL_0024:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::storages
    IL_0029:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Count()
    IL_002e:  blt.s      IL_000a

    IL_0030:  ldloc.0
    IL_0031:  ret
  } // end of method PipeNet::CurrentStored

  .method public hidebysig newslot virtual 
          instance void  PipeSystemTick() cil managed
  {
    // Code size       979 (0x3d3)
    .maxstack  5
    .locals init (float32 V_0,
             float32 V_1,
             float32 V_2,
             class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> V_3,
             float32 V_4,
             float32 V_5,
             float32 V_6,
             int32 V_7,
             class PipeSystem.CompResourceTrader V_8,
             float32 V_9,
             class PipeSystem.CompResourceTrader V_10,
             int32 V_11,
             class PipeSystem.CompResourceTrader V_12,
             class PipeSystem.CompResourceTrader V_13,
             float32 V_14,
             float32 V_15,
             float32 V_16,
             float32 V_17,
             float32 V_18,
             float32 V_19,
             int32 V_20)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool PipeSystem.PipeNet::receiversDirty
    IL_0006:  brtrue.s   IL_0010

    IL_0008:  ldarg.0
    IL_0009:  ldfld      bool PipeSystem.PipeNet::nextTickRDirty
    IL_000e:  brfalse.s  IL_001c

    IL_0010:  ldarg.0
    IL_0011:  ldarg.0
    IL_0012:  call       instance bool PipeSystem.PipeNet::ReceiversDirty()
    IL_0017:  stfld      bool PipeSystem.PipeNet::nextTickRDirty
    IL_001c:  ldarg.0
    IL_001d:  ldfld      bool PipeSystem.PipeNet::producersDirty
    IL_0022:  brfalse.s  IL_002a

    IL_0024:  ldarg.0
    IL_0025:  call       instance void PipeSystem.PipeNet::ProducersDirty()
    IL_002a:  ldc.i4.0
    IL_002b:  stloc.s    V_7
    IL_002d:  br.s       IL_0055

    IL_002f:  ldarg.0
    IL_0030:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::producersOff
    IL_0035:  ldloc.s    V_7
    IL_0037:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::get_Item(int32)
    IL_003c:  stloc.s    V_8
    IL_003e:  ldloc.s    V_8
    IL_0040:  callvirt   instance bool PipeSystem.CompResourceTrader::CanBeOn()
    IL_0045:  brfalse.s  IL_004f

    IL_0047:  ldloc.s    V_8
    IL_0049:  ldc.i4.1
    IL_004a:  callvirt   instance void PipeSystem.CompResourceTrader::set_ResourceOn(bool)
    IL_004f:  ldloc.s    V_7
    IL_0051:  ldc.i4.1
    IL_0052:  add
    IL_0053:  stloc.s    V_7
    IL_0055:  ldloc.s    V_7
    IL_0057:  ldarg.0
    IL_0058:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::producersOff
    IL_005d:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::get_Count()
    IL_0062:  blt.s      IL_002f

    IL_0064:  ldarg.0
    IL_0065:  ldarg.0
    IL_0066:  call       instance float32 PipeSystem.PipeNet::CurrentStored()
    IL_006b:  call       instance void PipeSystem.PipeNet::set_Stored(float32)
    IL_0070:  ldarg.0
    IL_0071:  call       instance float32 PipeSystem.PipeNet::get_TotalConsumptionThisTick()
    IL_0076:  stloc.0
    IL_0077:  ldarg.0
    IL_0078:  call       instance float32 PipeSystem.PipeNet::get_TotalProductionThisTick()
    IL_007d:  stloc.1
    IL_007e:  ldarg.0
    IL_007f:  ldarg.0
    IL_0080:  ldc.r4     0.0
    IL_0085:  dup
    IL_0086:  stloc.s    V_9
    IL_0088:  call       instance void PipeSystem.PipeNet::set_ExtraProductionThisTick(float32)
    IL_008d:  ldloc.s    V_9
    IL_008f:  call       instance void PipeSystem.PipeNet::set_ExtraConsumptionThisTick(float32)
    IL_0094:  ldloc.1
    IL_0095:  ldarg.0
    IL_0096:  call       instance float32 PipeSystem.PipeNet::get_Stored()
    IL_009b:  add
    IL_009c:  stloc.2
    IL_009d:  ldloc.2
    IL_009e:  ldloc.0
    IL_009f:  bge.un.s   IL_011e

    IL_00a1:  ldarg.0
    IL_00a2:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::receiversOn
    IL_00a7:  call       bool ['Assembly-CSharp']Verse.GenCollection::Any<class PipeSystem.CompResourceTrader>(class [mscorlib]System.Collections.Generic.List`1<!!0>)
    IL_00ac:  brfalse.s  IL_011e

    IL_00ae:  ldstr      "Turning off random building"
    IL_00b3:  call       void PipeSystem.PipeSystemDebug::Message(string)
    IL_00b8:  ldarg.0
    IL_00b9:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::receiversOn
    IL_00be:  call       !!0 ['Assembly-CSharp']Verse.GenCollection::RandomElement<class PipeSystem.CompResourceTrader>(class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_00c3:  stloc.s    V_10
    IL_00c5:  ldloc.s    V_10
    IL_00c7:  callvirt   instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_00cc:  ldfld      bool PipeSystem.CompProperties_ResourceTrader::visualOnlyConsumption
    IL_00d1:  brtrue.s   IL_00f1

    IL_00d3:  ldarg.0
    IL_00d4:  ldc.r4     0.0
    IL_00d9:  ldarg.0
    IL_00da:  call       instance float32 PipeSystem.PipeNet::get_Consumption()
    IL_00df:  ldloc.s    V_10
    IL_00e1:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_00e6:  sub
    IL_00e7:  call       float32 [mscorlib]System.Math::Max(float32,
                                                            float32)
    IL_00ec:  call       instance void PipeSystem.PipeNet::set_Consumption(float32)
    IL_00f1:  ldarg.0
    IL_00f2:  ldc.r4     0.0
    IL_00f7:  ldarg.0
    IL_00f8:  call       instance float32 PipeSystem.PipeNet::get_VisualConsumption()
    IL_00fd:  ldloc.s    V_10
    IL_00ff:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_0104:  sub
    IL_0105:  call       float32 [mscorlib]System.Math::Max(float32,
                                                            float32)
    IL_010a:  call       instance void PipeSystem.PipeNet::set_VisualConsumption(float32)
    IL_010f:  ldloc.s    V_10
    IL_0111:  ldc.i4.0
    IL_0112:  callvirt   instance void PipeSystem.CompResourceTrader::set_ResourceOn(bool)
    IL_0117:  ldarg.0
    IL_0118:  ldc.i4.1
    IL_0119:  stfld      bool PipeSystem.PipeNet::receiversDirty
    IL_011e:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::.ctor()
    IL_0123:  stloc.3
    IL_0124:  ldc.i4.0
    IL_0125:  stloc.s    V_11
    IL_0127:  br.s       IL_0161

    IL_0129:  ldarg.0
    IL_012a:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::receiversOff
    IL_012f:  ldloc.s    V_11
    IL_0131:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::get_Item(int32)
    IL_0136:  stloc.s    V_12
    IL_0138:  ldloc.s    V_12
    IL_013a:  callvirt   instance bool PipeSystem.CompResourceTrader::CanBeOn()
    IL_013f:  brfalse.s  IL_015b

    IL_0141:  ldloc.2
    IL_0142:  ldloc.0
    IL_0143:  ldloc.s    V_12
    IL_0145:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_014a:  add
    IL_014b:  sub
    IL_014c:  ldc.r4     0.0
    IL_0151:  ble.un.s   IL_015b

    IL_0153:  ldloc.3
    IL_0154:  ldloc.s    V_12
    IL_0156:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::Add(!0)
    IL_015b:  ldloc.s    V_11
    IL_015d:  ldc.i4.1
    IL_015e:  add
    IL_015f:  stloc.s    V_11
    IL_0161:  ldloc.s    V_11
    IL_0163:  ldarg.0
    IL_0164:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::receiversOff
    IL_0169:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::get_Count()
    IL_016e:  blt.s      IL_0129

    IL_0170:  ldloc.3
    IL_0171:  call       bool ['Assembly-CSharp']Verse.GenCollection::Any<class PipeSystem.CompResourceTrader>(class [mscorlib]System.Collections.Generic.List`1<!!0>)
    IL_0176:  brfalse.s  IL_01cf

    IL_0178:  ldstr      "Turning on random building"
    IL_017d:  call       void PipeSystem.PipeSystemDebug::Message(string)
    IL_0182:  ldloc.3
    IL_0183:  call       !!0 ['Assembly-CSharp']Verse.GenCollection::RandomElement<class PipeSystem.CompResourceTrader>(class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_0188:  stloc.s    V_13
    IL_018a:  ldloc.s    V_13
    IL_018c:  callvirt   instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_0191:  ldfld      bool PipeSystem.CompProperties_ResourceTrader::visualOnlyConsumption
    IL_0196:  brtrue.s   IL_01ac

    IL_0198:  ldarg.0
    IL_0199:  ldarg.0
    IL_019a:  call       instance float32 PipeSystem.PipeNet::get_Consumption()
    IL_019f:  ldloc.s    V_13
    IL_01a1:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_01a6:  add
    IL_01a7:  call       instance void PipeSystem.PipeNet::set_Consumption(float32)
    IL_01ac:  ldarg.0
    IL_01ad:  ldarg.0
    IL_01ae:  call       instance float32 PipeSystem.PipeNet::get_VisualConsumption()
    IL_01b3:  ldloc.s    V_13
    IL_01b5:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_01ba:  add
    IL_01bb:  call       instance void PipeSystem.PipeNet::set_VisualConsumption(float32)
    IL_01c0:  ldloc.s    V_13
    IL_01c2:  ldc.i4.1
    IL_01c3:  callvirt   instance void PipeSystem.CompResourceTrader::set_ResourceOn(bool)
    IL_01c8:  ldarg.0
    IL_01c9:  ldc.i4.1
    IL_01ca:  stfld      bool PipeSystem.PipeNet::receiversDirty
    IL_01cf:  ldloc.1
    IL_01d0:  ldloc.0
    IL_01d1:  sub
    IL_01d2:  stloc.s    V_4
    IL_01d4:  ldloc.s    V_4
    IL_01d6:  ldc.r4     0.0
    IL_01db:  bge.un.s   IL_0229

    IL_01dd:  ldloc.s    V_4
    IL_01df:  ldarg.0
    IL_01e0:  ldloc.s    V_4
    IL_01e2:  neg
    IL_01e3:  call       instance float32 PipeSystem.PipeNet::DrawFromOverflow(float32)
    IL_01e8:  add
    IL_01e9:  stloc.s    V_4
    IL_01eb:  ldarg.0
    IL_01ec:  ldloc.s    V_4
    IL_01ee:  neg
    IL_01ef:  ldarg.0
    IL_01f0:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::storages
    IL_01f5:  call       instance void PipeSystem.PipeNet::DrawAmongStorage(float32,
                                                                            class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>)
    IL_01fa:  ldarg.0
    IL_01fb:  ldarg.0
    IL_01fc:  call       instance float32 PipeSystem.PipeNet::get_Stored()
    IL_0201:  ldc.i4.1
    IL_0202:  call       instance float32 PipeSystem.PipeNet::DistributeAmongRefillables(float32,
                                                                                         bool)
    IL_0207:  pop
    IL_0208:  ldarg.0
    IL_0209:  ldarg.0
    IL_020a:  call       instance float32 PipeSystem.PipeNet::get_Stored()
    IL_020f:  ldc.i4.1
    IL_0210:  call       instance float32 PipeSystem.PipeNet::DistributeAmongProcessors(float32,
                                                                                        bool)
    IL_0215:  pop
    IL_0216:  ldarg.0
    IL_0217:  ldarg.0
    IL_0218:  call       instance float32 PipeSystem.PipeNet::get_Stored()
    IL_021d:  ldc.i4.1
    IL_021e:  call       instance float32 PipeSystem.PipeNet::DistributeAmongConverters(float32,
                                                                                        bool)
    IL_0223:  pop
    IL_0224:  br         IL_02f7

    IL_0229:  ldarg.0
    IL_022a:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::storages
    IL_022f:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Count()
    IL_0234:  ldc.i4.0
    IL_0235:  ble.s      IL_027f

    IL_0237:  ldarg.0
    IL_0238:  ldloc.s    V_4
    IL_023a:  ldloca.s   V_14
    IL_023c:  call       instance void PipeSystem.PipeNet::DistributeAmongStorage(float32,
                                                                                  float32&)
    IL_0241:  ldarg.0
    IL_0242:  ldloc.s    V_4
    IL_0244:  ldloc.s    V_14
    IL_0246:  sub
    IL_0247:  call       instance float32 PipeSystem.PipeNet::DistributeToOverflow(float32)
    IL_024c:  pop
    IL_024d:  ldarg.0
    IL_024e:  call       instance void PipeSystem.PipeNet::GetFromConverters()
    IL_0253:  ldarg.0
    IL_0254:  ldarg.0
    IL_0255:  call       instance float32 PipeSystem.PipeNet::get_Stored()
    IL_025a:  ldc.i4.1
    IL_025b:  call       instance float32 PipeSystem.PipeNet::DistributeAmongRefillables(float32,
                                                                                         bool)
    IL_0260:  pop
    IL_0261:  ldarg.0
    IL_0262:  ldarg.0
    IL_0263:  call       instance float32 PipeSystem.PipeNet::get_Stored()
    IL_0268:  ldc.i4.1
    IL_0269:  call       instance float32 PipeSystem.PipeNet::DistributeAmongProcessors(float32,
                                                                                        bool)
    IL_026e:  pop
    IL_026f:  ldarg.0
    IL_0270:  ldarg.0
    IL_0271:  call       instance float32 PipeSystem.PipeNet::get_Stored()
    IL_0276:  ldc.i4.1
    IL_0277:  call       instance float32 PipeSystem.PipeNet::DistributeAmongConverters(float32,
                                                                                        bool)
    IL_027c:  pop
    IL_027d:  br.s       IL_02f7

    IL_027f:  ldarg.0
    IL_0280:  ldfld      bool PipeSystem.PipeNet::resourceTradersWithLowPowerModeDirty
    IL_0285:  ldarg.0
    IL_0286:  ldc.r4     inf
    IL_028b:  call       instance float32 PipeSystem.PipeNet::DrawFromOverflow(float32)
    IL_0290:  stloc.s    V_15
    IL_0292:  ldloc.s    V_4
    IL_0294:  ldloc.s    V_15
    IL_0296:  add
    IL_0297:  stloc.s    V_4
    IL_0299:  ldloc.s    V_4
    IL_029b:  ldarg.0
    IL_029c:  ldloc.s    V_4
    IL_029e:  ldc.i4.1
    IL_029f:  call       instance float32 PipeSystem.PipeNet::DistributeAmongRefillables(float32,
                                                                                         bool)
    IL_02a4:  sub
    IL_02a5:  stloc.s    V_4
    IL_02a7:  ldloc.s    V_4
    IL_02a9:  ldarg.0
    IL_02aa:  ldloc.s    V_4
    IL_02ac:  ldc.i4.1
    IL_02ad:  call       instance float32 PipeSystem.PipeNet::DistributeAmongProcessors(float32,
                                                                                        bool)
    IL_02b2:  sub
    IL_02b3:  stloc.s    V_4
    IL_02b5:  ldloc.s    V_4
    IL_02b7:  ldarg.0
    IL_02b8:  ldloc.s    V_4
    IL_02ba:  ldc.i4.1
    IL_02bb:  call       instance float32 PipeSystem.PipeNet::DistributeAmongConverters(float32,
                                                                                        bool)
    IL_02c0:  sub
    IL_02c1:  stloc.s    V_4
    IL_02c3:  ldarg.0
    IL_02c4:  ldloc.s    V_4
    IL_02c6:  ldloc.s    V_15
    IL_02c8:  ldc.i4.0
    IL_02c9:  call       instance float32 PipeSystem.PipeNet::DistributeToOverflow(float32,
                                                                                   float32,
                                                                                   bool)
    IL_02ce:  pop
    IL_02cf:  brtrue.s   IL_02f7

    IL_02d1:  ldarg.0
    IL_02d2:  ldfld      float32 PipeSystem.PipeNet::overflowAmount
    IL_02d7:  ldc.r4     0.0
    IL_02dc:  cgt.un
    IL_02de:  ldc.i4.0
    IL_02df:  ceq
    IL_02e1:  ldloc.s    V_15
    IL_02e3:  ldc.r4     0.0
    IL_02e8:  cgt.un
    IL_02ea:  ldc.i4.0
    IL_02eb:  ceq
    IL_02ed:  xor
    IL_02ee:  brtrue.s   IL_02f7

    IL_02f0:  ldarg.0
    IL_02f1:  ldc.i4.0
    IL_02f2:  stfld      bool PipeSystem.PipeNet::resourceTradersWithLowPowerModeDirty
    IL_02f7:  ldarg.0
    IL_02f8:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::markedForTransfer
    IL_02fd:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Count()
    IL_0302:  ldc.i4.0
    IL_0303:  ble        IL_0391

    IL_0308:  ldc.r4     0.0
    IL_030d:  stloc.s    V_16
    IL_030f:  ldc.i4.0
    IL_0310:  stloc.s    V_20
    IL_0312:  br.s       IL_0331

    IL_0314:  ldloc.s    V_16
    IL_0316:  ldarg.0
    IL_0317:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::markedForTransfer
    IL_031c:  ldloc.s    V_20
    IL_031e:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Item(int32)
    IL_0323:  callvirt   instance float32 PipeSystem.CompResourceStorage::get_AmountStored()
    IL_0328:  add
    IL_0329:  stloc.s    V_16
    IL_032b:  ldloc.s    V_20
    IL_032d:  ldc.i4.1
    IL_032e:  add
    IL_032f:  stloc.s    V_20
    IL_0331:  ldloc.s    V_20
    IL_0333:  ldarg.0
    IL_0334:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::markedForTransfer
    IL_0339:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Count()
    IL_033e:  blt.s      IL_0314

    IL_0340:  ldarg.0
    IL_0341:  call       instance float32 PipeSystem.PipeNet::get_AvailableCapacity()
    IL_0346:  stloc.s    V_17
    IL_0348:  ldloc.s    V_17
    IL_034a:  ldloc.s    V_16
    IL_034c:  bgt.s      IL_0352

    IL_034e:  ldloc.s    V_17
    IL_0350:  br.s       IL_0354

    IL_0352:  ldloc.s    V_16
    IL_0354:  stloc.s    V_18
    IL_0356:  ldloc.s    V_18
    IL_0358:  ldarg.0
    IL_0359:  ldfld      class PipeSystem.PipeNetDef PipeSystem.PipeNet::def
    IL_035e:  ldfld      float32 PipeSystem.PipeNetDef::transferAmount
    IL_0363:  bgt.s      IL_0369

    IL_0365:  ldloc.s    V_18
    IL_0367:  br.s       IL_0374

    IL_0369:  ldarg.0
    IL_036a:  ldfld      class PipeSystem.PipeNetDef PipeSystem.PipeNet::def
    IL_036f:  ldfld      float32 PipeSystem.PipeNetDef::transferAmount
    IL_0374:  stloc.s    V_19
    IL_0376:  ldarg.0
    IL_0377:  ldloc.s    V_19
    IL_0379:  ldarg.0
    IL_037a:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::markedForTransfer
    IL_037f:  ldc.i4.0
    IL_0380:  call       instance void PipeSystem.PipeNet::DrawAmongStorage(float32,
                                                                            class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>,
                                                                            bool)
    IL_0385:  ldarg.0
    IL_0386:  ldloc.s    V_19
    IL_0388:  ldloca.s   V_9
    IL_038a:  ldnull
    IL_038b:  ldc.i4.0
    IL_038c:  call       instance void PipeSystem.PipeNet::DistributeAmongStorage(float32,
                                                                                  float32&,
                                                                                  class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>,
                                                                                  bool)
    IL_0391:  ldarg.0
    IL_0392:  call       instance float32 PipeSystem.PipeNet::get_AvailableCapacity()
    IL_0397:  stloc.s    V_5
    IL_0399:  ldarg.0
    IL_039a:  call       instance float32 PipeSystem.PipeNet::get_AvailableCapacityLastTick()
    IL_039f:  stloc.s    V_6
    IL_03a1:  ldarg.0
    IL_03a2:  ldloc.s    V_5
    IL_03a4:  call       instance void PipeSystem.PipeNet::set_AvailableCapacityLastTick(float32)
    IL_03a9:  ldarg.0
    IL_03aa:  ldfld      bool PipeSystem.PipeNet::resourceTradersWithLowPowerModeDirty
    IL_03af:  brtrue.s   IL_03cc

    IL_03b1:  ldloc.s    V_5
    IL_03b3:  ldc.r4     0.0
    IL_03b8:  cgt.un
    IL_03ba:  ldc.i4.0
    IL_03bb:  ceq
    IL_03bd:  ldloc.s    V_6
    IL_03bf:  ldc.r4     0.0
    IL_03c4:  cgt.un
    IL_03c6:  ldc.i4.0
    IL_03c7:  ceq
    IL_03c9:  xor
    IL_03ca:  brfalse.s  IL_03d2

    IL_03cc:  ldarg.0
    IL_03cd:  call       instance void PipeSystem.PipeNet::UpdateLowPowerModeConsumers()
    IL_03d2:  ret
  } // end of method PipeNet::PipeSystemTick

  .method assembly hidebysig instance float32 
          DistributeAmongProcessors(float32 available,
                                    [opt] bool drawFromStorage) cil managed
  {
    .param [2] = bool(true)
    // Code size       105 (0x69)
    .maxstack  3
    .locals init (float32 V_0,
             int32 V_1,
             float32 V_2)
    IL_0000:  ldc.r4     0.0
    IL_0005:  stloc.0
    IL_0006:  ldarg.0
    IL_0007:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceProcessor> PipeSystem.PipeNet::processors
    IL_000c:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceProcessor>::get_Count()
    IL_0011:  brfalse.s  IL_001b

    IL_0013:  ldarg.1
    IL_0014:  ldc.r4     0.0
    IL_0019:  bgt.un.s   IL_001d

    IL_001b:  ldloc.0
    IL_001c:  ret

    IL_001d:  ldc.i4.0
    IL_001e:  stloc.1
    IL_001f:  br.s       IL_0049

    IL_0021:  ldarg.0
    IL_0022:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceProcessor> PipeSystem.PipeNet::processors
    IL_0027:  ldloc.1
    IL_0028:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceProcessor>::get_Item(int32)
    IL_002d:  ldarg.1
    IL_002e:  callvirt   instance float32 PipeSystem.CompResourceProcessor::PushTo(float32)
    IL_0033:  stloc.2
    IL_0034:  ldarg.1
    IL_0035:  ldloc.2
    IL_0036:  sub
    IL_0037:  starg.s    available
    IL_0039:  ldloc.0
    IL_003a:  ldloc.2
    IL_003b:  add
    IL_003c:  stloc.0
    IL_003d:  ldarg.1
    IL_003e:  ldc.r4     0.0
    IL_0043:  ble.s      IL_0057

    IL_0045:  ldloc.1
    IL_0046:  ldc.i4.1
    IL_0047:  add
    IL_0048:  stloc.1
    IL_0049:  ldloc.1
    IL_004a:  ldarg.0
    IL_004b:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceProcessor> PipeSystem.PipeNet::processors
    IL_0050:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceProcessor>::get_Count()
    IL_0055:  blt.s      IL_0021

    IL_0057:  ldarg.2
    IL_0058:  brfalse.s  IL_0067

    IL_005a:  ldarg.0
    IL_005b:  ldloc.0
    IL_005c:  ldarg.0
    IL_005d:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::storages
    IL_0062:  call       instance void PipeSystem.PipeNet::DrawAmongStorage(float32,
                                                                            class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>)
    IL_0067:  ldloc.0
    IL_0068:  ret
  } // end of method PipeNet::DistributeAmongProcessors

  .method assembly hidebysig instance float32 
          DistributeAmongRefillables(float32 available,
                                     [opt] bool drawFromStorage) cil managed
  {
    .param [2] = bool(true)
    // Code size       105 (0x69)
    .maxstack  3
    .locals init (float32 V_0,
             int32 V_1,
             float32 V_2)
    IL_0000:  ldc.r4     0.0
    IL_0005:  stloc.0
    IL_0006:  ldarg.0
    IL_0007:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompRefillWithPipes> PipeSystem.PipeNet::refillables
    IL_000c:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompRefillWithPipes>::get_Count()
    IL_0011:  brfalse.s  IL_001b

    IL_0013:  ldarg.1
    IL_0014:  ldc.r4     0.0
    IL_0019:  bgt.un.s   IL_001d

    IL_001b:  ldloc.0
    IL_001c:  ret

    IL_001d:  ldc.i4.0
    IL_001e:  stloc.1
    IL_001f:  br.s       IL_0049

    IL_0021:  ldarg.0
    IL_0022:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompRefillWithPipes> PipeSystem.PipeNet::refillables
    IL_0027:  ldloc.1
    IL_0028:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompRefillWithPipes>::get_Item(int32)
    IL_002d:  ldarg.1
    IL_002e:  callvirt   instance float32 PipeSystem.CompRefillWithPipes::Refill(float32)
    IL_0033:  stloc.2
    IL_0034:  ldarg.1
    IL_0035:  ldloc.2
    IL_0036:  sub
    IL_0037:  starg.s    available
    IL_0039:  ldloc.0
    IL_003a:  ldloc.2
    IL_003b:  add
    IL_003c:  stloc.0
    IL_003d:  ldarg.1
    IL_003e:  ldc.r4     0.0
    IL_0043:  ble.s      IL_0057

    IL_0045:  ldloc.1
    IL_0046:  ldc.i4.1
    IL_0047:  add
    IL_0048:  stloc.1
    IL_0049:  ldloc.1
    IL_004a:  ldarg.0
    IL_004b:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompRefillWithPipes> PipeSystem.PipeNet::refillables
    IL_0050:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompRefillWithPipes>::get_Count()
    IL_0055:  blt.s      IL_0021

    IL_0057:  ldarg.2
    IL_0058:  brfalse.s  IL_0067

    IL_005a:  ldarg.0
    IL_005b:  ldloc.0
    IL_005c:  ldarg.0
    IL_005d:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::storages
    IL_0062:  call       instance void PipeSystem.PipeNet::DrawAmongStorage(float32,
                                                                            class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>)
    IL_0067:  ldloc.0
    IL_0068:  ret
  } // end of method PipeNet::DistributeAmongRefillables

  .method assembly hidebysig instance float32 
          DistributeAmongConverters(float32 available,
                                    [opt] bool drawFromStorage) cil managed
  {
    .param [2] = bool(true)
    // Code size       282 (0x11a)
    .maxstack  3
    .locals init (float32 V_0,
             class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing> V_1,
             int32 V_2,
             class PipeSystem.CompConvertToThing V_3,
             int32 V_4,
             class PipeSystem.CompConvertToThing V_5,
             int32 V_6,
             int32 V_7,
             int32 V_8,
             int32 V_9)
    IL_0000:  ldc.r4     0.0
    IL_0005:  stloc.0
    IL_0006:  ldarg.0
    IL_0007:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing> PipeSystem.PipeNet::thingConverters
    IL_000c:  call       bool ['Assembly-CSharp']Verse.GenCollection::Any<class PipeSystem.CompConvertToThing>(class [mscorlib]System.Collections.Generic.List`1<!!0>)
    IL_0011:  brfalse.s  IL_001b

    IL_0013:  ldarg.1
    IL_0014:  ldc.r4     0.0
    IL_0019:  bgt.un.s   IL_001d

    IL_001b:  ldloc.0
    IL_001c:  ret

    IL_001d:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing>::.ctor()
    IL_0022:  stloc.1
    IL_0023:  ldc.i4.0
    IL_0024:  stloc.2
    IL_0025:  br.s       IL_0050

    IL_0027:  ldarg.0
    IL_0028:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing> PipeSystem.PipeNet::thingConverters
    IL_002d:  ldloc.2
    IL_002e:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing>::get_Item(int32)
    IL_0033:  stloc.3
    IL_0034:  ldloc.3
    IL_0035:  callvirt   instance bool PipeSystem.CompConvertToThing::get_CanOutputNow()
    IL_003a:  brfalse.s  IL_004c

    IL_003c:  ldloc.3
    IL_003d:  callvirt   instance int32 PipeSystem.CompConvertToThing::get_MaxCanOutput()
    IL_0042:  ldc.i4.0
    IL_0043:  ble.s      IL_004c

    IL_0045:  ldloc.1
    IL_0046:  ldloc.3
    IL_0047:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing>::Add(!0)
    IL_004c:  ldloc.2
    IL_004d:  ldc.i4.1
    IL_004e:  add
    IL_004f:  stloc.2
    IL_0050:  ldloc.2
    IL_0051:  ldarg.0
    IL_0052:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing> PipeSystem.PipeNet::thingConverters
    IL_0057:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing>::get_Count()
    IL_005c:  blt.s      IL_0027

    IL_005e:  ldloc.1
    IL_005f:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing>::get_Count()
    IL_0064:  brtrue.s   IL_0068

    IL_0066:  ldloc.0
    IL_0067:  ret

    IL_0068:  ldc.i4.0
    IL_0069:  stloc.s    V_4
    IL_006b:  br         IL_00fb

    IL_0070:  ldloc.1
    IL_0071:  ldloc.s    V_4
    IL_0073:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing>::get_Item(int32)
    IL_0078:  stloc.s    V_5
    IL_007a:  ldloc.s    V_5
    IL_007c:  callvirt   instance bool PipeSystem.CompConvertToThing::get_CanOutputNow()
    IL_0081:  brfalse.s  IL_00f5

    IL_0083:  ldarg.1
    IL_0084:  ldloc.s    V_5
    IL_0086:  callvirt   instance class PipeSystem.CompProperties_ConvertResourceToThing PipeSystem.CompConvertToThing::get_Props()
    IL_008b:  ldfld      int32 PipeSystem.CompProperties_ConvertResourceToThing::ratio
    IL_0090:  conv.r4
    IL_0091:  div
    IL_0092:  conv.i4
    IL_0093:  stloc.s    V_6
    IL_0095:  ldloc.s    V_5
    IL_0097:  callvirt   instance int32 PipeSystem.CompConvertToThing::get_MaxCanOutput()
    IL_009c:  stloc.s    V_7
    IL_009e:  ldloc.s    V_7
    IL_00a0:  ldloc.s    V_6
    IL_00a2:  bgt.s      IL_00a8

    IL_00a4:  ldloc.s    V_7
    IL_00a6:  br.s       IL_00aa

    IL_00a8:  ldloc.s    V_6
    IL_00aa:  stloc.s    V_8
    IL_00ac:  ldloc.s    V_8
    IL_00ae:  ldc.i4.0
    IL_00af:  ble.s      IL_0108

    IL_00b1:  ldloc.s    V_5
    IL_00b3:  ldloc.s    V_8
    IL_00b5:  callvirt   instance void PipeSystem.CompConvertToThing::OutputResource(int32)
    IL_00ba:  ldloc.s    V_8
    IL_00bc:  ldloc.s    V_5
    IL_00be:  callvirt   instance class PipeSystem.CompProperties_ConvertResourceToThing PipeSystem.CompConvertToThing::get_Props()
    IL_00c3:  ldfld      int32 PipeSystem.CompProperties_ConvertResourceToThing::ratio
    IL_00c8:  mul
    IL_00c9:  stloc.s    V_9
    IL_00cb:  ldloc.0
    IL_00cc:  ldloc.s    V_9
    IL_00ce:  conv.r4
    IL_00cf:  add
    IL_00d0:  stloc.0
    IL_00d1:  ldarg.1
    IL_00d2:  ldloc.s    V_9
    IL_00d4:  conv.r4
    IL_00d5:  sub
    IL_00d6:  starg.s    available
    IL_00d8:  ldstr      "Converted {0} resource for {1}"
    IL_00dd:  ldloc.s    V_9
    IL_00df:  box        [mscorlib]System.Int32
    IL_00e4:  ldloc.s    V_8
    IL_00e6:  box        [mscorlib]System.Int32
    IL_00eb:  call       string [mscorlib]System.String::Format(string,
                                                                object,
                                                                object)
    IL_00f0:  call       void PipeSystem.PipeSystemDebug::Message(string)
    IL_00f5:  ldloc.s    V_4
    IL_00f7:  ldc.i4.1
    IL_00f8:  add
    IL_00f9:  stloc.s    V_4
    IL_00fb:  ldloc.s    V_4
    IL_00fd:  ldloc.1
    IL_00fe:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing>::get_Count()
    IL_0103:  blt        IL_0070

    IL_0108:  ldarg.2
    IL_0109:  brfalse.s  IL_0118

    IL_010b:  ldarg.0
    IL_010c:  ldloc.0
    IL_010d:  ldarg.0
    IL_010e:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::storages
    IL_0113:  call       instance void PipeSystem.PipeNet::DrawAmongStorage(float32,
                                                                            class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>)
    IL_0118:  ldloc.0
    IL_0119:  ret
  } // end of method PipeNet::DistributeAmongConverters

  .method assembly hidebysig instance void 
          GetFromConverters() cil managed
  {
    // Code size       225 (0xe1)
    .maxstack  4
    .locals init (int32 V_0,
             class PipeSystem.CompConvertToResource V_1,
             class ['Assembly-CSharp']Verse.Thing V_2,
             int32 V_3,
             float32 V_4,
             float32 V_5)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br         IL_00cf

    IL_0007:  ldarg.0
    IL_0008:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToResource> PipeSystem.PipeNet::resourceConverters
    IL_000d:  ldloc.0
    IL_000e:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToResource>::get_Item(int32)
    IL_0013:  stloc.1
    IL_0014:  ldloc.1
    IL_0015:  callvirt   instance bool PipeSystem.CompConvertToResource::get_CanInputNow()
    IL_001a:  brfalse    IL_00cb

    IL_001f:  ldloc.1
    IL_0020:  callvirt   instance class ['Assembly-CSharp']Verse.Thing PipeSystem.CompConvertToResource::get_HeldThing()
    IL_0025:  stloc.2
    IL_0026:  ldloc.2
    IL_0027:  brfalse    IL_00cb

    IL_002c:  ldloc.2
    IL_002d:  ldfld      int32 ['Assembly-CSharp']Verse.Thing::stackCount
    IL_0032:  ldloc.1
    IL_0033:  callvirt   instance class PipeSystem.CompProperties_ConvertThingToResource PipeSystem.CompConvertToResource::get_Props()
    IL_0038:  ldfld      int32 PipeSystem.CompProperties_ConvertThingToResource::ratio
    IL_003d:  mul
    IL_003e:  stloc.3
    IL_003f:  ldarg.0
    IL_0040:  ldfld      class PipeSystem.PipeNetDef PipeSystem.PipeNet::def
    IL_0045:  ldfld      float32 PipeSystem.PipeNetDef::convertAmount
    IL_004a:  ldc.r4     0.0
    IL_004f:  ble.un.s   IL_007c

    IL_0051:  ldc.i4.3
    IL_0052:  newarr     [mscorlib]System.Single
    IL_0057:  dup
    IL_0058:  ldc.i4.0
    IL_0059:  ldloc.3
    IL_005a:  conv.r4
    IL_005b:  stelem.r4
    IL_005c:  dup
    IL_005d:  ldc.i4.1
    IL_005e:  ldarg.0
    IL_005f:  call       instance float32 PipeSystem.PipeNet::get_AvailableCapacity()
    IL_0064:  stelem.r4
    IL_0065:  dup
    IL_0066:  ldc.i4.2
    IL_0067:  ldarg.0
    IL_0068:  ldfld      class PipeSystem.PipeNetDef PipeSystem.PipeNet::def
    IL_006d:  ldfld      float32 PipeSystem.PipeNetDef::convertAmount
    IL_0072:  stelem.r4
    IL_0073:  call       float32 [UnityEngine.CoreModule]UnityEngine.Mathf::Min(float32[])
    IL_0078:  stloc.s    V_4
    IL_007a:  br.s       IL_008b

    IL_007c:  ldloc.3
    IL_007d:  conv.r4
    IL_007e:  ldarg.0
    IL_007f:  call       instance float32 PipeSystem.PipeNet::get_AvailableCapacity()
    IL_0084:  call       float32 [UnityEngine.CoreModule]UnityEngine.Mathf::Min(float32,
                                                                                float32)
    IL_0089:  stloc.s    V_4
    IL_008b:  ldloc.s    V_4
    IL_008d:  ldc.r4     0.0
    IL_0092:  ble.un.s   IL_00cb

    IL_0094:  ldarg.0
    IL_0095:  ldloc.s    V_4
    IL_0097:  ldloca.s   V_5
    IL_0099:  call       instance void PipeSystem.PipeNet::DistributeAmongStorage(float32,
                                                                                  float32&)
    IL_009e:  ldloc.3
    IL_009f:  conv.r4
    IL_00a0:  ldloc.s    V_4
    IL_00a2:  bne.un.s   IL_00ad

    IL_00a4:  ldloc.2
    IL_00a5:  ldc.i4.0
    IL_00a6:  callvirt   instance void ['Assembly-CSharp']Verse.Entity::DeSpawn(valuetype ['Assembly-CSharp']Verse.DestroyMode)
    IL_00ab:  br.s       IL_00cb

    IL_00ad:  ldloc.2
    IL_00ae:  dup
    IL_00af:  ldfld      int32 ['Assembly-CSharp']Verse.Thing::stackCount
    IL_00b4:  ldloc.s    V_4
    IL_00b6:  ldloc.1
    IL_00b7:  callvirt   instance class PipeSystem.CompProperties_ConvertThingToResource PipeSystem.CompConvertToResource::get_Props()
    IL_00bc:  ldfld      int32 PipeSystem.CompProperties_ConvertThingToResource::ratio
    IL_00c1:  conv.r4
    IL_00c2:  div
    IL_00c3:  conv.i4
    IL_00c4:  sub
    IL_00c5:  stfld      int32 ['Assembly-CSharp']Verse.Thing::stackCount
    IL_00ca:  ret

    IL_00cb:  ldloc.0
    IL_00cc:  ldc.i4.1
    IL_00cd:  add
    IL_00ce:  stloc.0
    IL_00cf:  ldloc.0
    IL_00d0:  ldarg.0
    IL_00d1:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToResource> PipeSystem.PipeNet::resourceConverters
    IL_00d6:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToResource>::get_Count()
    IL_00db:  blt        IL_0007

    IL_00e0:  ret
  } // end of method PipeNet::GetFromConverters

  .method public hidebysig instance void 
          DistributeAmongStorage(float32 amount) cil managed
  {
    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       10 (0xa)
    .maxstack  3
    .locals init (float32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldloca.s   V_0
    IL_0004:  call       instance void PipeSystem.PipeNet::DistributeAmongStorage(float32,
                                                                                  float32&)
    IL_0009:  ret
  } // end of method PipeNet::DistributeAmongStorage

  .method public hidebysig instance void 
          DistributeAmongStorage(float32 amount,
                                 [out] float32& stored) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  ldnull
    IL_0004:  ldc.i4.1
    IL_0005:  call       instance void PipeSystem.PipeNet::DistributeAmongStorage(float32,
                                                                                  float32&,
                                                                                  class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>,
                                                                                  bool)
    IL_000a:  ret
  } // end of method PipeNet::DistributeAmongStorage

  .method public hidebysig instance void 
          DistributeAmongStorage(float32 amount,
                                 [opt] class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> storages,
                                 [opt] bool allowOverflow) cil managed
  {
    .param [2] = nullref
    .param [3] = bool(true)
    // Code size       12 (0xc)
    .maxstack  5
    .locals init (float32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldloca.s   V_0
    IL_0004:  ldarg.2
    IL_0005:  ldarg.3
    IL_0006:  call       instance void PipeSystem.PipeNet::DistributeAmongStorage(float32,
                                                                                  float32&,
                                                                                  class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>,
                                                                                  bool)
    IL_000b:  ret
  } // end of method PipeNet::DistributeAmongStorage

  .method public hidebysig instance void 
          DistributeAmongStorage(float32 amount,
                                 [out] float32& stored,
                                 [opt] class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> storages,
                                 [opt] bool allowOverflow) cil managed
  {
    .param [3] = nullref
    .param [4] = bool(true)
    // Code size       299 (0x12b)
    .maxstack  4
    .locals init (class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> V_0,
             float32 V_1,
             int32 V_2,
             int32 V_3,
             class PipeSystem.CompResourceStorage V_4,
             float32 V_5,
             int32 V_6,
             class PipeSystem.CompResourceStorage V_7,
             float32 V_8)
    IL_0000:  ldarg.2
    IL_0001:  ldc.r4     0.0
    IL_0006:  stind.r4
    IL_0007:  ldarg.1
    IL_0008:  ldc.r4     0.0
    IL_000d:  bgt.un.s   IL_0010

    IL_000f:  ret

    IL_0010:  ldarg.3
    IL_0011:  brtrue.s   IL_001b

    IL_0013:  ldarg.0
    IL_0014:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::storages
    IL_0019:  starg.s    storages
    IL_001b:  ldarg.3
    IL_001c:  call       bool ['Assembly-CSharp']Verse.GenCollection::Any<class PipeSystem.CompResourceStorage>(class [mscorlib]System.Collections.Generic.List`1<!!0>)
    IL_0021:  brtrue.s   IL_002d

    IL_0023:  ldarg.2
    IL_0024:  ldarg.0
    IL_0025:  ldarg.1
    IL_0026:  call       instance float32 PipeSystem.PipeNet::DistributeToOverflow(float32)
    IL_002b:  stind.r4
    IL_002c:  ret

    IL_002d:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::.ctor()
    IL_0032:  stloc.0
    IL_0033:  ldc.i4.0
    IL_0034:  stloc.3
    IL_0035:  br.s       IL_0063

    IL_0037:  ldarg.3
    IL_0038:  ldloc.3
    IL_0039:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Item(int32)
    IL_003e:  stloc.s    V_4
    IL_0040:  ldloc.s    V_4
    IL_0042:  ldfld      bool PipeSystem.CompResourceStorage::markedForTransfer
    IL_0047:  brtrue.s   IL_005f

    IL_0049:  ldloc.s    V_4
    IL_004b:  callvirt   instance float32 PipeSystem.CompResourceStorage::get_AmountCanAccept()
    IL_0050:  ldc.r4     0.0
    IL_0055:  ble.un.s   IL_005f

    IL_0057:  ldloc.0
    IL_0058:  ldloc.s    V_4
    IL_005a:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::Add(!0)
    IL_005f:  ldloc.3
    IL_0060:  ldc.i4.1
    IL_0061:  add
    IL_0062:  stloc.3
    IL_0063:  ldloc.3
    IL_0064:  ldarg.3
    IL_0065:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Count()
    IL_006a:  blt.s      IL_0037

    IL_006c:  ldloc.0
    IL_006d:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Count()
    IL_0072:  brtrue.s   IL_0075

    IL_0074:  ret

    IL_0075:  ldarg.1
    IL_0076:  ldarg.0
    IL_0077:  call       instance float32 PipeSystem.PipeNet::get_AvailableCapacity()
    IL_007c:  call       float32 [UnityEngine.CoreModule]UnityEngine.Mathf::Min(float32,
                                                                                float32)
    IL_0081:  stloc.1
    IL_0082:  ldc.i4.0
    IL_0083:  stloc.2
    IL_0084:  br         IL_010f

    IL_0089:  ldloc.2
    IL_008a:  ldc.i4.1
    IL_008b:  add
    IL_008c:  stloc.2
    IL_008d:  ldloc.2
    IL_008e:  ldc.i4     0x3e8
    IL_0093:  ble.s      IL_00a1

    IL_0095:  ldstr      "To many iteration in DistributeAmongStorage"
    IL_009a:  call       void PipeSystem.PipeSystemDebug::Message(string)
    IL_009f:  br.s       IL_011a

    IL_00a1:  ldloc.1
    IL_00a2:  ldloc.0
    IL_00a3:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Count()
    IL_00a8:  conv.r4
    IL_00a9:  div
    IL_00aa:  stloc.s    V_5
    IL_00ac:  ldc.i4.0
    IL_00ad:  stloc.s    V_6
    IL_00af:  br.s       IL_00fd

    IL_00b1:  ldloc.0
    IL_00b2:  ldloc.s    V_6
    IL_00b4:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Item(int32)
    IL_00b9:  stloc.s    V_7
    IL_00bb:  ldloc.s    V_7
    IL_00bd:  callvirt   instance float32 PipeSystem.CompResourceStorage::get_AmountCanAccept()
    IL_00c2:  ldloc.s    V_5
    IL_00c4:  call       float32 [UnityEngine.CoreModule]UnityEngine.Mathf::Min(float32,
                                                                                float32)
    IL_00c9:  stloc.s    V_8
    IL_00cb:  ldloc.s    V_7
    IL_00cd:  ldloc.s    V_8
    IL_00cf:  callvirt   instance void PipeSystem.CompResourceStorage::AddResource(float32)
    IL_00d4:  ldarg.2
    IL_00d5:  ldarg.2
    IL_00d6:  ldind.r4
    IL_00d7:  ldloc.s    V_8
    IL_00d9:  add
    IL_00da:  stind.r4
    IL_00db:  ldloc.s    V_7
    IL_00dd:  callvirt   instance float32 PipeSystem.CompResourceStorage::get_AmountCanAccept()
    IL_00e2:  ldc.r4     0.0
    IL_00e7:  bne.un.s   IL_00f2

    IL_00e9:  ldloc.0
    IL_00ea:  ldloc.s    V_7
    IL_00ec:  callvirt   instance bool class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::Remove(!0)
    IL_00f1:  pop
    IL_00f2:  ldloc.1
    IL_00f3:  ldloc.s    V_8
    IL_00f5:  sub
    IL_00f6:  stloc.1
    IL_00f7:  ldloc.s    V_6
    IL_00f9:  ldc.i4.1
    IL_00fa:  add
    IL_00fb:  stloc.s    V_6
    IL_00fd:  ldloc.s    V_6
    IL_00ff:  ldloc.0
    IL_0100:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Count()
    IL_0105:  blt.s      IL_00b1

    IL_0107:  ldloc.0
    IL_0108:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Count()
    IL_010d:  brfalse.s  IL_011a

    IL_010f:  ldloc.1
    IL_0110:  ldc.r4     0.0
    IL_0115:  bgt        IL_0089

    IL_011a:  ldarg.s    allowOverflow
    IL_011c:  brfalse.s  IL_012a

    IL_011e:  ldarg.2
    IL_011f:  ldarg.2
    IL_0120:  ldind.r4
    IL_0121:  ldarg.0
    IL_0122:  ldloc.1
    IL_0123:  call       instance float32 PipeSystem.PipeNet::DistributeToOverflow(float32)
    IL_0128:  add
    IL_0129:  stind.r4
    IL_012a:  ret
  } // end of method PipeNet::DistributeAmongStorage

  .method public hidebysig instance void 
          DrawAmongStorage(float32 amount,
                           [opt] class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> storages) cil managed
  {
    .param [2] = nullref
    // Code size       12 (0xc)
    .maxstack  5
    .locals init (float32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldloca.s   V_0
    IL_0004:  ldarg.2
    IL_0005:  ldc.i4.1
    IL_0006:  call       instance void PipeSystem.PipeNet::DrawAmongStorage(float32,
                                                                            float32&,
                                                                            class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>,
                                                                            bool)
    IL_000b:  ret
  } // end of method PipeNet::DrawAmongStorage

  .method public hidebysig instance void 
          DrawAmongStorage(float32 amount,
                           class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> storages,
                           bool drawFromOverflow) cil managed
  {
    // Code size       12 (0xc)
    .maxstack  5
    .locals init (float32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldloca.s   V_0
    IL_0004:  ldarg.2
    IL_0005:  ldarg.3
    IL_0006:  call       instance void PipeSystem.PipeNet::DrawAmongStorage(float32,
                                                                            float32&,
                                                                            class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>,
                                                                            bool)
    IL_000b:  ret
  } // end of method PipeNet::DrawAmongStorage

  .method public hidebysig instance void 
          DrawAmongStorage(float32 amount,
                           [out] float32& drawn,
                           class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> storages,
                           [opt] bool drawFromOverflow) cil managed
  {
    .param [4] = bool(true)
    // Code size       288 (0x120)
    .maxstack  3
    .locals init (class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> V_0,
             int32 V_1,
             int32 V_2,
             class PipeSystem.CompResourceStorage V_3,
             float32 V_4,
             int32 V_5,
             class PipeSystem.CompResourceStorage V_6,
             float32 V_7)
    IL_0000:  ldarg.2
    IL_0001:  ldc.r4     0.0
    IL_0006:  stind.r4
    IL_0007:  ldarg.1
    IL_0008:  ldc.r4     0.0
    IL_000d:  bgt.un.s   IL_0010

    IL_000f:  ret

    IL_0010:  ldarg.s    drawFromOverflow
    IL_0012:  brfalse.s  IL_002c

    IL_0014:  ldarg.2
    IL_0015:  ldarg.0
    IL_0016:  ldarg.1
    IL_0017:  call       instance float32 PipeSystem.PipeNet::DrawFromOverflow(float32)
    IL_001c:  stind.r4
    IL_001d:  ldarg.1
    IL_001e:  ldarg.2
    IL_001f:  ldind.r4
    IL_0020:  sub
    IL_0021:  starg.s    amount
    IL_0023:  ldarg.1
    IL_0024:  ldc.r4     0.0
    IL_0029:  bgt.un.s   IL_002c

    IL_002b:  ret

    IL_002c:  ldarg.3
    IL_002d:  brtrue.s   IL_0037

    IL_002f:  ldarg.0
    IL_0030:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::storages
    IL_0035:  starg.s    storages
    IL_0037:  ldarg.3
    IL_0038:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Count()
    IL_003d:  brtrue.s   IL_0040

    IL_003f:  ret

    IL_0040:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::.ctor()
    IL_0045:  stloc.0
    IL_0046:  ldc.i4.0
    IL_0047:  stloc.2
    IL_0048:  br.s       IL_006a

    IL_004a:  ldarg.3
    IL_004b:  ldloc.2
    IL_004c:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Item(int32)
    IL_0051:  stloc.3
    IL_0052:  ldloc.3
    IL_0053:  callvirt   instance float32 PipeSystem.CompResourceStorage::get_AmountStored()
    IL_0058:  ldc.r4     0.0
    IL_005d:  ble.un.s   IL_0066

    IL_005f:  ldloc.0
    IL_0060:  ldloc.3
    IL_0061:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::Add(!0)
    IL_0066:  ldloc.2
    IL_0067:  ldc.i4.1
    IL_0068:  add
    IL_0069:  stloc.2
    IL_006a:  ldloc.2
    IL_006b:  ldarg.3
    IL_006c:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Count()
    IL_0071:  blt.s      IL_004a

    IL_0073:  ldloc.0
    IL_0074:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Count()
    IL_0079:  brtrue.s   IL_007c

    IL_007b:  ret

    IL_007c:  ldc.i4.0
    IL_007d:  stloc.1
    IL_007e:  br         IL_0114

    IL_0083:  ldloc.1
    IL_0084:  ldc.i4.1
    IL_0085:  add
    IL_0086:  stloc.1
    IL_0087:  ldloc.1
    IL_0088:  ldc.i4     0x3e8
    IL_008d:  ble.s      IL_00a5

    IL_008f:  ldstr      "Too many iteration in DrawAmongStorage. Amount lef"
    + "t: {0}"
    IL_0094:  ldarg.1
    IL_0095:  box        [mscorlib]System.Single
    IL_009a:  call       string [mscorlib]System.String::Format(string,
                                                                object)
    IL_009f:  call       void PipeSystem.PipeSystemDebug::Message(string)
    IL_00a4:  ret

    IL_00a5:  ldarg.1
    IL_00a6:  ldloc.0
    IL_00a7:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Count()
    IL_00ac:  conv.r4
    IL_00ad:  div
    IL_00ae:  stloc.s    V_4
    IL_00b0:  ldc.i4.0
    IL_00b1:  stloc.s    V_5
    IL_00b3:  br.s       IL_0102

    IL_00b5:  ldloc.0
    IL_00b6:  ldloc.s    V_5
    IL_00b8:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Item(int32)
    IL_00bd:  stloc.s    V_6
    IL_00bf:  ldloc.s    V_6
    IL_00c1:  callvirt   instance float32 PipeSystem.CompResourceStorage::get_AmountStored()
    IL_00c6:  ldloc.s    V_4
    IL_00c8:  call       float32 [UnityEngine.CoreModule]UnityEngine.Mathf::Min(float32,
                                                                                float32)
    IL_00cd:  stloc.s    V_7
    IL_00cf:  ldloc.s    V_6
    IL_00d1:  ldloc.s    V_7
    IL_00d3:  callvirt   instance void PipeSystem.CompResourceStorage::DrawResource(float32)
    IL_00d8:  ldarg.2
    IL_00d9:  ldarg.2
    IL_00da:  ldind.r4
    IL_00db:  ldloc.s    V_7
    IL_00dd:  add
    IL_00de:  stind.r4
    IL_00df:  ldloc.s    V_6
    IL_00e1:  callvirt   instance float32 PipeSystem.CompResourceStorage::get_AmountStored()
    IL_00e6:  ldc.r4     0.0
    IL_00eb:  bne.un.s   IL_00f6

    IL_00ed:  ldloc.0
    IL_00ee:  ldloc.s    V_6
    IL_00f0:  callvirt   instance bool class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::Remove(!0)
    IL_00f5:  pop
    IL_00f6:  ldarg.1
    IL_00f7:  ldloc.s    V_7
    IL_00f9:  sub
    IL_00fa:  starg.s    amount
    IL_00fc:  ldloc.s    V_5
    IL_00fe:  ldc.i4.1
    IL_00ff:  add
    IL_0100:  stloc.s    V_5
    IL_0102:  ldloc.s    V_5
    IL_0104:  ldloc.0
    IL_0105:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Count()
    IL_010a:  blt.s      IL_00b5

    IL_010c:  ldloc.0
    IL_010d:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::get_Count()
    IL_0112:  brfalse.s  IL_011f

    IL_0114:  ldarg.1
    IL_0115:  ldc.r4     0.0
    IL_011a:  bgt        IL_0083

    IL_011f:  ret
  } // end of method PipeNet::DrawAmongStorage

  .method famorassem hidebysig instance float32 
          DistributeToOverflow(float32 amount) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldc.r4     -1.
    IL_0007:  ldc.i4.0
    IL_0008:  call       instance float32 PipeSystem.PipeNet::DistributeToOverflow(float32,
                                                                                   float32,
                                                                                   bool)
    IL_000d:  ret
  } // end of method PipeNet::DistributeToOverflow

  .method famorassem hidebysig instance float32 
          DistributeToOverflow(float32 amount,
                               bool ignoreCapacity) cil managed
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  brtrue.s   IL_000c

    IL_0005:  ldc.r4     -1.
    IL_000a:  br.s       IL_0011

    IL_000c:  ldc.r4     inf
    IL_0011:  ldc.i4.0
    IL_0012:  call       instance float32 PipeSystem.PipeNet::DistributeToOverflow(float32,
                                                                                   float32,
                                                                                   bool)
    IL_0017:  ret
  } // end of method PipeNet::DistributeToOverflow

  .method famorassem hidebysig instance float32 
          DistributeToOverflow(float32 amount,
                               float32 maxCapacityOverride,
                               [opt] bool overrideMinimum) cil managed
  {
    .param [3] = bool(false)
    // Code size       151 (0x97)
    .maxstack  3
    .locals init (float32 V_0)
    IL_0000:  ldarg.1
    IL_0001:  ldc.r4     0.0
    IL_0006:  ble.s      IL_0015

    IL_0008:  ldarg.0
    IL_0009:  ldfld      class PipeSystem.PipeNetDef PipeSystem.PipeNet::def
    IL_000e:  ldfld      bool PipeSystem.PipeNetDef::enableOverflow
    IL_0013:  brtrue.s   IL_001b

    IL_0015:  ldc.r4     0.0
    IL_001a:  ret

    IL_001b:  ldarg.3
    IL_001c:  brfalse.s  IL_0026

    IL_001e:  ldarg.2
    IL_001f:  ldc.r4     0.0
    IL_0024:  bgt.un.s   IL_005b

    IL_0026:  ldarg.0
    IL_0027:  call       instance float32 PipeSystem.PipeNet::get_TotalProductionThisTick()
    IL_002c:  ldarg.0
    IL_002d:  ldfld      class PipeSystem.PipeNetDef PipeSystem.PipeNet::def
    IL_0032:  ldfld      float32 PipeSystem.PipeNetDef::productionToOverflowCapacityRatio
    IL_0037:  mul
    IL_0038:  stloc.0
    IL_0039:  ldloc.0
    IL_003a:  ldarg.0
    IL_003b:  ldfld      class PipeSystem.PipeNetDef PipeSystem.PipeNet::def
    IL_0040:  ldfld      float32 PipeSystem.PipeNetDef::overflowAmount
    IL_0045:  bge.un.s   IL_0054

    IL_0047:  ldarg.0
    IL_0048:  ldfld      class PipeSystem.PipeNetDef PipeSystem.PipeNet::def
    IL_004d:  ldfld      float32 PipeSystem.PipeNetDef::overflowAmount
    IL_0052:  starg.s    maxCapacityOverride
    IL_0054:  ldarg.2
    IL_0055:  ldloc.0
    IL_0056:  bge.un.s   IL_005b

    IL_0058:  ldloc.0
    IL_0059:  starg.s    maxCapacityOverride
    IL_005b:  ldarg.2
    IL_005c:  ldarg.0
    IL_005d:  call       instance float32 PipeSystem.PipeNet::get_OverflowAmount()
    IL_0062:  sub
    IL_0063:  starg.s    maxCapacityOverride
    IL_0065:  ldarg.2
    IL_0066:  ldc.r4     0.0
    IL_006b:  bgt.un.s   IL_0073

    IL_006d:  ldc.r4     0.0
    IL_0072:  ret

    IL_0073:  ldarg.2
    IL_0074:  ldarg.1
    IL_0075:  blt.un.s   IL_0087

    IL_0077:  ldarg.0
    IL_0078:  ldarg.0
    IL_0079:  call       instance float32 PipeSystem.PipeNet::get_OverflowAmount()
    IL_007e:  ldarg.1
    IL_007f:  add
    IL_0080:  call       instance void PipeSystem.PipeNet::set_OverflowAmount(float32)
    IL_0085:  ldarg.1
    IL_0086:  ret

    IL_0087:  ldarg.0
    IL_0088:  ldarg.0
    IL_0089:  call       instance float32 PipeSystem.PipeNet::get_OverflowAmount()
    IL_008e:  ldarg.2
    IL_008f:  add
    IL_0090:  call       instance void PipeSystem.PipeNet::set_OverflowAmount(float32)
    IL_0095:  ldarg.2
    IL_0096:  ret
  } // end of method PipeNet::DistributeToOverflow

  .method famorassem hidebysig instance float32 
          DrawFromOverflow([opt] float32 amount) cil managed
  {
    .param [1] = float32(inf)
    // Code size       65 (0x41)
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  call       instance float32 PipeSystem.PipeNet::get_OverflowAmount()
    IL_0006:  ldc.r4     0.0
    IL_000b:  bgt.un.s   IL_0013

    IL_000d:  ldc.r4     0.0
    IL_0012:  ret

    IL_0013:  ldarg.0
    IL_0014:  call       instance float32 PipeSystem.PipeNet::get_OverflowAmount()
    IL_0019:  ldarg.1
    IL_001a:  blt.un.s   IL_002c

    IL_001c:  ldarg.0
    IL_001d:  ldarg.0
    IL_001e:  call       instance float32 PipeSystem.PipeNet::get_OverflowAmount()
    IL_0023:  ldarg.1
    IL_0024:  sub
    IL_0025:  call       instance void PipeSystem.PipeNet::set_OverflowAmount(float32)
    IL_002a:  ldarg.1
    IL_002b:  ret

    IL_002c:  ldarg.0
    IL_002d:  call       instance float32 PipeSystem.PipeNet::get_OverflowAmount()
    IL_0032:  starg.s    amount
    IL_0034:  ldarg.0
    IL_0035:  ldc.r4     0.0
    IL_003a:  call       instance void PipeSystem.PipeNet::set_OverflowAmount(float32)
    IL_003f:  ldarg.1
    IL_0040:  ret
  } // end of method PipeNet::DrawFromOverflow

  .method private hidebysig instance void 
          UpdateLowPowerModeConsumers() cil managed
  {
    // Code size       64 (0x40)
    .maxstack  3
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_002a

    IL_0004:  ldarg.0
    IL_0005:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::resourceTradersWithLowPowerMode
    IL_000a:  ldloc.0
    IL_000b:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::get_Item(int32)
    IL_0010:  ldarg.0
    IL_0011:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::resourceTradersWithLowPowerMode
    IL_0016:  ldloc.0
    IL_0017:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::get_Item(int32)
    IL_001c:  callvirt   instance bool PipeSystem.CompResourceTrader::get_ShouldBeLowPowerMode()
    IL_0021:  callvirt   instance void PipeSystem.CompResourceTrader::set_LowPowerModeOn(bool)
    IL_0026:  ldloc.0
    IL_0027:  ldc.i4.1
    IL_0028:  add
    IL_0029:  stloc.0
    IL_002a:  ldloc.0
    IL_002b:  ldarg.0
    IL_002c:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::resourceTradersWithLowPowerMode
    IL_0031:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::get_Count()
    IL_0036:  blt.s      IL_0004

    IL_0038:  ldarg.0
    IL_0039:  ldc.i4.0
    IL_003a:  stfld      bool PipeSystem.PipeNet::resourceTradersWithLowPowerModeDirty
    IL_003f:  ret
  } // end of method PipeNet::UpdateLowPowerModeConsumers

  .method assembly hidebysig instance void 
          UpdateLowPowerModeTrader(class PipeSystem.CompResourceTrader lowPowerModeTrader) cil managed
  {
    // Code size       239 (0xef)
    .maxstack  3
    .locals init (bool V_0,
             bool V_1)
    IL_0000:  ldarg.1
    IL_0001:  callvirt   instance bool PipeSystem.CompResourceTrader::get_ResourceOn()
    IL_0006:  brtrue.s   IL_0009

    IL_0008:  ret

    IL_0009:  ldarg.1
    IL_000a:  callvirt   instance bool PipeSystem.CompResourceTrader::get_LowPowerModeOn()
    IL_000f:  ldarg.1
    IL_0010:  callvirt   instance class PipeSystem.CompProperties_ResourceTrader PipeSystem.CompResourceTrader::get_Props()
    IL_0015:  ldfld      bool PipeSystem.CompProperties_ResourceTrader::visualOnlyConsumption
    IL_001a:  stloc.0
    IL_001b:  ldarg.1
    IL_001c:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_0021:  ldc.r4     0.0
    IL_0026:  clt
    IL_0028:  stloc.1
    IL_0029:  brfalse.s  IL_0038

    IL_002b:  ldloc.0
    IL_002c:  brfalse.s  IL_0033

    IL_002e:  ldloc.1
    IL_002f:  brfalse.s  IL_004b

    IL_0031:  br.s       IL_009b

    IL_0033:  ldloc.1
    IL_0034:  brfalse.s  IL_005f

    IL_0036:  br.s       IL_00b0

    IL_0038:  ldloc.0
    IL_0039:  brfalse.s  IL_0043

    IL_003b:  ldloc.1
    IL_003c:  brfalse.s  IL_0073

    IL_003e:  br         IL_00c5

    IL_0043:  ldloc.1
    IL_0044:  brfalse.s  IL_0087

    IL_0046:  br         IL_00da

    IL_004b:  ldarg.0
    IL_004c:  ldarg.0
    IL_004d:  call       instance float32 PipeSystem.PipeNet::get_VisualConsumption()
    IL_0052:  ldarg.1
    IL_0053:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_0058:  sub
    IL_0059:  call       instance void PipeSystem.PipeNet::set_VisualConsumption(float32)
    IL_005e:  ret

    IL_005f:  ldarg.0
    IL_0060:  ldarg.0
    IL_0061:  call       instance float32 PipeSystem.PipeNet::get_Consumption()
    IL_0066:  ldarg.1
    IL_0067:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_006c:  sub
    IL_006d:  call       instance void PipeSystem.PipeNet::set_Consumption(float32)
    IL_0072:  ret

    IL_0073:  ldarg.0
    IL_0074:  ldarg.0
    IL_0075:  call       instance float32 PipeSystem.PipeNet::get_VisualConsumption()
    IL_007a:  ldarg.1
    IL_007b:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_0080:  add
    IL_0081:  call       instance void PipeSystem.PipeNet::set_VisualConsumption(float32)
    IL_0086:  ret

    IL_0087:  ldarg.0
    IL_0088:  ldarg.0
    IL_0089:  call       instance float32 PipeSystem.PipeNet::get_Consumption()
    IL_008e:  ldarg.1
    IL_008f:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_0094:  add
    IL_0095:  call       instance void PipeSystem.PipeNet::set_Consumption(float32)
    IL_009a:  ret

    IL_009b:  ldarg.0
    IL_009c:  ldarg.0
    IL_009d:  call       instance float32 PipeSystem.PipeNet::get_VisualProduction()
    IL_00a2:  ldarg.1
    IL_00a3:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_00a8:  neg
    IL_00a9:  sub
    IL_00aa:  call       instance void PipeSystem.PipeNet::set_VisualProduction(float32)
    IL_00af:  ret

    IL_00b0:  ldarg.0
    IL_00b1:  ldarg.0
    IL_00b2:  call       instance float32 PipeSystem.PipeNet::get_Production()
    IL_00b7:  ldarg.1
    IL_00b8:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_00bd:  neg
    IL_00be:  sub
    IL_00bf:  call       instance void PipeSystem.PipeNet::set_Production(float32)
    IL_00c4:  ret

    IL_00c5:  ldarg.0
    IL_00c6:  ldarg.0
    IL_00c7:  call       instance float32 PipeSystem.PipeNet::get_VisualProduction()
    IL_00cc:  ldarg.1
    IL_00cd:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_00d2:  neg
    IL_00d3:  add
    IL_00d4:  call       instance void PipeSystem.PipeNet::set_VisualProduction(float32)
    IL_00d9:  ret

    IL_00da:  ldarg.0
    IL_00db:  ldarg.0
    IL_00dc:  call       instance float32 PipeSystem.PipeNet::get_Production()
    IL_00e1:  ldarg.1
    IL_00e2:  callvirt   instance float32 PipeSystem.CompResourceTrader::get_Consumption()
    IL_00e7:  neg
    IL_00e8:  add
    IL_00e9:  call       instance void PipeSystem.PipeNet::set_Production(float32)
    IL_00ee:  ret
  } // end of method PipeNet::UpdateLowPowerModeTrader

  .method public hidebysig newslot virtual 
          instance void  PostMake() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  stfld      bool PipeSystem.PipeNet::resourceTradersWithLowPowerModeDirty
    IL_0007:  ret
  } // end of method PipeNet::PostMake

  .method public hidebysig newslot virtual 
          instance void  PostPostMake() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method PipeNet::PostPostMake

  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    // Code size       106 (0x6a)
    .maxstack  5
    IL_0000:  ldstr      "PipeNet: {0} Stored: {1} AvailableCapacity: {2} Co"
    + "nsumption: {3} Production: {4} Overflow: {5}"
    IL_0005:  ldc.i4.6
    IL_0006:  newarr     [mscorlib]System.Object
    IL_000b:  dup
    IL_000c:  ldc.i4.0
    IL_000d:  ldarg.0
    IL_000e:  ldfld      class PipeSystem.PipeNetDef PipeSystem.PipeNet::def
    IL_0013:  ldfld      class PipeSystem.Resource PipeSystem.PipeNetDef::resource
    IL_0018:  ldfld      string PipeSystem.Resource::name
    IL_001d:  stelem.ref
    IL_001e:  dup
    IL_001f:  ldc.i4.1
    IL_0020:  ldarg.0
    IL_0021:  call       instance float32 PipeSystem.PipeNet::get_Stored()
    IL_0026:  box        [mscorlib]System.Single
    IL_002b:  stelem.ref
    IL_002c:  dup
    IL_002d:  ldc.i4.2
    IL_002e:  ldarg.0
    IL_002f:  call       instance float32 PipeSystem.PipeNet::get_AvailableCapacity()
    IL_0034:  box        [mscorlib]System.Single
    IL_0039:  stelem.ref
    IL_003a:  dup
    IL_003b:  ldc.i4.3
    IL_003c:  ldarg.0
    IL_003d:  call       instance float32 PipeSystem.PipeNet::get_VisualConsumption()
    IL_0042:  box        [mscorlib]System.Single
    IL_0047:  stelem.ref
    IL_0048:  dup
    IL_0049:  ldc.i4.4
    IL_004a:  ldarg.0
    IL_004b:  call       instance float32 PipeSystem.PipeNet::get_VisualProduction()
    IL_0050:  box        [mscorlib]System.Single
    IL_0055:  stelem.ref
    IL_0056:  dup
    IL_0057:  ldc.i4.5
    IL_0058:  ldarg.0
    IL_0059:  call       instance float32 PipeSystem.PipeNet::get_OverflowAmount()
    IL_005e:  box        [mscorlib]System.Single
    IL_0063:  stelem.ref
    IL_0064:  call       string [mscorlib]System.String::Format(string,
                                                                object[])
    IL_0069:  ret
  } // end of method PipeNet::ToString

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       172 (0xac)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResource>::.ctor()
    IL_0006:  stfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResource> PipeSystem.PipeNet::connectors
    IL_000b:  ldarg.0
    IL_000c:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::.ctor()
    IL_0011:  stfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::producers
    IL_0016:  ldarg.0
    IL_0017:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::.ctor()
    IL_001c:  stfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::receivers
    IL_0021:  ldarg.0
    IL_0022:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::.ctor()
    IL_0027:  stfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::storages
    IL_002c:  ldarg.0
    IL_002d:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing>::.ctor()
    IL_0032:  stfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToThing> PipeSystem.PipeNet::thingConverters
    IL_0037:  ldarg.0
    IL_0038:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToResource>::.ctor()
    IL_003d:  stfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompConvertToResource> PipeSystem.PipeNet::resourceConverters
    IL_0042:  ldarg.0
    IL_0043:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompRefillWithPipes>::.ctor()
    IL_0048:  stfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompRefillWithPipes> PipeSystem.PipeNet::refillables
    IL_004d:  ldarg.0
    IL_004e:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceProcessor>::.ctor()
    IL_0053:  stfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceProcessor> PipeSystem.PipeNet::processors
    IL_0058:  ldarg.0
    IL_0059:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompDeepExtractor>::.ctor()
    IL_005e:  stfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompDeepExtractor> PipeSystem.PipeNet::extractors
    IL_0063:  ldarg.0
    IL_0064:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::.ctor()
    IL_0069:  stfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::markedForTransfer
    IL_006e:  ldarg.0
    IL_006f:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::.ctor()
    IL_0074:  stfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::receiversOn
    IL_0079:  ldarg.0
    IL_007a:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::.ctor()
    IL_007f:  stfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::receiversOff
    IL_0084:  ldarg.0
    IL_0085:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::.ctor()
    IL_008a:  stfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::producersOn
    IL_008f:  ldarg.0
    IL_0090:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::.ctor()
    IL_0095:  stfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::producersOff
    IL_009a:  ldarg.0
    IL_009b:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader>::.ctor()
    IL_00a0:  stfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceTrader> PipeSystem.PipeNet::resourceTradersWithLowPowerMode
    IL_00a5:  ldarg.0
    IL_00a6:  call       instance void [mscorlib]System.Object::.ctor()
    IL_00ab:  ret
  } // end of method PipeNet::.ctor

  .method private hidebysig instance bool 
          '<UnregisterComp>b__79_0'(class PipeSystem.PipeNet x) cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       15 (0xf)
    .maxstack  8
    IL_0000:  ldarg.1
    IL_0001:  ldfld      class PipeSystem.PipeNetDef PipeSystem.PipeNet::def
    IL_0006:  ldarg.0
    IL_0007:  ldfld      class PipeSystem.PipeNetDef PipeSystem.PipeNet::def
    IL_000c:  ceq
    IL_000e:  ret
  } // end of method PipeNet::'<UnregisterComp>b__79_0'

  .property instance int32 NextTick()
  {
    .get instance int32 PipeSystem.PipeNet::get_NextTick()
    .set instance void PipeSystem.PipeNet::set_NextTick(int32)
  } // end of property PipeNet::NextTick
  .property instance float32 Consumption()
  {
    .get instance float32 PipeSystem.PipeNet::get_Consumption()
    .set instance void PipeSystem.PipeNet::set_Consumption(float32)
  } // end of property PipeNet::Consumption
  .property instance float32 VisualConsumption()
  {
    .get instance float32 PipeSystem.PipeNet::get_VisualConsumption()
    .set instance void PipeSystem.PipeNet::set_VisualConsumption(float32)
  } // end of property PipeNet::VisualConsumption
  .property instance float32 ExtraConsumptionThisTick()
  {
    .get instance float32 PipeSystem.PipeNet::get_ExtraConsumptionThisTick()
    .set instance void PipeSystem.PipeNet::set_ExtraConsumptionThisTick(float32)
  } // end of property PipeNet::ExtraConsumptionThisTick
  .property instance float32 TotalConsumptionThisTick()
  {
    .get instance float32 PipeSystem.PipeNet::get_TotalConsumptionThisTick()
  } // end of property PipeNet::TotalConsumptionThisTick
  .property instance float32 Production()
  {
    .get instance float32 PipeSystem.PipeNet::get_Production()
    .set instance void PipeSystem.PipeNet::set_Production(float32)
  } // end of property PipeNet::Production
  .property instance float32 VisualProduction()
  {
    .get instance float32 PipeSystem.PipeNet::get_VisualProduction()
    .set instance void PipeSystem.PipeNet::set_VisualProduction(float32)
  } // end of property PipeNet::VisualProduction
  .property instance float32 ExtraProductionThisTick()
  {
    .get instance float32 PipeSystem.PipeNet::get_ExtraProductionThisTick()
    .set instance void PipeSystem.PipeNet::set_ExtraProductionThisTick(float32)
  } // end of property PipeNet::ExtraProductionThisTick
  .property instance float32 TotalProductionThisTick()
  {
    .get instance float32 PipeSystem.PipeNet::get_TotalProductionThisTick()
  } // end of property PipeNet::TotalProductionThisTick
  .property instance float32 ExtractorRawProduction()
  {
    .get instance float32 PipeSystem.PipeNet::get_ExtractorRawProduction()
  } // end of property PipeNet::ExtractorRawProduction
  .property instance float32 Stored()
  {
    .get instance float32 PipeSystem.PipeNet::get_Stored()
    .set instance void PipeSystem.PipeNet::set_Stored(float32)
  } // end of property PipeNet::Stored
  .property instance float32 AvailableCapacity()
  {
    .get instance float32 PipeSystem.PipeNet::get_AvailableCapacity()
  } // end of property PipeNet::AvailableCapacity
  .property instance float32 AvailableCapacityLastTick()
  {
    .get instance float32 PipeSystem.PipeNet::get_AvailableCapacityLastTick()
    .set instance void PipeSystem.PipeNet::set_AvailableCapacityLastTick(float32)
  } // end of property PipeNet::AvailableCapacityLastTick
  .property instance float32 ThingConvertersMaxOutput()
  {
    .get instance float32 PipeSystem.PipeNet::get_ThingConvertersMaxOutput()
  } // end of property PipeNet::ThingConvertersMaxOutput
  .property instance float32 RefillableAmount()
  {
    .get instance float32 PipeSystem.PipeNet::get_RefillableAmount()
  } // end of property PipeNet::RefillableAmount
  .property instance float32 OverflowAmount()
  {
    .get instance float32 PipeSystem.PipeNet::get_OverflowAmount()
    .set instance void PipeSystem.PipeNet::set_OverflowAmount(float32)
  } // end of property PipeNet::OverflowAmount
} // end of class PipeSystem.PipeNet

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file F:\Repositories\rimworld_mods\.tmp\pipesystem_pipenet.res
