
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.8.3928.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.


// warning : THIS IS A PARTIAL DISASSEMBLY, NOT SUITABLE FOR RE-ASSEMBLING



.class public auto ansi beforefieldinit PipeSystem.PipeNet
       extends [mscorlib]System.Object
{
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

} // end of class PipeSystem.PipeNet

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file F:\Repositories\rimworld_mods\.tmp\pipesystem_drawoverflow.res
