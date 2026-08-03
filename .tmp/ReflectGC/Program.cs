using System; using System.IO; using System.Linq; using System.Reflection;
class P {
  static void Main() {
    string managed = @"E:\SteamLibrary\steamapps\common\RimWorld\RimWorldWin64_Data\Managed";
    AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += (s,e) => {
      var path = Path.Combine(managed, new AssemblyName(e.Name).Name + ".dll");
      return File.Exists(path) ? Assembly.ReflectionOnlyLoadFrom(path) : null;
    };
    var asm = Assembly.ReflectionOnlyLoadFrom(Path.Combine(managed, "Assembly-CSharp.dll"));
    Type Find(string n) {
      try { return asm.GetTypes().FirstOrDefault(t => t != null && t.Name == n); }
      catch (ReflectionTypeLoadException ex) { return ex.Types.FirstOrDefault(t => t != null && t.Name == n); }
    }
    var flags = BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    var ctx = Find("FloatMenuContext");
    Console.WriteLine("=== FloatMenuContext ===");
    foreach (var c in ctx.GetConstructors(flags)) Console.WriteLine(c);
    foreach (var f in ctx.GetFields(flags)) Console.WriteLine("F "+f.FieldType.Name+" "+f.Name);
    foreach (var p in ctx.GetProperties(flags)) Console.WriteLine("P "+p.PropertyType.Name+" "+p.Name);
    var sel = Find("Selector");
    Console.WriteLine("=== Selector === "+sel.FullName);
    foreach (var m in sel.GetMethods(flags|BindingFlags.DeclaredOnly))
      if (m.Name.IndexOf("Float",StringComparison.OrdinalIgnoreCase)>=0
       || m.Name.IndexOf("Click",StringComparison.OrdinalIgnoreCase)>=0
       || m.Name.IndexOf("Right",StringComparison.OrdinalIgnoreCase)>=0
       || m.Name.IndexOf("Option",StringComparison.OrdinalIgnoreCase)>=0
       || m.Name.IndexOf("Handle",StringComparison.OrdinalIgnoreCase)>=0)
        Console.WriteLine(m);
  }
}
