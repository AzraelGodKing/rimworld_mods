using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;
var path = @"E:\SteamLibrary\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll";
var decompiler = new CSharpDecompiler(path, new DecompilerSettings { ThrowOnAssemblyResolveErrors = false });
foreach (var name in new[]{"RimWorld.Planet.PositionData", "RimWorld.PositionData"}) {
  try { Console.WriteLine(decompiler.DecompileTypeAsString(new FullTypeName(name))); }
  catch (Exception e) { Console.WriteLine(name + ": " + e.Message); }
}
