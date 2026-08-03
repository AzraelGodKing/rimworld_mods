using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

var path = args[0];
var typeName = args[1];
var settings = new DecompilerSettings { ThrowOnAssemblyResolveErrors = false };
var decompiler = new CSharpDecompiler(path, settings);
Console.WriteLine(decompiler.DecompileTypeAsString(new FullTypeName(typeName)));
