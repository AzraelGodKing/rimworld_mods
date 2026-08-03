
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.8.3928.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.


// warning : THIS IS A PARTIAL DISASSEMBLY, NOT SUITABLE FOR RE-ASSEMBLING



.class public auto ansi beforefieldinit PipeSystem.PipeNet
       extends [mscorlib]System.Object
{
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

} // end of class PipeSystem.PipeNet

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file F:\Repositories\rimworld_mods\.tmp\ps_reg.res
