using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Xunit;

namespace Azrael.ModChecks
{
    /// <summary>
    /// AZR-462: [UnsafeAccessor] names are strings the compiler never checks;
    /// a wrong one only fails with MissingFieldException / MissingMethodException
    /// the first time the game calls it. Every accessor in a built 1.7 DLL must
    /// name a field or method that exists on its target type in the 1.7 refs
    /// (Krafs.Rimworld.Ref restored by the Strata.1.7 build).
    /// </summary>
    public class UnsafeAccessorNames
    {
        const string Krafs17 = "1.7.4998-unstable";

        [Fact]
        public void AccessorsNameRealMembers()
        {
            string repo = Suite.FindRepoRoot();
            Assert.False(string.IsNullOrEmpty(repo), "Could not find repo root (no Common/SafePatchAll.cs).");

            var errors = new List<string>();
            ModuleDefinition game = null;
            foreach ((string folder, string assembly) in RefPack.ModAssemblies)
            {
                string dll = Path.Combine(repo, folder, "1.7", "Assemblies", assembly + ".dll");
                if (!File.Exists(dll))
                    continue;

                using ModuleDefinition mod = ModuleDefinition.ReadModule(dll);
                foreach (MethodDefinition m in mod.Types.SelectMany(AllTypes).SelectMany(t => t.Methods))
                {
                    CustomAttribute attr = m.CustomAttributes.FirstOrDefault(a =>
                        a.AttributeType.FullName == "System.Runtime.CompilerServices.UnsafeAccessorAttribute");
                    if (attr == null)
                        continue;

                    if (game == null)
                    {
                        string path = FindGameRef();
                        if (path == null)
                        {
                            errors.Add("Krafs.Rimworld.Ref " + Krafs17
                                + " not restored; build Strata/Source/Strata.1.7.csproj first");
                            break;
                        }
                        game = ModuleDefinition.ReadModule(path);
                    }
                    string where = folder + " " + m.DeclaringType.Name + "." + m.Name;
                    int kind = Convert.ToInt32(attr.ConstructorArguments[0].Value);
                    string name = attr.Properties.FirstOrDefault(p => p.Name == "Name").Argument.Value as string
                        ?? m.Name;
                    if (m.Parameters.Count == 0)
                    {
                        errors.Add(where + ": accessor has no target parameter");
                        continue;
                    }
                    TypeReference target = m.Parameters[0].ParameterType;
                    if (target is ByReferenceType byRef)
                        target = byRef.ElementType;
                    TypeDefinition type = game.GetType(target.FullName);
                    if (type == null)
                    {
                        errors.Add(where + ": target type " + target.FullName + " not in 1.7 Assembly-CSharp");
                        continue;
                    }
                    // UnsafeAccessorKind: 0 Constructor, 1 Method, 2 StaticMethod, 3 Field, 4 StaticField.
                    bool found = kind == 3 || kind == 4
                        ? HasField(type, name)
                        : HasMethod(type, name);
                    if (!found)
                        errors.Add(where + ": " + type.FullName + " has no member '" + name + "' in 1.7");
                }
            }
            game?.Dispose();

            Assert.True(errors.Count == 0, errors.Count == 0
                ? ""
                : errors.Count + " UnsafeAccessorNames failure(s):\n  " + string.Join("\n  ", errors));
        }

        static IEnumerable<TypeDefinition> AllTypes(TypeDefinition t)
        {
            yield return t;
            foreach (TypeDefinition n in t.NestedTypes.SelectMany(AllTypes))
                yield return n;
        }

        // UnsafeAccessor binds to members declared on the target type itself,
        // never inherited ones, so only declared members count.
        static bool HasField(TypeDefinition type, string name) => type.Fields.Any(f => f.Name == name);

        static bool HasMethod(TypeDefinition type, string name) => type.Methods.Any(f => f.Name == name);

        static string FindGameRef()
        {
            foreach (string root in RefPack.NugetRoots())
            {
                string dll = Path.Combine(root, "krafs.rimworld.ref", Krafs17, "ref", "net10.0", "Assembly-CSharp.dll");
                if (File.Exists(dll))
                    return dll;
            }
            return null;
        }
    }
}
