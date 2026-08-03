
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.8.3928.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.


// warning : THIS IS A PARTIAL DISASSEMBLY, NOT SUITABLE FOR RE-ASSEMBLING



.class public auto ansi beforefieldinit PipeSystem.PipeNet
       extends [mscorlib]System.Object
{
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

} // end of class PipeSystem.PipeNet

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file F:\Repositories\rimworld_mods\.tmp\pipesystem_distoverflow.res
