using System.Reflection;
using System.Runtime.Loader;
var path = args[0];
var resolver = new PathAssemblyResolver(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.dll").Concat(new[]{typeof(object).Assembly.Location}));
using var mlc = new MetadataLoadContext(resolver);
var asm = mlc.LoadFromAssemblyPath(path);
foreach (var t in asm.GetTypes()) {
  if (t.Name.Contains("Gravship") && !t.Name.Contains("<>"))
    Console.WriteLine(t.FullName);
}
Console.WriteLine("--- methods on Planet.Gravship ---");
var g = asm.GetType("RimWorld.Planet.Gravship") ?? asm.GetType("RimWorld.Gravship");
if (g!=null) {
  Console.WriteLine("TYPE "+g.FullName);
  foreach (var m in g.GetMethods(BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)) {
    if (m.Name.Contains("Bring")||m.Name.Contains("Add")||m.Name.Contains("Thing")||m.Name.Contains("Engine")||m.Name.Contains("Pack")||m.Name.Contains("Copy"))
      Console.WriteLine("  "+m);
  }
  foreach (var p in g.GetProperties(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
    Console.WriteLine("  P "+p.PropertyType.Name+" "+p.Name);
}
Console.WriteLine("--- GravshipUtility ---");
var u = asm.GetType("RimWorld.GravshipUtility");
if (u!=null) foreach (var m in u.GetMethods(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)) {
  if (m.Name.Contains("Generate")||m.Name.Contains("Bring")||m.Name.Contains("Add")||m.Name.Contains("Arrive")||m.Name.Contains("Place"))
    Console.WriteLine("  "+m);
}
