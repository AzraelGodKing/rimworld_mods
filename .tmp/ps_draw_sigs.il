
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.8.3928.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.


// warning : THIS IS A PARTIAL DISASSEMBLY, NOT SUITABLE FOR RE-ASSEMBLING



.class public auto ansi beforefieldinit PipeSystem.PipeNet
       extends [mscorlib]System.Object
{
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

} // end of class PipeSystem.PipeNet

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file F:\Repositories\rimworld_mods\.tmp\ps_draw_sigs.res
