
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.8.3928.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.


// warning : THIS IS A PARTIAL DISASSEMBLY, NOT SUITABLE FOR RE-ASSEMBLING



.class public auto ansi beforefieldinit DubsBadHygiene.CompProperties_WaterStorage
       extends ['Assembly-CSharp']Verse.CompProperties
{
  .field public float32 WaterStorageCap
  .field public float32 AutoGenRate
  .field public int32 Tickrate
  .field public bool Auto
  .field public bool AutoOnRain
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       42 (0x2a)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.r4     10000.
    IL_0006:  stfld      float32 DubsBadHygiene.CompProperties_WaterStorage::WaterStorageCap
    IL_000b:  ldarg.0
    IL_000c:  ldc.i4.s   60
    IL_000e:  stfld      int32 DubsBadHygiene.CompProperties_WaterStorage::Tickrate
    IL_0013:  ldarg.0
    IL_0014:  call       instance void ['Assembly-CSharp']Verse.CompProperties::.ctor()
    IL_0019:  ldarg.0
    IL_001a:  ldtoken    DubsBadHygiene.CompWaterStorage
    IL_001f:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0024:  stfld      class [mscorlib]System.Type ['Assembly-CSharp']Verse.CompProperties::compClass
    IL_0029:  ret
  } // end of method CompProperties_WaterStorage::.ctor

} // end of class DubsBadHygiene.CompProperties_WaterStorage

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file F:\Repositories\rimworld_mods\.tmp\dbh_storage_props.res
