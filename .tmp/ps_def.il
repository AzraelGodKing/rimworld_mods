
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.8.3928.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.


// warning : THIS IS A PARTIAL DISASSEMBLY, NOT SUITABLE FOR RE-ASSEMBLING



.class public auto ansi beforefieldinit PipeSystem.PipeNetDef
       extends ['Assembly-CSharp']Verse.Def
{
  .field public class [mscorlib]System.Type pipeNetClass
  .field public class PipeSystem.Resource resource
  .field public class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.ThingDef> pipeDefs
  .field public class PipeSystem.DeconstructOption designator
  .field public class PipeSystem.OverlayOption overlayOptions
  .field public class [mscorlib]System.Collections.Generic.List`1<class PipeSystem.LinkOption> linkToRefuelables
  .field public float32 transferAmount
  .field public float32 convertAmount
  .field public bool noStorageAlert
  .field public bool foggedNetAlert
  .field public class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.ThingDef> alertProofDefs
  .field public class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.ThingDef> noLinkedGraphicDefs
  .field public bool enableOverflow
  .field public float32 productionToOverflowCapacityRatio
  .field public float32 overflowAmount
  .field assembly class [UnityEngine.CoreModule]UnityEngine.Material offMat
  .custom instance void ['Assembly-CSharp']Verse.UnsavedAttribute::.ctor(bool) = ( 01 00 00 00 00 ) 
  .field assembly class [UnityEngine.CoreModule]UnityEngine.Texture2D uiIcon
  .custom instance void ['Assembly-CSharp']Verse.UnsavedAttribute::.ctor(bool) = ( 01 00 00 00 00 ) 
  .field assembly string loweredName
  .custom instance void ['Assembly-CSharp']Verse.UnsavedAttribute::.ctor(bool) = ( 01 00 00 00 00 ) 
  .method public hidebysig virtual instance class [mscorlib]System.Collections.Generic.IEnumerable`1<string> 
          ConfigErrors() cil managed
  {
    // Code size       47 (0x2f)
    .maxstack  2
    .locals init (class [mscorlib]System.Collections.Generic.List`1<string> V_0)
    IL_0000:  ldarg.0
    IL_0001:  call       instance class [mscorlib]System.Collections.Generic.IEnumerable`1<string> ['Assembly-CSharp']Verse.Def::ConfigErrors()
    IL_0006:  call       class [mscorlib]System.Collections.Generic.List`1<!!0> [System.Core]System.Linq.Enumerable::ToList<string>(class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_000b:  stloc.0
    IL_000c:  ldarg.0
    IL_000d:  ldfld      class PipeSystem.OverlayOption PipeSystem.PipeNetDef::overlayOptions
    IL_0012:  brtrue.s   IL_0021

    IL_0014:  ldloc.0
    IL_0015:  ldstr      "overlayOptions can't be null. It's overlayColor ta"
    + "g is required."
    IL_001a:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_001f:  br.s       IL_002d

    IL_0021:  ldarg.0
    IL_0022:  ldfld      class PipeSystem.OverlayOption PipeSystem.PipeNetDef::overlayOptions
    IL_0027:  ldfld      valuetype [UnityEngine.CoreModule]UnityEngine.Color PipeSystem.OverlayOption::overlayColor
    IL_002c:  pop
    IL_002d:  ldloc.0
    IL_002e:  ret
  } // end of method PipeNetDef::ConfigErrors

  .method public hidebysig virtual instance void 
          ResolveReferences() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void ['Assembly-CSharp']Verse.Def::ResolveReferences()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.0
    IL_0008:  ldfld      class PipeSystem.Resource PipeSystem.PipeNetDef::resource
    IL_000d:  ldfld      string PipeSystem.Resource::name
    IL_0012:  callvirt   instance string [mscorlib]System.String::ToLower()
    IL_0017:  stfld      string PipeSystem.PipeNetDef::loweredName
    IL_001c:  ret
  } // end of method PipeNetDef::ResolveReferences

  .method public hidebysig virtual instance void 
          PostLoad() cil managed
  {
    // Code size       37 (0x25)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class PipeSystem.Resource PipeSystem.PipeNetDef::resource
    IL_0006:  ldfld      string PipeSystem.Resource::uiIconPath
    IL_000b:  brfalse.s  IL_001e

    IL_000d:  ldarg.0
    IL_000e:  ldftn      instance void PipeSystem.PipeNetDef::'<PostLoad>b__20_0'()
    IL_0014:  newobj     instance void [mscorlib]System.Action::.ctor(object,
                                                                      native int)
    IL_0019:  call       void ['Assembly-CSharp']Verse.LongEventHandler::ExecuteWhenFinished(class [mscorlib]System.Action)
    IL_001e:  ldarg.0
    IL_001f:  call       instance void ['Assembly-CSharp']Verse.Editable::PostLoad()
    IL_0024:  ret
  } // end of method PipeNetDef::PostLoad

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       96 (0x60)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldtoken    PipeSystem.PipeNet
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  stfld      class [mscorlib]System.Type PipeSystem.PipeNetDef::pipeNetClass
    IL_0010:  ldarg.0
    IL_0011:  ldc.r4     100.
    IL_0016:  stfld      float32 PipeSystem.PipeNetDef::transferAmount
    IL_001b:  ldarg.0
    IL_001c:  ldc.r4     -1.
    IL_0021:  stfld      float32 PipeSystem.PipeNetDef::convertAmount
    IL_0026:  ldarg.0
    IL_0027:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.ThingDef>::.ctor()
    IL_002c:  stfld      class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.ThingDef> PipeSystem.PipeNetDef::alertProofDefs
    IL_0031:  ldarg.0
    IL_0032:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.ThingDef>::.ctor()
    IL_0037:  stfld      class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.ThingDef> PipeSystem.PipeNetDef::noLinkedGraphicDefs
    IL_003c:  ldarg.0
    IL_003d:  ldc.i4.1
    IL_003e:  stfld      bool PipeSystem.PipeNetDef::enableOverflow
    IL_0043:  ldarg.0
    IL_0044:  ldc.r4     2.
    IL_0049:  stfld      float32 PipeSystem.PipeNetDef::productionToOverflowCapacityRatio
    IL_004e:  ldarg.0
    IL_004f:  ldc.r4     1.
    IL_0054:  stfld      float32 PipeSystem.PipeNetDef::overflowAmount
    IL_0059:  ldarg.0
    IL_005a:  call       instance void ['Assembly-CSharp']Verse.Def::.ctor()
    IL_005f:  ret
  } // end of method PipeNetDef::.ctor

  .method private hidebysig instance void 
          '<PostLoad>b__20_0'() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class PipeSystem.Resource PipeSystem.PipeNetDef::resource
    IL_0007:  ldfld      string PipeSystem.Resource::uiIconPath
    IL_000c:  ldc.i4.1
    IL_000d:  call       !0 class ['Assembly-CSharp']Verse.ContentFinder`1<class [UnityEngine.CoreModule]UnityEngine.Texture2D>::Get(string,
                                                                                                                                     bool)
    IL_0012:  stfld      class [UnityEngine.CoreModule]UnityEngine.Texture2D PipeSystem.PipeNetDef::uiIcon
    IL_0017:  ret
  } // end of method PipeNetDef::'<PostLoad>b__20_0'

} // end of class PipeSystem.PipeNetDef

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file F:\Repositories\rimworld_mods\.tmp\ps_def.res
