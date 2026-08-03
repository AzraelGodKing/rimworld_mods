
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.8.3928.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.


// warning : THIS IS A PARTIAL DISASSEMBLY, NOT SUITABLE FOR RE-ASSEMBLING



.class public auto ansi beforefieldinit DubsBadHygiene.CompWaterStorage
       extends ['Assembly-CSharp']Verse.ThingComp
{
  .custom instance void ['Assembly-CSharp']Verse.StaticConstructorOnStartup::.ctor() = ( 01 00 00 00 ) 
  .class auto ansi sealed nested private beforefieldinit '<CompGetGizmosExtra>d__37'
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
    .field public class DubsBadHygiene.CompWaterStorage '<>4__this'
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
      IL_0008:  stfld      int32 DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>1__state'
      IL_000d:  ldarg.0
      IL_000e:  call       int32 [mscorlib]System.Environment::get_CurrentManagedThreadId()
      IL_0013:  stfld      int32 DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>l__initialThreadId'
      IL_0018:  ret
    } // end of method '<CompGetGizmosExtra>d__37'::.ctor

    .method private hidebysig newslot virtual final 
            instance void  System.IDisposable.Dispose() cil managed
    {
      .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
      .override [mscorlib]System.IDisposable::Dispose
      // Code size       1 (0x1)
      .maxstack  8
      IL_0000:  ret
    } // end of method '<CompGetGizmosExtra>d__37'::System.IDisposable.Dispose

    .method private hidebysig newslot virtual final 
            instance bool  MoveNext() cil managed
    {
      .override [mscorlib]System.Collections.IEnumerator::MoveNext
      // Code size       396 (0x18c)
      .maxstack  5
      .locals init (int32 V_0,
               class DubsBadHygiene.CompWaterStorage V_1)
      IL_0000:  ldarg.0
      IL_0001:  ldfld      int32 DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>1__state'
      IL_0006:  stloc.0
      IL_0007:  ldarg.0
      IL_0008:  ldfld      class DubsBadHygiene.CompWaterStorage DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>4__this'
      IL_000d:  stloc.1
      IL_000e:  ldloc.0
      IL_000f:  switch     ( 
                            IL_002a,
                            IL_0092,
                            IL_010c,
                            IL_014b,
                            IL_0183)
      IL_0028:  ldc.i4.0
      IL_0029:  ret

      IL_002a:  ldarg.0
      IL_002b:  ldc.i4.m1
      IL_002c:  stfld      int32 DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>1__state'
      IL_0031:  ldarg.0
      IL_0032:  newobj     instance void ['Assembly-CSharp']Verse.Command_Action::.ctor()
      IL_0037:  dup
      IL_0038:  ldloc.1
      IL_0039:  ldftn      instance void DubsBadHygiene.CompWaterStorage::draindes()
      IL_003f:  newobj     instance void [mscorlib]System.Action::.ctor(object,
                                                                        native int)
      IL_0044:  stfld      class [mscorlib]System.Action ['Assembly-CSharp']Verse.Command_Action::action
      IL_0049:  dup
      IL_004a:  ldstr      "DrainTank"
      IL_004f:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.Translator::Translate(string)
      IL_0054:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
      IL_0059:  stfld      string ['Assembly-CSharp']Verse.Command::defaultLabel
      IL_005e:  dup
      IL_005f:  ldstr      "DrainTankDesc"
      IL_0064:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.Translator::Translate(string)
      IL_0069:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
      IL_006e:  stfld      string ['Assembly-CSharp']Verse.Command::defaultDesc
      IL_0073:  dup
      IL_0074:  ldstr      "DBH/UI/drainOut"
      IL_0079:  ldc.i4.1
      IL_007a:  call       !0 class ['Assembly-CSharp']Verse.ContentFinder`1<class [UnityEngine.CoreModule]UnityEngine.Texture2D>::Get(string,
                                                                                                                                       bool)
      IL_007f:  stfld      class [UnityEngine.CoreModule]UnityEngine.Texture ['Assembly-CSharp']Verse.Command::icon
      IL_0084:  stfld      class ['Assembly-CSharp']Verse.Gizmo DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>2__current'
      IL_0089:  ldarg.0
      IL_008a:  ldc.i4.1
      IL_008b:  stfld      int32 DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>1__state'
      IL_0090:  ldc.i4.1
      IL_0091:  ret

      IL_0092:  ldarg.0
      IL_0093:  ldc.i4.m1
      IL_0094:  stfld      int32 DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>1__state'
      IL_0099:  ldarg.0
      IL_009a:  newobj     instance void ['Assembly-CSharp']Verse.Command_Toggle::.ctor()
      IL_009f:  dup
      IL_00a0:  ldloc.1
      IL_00a1:  ldftn      instance bool DubsBadHygiene.CompWaterStorage::IsDraining()
      IL_00a7:  newobj     instance void class [mscorlib]System.Func`1<bool>::.ctor(object,
                                                                                    native int)
      IL_00ac:  stfld      class [mscorlib]System.Func`1<bool> ['Assembly-CSharp']Verse.Command_Toggle::isActive
      IL_00b1:  dup
      IL_00b2:  ldloc.1
      IL_00b3:  ldftn      instance void DubsBadHygiene.CompWaterStorage::DrainToggle()
      IL_00b9:  newobj     instance void [mscorlib]System.Action::.ctor(object,
                                                                        native int)
      IL_00be:  stfld      class [mscorlib]System.Action ['Assembly-CSharp']Verse.Command_Toggle::toggleAction
      IL_00c3:  dup
      IL_00c4:  ldstr      "TransferStorageTank"
      IL_00c9:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.Translator::Translate(string)
      IL_00ce:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
      IL_00d3:  stfld      string ['Assembly-CSharp']Verse.Command::defaultLabel
      IL_00d8:  dup
      IL_00d9:  ldstr      "TransferStorageTankDesc"
      IL_00de:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.Translator::Translate(string)
      IL_00e3:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
      IL_00e8:  stfld      string ['Assembly-CSharp']Verse.Command::defaultDesc
      IL_00ed:  dup
      IL_00ee:  ldstr      "DBH/UI/Valve"
      IL_00f3:  ldc.i4.1
      IL_00f4:  call       !0 class ['Assembly-CSharp']Verse.ContentFinder`1<class [UnityEngine.CoreModule]UnityEngine.Texture2D>::Get(string,
                                                                                                                                       bool)
      IL_00f9:  stfld      class [UnityEngine.CoreModule]UnityEngine.Texture ['Assembly-CSharp']Verse.Command::icon
      IL_00fe:  stfld      class ['Assembly-CSharp']Verse.Gizmo DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>2__current'
      IL_0103:  ldarg.0
      IL_0104:  ldc.i4.2
      IL_0105:  stfld      int32 DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>1__state'
      IL_010a:  ldc.i4.1
      IL_010b:  ret

      IL_010c:  ldarg.0
      IL_010d:  ldc.i4.m1
      IL_010e:  stfld      int32 DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>1__state'
      IL_0113:  ldsfld     bool ['Assembly-CSharp']Verse.DebugSettings::godMode
      IL_0118:  brfalse.s  IL_018a

      IL_011a:  ldarg.0
      IL_011b:  newobj     instance void ['Assembly-CSharp']Verse.Command_Action::.ctor()
      IL_0120:  dup
      IL_0121:  ldloc.1
      IL_0122:  ldftn      instance void DubsBadHygiene.CompWaterStorage::FillTank()
      IL_0128:  newobj     instance void [mscorlib]System.Action::.ctor(object,
                                                                        native int)
      IL_012d:  stfld      class [mscorlib]System.Action ['Assembly-CSharp']Verse.Command_Action::action
      IL_0132:  dup
      IL_0133:  ldstr      "Fill"
      IL_0138:  stfld      string ['Assembly-CSharp']Verse.Command::defaultLabel
      IL_013d:  stfld      class ['Assembly-CSharp']Verse.Gizmo DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>2__current'
      IL_0142:  ldarg.0
      IL_0143:  ldc.i4.3
      IL_0144:  stfld      int32 DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>1__state'
      IL_0149:  ldc.i4.1
      IL_014a:  ret

      IL_014b:  ldarg.0
      IL_014c:  ldc.i4.m1
      IL_014d:  stfld      int32 DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>1__state'
      IL_0152:  ldarg.0
      IL_0153:  newobj     instance void ['Assembly-CSharp']Verse.Command_Action::.ctor()
      IL_0158:  dup
      IL_0159:  ldloc.1
      IL_015a:  ldftn      instance void DubsBadHygiene.CompWaterStorage::contam()
      IL_0160:  newobj     instance void [mscorlib]System.Action::.ctor(object,
                                                                        native int)
      IL_0165:  stfld      class [mscorlib]System.Action ['Assembly-CSharp']Verse.Command_Action::action
      IL_016a:  dup
      IL_016b:  ldstr      "contam"
      IL_0170:  stfld      string ['Assembly-CSharp']Verse.Command::defaultLabel
      IL_0175:  stfld      class ['Assembly-CSharp']Verse.Gizmo DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>2__current'
      IL_017a:  ldarg.0
      IL_017b:  ldc.i4.4
      IL_017c:  stfld      int32 DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>1__state'
      IL_0181:  ldc.i4.1
      IL_0182:  ret

      IL_0183:  ldarg.0
      IL_0184:  ldc.i4.m1
      IL_0185:  stfld      int32 DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>1__state'
      IL_018a:  ldc.i4.0
      IL_018b:  ret
    } // end of method '<CompGetGizmosExtra>d__37'::MoveNext

    .method private hidebysig newslot specialname virtual final 
            instance class ['Assembly-CSharp']Verse.Gizmo 
            'System.Collections.Generic.IEnumerator<Verse.Gizmo>.get_Current'() cil managed
    {
      .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
      .override  method instance !0 class [mscorlib]System.Collections.Generic.IEnumerator`1<class ['Assembly-CSharp']Verse.Gizmo>::get_Current()
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class ['Assembly-CSharp']Verse.Gizmo DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>2__current'
      IL_0006:  ret
    } // end of method '<CompGetGizmosExtra>d__37'::'System.Collections.Generic.IEnumerator<Verse.Gizmo>.get_Current'

    .method private hidebysig newslot virtual final 
            instance void  System.Collections.IEnumerator.Reset() cil managed
    {
      .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
      .override [mscorlib]System.Collections.IEnumerator::Reset
      // Code size       6 (0x6)
      .maxstack  8
      IL_0000:  newobj     instance void [mscorlib]System.NotSupportedException::.ctor()
      IL_0005:  throw
    } // end of method '<CompGetGizmosExtra>d__37'::System.Collections.IEnumerator.Reset

    .method private hidebysig newslot specialname virtual final 
            instance object  System.Collections.IEnumerator.get_Current() cil managed
    {
      .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
      .override [mscorlib]System.Collections.IEnumerator::get_Current
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class ['Assembly-CSharp']Verse.Gizmo DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>2__current'
      IL_0006:  ret
    } // end of method '<CompGetGizmosExtra>d__37'::System.Collections.IEnumerator.get_Current

    .method private hidebysig newslot virtual final 
            instance class [mscorlib]System.Collections.Generic.IEnumerator`1<class ['Assembly-CSharp']Verse.Gizmo> 
            'System.Collections.Generic.IEnumerable<Verse.Gizmo>.GetEnumerator'() cil managed
    {
      .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
      .override  method instance class [mscorlib]System.Collections.Generic.IEnumerator`1<!0> class [mscorlib]System.Collections.Generic.IEnumerable`1<class ['Assembly-CSharp']Verse.Gizmo>::GetEnumerator()
      // Code size       55 (0x37)
      .maxstack  2
      .locals init (class DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37' V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldfld      int32 DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>1__state'
      IL_0006:  ldc.i4.s   -2
      IL_0008:  bne.un.s   IL_0022

      IL_000a:  ldarg.0
      IL_000b:  ldfld      int32 DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>l__initialThreadId'
      IL_0010:  call       int32 [mscorlib]System.Environment::get_CurrentManagedThreadId()
      IL_0015:  bne.un.s   IL_0022

      IL_0017:  ldarg.0
      IL_0018:  ldc.i4.0
      IL_0019:  stfld      int32 DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>1__state'
      IL_001e:  ldarg.0
      IL_001f:  stloc.0
      IL_0020:  br.s       IL_0035

      IL_0022:  ldc.i4.0
      IL_0023:  newobj     instance void DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::.ctor(int32)
      IL_0028:  stloc.0
      IL_0029:  ldloc.0
      IL_002a:  ldarg.0
      IL_002b:  ldfld      class DubsBadHygiene.CompWaterStorage DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>4__this'
      IL_0030:  stfld      class DubsBadHygiene.CompWaterStorage DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>4__this'
      IL_0035:  ldloc.0
      IL_0036:  ret
    } // end of method '<CompGetGizmosExtra>d__37'::'System.Collections.Generic.IEnumerable<Verse.Gizmo>.GetEnumerator'

    .method private hidebysig newslot virtual final 
            instance class [mscorlib]System.Collections.IEnumerator 
            System.Collections.IEnumerable.GetEnumerator() cil managed
    {
      .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
      .override [mscorlib]System.Collections.IEnumerable::GetEnumerator
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance class [mscorlib]System.Collections.Generic.IEnumerator`1<class ['Assembly-CSharp']Verse.Gizmo> DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'System.Collections.Generic.IEnumerable<Verse.Gizmo>.GetEnumerator'()
      IL_0006:  ret
    } // end of method '<CompGetGizmosExtra>d__37'::System.Collections.IEnumerable.GetEnumerator

    .property instance class ['Assembly-CSharp']Verse.Gizmo
            'System.Collections.Generic.IEnumerator<Verse.Gizmo>.Current'()
    {
      .get instance class ['Assembly-CSharp']Verse.Gizmo DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'System.Collections.Generic.IEnumerator<Verse.Gizmo>.get_Current'()
    } // end of property '<CompGetGizmosExtra>d__37'::'System.Collections.Generic.IEnumerator<Verse.Gizmo>.Current'
    .property instance object System.Collections.IEnumerator.Current()
    {
      .get instance object DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::System.Collections.IEnumerator.get_Current()
    } // end of property '<CompGetGizmosExtra>d__37'::System.Collections.IEnumerator.Current
  } // end of class '<CompGetGizmosExtra>d__37'

  .field public static initonly class [UnityEngine.CoreModule]UnityEngine.Material WaterBarFilledMat
  .field public static initonly class [UnityEngine.CoreModule]UnityEngine.Material BarUnfilledMat
  .field public static class [UnityEngine.CoreModule]UnityEngine.Material LowWaterMat
  .field public static initonly valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 BarSize
  .field public static class [UnityEngine.CoreModule]UnityEngine.Material ContaminationMat
  .field public float32 BaseAlt
  .field public bool DrawOverlay
  .field public class DubsBadHygiene.CompPipe PipeCompInt
  .field public bool DrainTank
  .field public valuetype DubsBadHygiene.ContaminationLevel WaterQuality
  .field public float32 WaterStorage
  .field public class ['Assembly-CSharp']RimWorld.CompPowerTrader powerComp
  .field public class ['Assembly-CSharp']RimWorld.CompRefuelable fuelComp
  .field public class ['Assembly-CSharp']RimWorld.CompBreakdownable breakdownableComp
  .method public hidebysig virtual instance void 
          PostSpawnSetup(bool respawningAfterLoad) cil managed
  {
    // Code size       59 (0x3b)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void ['Assembly-CSharp']Verse.ThingComp::PostSpawnSetup(bool)
    IL_0007:  ldarg.0
    IL_0008:  ldarg.0
    IL_0009:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_000e:  callvirt   instance !!0 ['Assembly-CSharp']Verse.ThingWithComps::GetComp<class ['Assembly-CSharp']RimWorld.CompPowerTrader>()
    IL_0013:  stfld      class ['Assembly-CSharp']RimWorld.CompPowerTrader DubsBadHygiene.CompWaterStorage::powerComp
    IL_0018:  ldarg.0
    IL_0019:  ldarg.0
    IL_001a:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_001f:  callvirt   instance !!0 ['Assembly-CSharp']Verse.ThingWithComps::GetComp<class ['Assembly-CSharp']RimWorld.CompRefuelable>()
    IL_0024:  stfld      class ['Assembly-CSharp']RimWorld.CompRefuelable DubsBadHygiene.CompWaterStorage::fuelComp
    IL_0029:  ldarg.0
    IL_002a:  ldarg.0
    IL_002b:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0030:  callvirt   instance !!0 ['Assembly-CSharp']Verse.ThingWithComps::GetComp<class ['Assembly-CSharp']RimWorld.CompBreakdownable>()
    IL_0035:  stfld      class ['Assembly-CSharp']RimWorld.CompBreakdownable DubsBadHygiene.CompWaterStorage::breakdownableComp
    IL_003a:  ret
  } // end of method CompWaterStorage::PostSpawnSetup

  .method public hidebysig specialname instance class DubsBadHygiene.CompPipe 
          get_PipeComp() cil managed
  {
    // Code size       32 (0x20)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class DubsBadHygiene.CompPipe DubsBadHygiene.CompWaterStorage::PipeCompInt
    IL_0006:  brtrue.s   IL_0019

    IL_0008:  ldarg.0
    IL_0009:  ldarg.0
    IL_000a:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_000f:  callvirt   instance !!0 ['Assembly-CSharp']Verse.ThingWithComps::GetComp<class DubsBadHygiene.CompPipe>()
    IL_0014:  stfld      class DubsBadHygiene.CompPipe DubsBadHygiene.CompWaterStorage::PipeCompInt
    IL_0019:  ldarg.0
    IL_001a:  ldfld      class DubsBadHygiene.CompPipe DubsBadHygiene.CompWaterStorage::PipeCompInt
    IL_001f:  ret
  } // end of method CompWaterStorage::get_PipeComp

  .method public hidebysig specialname instance float32 
          get_space() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldc.r4     0.0
    IL_0005:  ldarg.0
    IL_0006:  call       instance class DubsBadHygiene.CompProperties_WaterStorage DubsBadHygiene.CompWaterStorage::get_Props()
    IL_000b:  ldfld      float32 DubsBadHygiene.CompProperties_WaterStorage::WaterStorageCap
    IL_0010:  ldarg.0
    IL_0011:  ldfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_0016:  sub
    IL_0017:  call       float32 [UnityEngine.CoreModule]UnityEngine.Mathf::Max(float32,
                                                                                float32)
    IL_001c:  ret
  } // end of method CompWaterStorage::get_space

  .method public hidebysig specialname instance bool 
          get_LowCapacity() cil managed
  {
    // Code size       32 (0x20)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_0006:  conv.r8
    IL_0007:  ldarg.0
    IL_0008:  call       instance class DubsBadHygiene.CompProperties_WaterStorage DubsBadHygiene.CompWaterStorage::get_Props()
    IL_000d:  ldfld      float32 DubsBadHygiene.CompProperties_WaterStorage::WaterStorageCap
    IL_0012:  conv.r8
    IL_0013:  ldc.r8     0.20000000000000001
    IL_001c:  mul
    IL_001d:  clt
    IL_001f:  ret
  } // end of method CompWaterStorage::get_LowCapacity

  .method public hidebysig specialname instance float32 
          get_CapPercent() cil managed
  {
    // Code size       43 (0x2b)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance class DubsBadHygiene.CompProperties_WaterStorage DubsBadHygiene.CompWaterStorage::get_Props()
    IL_0006:  ldfld      float32 DubsBadHygiene.CompProperties_WaterStorage::WaterStorageCap
    IL_000b:  ldc.r4     0.0
    IL_0010:  bne.un.s   IL_0018

    IL_0012:  ldc.r4     0.0
    IL_0017:  ret

    IL_0018:  ldarg.0
    IL_0019:  ldfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_001e:  ldarg.0
    IL_001f:  call       instance class DubsBadHygiene.CompProperties_WaterStorage DubsBadHygiene.CompWaterStorage::get_Props()
    IL_0024:  ldfld      float32 DubsBadHygiene.CompProperties_WaterStorage::WaterStorageCap
    IL_0029:  div
    IL_002a:  ret
  } // end of method CompWaterStorage::get_CapPercent

  .method public hidebysig specialname instance class DubsBadHygiene.CompProperties_WaterStorage 
          get_Props() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class ['Assembly-CSharp']Verse.CompProperties ['Assembly-CSharp']Verse.ThingComp::props
    IL_0006:  castclass  DubsBadHygiene.CompProperties_WaterStorage
    IL_000b:  ret
  } // end of method CompWaterStorage::get_Props

  .method public hidebysig virtual instance void 
          PostDrawExtraSelectionOverlays() cil managed
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void ['Assembly-CSharp']Verse.ThingComp::PostDrawExtraSelectionOverlays()
    IL_0006:  ldarg.0
    IL_0007:  call       instance class DubsBadHygiene.CompPipe DubsBadHygiene.CompWaterStorage::get_PipeComp()
    IL_000c:  ldfld      class DubsBadHygiene.MapComponent_Hygiene DubsBadHygiene.CompPipe::MapComp
    IL_0011:  ldc.i4.1
    IL_0012:  stfld      bool DubsBadHygiene.MapComponent_Hygiene::MarkTowersForDraw
    IL_0017:  ret
  } // end of method CompWaterStorage::PostDrawExtraSelectionOverlays

  .method public hidebysig virtual instance void 
          PostExposeData() cil managed
  {
    // Code size       65 (0x41)
    .maxstack  4
    IL_0000:  ldarg.0
    IL_0001:  call       instance void ['Assembly-CSharp']Verse.ThingComp::PostExposeData()
    IL_0006:  ldarg.0
    IL_0007:  ldflda     valuetype DubsBadHygiene.ContaminationLevel DubsBadHygiene.CompWaterStorage::WaterQuality
    IL_000c:  ldstr      "WaterQuality"
    IL_0011:  ldc.i4.1
    IL_0012:  ldc.i4.0
    IL_0013:  call       void ['Assembly-CSharp']Verse.Scribe_Values::Look<valuetype DubsBadHygiene.ContaminationLevel>(!!0&,
                                                                                                                        string,
                                                                                                                        !!0,
                                                                                                                        bool)
    IL_0018:  ldarg.0
    IL_0019:  ldflda     float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_001e:  ldstr      "WaterStorage"
    IL_0023:  ldc.r4     0.0
    IL_0028:  ldc.i4.0
    IL_0029:  call       void ['Assembly-CSharp']Verse.Scribe_Values::Look<float32>(!!0&,
                                                                                    string,
                                                                                    !!0,
                                                                                    bool)
    IL_002e:  ldarg.0
    IL_002f:  ldflda     bool DubsBadHygiene.CompWaterStorage::DrainTank
    IL_0034:  ldstr      "DrainTank"
    IL_0039:  ldc.i4.0
    IL_003a:  ldc.i4.0
    IL_003b:  call       void ['Assembly-CSharp']Verse.Scribe_Values::Look<bool>(!!0&,
                                                                                 string,
                                                                                 !!0,
                                                                                 bool)
    IL_0040:  ret
  } // end of method CompWaterStorage::PostExposeData

  .method public hidebysig virtual instance void 
          PostDraw() cil managed
  {
    // Code size       305 (0x131)
    .maxstack  5
    .locals init (valuetype ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest V_0,
             valuetype ['Assembly-CSharp']Verse.Rot4 V_1)
    IL_0000:  ldarg.0
    IL_0001:  call       instance void ['Assembly-CSharp']Verse.ThingComp::PostDraw()
    IL_0006:  ldarg.0
    IL_0007:  ldfld      bool DubsBadHygiene.CompWaterStorage::DrawOverlay
    IL_000c:  brfalse    IL_00a6

    IL_0011:  ldloca.s   V_0
    IL_0013:  initobj    ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest
    IL_0019:  ldloca.s   V_0
    IL_001b:  ldarg.0
    IL_001c:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0021:  callvirt   instance valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 ['Assembly-CSharp']Verse.Thing::get_DrawPos()
    IL_0026:  call       valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 [UnityEngine.CoreModule]UnityEngine.Vector3::get_up()
    IL_002b:  ldc.r4     0.1
    IL_0030:  call       valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 [UnityEngine.CoreModule]UnityEngine.Vector3::op_Multiply(valuetype [UnityEngine.CoreModule]UnityEngine.Vector3,
                                                                                                                                        float32)
    IL_0035:  call       valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 [UnityEngine.CoreModule]UnityEngine.Vector3::op_Addition(valuetype [UnityEngine.CoreModule]UnityEngine.Vector3,
                                                                                                                                        valuetype [UnityEngine.CoreModule]UnityEngine.Vector3)
    IL_003a:  stfld      valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest::center
    IL_003f:  ldloca.s   V_0
    IL_0041:  ldsfld     valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 DubsBadHygiene.CompWaterStorage::BarSize
    IL_0046:  stfld      valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest::size
    IL_004b:  ldloca.s   V_0
    IL_004d:  ldarg.0
    IL_004e:  call       instance float32 DubsBadHygiene.CompWaterStorage::get_CapPercent()
    IL_0053:  stfld      float32 ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest::fillPercent
    IL_0058:  ldloca.s   V_0
    IL_005a:  ldsfld     class [UnityEngine.CoreModule]UnityEngine.Material DubsBadHygiene.CompWaterStorage::WaterBarFilledMat
    IL_005f:  stfld      class [UnityEngine.CoreModule]UnityEngine.Material ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest::filledMat
    IL_0064:  ldloca.s   V_0
    IL_0066:  ldsfld     class [UnityEngine.CoreModule]UnityEngine.Material DubsBadHygiene.CompWaterStorage::BarUnfilledMat
    IL_006b:  stfld      class [UnityEngine.CoreModule]UnityEngine.Material ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest::unfilledMat
    IL_0070:  ldloca.s   V_0
    IL_0072:  ldc.r4     5.0000001e-02
    IL_0077:  stfld      float32 ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest::margin
    IL_007c:  ldarg.0
    IL_007d:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0082:  callvirt   instance valuetype ['Assembly-CSharp']Verse.Rot4 ['Assembly-CSharp']Verse.Thing::get_Rotation()
    IL_0087:  stloc.1
    IL_0088:  ldloca.s   V_1
    IL_008a:  ldc.i4.1
    IL_008b:  call       instance void ['Assembly-CSharp']Verse.Rot4::Rotate(valuetype ['Assembly-CSharp']Verse.RotationDirection)
    IL_0090:  ldloca.s   V_0
    IL_0092:  ldloc.1
    IL_0093:  stfld      valuetype ['Assembly-CSharp']Verse.Rot4 ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest::rotation
    IL_0098:  ldloc.0
    IL_0099:  call       void ['Assembly-CSharp']Verse.GenDraw::DrawFillableBar(valuetype ['Assembly-CSharp']Verse.GenDraw/FillableBarRequest)
    IL_009e:  ldarg.0
    IL_009f:  ldc.i4.0
    IL_00a0:  stfld      bool DubsBadHygiene.CompWaterStorage::DrawOverlay
    IL_00a5:  ret

    IL_00a6:  ldarg.0
    IL_00a7:  ldfld      bool DubsBadHygiene.CompWaterStorage::DrainTank
    IL_00ac:  brfalse.s  IL_00d4

    IL_00ae:  ldarg.0
    IL_00af:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_00b4:  ldsfld     class [UnityEngine.CoreModule]UnityEngine.Material DubsBadHygiene.GraphicsCache::Draining
    IL_00b9:  ldarg.0
    IL_00ba:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_00bf:  callvirt   instance valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 ['Assembly-CSharp']Verse.Thing::get_DrawPos()
    IL_00c4:  ldsfld     class [UnityEngine.CoreModule]UnityEngine.Mesh ['Assembly-CSharp']Verse.MeshPool::plane08
    IL_00c9:  call       valuetype [UnityEngine.CoreModule]UnityEngine.Quaternion [UnityEngine.CoreModule]UnityEngine.Quaternion::get_identity()
    IL_00ce:  call       void DubsBadHygiene.DubUtils::RenderPulsingOverlay(class ['Assembly-CSharp']Verse.Thing,
                                                                            class [UnityEngine.CoreModule]UnityEngine.Material,
                                                                            valuetype [UnityEngine.CoreModule]UnityEngine.Vector3,
                                                                            class [UnityEngine.CoreModule]UnityEngine.Mesh,
                                                                            valuetype [UnityEngine.CoreModule]UnityEngine.Quaternion)
    IL_00d3:  ret

    IL_00d4:  ldarg.0
    IL_00d5:  ldfld      valuetype DubsBadHygiene.ContaminationLevel DubsBadHygiene.CompWaterStorage::WaterQuality
    IL_00da:  ldc.i4.2
    IL_00db:  bne.un.s   IL_0103

    IL_00dd:  ldarg.0
    IL_00de:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_00e3:  ldsfld     class [UnityEngine.CoreModule]UnityEngine.Material DubsBadHygiene.CompWaterStorage::ContaminationMat
    IL_00e8:  ldarg.0
    IL_00e9:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_00ee:  callvirt   instance valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 ['Assembly-CSharp']Verse.Thing::get_DrawPos()
    IL_00f3:  ldsfld     class [UnityEngine.CoreModule]UnityEngine.Mesh ['Assembly-CSharp']Verse.MeshPool::plane08
    IL_00f8:  call       valuetype [UnityEngine.CoreModule]UnityEngine.Quaternion [UnityEngine.CoreModule]UnityEngine.Quaternion::get_identity()
    IL_00fd:  call       void DubsBadHygiene.DubUtils::RenderPulsingOverlay(class ['Assembly-CSharp']Verse.Thing,
                                                                            class [UnityEngine.CoreModule]UnityEngine.Material,
                                                                            valuetype [UnityEngine.CoreModule]UnityEngine.Vector3,
                                                                            class [UnityEngine.CoreModule]UnityEngine.Mesh,
                                                                            valuetype [UnityEngine.CoreModule]UnityEngine.Quaternion)
    IL_0102:  ret

    IL_0103:  ldarg.0
    IL_0104:  call       instance bool DubsBadHygiene.CompWaterStorage::get_LowCapacity()
    IL_0109:  brfalse.s  IL_0130

    IL_010b:  ldarg.0
    IL_010c:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0111:  ldsfld     class [UnityEngine.CoreModule]UnityEngine.Material DubsBadHygiene.CompWaterStorage::LowWaterMat
    IL_0116:  ldarg.0
    IL_0117:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_011c:  callvirt   instance valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 ['Assembly-CSharp']Verse.Thing::get_DrawPos()
    IL_0121:  ldsfld     class [UnityEngine.CoreModule]UnityEngine.Mesh ['Assembly-CSharp']Verse.MeshPool::plane08
    IL_0126:  call       valuetype [UnityEngine.CoreModule]UnityEngine.Quaternion [UnityEngine.CoreModule]UnityEngine.Quaternion::get_identity()
    IL_012b:  call       void DubsBadHygiene.DubUtils::RenderPulsingOverlay(class ['Assembly-CSharp']Verse.Thing,
                                                                            class [UnityEngine.CoreModule]UnityEngine.Material,
                                                                            valuetype [UnityEngine.CoreModule]UnityEngine.Vector3,
                                                                            class [UnityEngine.CoreModule]UnityEngine.Mesh,
                                                                            valuetype [UnityEngine.CoreModule]UnityEngine.Quaternion)
    IL_0130:  ret
  } // end of method CompWaterStorage::PostDraw

  .method public hidebysig specialname instance bool 
          get_WorkingNow() cil managed
  {
    // Code size       90 (0x5a)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  call       instance bool DubsBadHygiene.CompWaterStorage::IsDraining()
    IL_0006:  brtrue.s   IL_0058

    IL_0008:  ldarg.0
    IL_0009:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_000e:  call       bool ['Assembly-CSharp']RimWorld.FlickUtility::WantsToBeOn(class ['Assembly-CSharp']Verse.Thing)
    IL_0013:  brfalse.s  IL_0058

    IL_0015:  ldarg.0
    IL_0016:  ldfld      class ['Assembly-CSharp']RimWorld.CompPowerTrader DubsBadHygiene.CompWaterStorage::powerComp
    IL_001b:  brfalse.s  IL_002a

    IL_001d:  ldarg.0
    IL_001e:  ldfld      class ['Assembly-CSharp']RimWorld.CompPowerTrader DubsBadHygiene.CompWaterStorage::powerComp
    IL_0023:  callvirt   instance bool ['Assembly-CSharp']RimWorld.CompPowerTrader::get_PowerOn()
    IL_0028:  brfalse.s  IL_0058

    IL_002a:  ldarg.0
    IL_002b:  ldfld      class ['Assembly-CSharp']RimWorld.CompRefuelable DubsBadHygiene.CompWaterStorage::fuelComp
    IL_0030:  brfalse.s  IL_003f

    IL_0032:  ldarg.0
    IL_0033:  ldfld      class ['Assembly-CSharp']RimWorld.CompRefuelable DubsBadHygiene.CompWaterStorage::fuelComp
    IL_0038:  callvirt   instance bool ['Assembly-CSharp']RimWorld.CompRefuelable::get_HasFuel()
    IL_003d:  brfalse.s  IL_0058

    IL_003f:  ldarg.0
    IL_0040:  ldfld      class ['Assembly-CSharp']RimWorld.CompBreakdownable DubsBadHygiene.CompWaterStorage::breakdownableComp
    IL_0045:  brfalse.s  IL_0056

    IL_0047:  ldarg.0
    IL_0048:  ldfld      class ['Assembly-CSharp']RimWorld.CompBreakdownable DubsBadHygiene.CompWaterStorage::breakdownableComp
    IL_004d:  callvirt   instance bool ['Assembly-CSharp']RimWorld.CompBreakdownable::get_BrokenDown()
    IL_0052:  ldc.i4.0
    IL_0053:  ceq
    IL_0055:  ret

    IL_0056:  ldc.i4.1
    IL_0057:  ret

    IL_0058:  ldc.i4.0
    IL_0059:  ret
  } // end of method CompWaterStorage::get_WorkingNow

  .method public hidebysig instance bool 
          IsDraining() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool DubsBadHygiene.CompWaterStorage::DrainTank
    IL_0006:  ret
  } // end of method CompWaterStorage::IsDraining

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

  .method public hidebysig instance void 
          DrainToggle() cil managed
  {
    .custom instance void ['0MultiplayerAPI']Multiplayer.API.SyncMethodAttribute::.ctor(valuetype ['0MultiplayerAPI']Multiplayer.API.SyncContext) = ( 01 00 00 00 00 00 00 00 ) 
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      bool DubsBadHygiene.CompWaterStorage::DrainTank
    IL_0007:  ldc.i4.0
    IL_0008:  ceq
    IL_000a:  stfld      bool DubsBadHygiene.CompWaterStorage::DrainTank
    IL_000f:  ret
  } // end of method CompWaterStorage::DrainToggle

  .method public hidebysig instance void 
          drain() cil managed
  {
    // Code size       40 (0x28)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.r4     0.0
    IL_0006:  stfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_000b:  ldarg.0
    IL_000c:  ldc.i4.1
    IL_000d:  stfld      valuetype DubsBadHygiene.ContaminationLevel DubsBadHygiene.CompWaterStorage::WaterQuality
    IL_0012:  ldsfld     class ['Assembly-CSharp']Verse.SoundDef DubsBadHygiene.DubDef::DrainHole
    IL_0017:  ldarg.0
    IL_0018:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_001d:  call       valuetype ['Assembly-CSharp']Verse.Sound.SoundInfo ['Assembly-CSharp']Verse.Sound.SoundInfo::op_Implicit(class ['Assembly-CSharp']Verse.Thing)
    IL_0022:  call       void ['Assembly-CSharp']Verse.Sound.SoundStarter::PlayOneShot(class ['Assembly-CSharp']Verse.SoundDef,
                                                                                       valuetype ['Assembly-CSharp']Verse.Sound.SoundInfo)
    IL_0027:  ret
  } // end of method CompWaterStorage::drain

  .method public hidebysig instance void 
          draindes() cil managed
  {
    .custom instance void ['0MultiplayerAPI']Multiplayer.API.SyncMethodAttribute::.ctor(valuetype ['0MultiplayerAPI']Multiplayer.API.SyncContext) = ( 01 00 00 00 00 00 00 00 ) 
    // Code size       87 (0x57)
    .maxstack  4
    .locals init (class ['Assembly-CSharp']Verse.Designation V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0006:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_000b:  ldfld      class ['Assembly-CSharp']Verse.DesignationManager ['Assembly-CSharp']Verse.Map::designationManager
    IL_0010:  ldarg.0
    IL_0011:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0016:  ldsfld     class ['Assembly-CSharp']Verse.DesignationDef DubsBadHygiene.DubDef::drainOutDes
    IL_001b:  callvirt   instance class ['Assembly-CSharp']Verse.Designation ['Assembly-CSharp']Verse.DesignationManager::DesignationOn(class ['Assembly-CSharp']Verse.Thing,
                                                                                                                                        class ['Assembly-CSharp']Verse.DesignationDef)
    IL_0020:  stloc.0
    IL_0021:  ldloc.0
    IL_0022:  brfalse.s  IL_002b

    IL_0024:  ldloc.0
    IL_0025:  callvirt   instance void ['Assembly-CSharp']Verse.Designation::Delete()
    IL_002a:  ret

    IL_002b:  ldarg.0
    IL_002c:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0031:  callvirt   instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
    IL_0036:  ldfld      class ['Assembly-CSharp']Verse.DesignationManager ['Assembly-CSharp']Verse.Map::designationManager
    IL_003b:  ldarg.0
    IL_003c:  ldfld      class ['Assembly-CSharp']Verse.ThingWithComps ['Assembly-CSharp']Verse.ThingComp::parent
    IL_0041:  call       valuetype ['Assembly-CSharp']Verse.LocalTargetInfo ['Assembly-CSharp']Verse.LocalTargetInfo::op_Implicit(class ['Assembly-CSharp']Verse.Thing)
    IL_0046:  ldsfld     class ['Assembly-CSharp']Verse.DesignationDef DubsBadHygiene.DubDef::drainOutDes
    IL_004b:  ldnull
    IL_004c:  newobj     instance void ['Assembly-CSharp']Verse.Designation::.ctor(valuetype ['Assembly-CSharp']Verse.LocalTargetInfo,
                                                                                   class ['Assembly-CSharp']Verse.DesignationDef,
                                                                                   class ['Assembly-CSharp']RimWorld.ColorDef)
    IL_0051:  callvirt   instance void ['Assembly-CSharp']Verse.DesignationManager::AddDesignation(class ['Assembly-CSharp']Verse.Designation)
    IL_0056:  ret
  } // end of method CompWaterStorage::draindes

  .method public hidebysig instance void 
          FillTank() cil managed
  {
    .custom instance void ['0MultiplayerAPI']Multiplayer.API.SyncMethodAttribute::.ctor(valuetype ['0MultiplayerAPI']Multiplayer.API.SyncContext) = ( 01 00 00 00 00 00 00 00 ) 
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  call       instance class DubsBadHygiene.CompProperties_WaterStorage DubsBadHygiene.CompWaterStorage::get_Props()
    IL_0007:  ldfld      float32 DubsBadHygiene.CompProperties_WaterStorage::WaterStorageCap
    IL_000c:  stfld      float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_0011:  ret
  } // end of method CompWaterStorage::FillTank

  .method public hidebysig instance void 
          contam() cil managed
  {
    .custom instance void ['0MultiplayerAPI']Multiplayer.API.SyncMethodAttribute::.ctor(valuetype ['0MultiplayerAPI']Multiplayer.API.SyncContext) = ( 01 00 00 00 00 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.2
    IL_0002:  stfld      valuetype DubsBadHygiene.ContaminationLevel DubsBadHygiene.CompWaterStorage::WaterQuality
    IL_0007:  ret
  } // end of method CompWaterStorage::contam

  .method public hidebysig virtual instance class [mscorlib]System.Collections.Generic.IEnumerable`1<class ['Assembly-CSharp']Verse.Gizmo> 
          CompGetGizmosExtra() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IteratorStateMachineAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 39 44 75 62 73 42 61 64 48 79 67 69 65 6E   // ..9DubsBadHygien
                                                                                                                                          65 2E 43 6F 6D 70 57 61 74 65 72 53 74 6F 72 61   // e.CompWaterStora
                                                                                                                                          67 65 2B 3C 43 6F 6D 70 47 65 74 47 69 7A 6D 6F   // ge+<CompGetGizmo
                                                                                                                                          73 45 78 74 72 61 3E 64 5F 5F 33 37 00 00 )       // sExtra>d__37..
    // Code size       15 (0xf)
    .maxstack  8
    IL_0000:  ldc.i4.s   -2
    IL_0002:  newobj     instance void DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::.ctor(int32)
    IL_0007:  dup
    IL_0008:  ldarg.0
    IL_0009:  stfld      class DubsBadHygiene.CompWaterStorage DubsBadHygiene.CompWaterStorage/'<CompGetGizmosExtra>d__37'::'<>4__this'
    IL_000e:  ret
  } // end of method CompWaterStorage::CompGetGizmosExtra

  .method public hidebysig virtual instance string 
          CompInspectStringExtra() cil managed
  {
    // Code size       405 (0x195)
    .maxstack  6
    .locals init (class [mscorlib]System.Text.StringBuilder V_0,
             float32 V_1)
    IL_0000:  ldarg.0
    IL_0001:  call       instance class ['Assembly-CSharp']Verse.IThingHolder ['Assembly-CSharp']Verse.ThingComp::get_ParentHolder()
    IL_0006:  isinst     ['Assembly-CSharp']RimWorld.MinifiedThing
    IL_000b:  brfalse.s  IL_0014

    IL_000d:  ldarg.0
    IL_000e:  call       instance string ['Assembly-CSharp']Verse.ThingComp::CompInspectStringExtra()
    IL_0013:  ret

    IL_0014:  newobj     instance void [mscorlib]System.Text.StringBuilder::.ctor()
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldstr      "WaterStorage"
    IL_0020:  ldarg.0
    IL_0021:  ldflda     float32 DubsBadHygiene.CompWaterStorage::WaterStorage
    IL_0026:  ldstr      "0.0"
    IL_002b:  call       instance string [mscorlib]System.Single::ToString(string)
    IL_0030:  call       valuetype ['Assembly-CSharp']Verse.NamedArgument ['Assembly-CSharp']Verse.NamedArgument::op_Implicit(string)
    IL_0035:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.TranslatorFormattedStringExtensions::Translate(string,
                                                                                                                                                 valuetype ['Assembly-CSharp']Verse.NamedArgument)
    IL_003a:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
    IL_003f:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
    IL_0044:  pop
    IL_0045:  ldsfld     bool ['Assembly-CSharp']Verse.DebugSettings::godMode
    IL_004a:  brfalse.s  IL_0077

    IL_004c:  ldloc.0
    IL_004d:  ldstr      "WaterPumpedPerTick: "
    IL_0052:  ldarg.0
    IL_0053:  call       instance class DubsBadHygiene.CompPipe DubsBadHygiene.CompWaterStorage::get_PipeComp()
    IL_0058:  callvirt   instance class DubsBadHygiene.PlumbingNet DubsBadHygiene.CompPipe::get_pipeNet()
    IL_005d:  ldflda     float32 DubsBadHygiene.PlumbingNet::WaterPumpedPerTick
    IL_0062:  ldstr      "0.00"
    IL_0067:  call       instance string [mscorlib]System.Single::ToString(string)
    IL_006c:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0071:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
    IL_0076:  pop
    IL_0077:  ldarg.0
    IL_0078:  call       instance class DubsBadHygiene.CompPipe DubsBadHygiene.CompWaterStorage::get_PipeComp()
    IL_007d:  callvirt   instance class DubsBadHygiene.PlumbingNet DubsBadHygiene.CompPipe::get_pipeNet()
    IL_0082:  brfalse    IL_0132

    IL_0087:  ldloc.0
    IL_0088:  ldstr      "TotalWaterStorage"
    IL_008d:  ldarg.0
    IL_008e:  call       instance class DubsBadHygiene.CompPipe DubsBadHygiene.CompWaterStorage::get_PipeComp()
    IL_0093:  callvirt   instance class DubsBadHygiene.PlumbingNet DubsBadHygiene.CompPipe::get_pipeNet()
    IL_0098:  callvirt   instance float32 DubsBadHygiene.PlumbingNet::get_WaterStorage()
    IL_009d:  stloc.1
    IL_009e:  ldloca.s   V_1
    IL_00a0:  ldstr      "0.0"
    IL_00a5:  call       instance string [mscorlib]System.Single::ToString(string)
    IL_00aa:  call       valuetype ['Assembly-CSharp']Verse.NamedArgument ['Assembly-CSharp']Verse.NamedArgument::op_Implicit(string)
    IL_00af:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.TranslatorFormattedStringExtensions::Translate(string,
                                                                                                                                                 valuetype ['Assembly-CSharp']Verse.NamedArgument)
    IL_00b4:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
    IL_00b9:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
    IL_00be:  pop
    IL_00bf:  ldloc.0
    IL_00c0:  ldstr      "PipedPumpCapacity"
    IL_00c5:  ldarg.0
    IL_00c6:  call       instance class DubsBadHygiene.CompPipe DubsBadHygiene.CompWaterStorage::get_PipeComp()
    IL_00cb:  callvirt   instance class DubsBadHygiene.PlumbingNet DubsBadHygiene.CompPipe::get_pipeNet()
    IL_00d0:  ldflda     float32 DubsBadHygiene.PlumbingNet::PumpingCapacitySum
    IL_00d5:  ldstr      "0"
    IL_00da:  call       instance string [mscorlib]System.Single::ToString(string)
    IL_00df:  call       valuetype ['Assembly-CSharp']Verse.NamedArgument ['Assembly-CSharp']Verse.NamedArgument::op_Implicit(string)
    IL_00e4:  ldarg.0
    IL_00e5:  call       instance class DubsBadHygiene.CompPipe DubsBadHygiene.CompWaterStorage::get_PipeComp()
    IL_00ea:  callvirt   instance class DubsBadHygiene.PlumbingNet DubsBadHygiene.CompPipe::get_pipeNet()
    IL_00ef:  ldflda     float32 DubsBadHygiene.PlumbingNet::GroundWaterCapacitySum
    IL_00f4:  ldstr      "0"
    IL_00f9:  call       instance string [mscorlib]System.Single::ToString(string)
    IL_00fe:  call       valuetype ['Assembly-CSharp']Verse.NamedArgument ['Assembly-CSharp']Verse.NamedArgument::op_Implicit(string)
    IL_0103:  ldarg.0
    IL_0104:  call       instance class DubsBadHygiene.CompPipe DubsBadHygiene.CompWaterStorage::get_PipeComp()
    IL_0109:  callvirt   instance class DubsBadHygiene.PlumbingNet DubsBadHygiene.CompPipe::get_pipeNet()
    IL_010e:  ldfld      float32 DubsBadHygiene.PlumbingNet::WaterCap
    IL_0113:  ldstr      "0.0"
    IL_0118:  call       string ['Assembly-CSharp']Verse.GenText::ToStringPercent(float32,
                                                                                  string)
    IL_011d:  call       valuetype ['Assembly-CSharp']Verse.NamedArgument ['Assembly-CSharp']Verse.NamedArgument::op_Implicit(string)
    IL_0122:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.TranslatorFormattedStringExtensions::Translate(string,
                                                                                                                                                 valuetype ['Assembly-CSharp']Verse.NamedArgument,
                                                                                                                                                 valuetype ['Assembly-CSharp']Verse.NamedArgument,
                                                                                                                                                 valuetype ['Assembly-CSharp']Verse.NamedArgument)
    IL_0127:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
    IL_012c:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
    IL_0131:  pop
    IL_0132:  ldarg.0
    IL_0133:  ldfld      valuetype DubsBadHygiene.ContaminationLevel DubsBadHygiene.CompWaterStorage::WaterQuality
    IL_0138:  brtrue.s   IL_0152

    IL_013a:  ldloc.0
    IL_013b:  ldstr      "TreatedWater"
    IL_0140:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.Translator::Translate(string)
    IL_0145:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
    IL_014a:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
    IL_014f:  pop
    IL_0150:  br.s       IL_0189

    IL_0152:  ldarg.0
    IL_0153:  ldfld      valuetype DubsBadHygiene.ContaminationLevel DubsBadHygiene.CompWaterStorage::WaterQuality
    IL_0158:  ldc.i4.2
    IL_0159:  bne.un.s   IL_0173

    IL_015b:  ldloc.0
    IL_015c:  ldstr      "ContaminatedWater"
    IL_0161:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.Translator::Translate(string)
    IL_0166:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
    IL_016b:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
    IL_0170:  pop
    IL_0171:  br.s       IL_0189

    IL_0173:  ldloc.0
    IL_0174:  ldstr      "UntreatedWater"
    IL_0179:  call       valuetype ['Assembly-CSharp']Verse.TaggedString ['Assembly-CSharp']Verse.Translator::Translate(string)
    IL_017e:  call       string ['Assembly-CSharp']Verse.TaggedString::op_Implicit(valuetype ['Assembly-CSharp']Verse.TaggedString)
    IL_0183:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
    IL_0188:  pop
    IL_0189:  ldloc.0
    IL_018a:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_018f:  call       string ['Assembly-CSharp']Verse.GenText::TrimEndNewlines(string)
    IL_0194:  ret
  } // end of method CompWaterStorage::CompInspectStringExtra

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.s   39
    IL_0003:  call       float32 ['Assembly-CSharp']Verse.Altitudes::AltitudeFor(valuetype ['Assembly-CSharp']Verse.AltitudeLayer)
    IL_0008:  stfld      float32 DubsBadHygiene.CompWaterStorage::BaseAlt
    IL_000d:  ldarg.0
    IL_000e:  ldc.i4.1
    IL_000f:  stfld      valuetype DubsBadHygiene.ContaminationLevel DubsBadHygiene.CompWaterStorage::WaterQuality
    IL_0014:  ldarg.0
    IL_0015:  call       instance void ['Assembly-CSharp']Verse.ThingComp::.ctor()
    IL_001a:  ret
  } // end of method CompWaterStorage::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       108 (0x6c)
    .maxstack  3
    IL_0000:  call       valuetype [UnityEngine.CoreModule]UnityEngine.Color [UnityEngine.CoreModule]UnityEngine.Color::get_cyan()
    IL_0005:  ldc.i4.0
    IL_0006:  call       class [UnityEngine.CoreModule]UnityEngine.Material ['Assembly-CSharp']Verse.SolidColorMaterials::SimpleSolidColorMaterial(valuetype [UnityEngine.CoreModule]UnityEngine.Color,
                                                                                                                                                   bool)
    IL_000b:  stsfld     class [UnityEngine.CoreModule]UnityEngine.Material DubsBadHygiene.CompWaterStorage::WaterBarFilledMat
    IL_0010:  ldc.r4     0.2
    IL_0015:  ldc.r4     0.2
    IL_001a:  ldc.r4     0.2
    IL_001f:  newobj     instance void [UnityEngine.CoreModule]UnityEngine.Color::.ctor(float32,
                                                                                        float32,
                                                                                        float32)
    IL_0024:  ldc.i4.0
    IL_0025:  call       class [UnityEngine.CoreModule]UnityEngine.Material ['Assembly-CSharp']Verse.SolidColorMaterials::SimpleSolidColorMaterial(valuetype [UnityEngine.CoreModule]UnityEngine.Color,
                                                                                                                                                   bool)
    IL_002a:  stsfld     class [UnityEngine.CoreModule]UnityEngine.Material DubsBadHygiene.CompWaterStorage::BarUnfilledMat
    IL_002f:  ldstr      "DBH/UI/waste_water"
    IL_0034:  ldsfld     class [UnityEngine.CoreModule]UnityEngine.Shader ['Assembly-CSharp']Verse.ShaderDatabase::MetaOverlay
    IL_0039:  call       class [UnityEngine.CoreModule]UnityEngine.Material ['Assembly-CSharp']Verse.MaterialPool::MatFrom(string,
                                                                                                                           class [UnityEngine.CoreModule]UnityEngine.Shader)
    IL_003e:  stsfld     class [UnityEngine.CoreModule]UnityEngine.Material DubsBadHygiene.CompWaterStorage::LowWaterMat
    IL_0043:  ldc.r4     0.69999999
    IL_0048:  ldc.r4     0.2
    IL_004d:  newobj     instance void [UnityEngine.CoreModule]UnityEngine.Vector2::.ctor(float32,
                                                                                          float32)
    IL_0052:  stsfld     valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 DubsBadHygiene.CompWaterStorage::BarSize
    IL_0057:  ldstr      "DBH/UI/contamination"
    IL_005c:  ldsfld     class [UnityEngine.CoreModule]UnityEngine.Shader ['Assembly-CSharp']Verse.ShaderDatabase::MetaOverlay
    IL_0061:  call       class [UnityEngine.CoreModule]UnityEngine.Material ['Assembly-CSharp']Verse.MaterialPool::MatFrom(string,
                                                                                                                           class [UnityEngine.CoreModule]UnityEngine.Shader)
    IL_0066:  stsfld     class [UnityEngine.CoreModule]UnityEngine.Material DubsBadHygiene.CompWaterStorage::ContaminationMat
    IL_006b:  ret
  } // end of method CompWaterStorage::.cctor

  .property instance class DubsBadHygiene.CompPipe
          PipeComp()
  {
    .get instance class DubsBadHygiene.CompPipe DubsBadHygiene.CompWaterStorage::get_PipeComp()
  } // end of property CompWaterStorage::PipeComp
  .property instance float32 space()
  {
    .get instance float32 DubsBadHygiene.CompWaterStorage::get_space()
  } // end of property CompWaterStorage::space
  .property instance bool LowCapacity()
  {
    .get instance bool DubsBadHygiene.CompWaterStorage::get_LowCapacity()
  } // end of property CompWaterStorage::LowCapacity
  .property instance float32 CapPercent()
  {
    .get instance float32 DubsBadHygiene.CompWaterStorage::get_CapPercent()
  } // end of property CompWaterStorage::CapPercent
  .property instance class DubsBadHygiene.CompProperties_WaterStorage
          Props()
  {
    .get instance class DubsBadHygiene.CompProperties_WaterStorage DubsBadHygiene.CompWaterStorage::get_Props()
  } // end of property CompWaterStorage::Props
  .property instance bool WorkingNow()
  {
    .get instance bool DubsBadHygiene.CompWaterStorage::get_WorkingNow()
  } // end of property CompWaterStorage::WorkingNow
} // end of class DubsBadHygiene.CompWaterStorage

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file F:\Repositories\rimworld_mods\.tmp\dbh_wstor.res
