using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Mono.Cecil;

namespace Azrael.ModChecks
{
    /// <summary>
    /// Reads Krafs ref assemblies and built mod DLLs with Cecil (do not
    /// Assembly.LoadFrom ref packs — they throw at runtime).
    /// </summary>
    sealed class RefPack : IDisposable
    {
        readonly List<AssemblyDefinition> _held = new List<AssemblyDefinition>();
        readonly Dictionary<string, TypeDefinition> _byFull =
            new Dictionary<string, TypeDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, TypeDefinition> _byName =
            new Dictionary<string, TypeDefinition>(StringComparer.Ordinal);

        public static readonly (string Folder, string Assembly)[] ModAssemblies =
        {
            ("Azrael", "Azrael"),
            ("DateNight", "DateNight"),
            ("Deep Colony", "DeepColony"),
            ("Homesteader", "Homesteader"),
            ("LivingWorld", "LivingWorld"),
            ("Nemesis", "Nemesis"),
            ("Niceties", "Niceties"),
            ("Stormproof", "Stormproof"),
            ("Strata", "Strata"),
        };

        public static RefPack Load(string repo)
        {
            var pack = new RefPack();
            string csharp = FindAssemblyCSharp();
            if (csharp == null)
            {
                throw new InvalidOperationException(
                    "Krafs Assembly-CSharp.dll not found. Restore Krafs.Rimworld.Ref 1.6.4871.");
            }

            string refDir = Path.GetDirectoryName(csharp);
            foreach (string dll in Directory.GetFiles(refDir, "*.dll"))
                pack.TryAdd(dll);

            foreach ((string folder, string asm) in ModAssemblies)
            {
                string path = Path.Combine(repo, folder, "Assemblies", asm + ".dll");
                if (File.Exists(path))
                    pack.TryAdd(path);
            }

            return pack;
        }

        public TypeDefinition FindType(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            if (name.IndexOf('.') >= 0)
            {
                if (_byFull.TryGetValue(name, out TypeDefinition full))
                    return full;
                int last = name.LastIndexOf('.');
                return FindType(name.Substring(last + 1));
            }

            return _byName.TryGetValue(name, out TypeDefinition simple) ? simple : null;
        }

        public TypeDefinition Resolve(TypeReference tr)
        {
            if (tr == null)
                return null;
            if (tr is TypeDefinition td)
                return td;
            if (_byFull.TryGetValue(tr.FullName, out TypeDefinition byFull))
                return byFull;
            return FindType(tr.Name);
        }

        public bool HasMethod(TypeDefinition type, string name)
        {
            if (type == null || string.IsNullOrEmpty(name))
                return false;
            for (TypeDefinition t = type; t != null; t = Resolve(t.BaseType))
            {
                foreach (MethodDefinition m in t.Methods)
                {
                    if (m.Name == name)
                        return true;
                }
            }
            return false;
        }

        public bool TryGetField(TypeDefinition type, string name, out TypeReference fieldType)
        {
            fieldType = null;
            if (type == null || string.IsNullOrEmpty(name))
                return false;
            FieldDefinition fuzzyField = null;
            PropertyDefinition fuzzyProp = null;
            for (TypeDefinition t = type; t != null; t = Resolve(t.BaseType))
            {
                foreach (FieldDefinition f in t.Fields)
                {
                    if (f.IsStatic)
                        continue;
                    if (f.Name == name)
                    {
                        fieldType = f.FieldType;
                        return true;
                    }
                    if (fuzzyField == null && string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase))
                        fuzzyField = f;
                }

                foreach (PropertyDefinition p in t.Properties)
                {
                    if (p.GetMethod == null || p.GetMethod.IsStatic)
                        continue;
                    if (p.Name == name)
                    {
                        fieldType = p.PropertyType;
                        return true;
                    }
                    if (fuzzyProp == null && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                        fuzzyProp = p;
                }
            }

            if (fuzzyField != null)
            {
                fieldType = fuzzyField.FieldType;
                return true;
            }
            if (fuzzyProp != null)
            {
                fieldType = fuzzyProp.PropertyType;
                return true;
            }
            return false;
        }

        public TypeReference ListElementType(TypeReference tr)
        {
            if (tr is ArrayType at)
                return at.ElementType;
            if (tr is GenericInstanceType git
                && git.ElementType != null
                && (git.ElementType.Name == "List`1" || git.ElementType.Name == "HashSet`1"))
            {
                return git.GenericArguments.Count > 0 ? git.GenericArguments[0] : null;
            }
            return null;
        }

        public bool IsListLike(TypeReference tr) => ListElementType(tr) != null;

        void TryAdd(string path)
        {
            if (!File.Exists(path))
                return;
            try
            {
                Add(path);
            }
            catch
            {
                /* skip unreadable companion refs */
            }
        }

        void Add(string path)
        {
            var rp = new ReaderParameters
            {
                ReadWrite = false,
                InMemory = true,
                ReadingMode = ReadingMode.Deferred,
            };
            AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(path, rp);
            _held.Add(asm);
            foreach (ModuleDefinition mod in asm.Modules)
            {
                foreach (TypeDefinition type in mod.Types)
                    Index(type);
            }
        }

        void Index(TypeDefinition type)
        {
            if (type == null || type.Name.StartsWith("<", StringComparison.Ordinal))
                return;
            if (!_byFull.ContainsKey(type.FullName))
                _byFull[type.FullName] = type;
            if (!_byName.ContainsKey(type.Name))
                _byName[type.Name] = type;
            foreach (TypeDefinition nested in type.NestedTypes)
                Index(nested);
        }

        static string FindAssemblyCSharp()
        {
            string beside = Path.Combine(AppContext.BaseDirectory, "krafs-ref", "Assembly-CSharp.dll");
            if (File.Exists(beside))
                return beside;

            string version = "1.6.4871";
            foreach (string root in NugetRoots())
            {
                string dir = Path.Combine(root, "krafs.rimworld.ref", version);
                string dll = Path.Combine(dir, "ref", "net472", "Assembly-CSharp.dll");
                if (File.Exists(dll))
                    return dll;

                string nupkg = Path.Combine(dir, "krafs.rimworld.ref." + version + ".nupkg");
                if (!File.Exists(nupkg))
                    continue;
                string extract = Path.Combine(Path.GetTempPath(), "azrael-krafs-ref-" + version);
                dll = Path.Combine(extract, "ref", "net472", "Assembly-CSharp.dll");
                if (!File.Exists(dll))
                {
                    if (Directory.Exists(extract))
                        Directory.Delete(extract, true);
                    ZipFile.ExtractToDirectory(nupkg, extract);
                }
                if (File.Exists(dll))
                    return dll;
            }

            return null;
        }

        static IEnumerable<string> NugetRoots()
        {
            string env = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
            if (!string.IsNullOrEmpty(env))
                yield return env;
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(home))
                yield return Path.Combine(home, ".nuget", "packages");
        }

        public void Dispose()
        {
            foreach (AssemblyDefinition asm in _held)
                asm.Dispose();
            _held.Clear();
        }
    }
}
