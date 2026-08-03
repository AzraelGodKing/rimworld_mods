
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.8.3928.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.


// warning : THIS IS A PARTIAL DISASSEMBLY, NOT SUITABLE FOR RE-ASSEMBLING



.class public auto ansi beforefieldinit PipeSystem.CompResourceStorage
       extends PipeSystem.CompResource
{
  .custom instance void ['Assembly-CSharp']Verse.StaticConstructorOnStartup::.ctor() = ( 01 00 00 00 ) 
  .class auto ansi sealed nested private beforefieldinit '<CompGetGizmosExtra>d__42'
         extends [mscorlib]System.Object
         implements class [mscorlib]System.Collections.Generic.IEnumerable`1<class ['Assembly-CSharp']Verse.Gizmo>,
                    [mscorlib]System.Collections.IEnumerable,
                    class [mscorlib]System.Collections.Generic.IEnumerator`1<class ['Assembly-CSharp']Verse.Gizmo>,
                    [mscorlib]System.IDisposable,
                    [mscorlib]System.Collections.IEnumerator
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .field private int32 '<>1__state'
    .field private class ['Assembly-CSharp']Verse.Gizmo '<>2__current'
    .field private int32 '<>l__initialThreadId'
    .field public class PipeSystem.CompResourceStorage '<>4__this'
    .field private class [mscorlib]System.Collections.Generic.IEnumerator`1<class ['Assembly-CSharp']Verse.Gizmo> '<>7__wrap1'
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(int32 '<>1__state') cil managed
    {
      .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
      // Code size       25 (0x19)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ldarg.0
      IL_0007:  ldarg.1
      IL_0008:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
      IL_000d:  ldarg.0
      IL_000e:  call       int32 [mscorlib]System.Environment::get_CurrentManagedThreadId()
      IL_0013:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>l__initialThreadId'
      IL_0018:  ret
    } // end of method '<CompGetGizmosExtra>d__42'::.ctor

    .method private hidebysig newslot virtual final 
            instance void  System.IDisposable.Dispose() cil managed
    {
      .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
      .override [mscorlib]System.IDisposable::Dispose
      // Code size       42 (0x2a)
      .maxstack  2
      .locals init (int32 V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
      IL_0006:  stloc.0
      IL_0007:  ldloc.0
      IL_0008:  ldc.i4.s   -3
      IL_000a:  beq.s      IL_0010

      IL_000c:  ldloc.0
      IL_000d:  ldc.i4.1
      IL_000e:  bne.un.s   IL_001a

      IL_0010:  nop
      .try
      {
        IL_0011:  leave.s    IL_001a

      }  // end .try
      finally
      {
        IL_0013:  ldarg.0
        IL_0014:  call       instance void PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>m__Finally1'()
        IL_0019:  endfinally
      }  // end handler
      IL_001a:  ldarg.0
      IL_001b:  ldnull
      IL_001c:  stfld      class [mscorlib]System.Collections.Generic.IEnumerator`1<class ['Assembly-CSharp']Verse.Gizmo> PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>7__wrap1'
      IL_0021:  ldarg.0
      IL_0022:  ldc.i4.s   -2
      IL_0024:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
      IL_0029:  ret
    } // end of method '<CompGetGizmosExtra>d__42'::System.IDisposable.Dispose

    .method private hidebysig newslot virtual final 
            instance bool  MoveNext() cil managed
    {
      .override [mscorlib]System.Collections.IEnumerator::MoveNext
      // Code size       664 (0x298)
      .maxstack  5
      .locals init (bool V_0,
               int32 V_1,
               class PipeSystem.CompResourceStorage V_2,
               class ['Assembly-CSharp']Verse.Gizmo V_3,
               valuetype ['Assembly-CSharp']Verse.NamedArgument[] V_4)
      .try
      {
        IL_0000:  ldarg.0
        IL_0001:  ldfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
        IL_0006:  stloc.1
        IL_0007:  ldarg.0
        IL_0008:  ldfld      class PipeSystem.CompResourceStorage PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>4__this'
        IL_000d:  stloc.2
        IL_000e:  ldloc.1
        IL_000f:  switch     ( 
                              IL_003b,
                              IL_007e,
                              IL_00c2,
                              IL_00eb,
                              IL_01c9,
                              IL_0210,
                              IL_024a,
                              IL_0284)
        IL_0034:  ldc.i4.0
        IL_0035:  stloc.0
        IL_0036:  leave      IL_0296

        IL_003b:  ldarg.0
        IL_003c:  ldc.i4.m1
        IL_003d:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
        IL_0042:  ldarg.0
        IL_0043:  ldloc.2
        IL_0044:  call       instance class [mscorlib]System.Collections.Generic.IEnumerable`1<class ['Assembly-CSharp']Verse.Gizmo> PipeSystem.CompResourceStorage::'<>n__0'()
        IL_0049:  callvirt   instance class [mscorlib]System.Collections.Generic.IEnumerator`1<!0> class [mscorlib]System.Collections.Generic.IEnumerable`1<class ['Assembly-CSharp']Verse.Gizmo>::GetEnumerator()
        IL_004e:  stfld      class [mscorlib]System.Collections.Generic.IEnumerator`1<class ['Assembly-CSharp']Verse.Gizmo> PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>7__wrap1'
        IL_0053:  ldarg.0
        IL_0054:  ldc.i4.s   -3
        IL_0056:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
        IL_005b:  br.s       IL_0086

        IL_005d:  ldarg.0
        IL_005e:  ldfld      class [mscorlib]System.Collections.Generic.IEnumerator`1<class ['Assembly-CSharp']Verse.Gizmo> PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>7__wrap1'
        IL_0063:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.IEnumerator`1<class ['Assembly-CSharp']Verse.Gizmo>::get_Current()
        IL_0068:  stloc.3
        IL_0069:  ldarg.0
        IL_006a:  ldloc.3
        IL_006b:  stfld      class ['Assembly-CSharp']Verse.Gizmo PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>2__current'
        IL_0070:  ldarg.0
        IL_0071:  ldc.i4.1
        IL_0072:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
        IL_0077:  ldc.i4.1
        IL_0078:  stloc.0
        IL_0079:  leave      IL_0296

        IL_007e:  ldarg.0
        IL_007f:  ldc.i4.s   -3
        IL_0081:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
        IL_0086:  ldarg.0
        IL_0087:  ldfld      class [mscorlib]System.Collections.Generic.IEnumerator`1<class ['Assembly-CSharp']Verse.Gizmo> PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>7__wrap1'
        IL_008c:  callvirt   instance bool [mscorlib]System.Collections.IEnumerator::MoveNext()
        IL_0091:  brtrue.s   IL_005d

        IL_0093:  ldarg.0
        IL_0094:  call       instance void PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>m__Finally1'()
        IL_0099:  ldarg.0
        IL_009a:  ldnull
        IL_009b:  stfld      class [mscorlib]System.Collections.Generic.IEnumerator`1<class ['Assembly-CSharp']Verse.Gizmo> PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>7__wrap1'
        IL_00a0:  ldloc.2
        IL_00a1:  ldfld      class ['Assembly-CSharp']Verse.Command_Action PipeSystem.CompResourceStorage::transferGizmo
        IL_00a6:  brfalse.s  IL_00c9

        IL_00a8:  ldarg.0
        IL_00a9:  ldloc.2
        IL_00aa:  ldfld      class ['Assembly-CSharp']Verse.Command_Action PipeSystem.CompResourceStorage::transferGizmo
        IL_00af:  stfld      class ['Assembly-CSharp']Verse.Gizmo PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>2__current'
        IL_00b4:  ldarg.0
        IL_00b5:  ldc.i4.2
        IL_00b6:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
        IL_00bb:  ldc.i4.1
        IL_00bc:  stloc.0
        IL_00bd:  leave      IL_0296

        IL_00c2:  ldarg.0
        IL_00c3:  ldc.i4.m1
        IL_00c4:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
        IL_00c9:  ldloc.2
        IL_00ca:  ldfld      class ['Assembly-CSharp']Verse.Command_Toggle PipeSystem.CompResourceStorage::refillGizmo
        IL_00cf:  brfalse.s  IL_00f2

        IL_00d1:  ldarg.0
        IL_00d2:  ldloc.2
        IL_00d3:  ldfld      class ['Assembly-CSharp']Verse.Command_Toggle PipeSystem.CompResourceStorage::refillGizmo
        IL_00d8:  stfld      class ['Assembly-CSharp']Verse.Gizmo PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>2__current'
        IL_00dd:  ldarg.0
        IL_00de:  ldc.i4.3
        IL_00df:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
        IL_00e4:  ldc.i4.1
        IL_00e5:  stloc.0
        IL_00e6:  leave      IL_0296

        IL_00eb:  ldarg.0
        IL_00ec:  ldc.i4.m1
        IL_00ed:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
        IL_00f2:  ldloc.2
        IL_00f3:  ldfld      class ['Assembly-CSharp']Verse.Command_Action PipeSystem.CompResourceStorage::extractGizmo
        IL_00f8:  brfalse    IL_01d0

        IL_00fd:  ldloc.2
        IL_00fe:  call       instance valuetype ['Assembly-CSharp']Verse.NamedArgument[] PipeSystem.CompResourceStorage::ExtractResourceArguments()
        IL_0103:  stloc.s    V_4
        IL_0105:  ldloc.2
        IL_0106:  ldfld      class ['Assembly-CSharp']Verse.Command_Action PipeSystem.CompResourceStorage::extractGizmo
        IL_010b:  ldloc.2
        IL_010c:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
        IL_0111:  ldfld      class PipeSystem.ExtractOptions PipeSystem.CompProperties_ResourceStorage::extractOptions
        IL_0116:  ldfld      string PipeSystem.ExtractOptions::labelKey
        IL_011b:  ldloc.s    V_4
        IL_011d:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.TranslatorFormattedStringExtensions::Translate(string,
                                                                                                                                                     valuetype ['Assembly-CSharp']Verse.NamedArgument[])
        IL_0122:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
        IL_0127:  stfld      string ['Assembly-CSharp']Verse.Command::defaultLabel
        IL_012c:  ldloc.2
        IL_012d:  ldfld      class ['Assembly-CSharp']Verse.Command_Action PipeSystem.CompResourceStorage::extractGizmo
        IL_0132:  ldloc.2
        IL_0133:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
        IL_0138:  ldfld      class PipeSystem.ExtractOptions PipeSystem.CompProperties_ResourceStorage::extractOptions
        IL_013d:  ldfld      string PipeSystem.ExtractOptions::descKey
        IL_0142:  ldloc.s    V_4
        IL_0144:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.TranslatorFormattedStringExtensions::Translate(string,
                                                                                                                                                     valuetype ['Assembly-CSharp']Verse.NamedArgument[])
        IL_0149:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
        IL_014e:  stfld      string ['Assembly-CSharp']Verse.Command::defaultDesc
        IL_0153:  ldloc.2
        IL_0154:  call       instance bool PipeSystem.CompResourceStorage::get_CanExtract()
        IL_0159:  brfalse.s  IL_0169

        IL_015b:  ldloc.2
        IL_015c:  ldfld      class ['Assembly-CSharp']Verse.Command_Action PipeSystem.CompResourceStorage::extractGizmo
        IL_0161:  ldc.i4.0
        IL_0162:  callvirt   instance void ['Assembly-CSharp']Verse.Gizmo::set_Disabled(bool)
        IL_0167:  br.s       IL_01af

        IL_0169:  ldloc.2
        IL_016a:  ldfld      class ['Assembly-CSharp']Verse.Command_Action PipeSystem.CompResourceStorage::extractGizmo
        IL_016f:  ldloc.2
        IL_0170:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
        IL_0175:  ldfld      class PipeSystem.ExtractOptions PipeSystem.CompProperties_ResourceStorage::extractOptions
        IL_017a:  ldfld      string PipeSystem.ExtractOptions::disabledReasonKey
        IL_017f:  call       bool ['Assembly-CSharp']Verse.GenText::NullOrEmpty(string)
        IL_0184:  brtrue.s   IL_019f

        IL_0186:  ldloc.2
        IL_0187:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
        IL_018c:  ldfld      class PipeSystem.ExtractOptions PipeSystem.CompProperties_ResourceStorage::extractOptions
        IL_0191:  ldfld      string PipeSystem.ExtractOptions::disabledReasonKey
        IL_0196:  ldloc.s    V_4
        IL_0198:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.TranslatorFormattedStringExtensions::Translate(string,
                                                                                                                                                     valuetype ['Assembly-CSharp']Verse.NamedArgument[])
        IL_019d:  br.s       IL_01a5

        IL_019f:  ldnull
        IL_01a0:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.TaggedString::op_Implicit(string)
        IL_01a5:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
        IL_01aa:  callvirt   instance void ['Assembly-CSharp']Verse.Gizmo::Disable(string)
        IL_01af:  ldarg.0
        IL_01b0:  ldloc.2
        IL_01b1:  ldfld      class ['Assembly-CSharp']Verse.Command_Action PipeSystem.CompResourceStorage::extractGizmo
        IL_01b6:  stfld      class ['Assembly-CSharp']Verse.Gizmo PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>2__current'
        IL_01bb:  ldarg.0
        IL_01bc:  ldc.i4.4
        IL_01bd:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
        IL_01c2:  ldc.i4.1
        IL_01c3:  stloc.0
        IL_01c4:  leave      IL_0296

        IL_01c9:  ldarg.0
        IL_01ca:  ldc.i4.m1
        IL_01cb:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
        IL_01d0:  call       bool ['Assembly-CSharp']Verse.DebugSettings::get_ShowDevGizmos()
        IL_01d5:  brfalse    IL_028b

        IL_01da:  ldarg.0
        IL_01db:  newobj     instance void ['Assembly-CSharp']Verse.Command_Action::.ctor()
        IL_01e0:  dup
        IL_01e1:  ldstr      "DEBUG: Fill"
        IL_01e6:  stfld      string ['Assembly-CSharp']Verse.Command::defaultLabel
        IL_01eb:  dup
        IL_01ec:  ldloc.2
        IL_01ed:  ldftn      instance void PipeSystem.CompResourceStorage::'<CompGetGizmosExtra>b__42_0'()
        IL_01f3:  newobj     instance void [mscorlib]System.Action::.ctor(object,
                                                                          native int)
        IL_01f8:  stfld      class [mscorlib]System.Action ['Assembly-CSharp']Verse.Command_Action::action
        IL_01fd:  stfld      class ['Assembly-CSharp']Verse.Gizmo PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>2__current'
        IL_0202:  ldarg.0
        IL_0203:  ldc.i4.5
        IL_0204:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
        IL_0209:  ldc.i4.1
        IL_020a:  stloc.0
        IL_020b:  leave      IL_0296

        IL_0210:  ldarg.0
        IL_0211:  ldc.i4.m1
        IL_0212:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
        IL_0217:  ldarg.0
        IL_0218:  newobj     instance void ['Assembly-CSharp']Verse.Command_Action::.ctor()
        IL_021d:  dup
        IL_021e:  ldstr      "DEBUG: Add 5"
        IL_0223:  stfld      string ['Assembly-CSharp']Verse.Command::defaultLabel
        IL_0228:  dup
        IL_0229:  ldloc.2
        IL_022a:  ldftn      instance void PipeSystem.CompResourceStorage::'<CompGetGizmosExtra>b__42_1'()
        IL_0230:  newobj     instance void [mscorlib]System.Action::.ctor(object,
                                                                          native int)
        IL_0235:  stfld      class [mscorlib]System.Action ['Assembly-CSharp']Verse.Command_Action::action
        IL_023a:  stfld      class ['Assembly-CSharp']Verse.Gizmo PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>2__current'
        IL_023f:  ldarg.0
        IL_0240:  ldc.i4.6
        IL_0241:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
        IL_0246:  ldc.i4.1
        IL_0247:  stloc.0
        IL_0248:  leave.s    IL_0296

        IL_024a:  ldarg.0
        IL_024b:  ldc.i4.m1
        IL_024c:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
        IL_0251:  ldarg.0
        IL_0252:  newobj     instance void ['Assembly-CSharp']Verse.Command_Action::.ctor()
        IL_0257:  dup
        IL_0258:  ldstr      "DEBUG: Empty"
        IL_025d:  stfld      string ['Assembly-CSharp']Verse.Command::defaultLabel
        IL_0262:  dup
        IL_0263:  ldloc.2
        IL_0264:  ldftn      instance void PipeSystem.CompResourceStorage::'<CompGetGizmosExtra>b__42_2'()
        IL_026a:  newobj     instance void [mscorlib]System.Action::.ctor(object,
                                                                          native int)
        IL_026f:  stfld      class [mscorlib]System.Action ['Assembly-CSharp']Verse.Command_Action::action
        IL_0274:  stfld      class ['Assembly-CSharp']Verse.Gizmo PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>2__current'
        IL_0279:  ldarg.0
        IL_027a:  ldc.i4.7
        IL_027b:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
        IL_0280:  ldc.i4.1
        IL_0281:  stloc.0
        IL_0282:  leave.s    IL_0296

        IL_0284:  ldarg.0
        IL_0285:  ldc.i4.m1
        IL_0286:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
        IL_028b:  ldc.i4.0
        IL_028c:  stloc.0
        IL_028d:  leave.s    IL_0296

      }  // end .try
      fault
      {
        IL_028f:  ldarg.0
        IL_0290:  call       instance void PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::System.IDisposable.Dispose()
        IL_0295:  endfinally
      }  // end handler
      IL_0296:  ldloc.0
      IL_0297:  ret
    } // end of method '<CompGetGizmosExtra>d__42'::MoveNext

    .method private hidebysig instance void 
            '<>m__Finally1'() cil managed
    {
      // Code size       27 (0x1b)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.m1
      IL_0002:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
      IL_0007:  ldarg.0
      IL_0008:  ldfld      class [mscorlib]System.Collections.Generic.IEnumerator`1<class ['Assembly-CSharp']Verse.Gizmo> PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>7__wrap1'
      IL_000d:  brfalse.s  IL_001a

      IL_000f:  ldarg.0
      IL_0010:  ldfld      class [mscorlib]System.Collections.Generic.IEnumerator`1<class ['Assembly-CSharp']Verse.Gizmo> PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>7__wrap1'
      IL_0015:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
      IL_001a:  ret
    } // end of method '<CompGetGizmosExtra>d__42'::'<>m__Finally1'

    .method private hidebysig newslot specialname virtual final 
            instance class ['Assembly-CSharp']Verse.Gizmo 
            'System.Collections.Generic.IEnumerator<Verse.Gizmo>.get_Current'() cil managed
    {
      .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
      .override  method instance !0 class [mscorlib]System.Collections.Generic.IEnumerator`1<class ['Assembly-CSharp']Verse.Gizmo>::get_Current()
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class ['Assembly-CSharp']Verse.Gizmo PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>2__current'
      IL_0006:  ret
    } // end of method '<CompGetGizmosExtra>d__42'::'System.Collections.Generic.IEnumerator<Verse.Gizmo>.get_Current'

    .method private hidebysig newslot virtual final 
            instance void  System.Collections.IEnumerator.Reset() cil managed
    {
      .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
      .override [mscorlib]System.Collections.IEnumerator::Reset
      // Code size       6 (0x6)
      .maxstack  8
      IL_0000:  newobj     instance void [mscorlib]System.NotSupportedException::.ctor()
      IL_0005:  throw
    } // end of method '<CompGetGizmosExtra>d__42'::System.Collections.IEnumerator.Reset

    .method private hidebysig newslot specialname virtual final 
            instance object  System.Collections.IEnumerator.get_Current() cil managed
    {
      .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
      .override [mscorlib]System.Collections.IEnumerator::get_Current
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class ['Assembly-CSharp']Verse.Gizmo PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>2__current'
      IL_0006:  ret
    } // end of method '<CompGetGizmosExtra>d__42'::System.Collections.IEnumerator.get_Current

    .method private hidebysig newslot virtual final 
            instance class [mscorlib]System.Collections.Generic.IEnumerator`1<class ['Assembly-CSharp']Verse.Gizmo> 
            'System.Collections.Generic.IEnumerable<Verse.Gizmo>.GetEnumerator'() cil managed
    {
      .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
      .override  method instance class [mscorlib]System.Collections.Generic.IEnumerator`1<!0> class [mscorlib]System.Collections.Generic.IEnumerable`1<class ['Assembly-CSharp']Verse.Gizmo>::GetEnumerator()
      // Code size       55 (0x37)
      .maxstack  2
      .locals init (class PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42' V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
      IL_0006:  ldc.i4.s   -2
      IL_0008:  bne.un.s   IL_0022

      IL_000a:  ldarg.0
      IL_000b:  ldfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>l__initialThreadId'
      IL_0010:  call       int32 [mscorlib]System.Environment::get_CurrentManagedThreadId()
      IL_0015:  bne.un.s   IL_0022

      IL_0017:  ldarg.0
      IL_0018:  ldc.i4.0
      IL_0019:  stfld      int32 PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>1__state'
      IL_001e:  ldarg.0
      IL_001f:  stloc.0
      IL_0020:  br.s       IL_0035

      IL_0022:  ldc.i4.0
      IL_0023:  newobj     instance void PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::.ctor(int32)
      IL_0028:  stloc.0
      IL_0029:  ldloc.0
      IL_002a:  ldarg.0
      IL_002b:  ldfld      class PipeSystem.CompResourceStorage PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>4__this'
      IL_0030:  stfld      class PipeSystem.CompResourceStorage PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>4__this'
      IL_0035:  ldloc.0
      IL_0036:  ret
    } // end of method '<CompGetGizmosExtra>d__42'::'System.Collections.Generic.IEnumerable<Verse.Gizmo>.GetEnumerator'

    .method private hidebysig newslot virtual final 
            instance class [mscorlib]System.Collections.IEnumerator 
            System.Collections.IEnumerable.GetEnumerator() cil managed
    {
      .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
      .override [mscorlib]System.Collections.IEnumerable::GetEnumerator
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance class [mscorlib]System.Collections.Generic.IEnumerator`1<class ['Assembly-CSharp']Verse.Gizmo> PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'System.Collections.Generic.IEnumerable<Verse.Gizmo>.GetEnumerator'()
      IL_0006:  ret
    } // end of method '<CompGetGizmosExtra>d__42'::System.Collections.IEnumerable.GetEnumerator

    .property instance class ['Assembly-CSharp']Verse.Gizmo
            'System.Collections.Generic.IEnumerator<Verse.Gizmo>.Current'()
    {
      .get instance class ['Assembly-CSharp']Verse.Gizmo PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'System.Collections.Generic.IEnumerator<Verse.Gizmo>.get_Current'()
    } // end of property '<CompGetGizmosExtra>d__42'::'System.Collections.Generic.IEnumerator<Verse.Gizmo>.Current'
    .property instance object System.Collections.IEnumerator.Current()
    {
      .get instance object PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::System.Collections.IEnumerator.get_Current()
    } // end of property '<CompGetGizmosExtra>d__42'::System.Collections.IEnumerator.Current
  } // end of class '<CompGetGizmosExtra>d__42'

  .field public bool markedForExtract
  .field public bool markedForTransfer
  .field public bool markedForRefill
  .field public float32 extractResourceAmount
  .field private float32 amountStored
  .field private int32 ticksWithoutPower
  .field private bool isBreakdownable
  .field private valuetype ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest 'request'
  .field private class ['Assembly-CSharp']Verse.Command_Action extractGizmo
  .field private class ['Assembly-CSharp']Verse.Command_Action transferGizmo
  .field private class ['Assembly-CSharp']Verse.Command_Toggle refillGizmo
  .field private class PipeSystem.PipeNetOverlayDrawer pipeNetOverlayDrawer
  .field private static initonly class [UnityEngine.CoreModule]UnityEngine.Texture2D transferIcon
  .field private bool '<ContentCanRot>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig specialname instance class PipeSystem.CompProperties_ResourceStorage 
          get_Props() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class ['Assembly-CSharp']Verse.CompProperties ['Assembly-CSharp']Verse.ThingComp::props
    IL_0006:  castclass  PipeSystem.CompProperties_ResourceStorage
    IL_000b:  ret
  } // end of method CompResourceStorage::get_Props

  .method public hidebysig specialname instance float32 
          get_AmountStored() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float32 PipeSystem.CompResourceStorage::amountStored
    IL_0006:  ret
  } // end of method CompResourceStorage::get_AmountStored

  .method private hidebysig specialname instance void 
          set_AmountStored(float32 'value') cil managed
  {
    // Code size       71 (0x47)
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      float32 PipeSystem.CompResourceStorage::amountStored
    IL_0007:  ldarg.0
    IL_0008:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_000d:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_0012:  brfalse.s  IL_0046

    IL_0014:  ldarg.0
    IL_0015:  call       instance bool PipeSystem.CompResourceStorage::get_CanExtract()
    IL_001a:  brtrue.s   IL_0046

    IL_001c:  ldarg.0
    IL_001d:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0022:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_0027:  ldfld      class ['Assembly-CSharp']Verse.DesignationManager ['Assembly-CSharp']Verse.Map::designationManager
    IL_002c:  ldarg.0
    IL_002d:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0032:  ldsfld     class ['Assembly-CSharp']Verse.DesignationDef PipeSystem.PSDefOf::PS_Drain
    IL_0037:  callvirt   instance class ['Assembly-CSharp']Verse.Designation ['Assembly-CSharp']Verse.DesignationManager::DesignationOn(class ['Assembly-CSharp']Verse.Thing,
                                                                                                                                        class ['Assembly-CSharp']Verse.DesignationDef)
    IL_003c:  dup
    IL_003d:  brtrue.s   IL_0041

    IL_003f:  pop
    IL_0040:  ret

    IL_0041:  call       instance void ['Assembly-CSharp']Verse.Designation::Delete()
    IL_0046:  ret
  } // end of method CompResourceStorage::set_AmountStored

  .method public hidebysig specialname instance float32 
          get_AmountStoredPct() cil managed
  {
    // Code size       19 (0x13)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float32 PipeSystem.CompResourceStorage::amountStored
    IL_0006:  ldarg.0
    IL_0007:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_000c:  ldfld      float32 PipeSystem.CompProperties_ResourceStorage::storageCapacity
    IL_0011:  div
    IL_0012:  ret
  } // end of method CompResourceStorage::get_AmountStoredPct

  .method public hidebysig specialname instance float32 
          get_AmountCanAccept() cil managed
  {
    // Code size       78 (0x4e)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool PipeSystem.CompResourceStorage::isBreakdownable
    IL_0006:  brfalse.s  IL_001b

    IL_0008:  ldarg.0
    IL_0009:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_000e:  call       bool ['Assembly-CSharp']RimWorld.BreakdownableUtility::IsBrokenDown(class ['Assembly-CSharp']Verse.Thing)
    IL_0013:  brfalse.s  IL_001b

    IL_0015:  ldc.r4     0.0
    IL_001a:  ret

    IL_001b:  ldarg.0
    IL_001c:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_0021:  ldfld      bool PipeSystem.CompProperties_ResourceStorage::contentRequirePower
    IL_0026:  brfalse.s  IL_003b

    IL_0028:  ldarg.0
    IL_0029:  ldfld      class ['Assembly-CSharp']RimWorld.CompPowerTrader PipeSystem.CompResource::powerComp
    IL_002e:  callvirt   instance bool ['Assembly-CSharp']RimWorld.CompPowerTrader::get_PowerOn()
    IL_0033:  brtrue.s   IL_003b

    IL_0035:  ldc.r4     0.0
    IL_003a:  ret

    IL_003b:  ldarg.0
    IL_003c:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_0041:  ldfld      float32 PipeSystem.CompProperties_ResourceStorage::storageCapacity
    IL_0046:  ldarg.0
    IL_0047:  ldfld      float32 PipeSystem.CompResourceStorage::amountStored
    IL_004c:  sub
    IL_004d:  ret
  } // end of method CompResourceStorage::get_AmountCanAccept

  .method public hidebysig specialname instance bool 
          get_ContentCanRot() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool PipeSystem.CompResourceStorage::'<ContentCanRot>k__BackingField'
    IL_0006:  ret
  } // end of method CompResourceStorage::get_ContentCanRot

  .method private hidebysig specialname instance void 
          set_ContentCanRot(bool 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      bool PipeSystem.CompResourceStorage::'<ContentCanRot>k__BackingField'
    IL_0007:  ret
  } // end of method CompResourceStorage::set_ContentCanRot

  .method private hidebysig specialname instance bool 
          get_CanExtract() cil managed
  {
    // Code size       33 (0x21)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_0006:  ldfld      class PipeSystem.ExtractOptions PipeSystem.CompProperties_ResourceStorage::extractOptions
    IL_000b:  brtrue.s   IL_000f

    IL_000d:  ldc.i4.0
    IL_000e:  ret

    IL_000f:  ldarg.0
    IL_0010:  call       instance valuetype [mscorlib]System.ValueTuple`2<float32,int32> PipeSystem.CompResourceStorage::CurrentManualExtractAmount()
    IL_0015:  ldfld      !1 valuetype [mscorlib]System.ValueTuple`2<float32,int32>::Item2
    IL_001a:  ldc.i4.1
    IL_001b:  clt
    IL_001d:  ldc.i4.0
    IL_001e:  ceq
    IL_0020:  ret
  } // end of method CompResourceStorage::get_CanExtract

  .method public hidebysig virtual instance void 
          CompTickInterval(int32 delta) cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void PipeSystem.CompResourceStorage::Tick(int32)
    IL_0007:  ret
  } // end of method CompResourceStorage::CompTickInterval

  .method public hidebysig virtual instance void 
          CompTickRare() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4     0xfa
    IL_0006:  call       instance void PipeSystem.CompResourceStorage::Tick(int32)
    IL_000b:  ret
  } // end of method CompResourceStorage::CompTickRare

  .method public hidebysig virtual instance void 
          CompTickLong() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4     0x7d0
    IL_0006:  call       instance void PipeSystem.CompResourceStorage::Tick(int32)
    IL_000b:  ret
  } // end of method CompResourceStorage::CompTickLong

  .method private hidebysig instance void 
          Tick([opt] int32 ticks) cil managed
  {
    .param [1] = int32(0x00000001)
    // Code size       219 (0xdb)
    .maxstack  4
    .locals init (class ['Assembly-CSharp']Verse.Map V_0)
    IL_0000:  ldarg.0
    IL_0001:  call       instance bool PipeSystem.CompResourceStorage::get_ContentCanRot()
    IL_0006:  brtrue.s   IL_0009

    IL_0008:  ret

    IL_0009:  ldarg.0
    IL_000a:  ldfld      class ['Assembly-CSharp']RimWorld.CompPowerTrader PipeSystem.CompResource::powerComp
    IL_000f:  callvirt   instance bool ['Assembly-CSharp']RimWorld.CompPowerTrader::get_PowerOn()
    IL_0014:  brtrue     IL_00d3

    IL_0019:  ldarg.0
    IL_001a:  ldfld      float32 PipeSystem.CompResourceStorage::amountStored
    IL_001f:  ldc.r4     0.0
    IL_0024:  ble.un     IL_00d3

    IL_0029:  ldarg.0
    IL_002a:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_002f:  ldfld      bool PipeSystem.CompProperties_ResourceStorage::preventRotInNegativeTemp
    IL_0034:  brfalse.s  IL_005e

    IL_0036:  ldarg.0
    IL_0037:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_003c:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_MapHeld()
    IL_0041:  stloc.0
    IL_0042:  ldloc.0
    IL_0043:  brfalse.s  IL_005d

    IL_0045:  ldarg.0
    IL_0046:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_004b:  callvirt   instance valuetype ['Assembly-CSharp']Verse.IntVec3 ['Assembly-CSharp']Verse.Thing::get_Position()
    IL_0050:  ldloc.0
    IL_0051:  call       float32 ['Assembly-CSharp']Verse.GridsUtility::GetTemperature(valuetype ['Assembly-CSharp']Verse.IntVec3,
                                                                                       class ['Assembly-CSharp']Verse.Map)
    IL_0056:  ldc.r4     0.0
    IL_005b:  bge.un.s   IL_005e

    IL_005d:  ret

    IL_005e:  ldarg.0
    IL_005f:  ldarg.0
    IL_0060:  ldfld      int32 PipeSystem.CompResourceStorage::ticksWithoutPower
    IL_0065:  ldarg.1
    IL_0066:  add
    IL_0067:  stfld      int32 PipeSystem.CompResourceStorage::ticksWithoutPower
    IL_006c:  ldarg.0
    IL_006d:  ldfld      int32 PipeSystem.CompResourceStorage::ticksWithoutPower
    IL_0072:  conv.r4
    IL_0073:  ldc.r4     60000.
    IL_0078:  ldarg.0
    IL_0079:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_007e:  ldfld      float32 PipeSystem.CompProperties_ResourceStorage::daysToRotStart
    IL_0083:  mul
    IL_0084:  ble.un.s   IL_00da

    IL_0086:  ldarg.0
    IL_0087:  ldc.r4     0.0
    IL_008c:  call       instance void PipeSystem.CompResourceStorage::set_AmountStored(float32)
    IL_0091:  ldstr      "PipeSystem_StorageContentRotted"
    IL_0096:  ldarg.0
    IL_0097:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_009c:  ldfld      class ['Assembly-CSharp']Verse.ThingDef ['Assembly-CSharp']Verse.Thing::def
    IL_00a1:  callvirt   instance valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.Def::get_LabelCap()
    IL_00a6:  call       valuetype ['Assembly-CSharp']Verse.NamedArgument ['Assembly-CSharp']Verse.NamedArgument::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
    IL_00ab:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.TranslatorFormattedStringExtensions::Translate(string,
                                                                                                                                                 valuetype ['Assembly-CSharp']Verse.NamedArgument)
    IL_00b0:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
    IL_00b5:  ldarg.0
    IL_00b6:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_00bb:  call       class ['Assembly-CSharp']Verse.LookTargets ['Assembly-CSharp']Verse.LookTargets::op_Implicit(class ['Assembly-CSharp']Verse.Thing)
    IL_00c0:  ldsfld     class ['Assembly-CSharp']Verse.MessageTypeDef ['Assembly-CSharp']RimWorld.MessageTypeDefOf::NegativeEvent
    IL_00c5:  ldc.i4.1
    IL_00c6:  call       void ['Assembly-CSharp']Verse.Messages::Message(string,
                                                                         class ['Assembly-CSharp']Verse.LookTargets,
                                                                         class ['Assembly-CSharp']Verse.MessageTypeDef,
                                                                         bool)
    IL_00cb:  ldarg.0
    IL_00cc:  ldc.i4.0
    IL_00cd:  stfld      int32 PipeSystem.CompResourceStorage::ticksWithoutPower
    IL_00d2:  ret

    IL_00d3:  ldarg.0
    IL_00d4:  ldc.i4.0
    IL_00d5:  stfld      int32 PipeSystem.CompResourceStorage::ticksWithoutPower
    IL_00da:  ret
  } // end of method CompResourceStorage::Tick

  .method public hidebysig virtual instance void 
          PostSpawnSetup(bool respawningAfterLoad) cil managed
  {
    // Code size       864 (0x360)
    .maxstack  5
    .locals init (valuetype ['Assembly-CSharp']Verse.Rot4 V_0,
             bool V_1,
             valuetype ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest V_2,
             valuetype ['Assembly-CSharp']Verse.Rot4 V_3,
             valuetype ['Assembly-CSharp']Verse.NamedArgument[] V_4)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void PipeSystem.CompResource::PostSpawnSetup(bool)
    IL_0007:  ldarg.0
    IL_0008:  call       void PipeSystem.CachedCompResourceStorage::Cache(class PipeSystem.CompResourceStorage)
    IL_000d:  ldarg.0
    IL_000e:  ldarg.0
    IL_000f:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0014:  call       !!0 ['Assembly-CSharp']Verse.ThingCompUtility::TryGetComp<class ['Assembly-CSharp']RimWorld.CompBreakdownable>(class ['Assembly-CSharp']Verse.Thing)
    IL_0019:  ldnull
    IL_001a:  cgt.un
    IL_001c:  stfld      bool PipeSystem.CompResourceStorage::isBreakdownable
    IL_0021:  ldarg.0
    IL_0022:  ldarg.0
    IL_0023:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_0028:  ldfld      bool PipeSystem.CompProperties_ResourceStorage::contentRequirePower
    IL_002d:  brfalse.s  IL_003a

    IL_002f:  ldarg.0
    IL_0030:  ldfld      class ['Assembly-CSharp']RimWorld.CompPowerTrader PipeSystem.CompResource::powerComp
    IL_0035:  ldnull
    IL_0036:  cgt.un
    IL_0038:  br.s       IL_003b

    IL_003a:  ldc.i4.0
    IL_003b:  call       instance void PipeSystem.CompResourceStorage::set_ContentCanRot(bool)
    IL_0040:  ldarg.0
    IL_0041:  ldarg.0
    IL_0042:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0047:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_004c:  callvirt   instance !!0 ['Assembly-CSharp']Verse.Map::GetComponent<class PipeSystem.PipeNetOverlayDrawer>()
    IL_0051:  stfld      class PipeSystem.PipeNetOverlayDrawer PipeSystem.CompResourceStorage::pipeNetOverlayDrawer
    IL_0056:  ldloca.s   V_2
    IL_0058:  initobj    ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest
    IL_005e:  ldloca.s   V_2
    IL_0060:  ldarg.0
    IL_0061:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0066:  callvirt   instance valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 ['Assembly-CSharp']Verse.Thing::get_DrawPos()
    IL_006b:  ldarg.0
    IL_006c:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_0071:  ldfld      valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 PipeSystem.CompProperties_ResourceStorage::centerOffset
    IL_0076:  call       valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 [UnityEngine.CoreModule]UnityEngine.Vector3::op_Addition(valuetype [UnityEngine.CoreModule]UnityEngine.Vector3,
                                                                                                                                        valuetype [UnityEngine.CoreModule]UnityEngine.Vector3)
    IL_007b:  call       valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 [UnityEngine.CoreModule]UnityEngine.Vector3::get_up()
    IL_0080:  ldc.r4     0.1
    IL_0085:  call       valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 [UnityEngine.CoreModule]UnityEngine.Vector3::op_Multiply(valuetype [UnityEngine.CoreModule]UnityEngine.Vector3,
                                                                                                                                        float32)
    IL_008a:  call       valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 [UnityEngine.CoreModule]UnityEngine.Vector3::op_Addition(valuetype [UnityEngine.CoreModule]UnityEngine.Vector3,
                                                                                                                                        valuetype [UnityEngine.CoreModule]UnityEngine.Vector3)
    IL_008f:  stfld      valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest::center
    IL_0094:  ldloca.s   V_2
    IL_0096:  ldarg.0
    IL_0097:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_009c:  ldfld      valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 PipeSystem.CompProperties_ResourceStorage::barSize
    IL_00a1:  stfld      valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest::size
    IL_00a6:  ldloca.s   V_2
    IL_00a8:  ldarg.0
    IL_00a9:  call       instance float32 PipeSystem.CompResourceStorage::get_AmountStoredPct()
    IL_00ae:  stfld      float32 ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest::fillPercent
    IL_00b3:  ldloca.s   V_2
    IL_00b5:  ldsfld     class [mscorlib]System.Collections.Generic.Dictionary`2<class PipeSystem.PipeNetDef,class [UnityEngine.CoreModule]UnityEngine.Material> PipeSystem.MaterialCreator::materials
    IL_00ba:  ldarg.0
    IL_00bb:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_00c0:  ldfld      class PipeSystem.PipeNetDef PipeSystem.CompProperties_Resource::pipeNet
    IL_00c5:  ldsfld     class [UnityEngine.CoreModule]UnityEngine.Material PipeSystem.MaterialCreator::BarFallbackMat
    IL_00ca:  call       !!1 ['Assembly-CSharp']Verse.GenCollection::TryGetValue<class PipeSystem.PipeNetDef,class [UnityEngine.CoreModule]UnityEngine.Material>(class [mscorlib]System.Collections.Generic.IReadOnlyDictionary`2<!!0,!!1>,
                                                                                                                                                                 !!0,
                                                                                                                                                                 !!1)
    IL_00cf:  stfld      class [UnityEngine.CoreModule]UnityEngine.Material ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest::filledMat
    IL_00d4:  ldloca.s   V_2
    IL_00d6:  ldsfld     class [UnityEngine.CoreModule]UnityEngine.Material PipeSystem.MaterialCreator::BarUnfilledMat
    IL_00db:  stfld      class [UnityEngine.CoreModule]UnityEngine.Material ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest::unfilledMat
    IL_00e0:  ldloca.s   V_2
    IL_00e2:  ldarg.0
    IL_00e3:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_00e8:  ldfld      float32 PipeSystem.CompProperties_ResourceStorage::margin
    IL_00ed:  stfld      float32 ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest::margin
    IL_00f2:  ldarg.0
    IL_00f3:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_00f8:  ldfld      bool PipeSystem.CompProperties_ResourceStorage::barHorizontal
    IL_00fd:  ldarg.0
    IL_00fe:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_0103:  ldfld      bool PipeSystem.CompProperties_ResourceStorage::rotateBarWithBuilding
    IL_0108:  stloc.1
    IL_0109:  brtrue.s   IL_0110

    IL_010b:  ldloc.1
    IL_010c:  brfalse.s  IL_0115

    IL_010e:  br.s       IL_0125

    IL_0110:  ldloc.1
    IL_0111:  brfalse.s  IL_011d

    IL_0113:  br.s       IL_013c

    IL_0115:  ldsfld     valuetype ['Assembly-CSharp']Verse.Rot4 ['Assembly-CSharp']Verse.Rot4::East
    IL_011a:  stloc.0
    IL_011b:  br.s       IL_0148

    IL_011d:  ldsfld     valuetype ['Assembly-CSharp']Verse.Rot4 ['Assembly-CSharp']Verse.Rot4::North
    IL_0122:  stloc.0
    IL_0123:  br.s       IL_0148

    IL_0125:  ldarg.0
    IL_0126:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_012b:  callvirt   instance valuetype ['Assembly-CSharp']Verse.Rot4 ['Assembly-CSharp']Verse.Thing::get_Rotation()
    IL_0130:  stloc.3
    IL_0131:  ldloca.s   V_3
    IL_0133:  ldc.i4.1
    IL_0134:  call       instance valuetype ['Assembly-CSharp']Verse.Rot4 ['Assembly-CSharp']Verse.Rot4::Rotated(valuetype ['Assembly-CSharp']Verse.RotationDirection)
    IL_0139:  stloc.0
    IL_013a:  br.s       IL_0148

    IL_013c:  ldarg.0
    IL_013d:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0142:  callvirt   instance valuetype ['Assembly-CSharp']Verse.Rot4 ['Assembly-CSharp']Verse.Thing::get_Rotation()
    IL_0147:  stloc.0
    IL_0148:  ldloca.s   V_2
    IL_014a:  ldloc.0
    IL_014b:  stfld      valuetype ['Assembly-CSharp']Verse.Rot4 ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest::rotation
    IL_0150:  ldarg.0
    IL_0151:  ldloc.2
    IL_0152:  stfld      valuetype ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest PipeSystem.CompResourceStorage::'request'
    IL_0157:  ldarg.0
    IL_0158:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_015d:  ldfld      class PipeSystem.ExtractOptions PipeSystem.CompProperties_ResourceStorage::extractOptions
    IL_0162:  brfalse    IL_020e

    IL_0167:  ldarg.0
    IL_0168:  call       instance valuetype ['Assembly-CSharp']Verse.NamedArgument[] PipeSystem.CompResourceStorage::ExtractResourceArguments()
    IL_016d:  stloc.s    V_4
    IL_016f:  ldarg.0
    IL_0170:  ldarg.0
    IL_0171:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_0176:  ldfld      class PipeSystem.ExtractOptions PipeSystem.CompProperties_ResourceStorage::extractOptions
    IL_017b:  ldfld      float32 PipeSystem.ExtractOptions::ratio
    IL_0180:  ldarg.0
    IL_0181:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_0186:  ldfld      class PipeSystem.ExtractOptions PipeSystem.CompProperties_ResourceStorage::extractOptions
    IL_018b:  ldfld      int32 PipeSystem.ExtractOptions::extractAmount
    IL_0190:  conv.r4
    IL_0191:  mul
    IL_0192:  stfld      float32 PipeSystem.CompResourceStorage::extractResourceAmount
    IL_0197:  ldarg.0
    IL_0198:  newobj     instance void ['Assembly-CSharp']Verse.Command_Action::.ctor()
    IL_019d:  dup
    IL_019e:  ldarg.0
    IL_019f:  ldftn      instance void PipeSystem.CompResourceStorage::'<PostSpawnSetup>b__32_0'()
    IL_01a5:  newobj     instance void [mscorlib]System.Action::.ctor(object,
                                                                      native int)
    IL_01aa:  stfld      class [mscorlib]System.Action ['Assembly-CSharp']Verse.Command_Action::action
    IL_01af:  dup
    IL_01b0:  ldarg.0
    IL_01b1:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_01b6:  ldfld      class PipeSystem.ExtractOptions PipeSystem.CompProperties_ResourceStorage::extractOptions
    IL_01bb:  ldfld      string PipeSystem.ExtractOptions::labelKey
    IL_01c0:  ldloc.s    V_4
    IL_01c2:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.TranslatorFormattedStringExtensions::Translate(string,
                                                                                                                                                 valuetype ['Assembly-CSharp']Verse.NamedArgument[])
    IL_01c7:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
    IL_01cc:  stfld      string ['Assembly-CSharp']Verse.Command::defaultLabel
    IL_01d1:  dup
    IL_01d2:  ldarg.0
    IL_01d3:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_01d8:  ldfld      class PipeSystem.ExtractOptions PipeSystem.CompProperties_ResourceStorage::extractOptions
    IL_01dd:  ldfld      string PipeSystem.ExtractOptions::descKey
    IL_01e2:  ldloc.s    V_4
    IL_01e4:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.TranslatorFormattedStringExtensions::Translate(string,
                                                                                                                                                 valuetype ['Assembly-CSharp']Verse.NamedArgument[])
    IL_01e9:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
    IL_01ee:  stfld      string ['Assembly-CSharp']Verse.Command::defaultDesc
    IL_01f3:  dup
    IL_01f4:  ldarg.0
    IL_01f5:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_01fa:  ldfld      class PipeSystem.ExtractOptions PipeSystem.CompProperties_ResourceStorage::extractOptions
    IL_01ff:  ldfld      class [UnityEngine.CoreModule]UnityEngine.Texture2D PipeSystem.ExtractOptions::tex
    IL_0204:  stfld      class [UnityEngine.CoreModule]UnityEngine.Texture ['Assembly-CSharp']Verse.Command::icon
    IL_0209:  stfld      class ['Assembly-CSharp']Verse.Command_Action PipeSystem.CompResourceStorage::extractGizmo
    IL_020e:  ldarg.0
    IL_020f:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_0214:  ldfld      class PipeSystem.RefillOptions PipeSystem.CompProperties_ResourceStorage::refillOptions
    IL_0219:  brfalse    IL_02af

    IL_021e:  ldarg.0
    IL_021f:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_0224:  ldfld      class PipeSystem.RefillOptions PipeSystem.CompProperties_ResourceStorage::refillOptions
    IL_0229:  ldfld      bool PipeSystem.RefillOptions::alwaysRefill
    IL_022e:  brtrue.s   IL_02af

    IL_0230:  ldarg.0
    IL_0231:  newobj     instance void ['Assembly-CSharp']Verse.Command_Toggle::.ctor()
    IL_0236:  dup
    IL_0237:  ldarg.0
    IL_0238:  ldftn      instance bool PipeSystem.CompResourceStorage::'<PostSpawnSetup>b__32_1'()
    IL_023e:  newobj     instance void class [mscorlib]System.Func`1<bool>::.ctor(object,
                                                                                  native int)
    IL_0243:  stfld      class [mscorlib]System.Func`1<bool> ['Assembly-CSharp']Verse.Command_Toggle::isActive
    IL_0248:  dup
    IL_0249:  ldarg.0
    IL_024a:  ldftn      instance void PipeSystem.CompResourceStorage::'<PostSpawnSetup>b__32_2'()
    IL_0250:  newobj     instance void [mscorlib]System.Action::.ctor(object,
                                                                      native int)
    IL_0255:  stfld      class [mscorlib]System.Action ['Assembly-CSharp']Verse.Command_Toggle::toggleAction
    IL_025a:  dup
    IL_025b:  ldstr      "PipeSystem_AllowManualRefill"
    IL_0260:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.Translator::Translate(string)
    IL_0265:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
    IL_026a:  stfld      string ['Assembly-CSharp']Verse.Command::defaultLabel
    IL_026f:  dup
    IL_0270:  ldstr      "PipeSystem_AllowManualRefillDesc"
    IL_0275:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.Translator::Translate(string)
    IL_027a:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
    IL_027f:  stfld      string ['Assembly-CSharp']Verse.Command::defaultDesc
    IL_0284:  dup
    IL_0285:  ldsfld     class [UnityEngine.CoreModule]UnityEngine.Texture2D ['Assembly-CSharp']RimWorld.TexCommand::ForbidOff
    IL_028a:  stfld      class [UnityEngine.CoreModule]UnityEngine.Texture ['Assembly-CSharp']Verse.Command::icon
    IL_028f:  stfld      class ['Assembly-CSharp']Verse.Command_Toggle PipeSystem.CompResourceStorage::refillGizmo
    IL_0294:  ldarg.0
    IL_0295:  ldfld      bool PipeSystem.CompResourceStorage::markedForRefill
    IL_029a:  brfalse.s  IL_02df

    IL_029c:  ldarg.0
    IL_029d:  call       instance class PipeSystem.PipeNetManager PipeSystem.CompResource::get_PipeNetManager()
    IL_02a2:  ldarg.0
    IL_02a3:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_02a8:  callvirt   instance void PipeSystem.PipeNetManager::UpdateRefillableWith(class ['Assembly-CSharp']Verse.Thing)
    IL_02ad:  br.s       IL_02df

    IL_02af:  ldarg.0
    IL_02b0:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_02b5:  ldfld      class PipeSystem.RefillOptions PipeSystem.CompProperties_ResourceStorage::refillOptions
    IL_02ba:  brfalse.s  IL_02df

    IL_02bc:  ldarg.0
    IL_02bd:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_02c2:  ldfld      class PipeSystem.RefillOptions PipeSystem.CompProperties_ResourceStorage::refillOptions
    IL_02c7:  ldfld      bool PipeSystem.RefillOptions::alwaysRefill
    IL_02cc:  brfalse.s  IL_02df

    IL_02ce:  ldarg.0
    IL_02cf:  call       instance class PipeSystem.PipeNetManager PipeSystem.CompResource::get_PipeNetManager()
    IL_02d4:  ldarg.0
    IL_02d5:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_02da:  callvirt   instance void PipeSystem.PipeNetManager::UpdateRefillableWith(class ['Assembly-CSharp']Verse.Thing)
    IL_02df:  ldarg.0
    IL_02e0:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_02e5:  ldfld      bool PipeSystem.CompProperties_ResourceStorage::addTransferGizmo
    IL_02ea:  brfalse.s  IL_035f

    IL_02ec:  ldarg.0
    IL_02ed:  newobj     instance void ['Assembly-CSharp']Verse.Command_Action::.ctor()
    IL_02f2:  dup
    IL_02f3:  ldarg.0
    IL_02f4:  ldftn      instance void PipeSystem.CompResourceStorage::'<PostSpawnSetup>b__32_3'()
    IL_02fa:  newobj     instance void [mscorlib]System.Action::.ctor(object,
                                                                      native int)
    IL_02ff:  stfld      class [mscorlib]System.Action ['Assembly-CSharp']Verse.Command_Action::action
    IL_0304:  dup
    IL_0305:  ldstr      "PipeSystem_TransferContent"
    IL_030a:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.Translator::Translate(string)
    IL_030f:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
    IL_0314:  stfld      string ['Assembly-CSharp']Verse.Command::defaultLabel
    IL_0319:  dup
    IL_031a:  ldstr      "PipeSystem_TransferContentDesc"
    IL_031f:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.Translator::Translate(string)
    IL_0324:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
    IL_0329:  stfld      string ['Assembly-CSharp']Verse.Command::defaultDesc
    IL_032e:  dup
    IL_032f:  ldsfld     class [UnityEngine.CoreModule]UnityEngine.Texture2D PipeSystem.CompResourceStorage::transferIcon
    IL_0334:  stfld      class [UnityEngine.CoreModule]UnityEngine.Texture ['Assembly-CSharp']Verse.Command::icon
    IL_0339:  stfld      class ['Assembly-CSharp']Verse.Command_Action PipeSystem.CompResourceStorage::transferGizmo
    IL_033e:  ldarg.0
    IL_033f:  ldfld      class PipeSystem.PipeNetOverlayDrawer PipeSystem.CompResourceStorage::pipeNetOverlayDrawer
    IL_0344:  dup
    IL_0345:  brtrue.s   IL_0349

    IL_0347:  pop
    IL_0348:  ret

    IL_0349:  ldarg.0
    IL_034a:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_034f:  ldsfld     class [UnityEngine.CoreModule]UnityEngine.Material PipeSystem.MaterialCreator::transferMat
    IL_0354:  ldarg.0
    IL_0355:  ldfld      bool PipeSystem.CompResourceStorage::markedForTransfer
    IL_035a:  call       instance void PipeSystem.PipeNetOverlayDrawer::ToggleStatic(class ['Assembly-CSharp']Verse.Thing,
                                                                                     class [UnityEngine.CoreModule]UnityEngine.Material,
                                                                                     bool)
    IL_035f:  ret
  } // end of method CompResourceStorage::PostSpawnSetup

  .method public hidebysig virtual instance void 
          PostDeSpawn(class ['Assembly-CSharp']Verse.Map map,
                      [opt] valuetype ['Assembly-CSharp']Verse.DestroyMode mode) cil managed
  {
    .param [2] = uint8(0x00)
    // Code size       37 (0x25)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  call       instance void PipeSystem.CompResource::PostDeSpawn(class ['Assembly-CSharp']Verse.Map,
                                                                            valuetype ['Assembly-CSharp']Verse.DestroyMode)
    IL_0008:  ldarg.0
    IL_0009:  ldfld      class PipeSystem.PipeNetOverlayDrawer PipeSystem.CompResourceStorage::pipeNetOverlayDrawer
    IL_000e:  dup
    IL_000f:  brtrue.s   IL_0013

    IL_0011:  pop
    IL_0012:  ret

    IL_0013:  ldarg.0
    IL_0014:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0019:  ldsfld     class [UnityEngine.CoreModule]UnityEngine.Material PipeSystem.MaterialCreator::transferMat
    IL_001e:  ldc.i4.0
    IL_001f:  call       instance void PipeSystem.PipeNetOverlayDrawer::ToggleStatic(class ['Assembly-CSharp']Verse.Thing,
                                                                                     class [UnityEngine.CoreModule]UnityEngine.Material,
                                                                                     bool)
    IL_0024:  ret
  } // end of method CompResourceStorage::PostDeSpawn

  .method public hidebysig virtual instance void 
          PostDraw() cil managed
  {
    // Code size       48 (0x30)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void ['Assembly-CSharp']Verse.ThingComp::PostDraw()
    IL_0006:  ldarg.0
    IL_0007:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_000c:  ldfld      bool PipeSystem.CompProperties_ResourceStorage::drawStorageBar
    IL_0011:  brfalse.s  IL_002f

    IL_0013:  ldarg.0
    IL_0014:  ldflda     valuetype ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest PipeSystem.CompResourceStorage::'request'
    IL_0019:  ldarg.0
    IL_001a:  call       instance float32 PipeSystem.CompResourceStorage::get_AmountStoredPct()
    IL_001f:  stfld      float32 ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest::fillPercent
    IL_0024:  ldarg.0
    IL_0025:  ldfld      valuetype ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest PipeSystem.CompResourceStorage::'request'
    IL_002a:  call       void ['Assembly-CSharp']Verse.GenDraw::DrawFillableBar(valuetype ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest)
    IL_002f:  ret
  } // end of method CompResourceStorage::PostDraw

  .method public hidebysig virtual instance void 
          PostExposeData() cil managed
  {
    // Code size       137 (0x89)
    .maxstack  4
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float32 PipeSystem.CompResourceStorage::amountStored
    IL_0006:  ldarg.0
    IL_0007:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_000c:  ldfld      float32 PipeSystem.CompProperties_ResourceStorage::storageCapacity
    IL_0011:  ble.un.s   IL_0024

    IL_0013:  ldarg.0
    IL_0014:  ldarg.0
    IL_0015:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_001a:  ldfld      float32 PipeSystem.CompProperties_ResourceStorage::storageCapacity
    IL_001f:  call       instance void PipeSystem.CompResourceStorage::set_AmountStored(float32)
    IL_0024:  ldarg.0
    IL_0025:  ldflda     float32 PipeSystem.CompResourceStorage::amountStored
    IL_002a:  ldstr      "storedResource"
    IL_002f:  ldc.r4     0.0
    IL_0034:  ldc.i4.0
    IL_0035:  call       void ['Assembly-CSharp']Verse.Scribe_Values::Look<float32>(!!0&,
                                                                                    string,
                                                                                    !!0,
                                                                                    bool)
    IL_003a:  ldarg.0
    IL_003b:  ldflda     int32 PipeSystem.CompResourceStorage::ticksWithoutPower
    IL_0040:  ldstr      "tickWithoutPower"
    IL_0045:  ldc.i4.0
    IL_0046:  ldc.i4.0
    IL_0047:  call       void ['Assembly-CSharp']Verse.Scribe_Values::Look<int32>(!!0&,
                                                                                  string,
                                                                                  !!0,
                                                                                  bool)
    IL_004c:  ldarg.0
    IL_004d:  ldflda     bool PipeSystem.CompResourceStorage::markedForExtract
    IL_0052:  ldstr      "markedForExtract"
    IL_0057:  ldc.i4.0
    IL_0058:  ldc.i4.0
    IL_0059:  call       void ['Assembly-CSharp']Verse.Scribe_Values::Look<bool>(!!0&,
                                                                                 string,
                                                                                 !!0,
                                                                                 bool)
    IL_005e:  ldarg.0
    IL_005f:  ldflda     bool PipeSystem.CompResourceStorage::markedForTransfer
    IL_0064:  ldstr      "markedForTransfer"
    IL_0069:  ldc.i4.0
    IL_006a:  ldc.i4.0
    IL_006b:  call       void ['Assembly-CSharp']Verse.Scribe_Values::Look<bool>(!!0&,
                                                                                 string,
                                                                                 !!0,
                                                                                 bool)
    IL_0070:  ldarg.0
    IL_0071:  ldflda     bool PipeSystem.CompResourceStorage::markedForRefill
    IL_0076:  ldstr      "markedForRefill"
    IL_007b:  ldc.i4.0
    IL_007c:  ldc.i4.0
    IL_007d:  call       void ['Assembly-CSharp']Verse.Scribe_Values::Look<bool>(!!0&,
                                                                                 string,
                                                                                 !!0,
                                                                                 bool)
    IL_0082:  ldarg.0
    IL_0083:  call       instance void ['Assembly-CSharp']Verse.ThingComp::PostExposeData()
    IL_0088:  ret
  } // end of method CompResourceStorage::PostExposeData

  .method public hidebysig virtual instance void 
          PostDestroy(valuetype ['Assembly-CSharp']Verse.DestroyMode mode,
                      class ['Assembly-CSharp']Verse.Map previousMap) cil managed
  {
    // Code size       533 (0x215)
    .maxstack  8
    .locals init (bool V_0,
             bool V_1,
             valuetype ['Assembly-CSharp']Verse.IntVec3 V_2,
             valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class PipeSystem.DestroyOption> V_3,
             class PipeSystem.DestroyOption V_4,
             int32 V_5,
             class ['Assembly-CSharp']Verse.ThingDef V_6,
             valuetype ['Assembly-CSharp']Verse.DestroyMode V_7,
             int32 V_8,
             valuetype ['Assembly-CSharp']Verse.ThingPlaceMode V_9,
             int32 V_10,
             class ['Assembly-CSharp']Verse.Thing V_11,
             class ['Assembly-CSharp']RimWorld.Faction V_12,
             int32 V_13,
             valuetype [mscorlib]System.Nullable`1<valuetype ['Assembly-CSharp']Verse.Rot4> V_14)
    IL_0000:  ldarg.2
    IL_0001:  brfalse.s  IL_0015

    IL_0003:  ldarg.0
    IL_0004:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_0009:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.DestroyOption> PipeSystem.CompProperties_ResourceStorage::destroyOptions
    IL_000e:  call       bool ['Assembly-CSharp']Verse.GenList::HasData<class PipeSystem.DestroyOption>(class [mscorlib]System.Collections.Generic.IList`1<!!0>)
    IL_0013:  br.s       IL_0016

    IL_0015:  ldc.i4.0
    IL_0016:  stloc.0
    IL_0017:  ldloc.0
    IL_0018:  brfalse.s  IL_002c

    IL_001a:  ldarg.1
    IL_001b:  ldc.i4.2
    IL_001c:  sub
    IL_001d:  ldc.i4.2
    IL_001e:  ble.un.s   IL_0024

    IL_0020:  ldarg.1
    IL_0021:  ldc.i4.7
    IL_0022:  bne.un.s   IL_0028

    IL_0024:  ldc.i4.1
    IL_0025:  stloc.1
    IL_0026:  br.s       IL_002a

    IL_0028:  ldc.i4.0
    IL_0029:  stloc.1
    IL_002a:  ldloc.1
    IL_002b:  stloc.0
    IL_002c:  ldloc.0
    IL_002d:  brfalse    IL_020c

    IL_0032:  ldarg.0
    IL_0033:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0038:  callvirt   instance valuetype ['Assembly-CSharp']Verse.IntVec3 ['Assembly-CSharp']Verse.Thing::get_Position()
    IL_003d:  stloc.2
    IL_003e:  ldarg.0
    IL_003f:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_0044:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.DestroyOption> PipeSystem.CompProperties_ResourceStorage::destroyOptions
    IL_0049:  callvirt   instance valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<!0> class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.DestroyOption>::GetEnumerator()
    IL_004e:  stloc.3
    .try
    {
      IL_004f:  br         IL_01f0

      IL_0054:  ldloca.s   V_3
      IL_0056:  call       instance !0 valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class PipeSystem.DestroyOption>::get_Current()
      IL_005b:  stloc.s    V_4
      IL_005d:  ldarg.1
      IL_005e:  stloc.s    V_7
      IL_0060:  ldloc.s    V_7
      IL_0062:  ldc.i4.2
      IL_0063:  sub
      IL_0064:  switch     ( 
                            IL_0083,
                            IL_0091,
                            IL_009f,
                            IL_00b9,
                            IL_00b9,
                            IL_00ad)
      IL_0081:  br.s       IL_00b9

      IL_0083:  ldloc.s    V_4
      IL_0085:  ldfld      bool PipeSystem.DestroyOption::spawnWhenDestroyed
      IL_008a:  brfalse    IL_01f0

      IL_008f:  br.s       IL_00b9

      IL_0091:  ldloc.s    V_4
      IL_0093:  ldfld      bool PipeSystem.DestroyOption::spawnWhenDestroyed
      IL_0098:  brfalse    IL_01f0

      IL_009d:  br.s       IL_00b9

      IL_009f:  ldloc.s    V_4
      IL_00a1:  ldfld      bool PipeSystem.DestroyOption::spawnWhenDeconstructed
      IL_00a6:  brfalse    IL_01f0

      IL_00ab:  br.s       IL_00b9

      IL_00ad:  ldloc.s    V_4
      IL_00af:  ldfld      bool PipeSystem.DestroyOption::spawnWhenRefunded
      IL_00b4:  brfalse    IL_01f0

      IL_00b9:  ldloc.s    V_4
      IL_00bb:  ldfld      int32 PipeSystem.DestroyOption::amount
      IL_00c0:  stloc.s    V_5
      IL_00c2:  ldloc.s    V_4
      IL_00c4:  ldfld      float32 PipeSystem.DestroyOption::ratio
      IL_00c9:  ldc.r4     0.0
      IL_00ce:  call       bool [UnityEngine.CoreModule]UnityEngine.Mathf::Approximately(float32,
                                                                                         float32)
      IL_00d3:  brtrue.s   IL_00ed

      IL_00d5:  ldloc.s    V_5
      IL_00d7:  ldarg.0
      IL_00d8:  ldfld      float32 PipeSystem.CompResourceStorage::amountStored
      IL_00dd:  ldloc.s    V_4
      IL_00df:  ldfld      float32 PipeSystem.DestroyOption::ratio
      IL_00e4:  div
      IL_00e5:  call       int32 [UnityEngine.CoreModule]UnityEngine.Mathf::FloorToInt(float32)
      IL_00ea:  add
      IL_00eb:  stloc.s    V_5
      IL_00ed:  ldloc.s    V_4
      IL_00ef:  ldfld      class ['Assembly-CSharp']Verse.ThingDef PipeSystem.DestroyOption::thing
      IL_00f4:  stloc.s    V_6
      IL_00f6:  ldloc.s    V_6
      IL_00f8:  callvirt   instance bool ['Assembly-CSharp']Verse.ThingDef::get_IsFilth()
      IL_00fd:  brfalse.s  IL_0131

      IL_00ff:  ldc.i4.0
      IL_0100:  stloc.s    V_8
      IL_0102:  br.s       IL_0126

      IL_0104:  ldloc.2
      IL_0105:  ldarg.2
      IL_0106:  ldloc.s    V_4
      IL_0108:  ldfld      int32 PipeSystem.DestroyOption::maxRadius
      IL_010d:  conv.r4
      IL_010e:  ldnull
      IL_010f:  call       valuetype ['Assembly-CSharp']Verse.IntVec3 ['Assembly-CSharp']Verse.CellFinder::StandableCellNear(valuetype ['Assembly-CSharp']Verse.IntVec3,
                                                                                                                             class ['Assembly-CSharp']Verse.Map,
                                                                                                                             float32,
                                                                                                                             class [mscorlib]System.Predicate`1<valuetype ['Assembly-CSharp']Verse.IntVec3>)
      IL_0114:  ldarg.2
      IL_0115:  ldloc.s    V_6
      IL_0117:  ldc.i4.1
      IL_0118:  ldc.i4.0
      IL_0119:  ldc.i4.1
      IL_011a:  call       bool ['Assembly-CSharp']RimWorld.FilthMaker::TryMakeFilth(valuetype ['Assembly-CSharp']Verse.IntVec3,
                                                                                     class ['Assembly-CSharp']Verse.Map,
                                                                                     class ['Assembly-CSharp']Verse.ThingDef,
                                                                                     int32,
                                                                                     valuetype ['Assembly-CSharp']RimWorld.FilthSourceFlags,
                                                                                     bool)
      IL_011f:  pop
      IL_0120:  ldloc.s    V_8
      IL_0122:  ldc.i4.1
      IL_0123:  add
      IL_0124:  stloc.s    V_8
      IL_0126:  ldloc.s    V_8
      IL_0128:  ldloc.s    V_5
      IL_012a:  blt.s      IL_0104

      IL_012c:  br         IL_01f0

      IL_0131:  ldloc.s    V_4
      IL_0133:  ldfld      int32 PipeSystem.DestroyOption::maxRadius
      IL_0138:  ldc.i4.0
      IL_0139:  ble.s      IL_0154

      IL_013b:  ldc.i4.2
      IL_013c:  stloc.s    V_9
      IL_013e:  ldloc.s    V_4
      IL_0140:  ldfld      int32 PipeSystem.DestroyOption::maxRadius
      IL_0145:  ldloc.s    V_4
      IL_0147:  ldfld      int32 PipeSystem.DestroyOption::maxRadius
      IL_014c:  mul
      IL_014d:  stloc.s    V_10
      IL_014f:  br         IL_01e8

      IL_0154:  ldc.i4.1
      IL_0155:  stloc.s    V_9
      IL_0157:  ldc.i4.1
      IL_0158:  stloc.s    V_10
      IL_015a:  br         IL_01e8

      IL_015f:  ldloc.s    V_6
      IL_0161:  ldnull
      IL_0162:  call       class ['Assembly-CSharp']Verse.Thing ['Assembly-CSharp']Verse.ThingMaker::MakeThing(class ['Assembly-CSharp']Verse.ThingDef,
                                                                                                               class ['Assembly-CSharp']Verse.ThingDef)
      IL_0167:  stloc.s    V_11
      IL_0169:  ldloc.s    V_11
      IL_016b:  ldfld      class ['Assembly-CSharp']Verse.ThingDef ['Assembly-CSharp']Verse.Thing::def
      IL_0170:  callvirt   instance bool ['Assembly-CSharp']Verse.ThingDef::get_CanHaveFaction()
      IL_0175:  brfalse.s  IL_0192

      IL_0177:  ldarg.0
      IL_0178:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
      IL_017d:  callvirt   instance class ['Assembly-CSharp']RimWorld.Faction ['Assembly-CSharp']Verse.Thing::get_Faction()
      IL_0182:  stloc.s    V_12
      IL_0184:  ldloc.s    V_12
      IL_0186:  brfalse.s  IL_0192

      IL_0188:  ldloc.s    V_11
      IL_018a:  ldloc.s    V_12
      IL_018c:  ldnull
      IL_018d:  callvirt   instance void ['Assembly-CSharp']Verse.Thing::SetFaction(class ['Assembly-CSharp']RimWorld.Faction,
                                                                                    class ['Assembly-CSharp']Verse.Pawn)
      IL_0192:  ldloc.s    V_4
      IL_0194:  ldfld      bool PipeSystem.DestroyOption::spawnMinified
      IL_0199:  brfalse.s  IL_01a4

      IL_019b:  ldloc.s    V_11
      IL_019d:  call       class ['Assembly-CSharp']Verse.Thing ['Assembly-CSharp']RimWorld.MinifyUtility::TryMakeMinified(class ['Assembly-CSharp']Verse.Thing)
      IL_01a2:  stloc.s    V_11
      IL_01a4:  ldloc.s    V_11
      IL_01a6:  ldloc.s    V_5
      IL_01a8:  ldloc.s    V_11
      IL_01aa:  ldfld      class ['Assembly-CSharp']Verse.ThingDef ['Assembly-CSharp']Verse.Thing::def
      IL_01af:  ldfld      int32 ['Assembly-CSharp']Verse.ThingDef::stackLimit
      IL_01b4:  call       int32 [UnityEngine.CoreModule]UnityEngine.Mathf::Min(int32,
                                                                                int32)
      IL_01b9:  stfld      int32 ['Assembly-CSharp']Verse.Thing::stackCount
      IL_01be:  ldloc.s    V_5
      IL_01c0:  ldloc.s    V_11
      IL_01c2:  ldfld      int32 ['Assembly-CSharp']Verse.Thing::stackCount
      IL_01c7:  sub
      IL_01c8:  stloc.s    V_5
      IL_01ca:  ldloc.s    V_11
      IL_01cc:  ldloc.2
      IL_01cd:  ldarg.2
      IL_01ce:  ldloc.s    V_9
      IL_01d0:  ldnull
      IL_01d1:  ldnull
      IL_01d2:  ldloc.s    V_10
      IL_01d4:  stloc.s    V_13
      IL_01d6:  ldloca.s   V_14
      IL_01d8:  initobj    valuetype [mscorlib]System.Nullable`1<valuetype ['Assembly-CSharp']Verse.Rot4>
      IL_01de:  ldloc.s    V_14
      IL_01e0:  ldloc.s    V_13
      IL_01e2:  call       bool ['Assembly-CSharp']Verse.GenPlace::TryPlaceThing(class ['Assembly-CSharp']Verse.Thing,
                                                                                 valuetype ['Assembly-CSharp']Verse.IntVec3,
                                                                                 class ['Assembly-CSharp']Verse.Map,
                                                                                 valuetype ['Assembly-CSharp']Verse.ThingPlaceMode,
                                                                                 class [mscorlib]System.Action`2<class ['Assembly-CSharp']Verse.Thing,int32>,
                                                                                 class [mscorlib]System.Predicate`1<valuetype ['Assembly-CSharp']Verse.IntVec3>,
                                                                                 valuetype [mscorlib]System.Nullable`1<valuetype ['Assembly-CSharp']Verse.Rot4>,
                                                                                 int32)
      IL_01e7:  pop
      IL_01e8:  ldloc.s    V_5
      IL_01ea:  ldc.i4.0
      IL_01eb:  bgt        IL_015f

      IL_01f0:  ldloca.s   V_3
      IL_01f2:  call       instance bool valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class PipeSystem.DestroyOption>::MoveNext()
      IL_01f7:  brtrue     IL_0054

      IL_01fc:  leave.s    IL_020c

    }  // end .try
    finally
    {
      IL_01fe:  ldloca.s   V_3
      IL_0200:  constrained. valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class PipeSystem.DestroyOption>
      IL_0206:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
      IL_020b:  endfinally
    }  // end handler
    IL_020c:  ldarg.0
    IL_020d:  ldarg.1
    IL_020e:  ldarg.2
    IL_020f:  call       instance void ['Assembly-CSharp']Verse.ThingComp::PostDestroy(valuetype ['Assembly-CSharp']Verse.DestroyMode,
                                                                                       class ['Assembly-CSharp']Verse.Map)
    IL_0214:  ret
  } // end of method CompResourceStorage::PostDestroy

  .method public hidebysig instance void 
          AddResource(float32 amount) cil managed
  {
    // Code size       82 (0x52)
    .maxstack  3
    IL_0000:  ldarg.1
    IL_0001:  ldc.r4     0.0
    IL_0006:  bge.un.s   IL_001f

    IL_0008:  ldstr      "[PipeSystem] Cannot add negative resource "
    IL_000d:  ldarga.s   amount
    IL_000f:  call       instance string [mscorlib]System.Single::ToString()
    IL_0014:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0019:  call       void ['Assembly-CSharp']Verse.Log::Error(string)
    IL_001e:  ret

    IL_001f:  ldarg.0
    IL_0020:  ldarg.0
    IL_0021:  call       instance float32 PipeSystem.CompResourceStorage::get_AmountStored()
    IL_0026:  ldarg.1
    IL_0027:  add
    IL_0028:  call       instance void PipeSystem.CompResourceStorage::set_AmountStored(float32)
    IL_002d:  ldarg.0
    IL_002e:  ldfld      float32 PipeSystem.CompResourceStorage::amountStored
    IL_0033:  ldarg.0
    IL_0034:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_0039:  ldfld      float32 PipeSystem.CompProperties_ResourceStorage::storageCapacity
    IL_003e:  ble.un.s   IL_0051

    IL_0040:  ldarg.0
    IL_0041:  ldarg.0
    IL_0042:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_0047:  ldfld      float32 PipeSystem.CompProperties_ResourceStorage::storageCapacity
    IL_004c:  call       instance void PipeSystem.CompResourceStorage::set_AmountStored(float32)
    IL_0051:  ret
  } // end of method CompResourceStorage::AddResource

  .method public hidebysig instance void 
          DrawResource(float32 amount) cil managed
  {
    // Code size       72 (0x48)
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  call       instance float32 PipeSystem.CompResourceStorage::get_AmountStored()
    IL_0007:  ldarg.1
    IL_0008:  sub
    IL_0009:  call       instance void PipeSystem.CompResourceStorage::set_AmountStored(float32)
    IL_000e:  ldarg.0
    IL_000f:  ldfld      float32 PipeSystem.CompResourceStorage::amountStored
    IL_0014:  ldc.r4     0.0
    IL_0019:  bge.un.s   IL_0047

    IL_001b:  ldstr      "[PipeSystem] Drawing resource we don't have from "
    IL_0020:  ldarg.0
    IL_0021:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0026:  dup
    IL_0027:  brtrue.s   IL_002d

    IL_0029:  pop
    IL_002a:  ldnull
    IL_002b:  br.s       IL_0032

    IL_002d:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0032:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0037:  call       void ['Assembly-CSharp']Verse.Log::Error(string)
    IL_003c:  ldarg.0
    IL_003d:  ldc.r4     0.0
    IL_0042:  call       instance void PipeSystem.CompResourceStorage::set_AmountStored(float32)
    IL_0047:  ret
  } // end of method CompResourceStorage::DrawResource

  .method public hidebysig instance void 
          Empty() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.r4     0.0
    IL_0006:  call       instance void PipeSystem.CompResourceStorage::set_AmountStored(float32)
    IL_000b:  ret
  } // end of method CompResourceStorage::Empty

  .method public hidebysig virtual instance void 
          ReceiveCompSignal(string signal) cil managed
  {
    // Code size       25 (0x19)
    .maxstack  8
    IL_0000:  ldarg.1
    IL_0001:  ldstr      "Breakdown"
    IL_0006:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_000b:  brfalse.s  IL_0018

    IL_000d:  ldarg.0
    IL_000e:  ldc.r4     0.0
    IL_0013:  call       instance void PipeSystem.CompResourceStorage::set_AmountStored(float32)
    IL_0018:  ret
  } // end of method CompResourceStorage::ReceiveCompSignal

  .method public hidebysig virtual instance string 
          CompInspectStringExtra() cil managed
  {
    // Code size       341 (0x155)
    .maxstack  8
    .locals init (class [mscorlib]System.Text.StringBuilder V_0)
    IL_0000:  newobj     instance void [mscorlib]System.Text.StringBuilder::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldarg.0
    IL_0007:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_000c:  ldfld      bool PipeSystem.CompProperties_ResourceStorage::addStorageInfo
    IL_0011:  brfalse.s  IL_007b

    IL_0013:  ldloc.0
    IL_0014:  ldstr      "{0} {1:##0} / {2:F0} {3}"
    IL_0019:  ldc.i4.4
    IL_001a:  newarr     [mscorlib]System.Object
    IL_001f:  dup
    IL_0020:  ldc.i4.0
    IL_0021:  ldstr      "PipeSystem_ResourceStored"
    IL_0026:  ldarg.0
    IL_0027:  call       instance class PipeSystem.Resource PipeSystem.CompResource::get_Resource()
    IL_002c:  ldfld      string PipeSystem.Resource::name
    IL_0031:  call       valuetype ['Assembly-CSharp']Verse.NamedArgument ['Assembly-CSharp']Verse.NamedArgument::op_Implicit(string)
    IL_0036:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.TranslatorFormattedStringExtensions::Translate(string,
                                                                                                                                                 valuetype ['Assembly-CSharp']Verse.NamedArgument)
    IL_003b:  box        ['Assembly-CSharp']Verse.TaggedString
    IL_0040:  stelem.ref
    IL_0041:  dup
    IL_0042:  ldc.i4.1
    IL_0043:  ldarg.0
    IL_0044:  ldfld      float32 PipeSystem.CompResourceStorage::amountStored
    IL_0049:  box        [mscorlib]System.Single
    IL_004e:  stelem.ref
    IL_004f:  dup
    IL_0050:  ldc.i4.2
    IL_0051:  ldarg.0
    IL_0052:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_0057:  ldfld      float32 PipeSystem.CompProperties_ResourceStorage::storageCapacity
    IL_005c:  box        [mscorlib]System.Single
    IL_0061:  stelem.ref
    IL_0062:  dup
    IL_0063:  ldc.i4.3
    IL_0064:  ldarg.0
    IL_0065:  call       instance class PipeSystem.Resource PipeSystem.CompResource::get_Resource()
    IL_006a:  ldfld      string PipeSystem.Resource::unit
    IL_006f:  stelem.ref
    IL_0070:  call       string [mscorlib]System.String::Format(string,
                                                                object[])
    IL_0075:  call       class [mscorlib]System.Text.StringBuilder ['Assembly-CSharp']Verse.GenText::AppendInNewLine(class [mscorlib]System.Text.StringBuilder,
                                                                                                                     string)
    IL_007a:  pop
    IL_007b:  ldarg.0
    IL_007c:  ldfld      bool PipeSystem.CompResourceStorage::markedForTransfer
    IL_0081:  brfalse.s  IL_0099

    IL_0083:  ldloc.0
    IL_0084:  ldstr      "PipeSystem_MarkedToTransferContent"
    IL_0089:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.Translator::Translate(string)
    IL_008e:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
    IL_0093:  call       class [mscorlib]System.Text.StringBuilder ['Assembly-CSharp']Verse.GenText::AppendInNewLine(class [mscorlib]System.Text.StringBuilder,
                                                                                                                     string)
    IL_0098:  pop
    IL_0099:  ldarg.0
    IL_009a:  call       instance bool PipeSystem.CompResourceStorage::get_ContentCanRot()
    IL_009f:  brfalse    IL_013c

    IL_00a4:  ldarg.0
    IL_00a5:  ldfld      class ['Assembly-CSharp']RimWorld.CompPowerTrader PipeSystem.CompResource::powerComp
    IL_00aa:  callvirt   instance bool ['Assembly-CSharp']RimWorld.CompPowerTrader::get_PowerOn()
    IL_00af:  brtrue     IL_013c

    IL_00b4:  ldarg.0
    IL_00b5:  ldfld      float32 PipeSystem.CompResourceStorage::amountStored
    IL_00ba:  ldc.r4     0.0
    IL_00bf:  ble.un.s   IL_013c

    IL_00c1:  ldarg.0
    IL_00c2:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_00c7:  ldfld      bool PipeSystem.CompProperties_ResourceStorage::preventRotInNegativeTemp
    IL_00cc:  brfalse.s  IL_00fd

    IL_00ce:  ldarg.0
    IL_00cf:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_00d4:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_MapHeld()
    IL_00d9:  brfalse.s  IL_00fd

    IL_00db:  ldarg.0
    IL_00dc:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_00e1:  callvirt   instance valuetype ['Assembly-CSharp']Verse.IntVec3 ['Assembly-CSharp']Verse.Thing::get_Position()
    IL_00e6:  ldarg.0
    IL_00e7:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_00ec:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_MapHeld()
    IL_00f1:  call       float32 ['Assembly-CSharp']Verse.GridsUtility::GetTemperature(valuetype ['Assembly-CSharp']Verse.IntVec3,
                                                                                       class ['Assembly-CSharp']Verse.Map)
    IL_00f6:  ldc.r4     0.0
    IL_00fb:  blt.un.s   IL_013c

    IL_00fd:  ldloc.0
    IL_00fe:  ldstr      "PipeSystem_ContentWillRot"
    IL_0103:  ldc.r4     60000.
    IL_0108:  ldarg.0
    IL_0109:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_010e:  ldfld      float32 PipeSystem.CompProperties_ResourceStorage::daysToRotStart
    IL_0113:  mul
    IL_0114:  ldarg.0
    IL_0115:  ldfld      int32 PipeSystem.CompResourceStorage::ticksWithoutPower
    IL_011a:  conv.r4
    IL_011b:  sub
    IL_011c:  conv.i4
    IL_011d:  ldc.i4.1
    IL_011e:  ldc.i4.0
    IL_011f:  ldc.i4.1
    IL_0120:  ldc.i4.1
    IL_0121:  ldc.i4.0
    IL_0122:  call       string ['Assembly-CSharp']RimWorld.GenDate::ToStringTicksToPeriod(int32,
                                                                                           bool,
                                                                                           bool,
                                                                                           bool,
                                                                                           bool,
                                                                                           bool)
    IL_0127:  call       valuetype ['Assembly-CSharp']Verse.NamedArgument ['Assembly-CSharp']Verse.NamedArgument::op_Implicit(string)
    IL_012c:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.TranslatorFormattedStringExtensions::Translate(string,
                                                                                                                                                 valuetype ['Assembly-CSharp']Verse.NamedArgument)
    IL_0131:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
    IL_0136:  call       class [mscorlib]System.Text.StringBuilder ['Assembly-CSharp']Verse.GenText::AppendInNewLine(class [mscorlib]System.Text.StringBuilder,
                                                                                                                     string)
    IL_013b:  pop
    IL_013c:  ldloc.0
    IL_013d:  ldarg.0
    IL_013e:  call       instance string PipeSystem.CompResource::CompInspectStringExtra()
    IL_0143:  call       class [mscorlib]System.Text.StringBuilder ['Assembly-CSharp']Verse.GenText::AppendInNewLine(class [mscorlib]System.Text.StringBuilder,
                                                                                                                     string)
    IL_0148:  pop
    IL_0149:  ldloc.0
    IL_014a:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_014f:  call       string ['Assembly-CSharp']Verse.GenText::TrimEndNewlines(string)
    IL_0154:  ret
  } // end of method CompResourceStorage::CompInspectStringExtra

  .method public hidebysig virtual instance class [mscorlib]System.Collections.Generic.IEnumerable`1<class ['Assembly-CSharp']Verse.Gizmo> 
          CompGetGizmosExtra() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IteratorStateMachineAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 38 50 69 70 65 53 79 73 74 65 6D 2E 43 6F   // ..8PipeSystem.Co
                                                                                                                                          6D 70 52 65 73 6F 75 72 63 65 53 74 6F 72 61 67   // mpResourceStorag
                                                                                                                                          65 2B 3C 43 6F 6D 70 47 65 74 47 69 7A 6D 6F 73   // e+<CompGetGizmos
                                                                                                                                          45 78 74 72 61 3E 64 5F 5F 34 32 00 00 )          // Extra>d__42..
    // Code size       15 (0xf)
    .maxstack  8
    IL_0000:  ldc.i4.s   -2
    IL_0002:  newobj     instance void PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::.ctor(int32)
    IL_0007:  dup
    IL_0008:  ldarg.0
    IL_0009:  stfld      class PipeSystem.CompResourceStorage PipeSystem.CompResourceStorage/'<CompGetGizmosExtra>d__42'::'<>4__this'
    IL_000e:  ret
  } // end of method CompResourceStorage::CompGetGizmosExtra

  .method private hidebysig instance void 
          UpdateDesignation(class ['Assembly-CSharp']Verse.Thing t) cil managed
  {
    // Code size       67 (0x43)
    .maxstack  4
    .locals init (class ['Assembly-CSharp']Verse.Designation V_0)
    IL_0000:  ldarg.1
    IL_0001:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_0006:  ldfld      class ['Assembly-CSharp']Verse.DesignationManager ['Assembly-CSharp']Verse.Map::designationManager
    IL_000b:  ldarg.1
    IL_000c:  ldsfld     class ['Assembly-CSharp']Verse.DesignationDef PipeSystem.PSDefOf::PS_Drain
    IL_0011:  callvirt   instance class ['Assembly-CSharp']Verse.Designation ['Assembly-CSharp']Verse.DesignationManager::DesignationOn(class ['Assembly-CSharp']Verse.Thing,
                                                                                                                                        class ['Assembly-CSharp']Verse.DesignationDef)
    IL_0016:  stloc.0
    IL_0017:  ldloc.0
    IL_0018:  brtrue.s   IL_003c

    IL_001a:  ldarg.1
    IL_001b:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_0020:  ldfld      class ['Assembly-CSharp']Verse.DesignationManager ['Assembly-CSharp']Verse.Map::designationManager
    IL_0025:  ldarg.1
    IL_0026:  call       valuetype ['Assembly-CSharp']Verse.LocalTargetInfo ['Assembly-CSharp']Verse.LocalTargetInfo::op_Implicit(class ['Assembly-CSharp']Verse.Thing)
    IL_002b:  ldsfld     class ['Assembly-CSharp']Verse.DesignationDef PipeSystem.PSDefOf::PS_Drain
    IL_0030:  ldnull
    IL_0031:  newobj     instance void ['Assembly-CSharp']Verse.Designation::.ctor(valuetype ['Assembly-CSharp']Verse.LocalTargetInfo,
                                                                                   class ['Assembly-CSharp']Verse.DesignationDef,
                                                                                   class ['Assembly-CSharp']RimWorld.ColorDef)
    IL_0036:  callvirt   instance void ['Assembly-CSharp']Verse.DesignationManager::AddDesignation(class ['Assembly-CSharp']Verse.Designation)
    IL_003b:  ret

    IL_003c:  ldloc.0
    IL_003d:  callvirt   instance void ['Assembly-CSharp']Verse.Designation::Delete()
    IL_0042:  ret
  } // end of method CompResourceStorage::UpdateDesignation

  .method private hidebysig instance valuetype ['Assembly-CSharp']Verse.NamedArgument[] 
          ExtractResourceArguments() cil managed
  {
    // Code size       283 (0x11b)
    .maxstack  5
    .locals init (class PipeSystem.ExtractOptions V_0,
             float32 V_1,
             int32 V_2)
    IL_0000:  ldarg.0
    IL_0001:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_0006:  ldfld      class PipeSystem.ExtractOptions PipeSystem.CompProperties_ResourceStorage::extractOptions
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  brtrue.s   IL_0015

    IL_000f:  call       !!0[] [mscorlib]System.Array::Empty<valuetype ['Assembly-CSharp']Verse.NamedArgument>()
    IL_0014:  ret

    IL_0015:  ldarg.0
    IL_0016:  call       instance valuetype [mscorlib]System.ValueTuple`2<float32,int32> PipeSystem.CompResourceStorage::CurrentManualExtractAmount()
    IL_001b:  dup
    IL_001c:  ldfld      !0 valuetype [mscorlib]System.ValueTuple`2<float32,int32>::Item1
    IL_0021:  stloc.1
    IL_0022:  ldfld      !1 valuetype [mscorlib]System.ValueTuple`2<float32,int32>::Item2
    IL_0027:  stloc.2
    IL_0028:  ldc.i4.8
    IL_0029:  newarr     ['Assembly-CSharp']Verse.NamedArgument
    IL_002e:  dup
    IL_002f:  ldc.i4.0
    IL_0030:  ldloc.0
    IL_0031:  ldfld      bool PipeSystem.ExtractOptions::extractExactAmount
    IL_0036:  brtrue.s   IL_0040

    IL_0038:  ldloc.0
    IL_0039:  ldfld      float32 PipeSystem.ExtractOptions::ratio
    IL_003e:  br.s       IL_0046

    IL_0040:  ldarg.0
    IL_0041:  ldfld      float32 PipeSystem.CompResourceStorage::extractResourceAmount
    IL_0046:  box        [mscorlib]System.Single
    IL_004b:  ldstr      "RESOURCEMIN"
    IL_0050:  call       valuetype ['Assembly-CSharp']Verse.NamedArgument ['Assembly-CSharp']Verse.NamedArgumentUtility::Named(object,
                                                                                                                               string)
    IL_0055:  stelem     ['Assembly-CSharp']Verse.NamedArgument
    IL_005a:  dup
    IL_005b:  ldc.i4.1
    IL_005c:  ldarg.0
    IL_005d:  ldfld      float32 PipeSystem.CompResourceStorage::amountStored
    IL_0062:  box        [mscorlib]System.Single
    IL_0067:  ldstr      "RESOURCECOUNT"
    IL_006c:  call       valuetype ['Assembly-CSharp']Verse.NamedArgument ['Assembly-CSharp']Verse.NamedArgumentUtility::Named(object,
                                                                                                                               string)
    IL_0071:  stelem     ['Assembly-CSharp']Verse.NamedArgument
    IL_0076:  dup
    IL_0077:  ldc.i4.2
    IL_0078:  ldloc.1
    IL_0079:  box        [mscorlib]System.Single
    IL_007e:  ldstr      "RESOURCEEXTRACTCOUNT"
    IL_0083:  call       valuetype ['Assembly-CSharp']Verse.NamedArgument ['Assembly-CSharp']Verse.NamedArgumentUtility::Named(object,
                                                                                                                               string)
    IL_0088:  stelem     ['Assembly-CSharp']Verse.NamedArgument
    IL_008d:  dup
    IL_008e:  ldc.i4.3
    IL_008f:  ldarg.0
    IL_0090:  ldfld      float32 PipeSystem.CompResourceStorage::extractResourceAmount
    IL_0095:  box        [mscorlib]System.Single
    IL_009a:  ldstr      "RESOURCEMAX"
    IL_009f:  call       valuetype ['Assembly-CSharp']Verse.NamedArgument ['Assembly-CSharp']Verse.NamedArgumentUtility::Named(object,
                                                                                                                               string)
    IL_00a4:  stelem     ['Assembly-CSharp']Verse.NamedArgument
    IL_00a9:  dup
    IL_00aa:  ldc.i4.4
    IL_00ab:  ldloc.0
    IL_00ac:  ldfld      bool PipeSystem.ExtractOptions::extractExactAmount
    IL_00b1:  brtrue.s   IL_00b6

    IL_00b3:  ldc.i4.1
    IL_00b4:  br.s       IL_00bc

    IL_00b6:  ldloc.0
    IL_00b7:  ldfld      int32 PipeSystem.ExtractOptions::extractAmount
    IL_00bc:  box        [mscorlib]System.Int32
    IL_00c1:  ldstr      "THINGMIN"
    IL_00c6:  call       valuetype ['Assembly-CSharp']Verse.NamedArgument ['Assembly-CSharp']Verse.NamedArgumentUtility::Named(object,
                                                                                                                               string)
    IL_00cb:  stelem     ['Assembly-CSharp']Verse.NamedArgument
    IL_00d0:  dup
    IL_00d1:  ldc.i4.5
    IL_00d2:  ldloc.2
    IL_00d3:  box        [mscorlib]System.Int32
    IL_00d8:  ldstr      "THINGCOUNT"
    IL_00dd:  call       valuetype ['Assembly-CSharp']Verse.NamedArgument ['Assembly-CSharp']Verse.NamedArgumentUtility::Named(object,
                                                                                                                               string)
    IL_00e2:  stelem     ['Assembly-CSharp']Verse.NamedArgument
    IL_00e7:  dup
    IL_00e8:  ldc.i4.6
    IL_00e9:  ldloc.2
    IL_00ea:  box        [mscorlib]System.Int32
    IL_00ef:  ldstr      "THINGEXTRACTCOUNT"
    IL_00f4:  call       valuetype ['Assembly-CSharp']Verse.NamedArgument ['Assembly-CSharp']Verse.NamedArgumentUtility::Named(object,
                                                                                                                               string)
    IL_00f9:  stelem     ['Assembly-CSharp']Verse.NamedArgument
    IL_00fe:  dup
    IL_00ff:  ldc.i4.7
    IL_0100:  ldloc.0
    IL_0101:  ldfld      int32 PipeSystem.ExtractOptions::extractAmount
    IL_0106:  box        [mscorlib]System.Int32
    IL_010b:  ldstr      "THINGMAX"
    IL_0110:  call       valuetype ['Assembly-CSharp']Verse.NamedArgument ['Assembly-CSharp']Verse.NamedArgumentUtility::Named(object,
                                                                                                                               string)
    IL_0115:  stelem     ['Assembly-CSharp']Verse.NamedArgument
    IL_011a:  ret
  } // end of method CompResourceStorage::ExtractResourceArguments

  .method public hidebysig instance valuetype [mscorlib]System.ValueTuple`2<float32,int32> 
          CurrentManualExtractAmount() cil managed
  {
    .param [0]
    .custom instance void [mscorlib]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[]) = ( 01 00 02 00 00 00 0E 72 65 73 6F 75 72 63 65 41   // .......resourceA
                                                                                                                    6D 6F 75 6E 74 0A 69 74 65 6D 41 6D 6F 75 6E 74   // mount.itemAmount
                                                                                                                    00 00 ) 
    // Code size       125 (0x7d)
    .maxstack  2
    .locals init (class PipeSystem.ExtractOptions V_0,
             int32 V_1)
    IL_0000:  ldarg.0
    IL_0001:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_0006:  ldfld      class PipeSystem.ExtractOptions PipeSystem.CompProperties_ResourceStorage::extractOptions
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  brtrue.s   IL_001b

    IL_000f:  ldc.r4     0.0
    IL_0014:  ldc.i4.0
    IL_0015:  newobj     instance void valuetype [mscorlib]System.ValueTuple`2<float32,int32>::.ctor(!0,
                                                                                                     !1)
    IL_001a:  ret

    IL_001b:  ldloc.0
    IL_001c:  ldfld      bool PipeSystem.ExtractOptions::extractExactAmount
    IL_0021:  brfalse.s  IL_004f

    IL_0023:  ldarg.0
    IL_0024:  call       instance float32 PipeSystem.CompResourceStorage::get_AmountStored()
    IL_0029:  ldarg.0
    IL_002a:  ldfld      float32 PipeSystem.CompResourceStorage::extractResourceAmount
    IL_002f:  bge.un.s   IL_003d

    IL_0031:  ldc.r4     0.0
    IL_0036:  ldc.i4.0
    IL_0037:  newobj     instance void valuetype [mscorlib]System.ValueTuple`2<float32,int32>::.ctor(!0,
                                                                                                     !1)
    IL_003c:  ret

    IL_003d:  ldarg.0
    IL_003e:  ldfld      float32 PipeSystem.CompResourceStorage::extractResourceAmount
    IL_0043:  ldloc.0
    IL_0044:  ldfld      int32 PipeSystem.ExtractOptions::extractAmount
    IL_0049:  newobj     instance void valuetype [mscorlib]System.ValueTuple`2<float32,int32>::.ctor(!0,
                                                                                                     !1)
    IL_004e:  ret

    IL_004f:  ldarg.0
    IL_0050:  call       instance float32 PipeSystem.CompResourceStorage::get_AmountStored()
    IL_0055:  ldarg.0
    IL_0056:  ldfld      float32 PipeSystem.CompResourceStorage::extractResourceAmount
    IL_005b:  call       float32 [UnityEngine.CoreModule]UnityEngine.Mathf::Min(float32,
                                                                                float32)
    IL_0060:  ldloc.0
    IL_0061:  ldfld      float32 PipeSystem.ExtractOptions::ratio
    IL_0066:  div
    IL_0067:  call       int32 [UnityEngine.CoreModule]UnityEngine.Mathf::FloorToInt(float32)
    IL_006c:  stloc.1
    IL_006d:  ldloc.1
    IL_006e:  conv.r4
    IL_006f:  ldloc.0
    IL_0070:  ldfld      float32 PipeSystem.ExtractOptions::ratio
    IL_0075:  mul
    IL_0076:  ldloc.1
    IL_0077:  newobj     instance void valuetype [mscorlib]System.ValueTuple`2<float32,int32>::.ctor(!0,
                                                                                                     !1)
    IL_007c:  ret
  } // end of method CompResourceStorage::CurrentManualExtractAmount

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void PipeSystem.CompResource::.ctor()
    IL_0006:  ret
  } // end of method CompResourceStorage::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       17 (0x11)
    .maxstack  8
    IL_0000:  ldstr      "UI/TransferStorageContent"
    IL_0005:  ldc.i4.1
    IL_0006:  call       !0 class ['Assembly-CSharp']Verse.ContentFinder`1<class [UnityEngine.CoreModule]UnityEngine.Texture2D>::Get(string,
                                                                                                                                     bool)
    IL_000b:  stsfld     class [UnityEngine.CoreModule]UnityEngine.Texture2D PipeSystem.CompResourceStorage::transferIcon
    IL_0010:  ret
  } // end of method CompResourceStorage::.cctor

  .method private hidebysig instance void 
          '<PostSpawnSetup>b__32_0'() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       28 (0x1c)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      bool PipeSystem.CompResourceStorage::markedForExtract
    IL_0007:  ldc.i4.0
    IL_0008:  ceq
    IL_000a:  stfld      bool PipeSystem.CompResourceStorage::markedForExtract
    IL_000f:  ldarg.0
    IL_0010:  ldarg.0
    IL_0011:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0016:  call       instance void PipeSystem.CompResourceStorage::UpdateDesignation(class ['Assembly-CSharp']Verse.Thing)
    IL_001b:  ret
  } // end of method CompResourceStorage::'<PostSpawnSetup>b__32_0'

  .method private hidebysig instance bool 
          '<PostSpawnSetup>b__32_1'() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool PipeSystem.CompResourceStorage::markedForRefill
    IL_0006:  ret
  } // end of method CompResourceStorage::'<PostSpawnSetup>b__32_1'

  .method private hidebysig instance void 
          '<PostSpawnSetup>b__32_2'() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       33 (0x21)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      bool PipeSystem.CompResourceStorage::markedForRefill
    IL_0007:  ldc.i4.0
    IL_0008:  ceq
    IL_000a:  stfld      bool PipeSystem.CompResourceStorage::markedForRefill
    IL_000f:  ldarg.0
    IL_0010:  call       instance class PipeSystem.PipeNetManager PipeSystem.CompResource::get_PipeNetManager()
    IL_0015:  ldarg.0
    IL_0016:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_001b:  callvirt   instance void PipeSystem.PipeNetManager::UpdateRefillableWith(class ['Assembly-CSharp']Verse.Thing)
    IL_0020:  ret
  } // end of method CompResourceStorage::'<PostSpawnSetup>b__32_2'

  .method private hidebysig instance void 
          '<PostSpawnSetup>b__32_3'() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       129 (0x81)
    .maxstack  4
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      bool PipeSystem.CompResourceStorage::markedForTransfer
    IL_0007:  ldc.i4.0
    IL_0008:  ceq
    IL_000a:  stfld      bool PipeSystem.CompResourceStorage::markedForTransfer
    IL_000f:  ldarg.0
    IL_0010:  ldfld      bool PipeSystem.CompResourceStorage::markedForTransfer
    IL_0015:  brfalse.s  IL_003c

    IL_0017:  ldarg.0
    IL_0018:  callvirt   instance class PipeSystem.PipeNet PipeSystem.CompResource::get_PipeNet()
    IL_001d:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::markedForTransfer
    IL_0022:  ldarg.0
    IL_0023:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::Add(!0)
    IL_0028:  ldarg.0
    IL_0029:  callvirt   instance class PipeSystem.PipeNet PipeSystem.CompResource::get_PipeNet()
    IL_002e:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::storages
    IL_0033:  ldarg.0
    IL_0034:  callvirt   instance bool class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::Remove(!0)
    IL_0039:  pop
    IL_003a:  br.s       IL_005f

    IL_003c:  ldarg.0
    IL_003d:  callvirt   instance class PipeSystem.PipeNet PipeSystem.CompResource::get_PipeNet()
    IL_0042:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::markedForTransfer
    IL_0047:  ldarg.0
    IL_0048:  callvirt   instance bool class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::Remove(!0)
    IL_004d:  pop
    IL_004e:  ldarg.0
    IL_004f:  callvirt   instance class PipeSystem.PipeNet PipeSystem.CompResource::get_PipeNet()
    IL_0054:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage> PipeSystem.PipeNet::storages
    IL_0059:  ldarg.0
    IL_005a:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.CompResourceStorage>::Add(!0)
    IL_005f:  ldarg.0
    IL_0060:  ldfld      class PipeSystem.PipeNetOverlayDrawer PipeSystem.CompResourceStorage::pipeNetOverlayDrawer
    IL_0065:  dup
    IL_0066:  brtrue.s   IL_006a

    IL_0068:  pop
    IL_0069:  ret

    IL_006a:  ldarg.0
    IL_006b:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0070:  ldsfld     class [UnityEngine.CoreModule]UnityEngine.Material PipeSystem.MaterialCreator::transferMat
    IL_0075:  ldarg.0
    IL_0076:  ldfld      bool PipeSystem.CompResourceStorage::markedForTransfer
    IL_007b:  call       instance void PipeSystem.PipeNetOverlayDrawer::ToggleStatic(class ['Assembly-CSharp']Verse.Thing,
                                                                                     class [UnityEngine.CoreModule]UnityEngine.Material,
                                                                                     bool)
    IL_0080:  ret
  } // end of method CompResourceStorage::'<PostSpawnSetup>b__32_3'

  .method private hidebysig instance void 
          '<CompGetGizmosExtra>b__42_0'() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_0007:  ldfld      float32 PipeSystem.CompProperties_ResourceStorage::storageCapacity
    IL_000c:  call       instance void PipeSystem.CompResourceStorage::set_AmountStored(float32)
    IL_0011:  ret
  } // end of method CompResourceStorage::'<CompGetGizmosExtra>b__42_0'

  .method private hidebysig instance void 
          '<CompGetGizmosExtra>b__42_1'() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       62 (0x3e)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float32 PipeSystem.CompResourceStorage::amountStored
    IL_0006:  ldc.r4     5.
    IL_000b:  add
    IL_000c:  ldarg.0
    IL_000d:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_0012:  ldfld      float32 PipeSystem.CompProperties_ResourceStorage::storageCapacity
    IL_0017:  ble.un.s   IL_002b

    IL_0019:  ldarg.0
    IL_001a:  ldarg.0
    IL_001b:  call       instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
    IL_0020:  ldfld      float32 PipeSystem.CompProperties_ResourceStorage::storageCapacity
    IL_0025:  call       instance void PipeSystem.CompResourceStorage::set_AmountStored(float32)
    IL_002a:  ret

    IL_002b:  ldarg.0
    IL_002c:  ldarg.0
    IL_002d:  call       instance float32 PipeSystem.CompResourceStorage::get_AmountStored()
    IL_0032:  ldc.r4     5.
    IL_0037:  add
    IL_0038:  call       instance void PipeSystem.CompResourceStorage::set_AmountStored(float32)
    IL_003d:  ret
  } // end of method CompResourceStorage::'<CompGetGizmosExtra>b__42_1'

  .method private hidebysig instance void 
          '<CompGetGizmosExtra>b__42_2'() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.r4     0.0
    IL_0006:  call       instance void PipeSystem.CompResourceStorage::set_AmountStored(float32)
    IL_000b:  ret
  } // end of method CompResourceStorage::'<CompGetGizmosExtra>b__42_2'

  .method private hidebysig instance class [mscorlib]System.Collections.Generic.IEnumerable`1<class ['Assembly-CSharp']Verse.Gizmo> 
          '<>n__0'() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance class [mscorlib]System.Collections.Generic.IEnumerable`1<class ['Assembly-CSharp']Verse.Gizmo> ['Assembly-CSharp']Verse.ThingComp::CompGetGizmosExtra()
    IL_0006:  ret
  } // end of method CompResourceStorage::'<>n__0'

  .property instance class PipeSystem.CompProperties_ResourceStorage
          Props()
  {
    .get instance class PipeSystem.CompProperties_ResourceStorage PipeSystem.CompResourceStorage::get_Props()
  } // end of property CompResourceStorage::Props
  .property instance float32 AmountStored()
  {
    .get instance float32 PipeSystem.CompResourceStorage::get_AmountStored()
    .set instance void PipeSystem.CompResourceStorage::set_AmountStored(float32)
  } // end of property CompResourceStorage::AmountStored
  .property instance float32 AmountStoredPct()
  {
    .get instance float32 PipeSystem.CompResourceStorage::get_AmountStoredPct()
  } // end of property CompResourceStorage::AmountStoredPct
  .property instance float32 AmountCanAccept()
  {
    .get instance float32 PipeSystem.CompResourceStorage::get_AmountCanAccept()
  } // end of property CompResourceStorage::AmountCanAccept
  .property instance bool ContentCanRot()
  {
    .get instance bool PipeSystem.CompResourceStorage::get_ContentCanRot()
    .set instance void PipeSystem.CompResourceStorage::set_ContentCanRot(bool)
  } // end of property CompResourceStorage::ContentCanRot
  .property instance bool CanExtract()
  {
    .get instance bool PipeSystem.CompResourceStorage::get_CanExtract()
  } // end of property CompResourceStorage::CanExtract
} // end of class PipeSystem.CompResourceStorage

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file F:\Repositories\rimworld_mods\.tmp\ps_stor_class.res
