using System;
using System.IO;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

class P {
  static int Main(string[] args) {
    var pe = new PEFile(args[0]);
    var resolver = new UniversalAssemblyResolver(args[0], false, pe.Metadata.DetectTargetFrameworkId());
    var decompiler = new CSharpDecompiler(pe, resolver, new DecompilerSettings());
    var name = args[1];
    try {
      var text = decompiler.DecompileTypeAsString(new FullTypeName(name));
      File.WriteAllText(args[2], text);
      Console.WriteLine("OK " + name + " len=" + text.Length);
    } catch (Exception ex) {
      Console.WriteLine("FAIL " + name + ": " + ex);
      // list matching type names
      var ts = new DecompilerTypeSystem(pe, resolver);
      foreach (var t in ts.GetAllTypeDefinitions()) {
        if (t.Name.IndexOf("Lovin", StringComparison.OrdinalIgnoreCase) >= 0 || t.Name.IndexOf("ChancePerHour", StringComparison.OrdinalIgnoreCase) >= 0)
          Console.WriteLine("TYPE " + t.FullName);
      }
    }
    return 0;
  }
}
