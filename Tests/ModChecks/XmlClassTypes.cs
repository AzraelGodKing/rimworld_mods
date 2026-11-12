using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Xunit;

namespace Azrael.ModChecks
{
    /// <summary>
    /// XML Class="Mod.Type" must resolve in every versioned DLL that game version
    /// can load. AZR-741: Workshop Stormproof 2.0.1 shipped Patches that name
    /// StatPart_Brownout while both Assemblies folders held the same pre-2.0 DLL
    /// that still used Harmony workbench brownout and lacked the type.
    /// </summary>
    public class XmlClassTypes
    {
        static readonly Regex ClassAttr = new Regex(
            @"Class\s*=\s*""([A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)+)""",
            RegexOptions.Compiled);

        [Fact]
        public void ModXmlClassesExistInEachVersionDll()
        {
            string repo = FindRepoRoot();
            Assert.False(string.IsNullOrEmpty(repo), "Could not find repo root (no Common/SafePatchAll.cs).");

            var errors = new List<string>();
            foreach ((string folder, string assembly) in RefPack.ModAssemblies)
            {
                string modRoot = Path.Combine(repo, folder);
                if (!Directory.Exists(modRoot))
                    continue;

                HashSet<string> needed = CollectOwnClasses(modRoot, assembly);
                if (needed.Count == 0)
                    continue;

                string dll16 = Path.Combine(modRoot, "1.6", "Assemblies", assembly + ".dll");
                string dll17 = Path.Combine(modRoot, "1.7", "Assemblies", assembly + ".dll");
                string dllRoot = Path.Combine(modRoot, "Assemblies", assembly + ".dll");

                var present = new List<(string Label, string Path)>();
                if (File.Exists(dll16))
                    present.Add(("1.6", dll16));
                if (File.Exists(dll17))
                    present.Add(("1.7", dll17));
                if (File.Exists(dllRoot))
                    present.Add(("root", dllRoot));

                if (present.Count == 0)
                {
                    // No built DLL yet (local checkout without build). Fall back to Source.
                    CheckSourceHasTypes(modRoot, assembly, needed, errors);
                    continue;
                }

                if (File.Exists(dll16) && File.Exists(dll17)
                    && FilesEqual(dll16, dll17))
                {
                    errors.Add(folder + ": 1.6 and 1.7 " + assembly
                        + ".dll are byte-identical — each version needs its own build "
                        + "(Workshop AZR-741 shipped one stale DLL in both folders)");
                }

                foreach ((string label, string path) in present)
                    CheckDllHasTypes(folder, label, path, needed, errors);
            }

            Assert.True(errors.Count == 0, errors.Count == 0
                ? ""
                : errors.Count + " XmlClassTypes failure(s):\n  " + string.Join("\n  ", errors));
        }

        static HashSet<string> CollectOwnClasses(string modRoot, string assembly)
        {
            var needed = new HashSet<string>(StringComparer.Ordinal);
            string prefix = assembly + ".";
            foreach (string xml in Directory.GetFiles(modRoot, "*.xml", SearchOption.AllDirectories))
            {
                string norm = xml.Replace('\\', '/');
                if (norm.Contains("/obj/") || norm.Contains("/bin/") || norm.Contains("/Source/"))
                    continue;
                // Languages / About never carry gameplay Class hooks.
                if (norm.Contains("/Languages/") || norm.Contains("/About/"))
                    continue;
                string text;
                try
                {
                    text = File.ReadAllText(xml);
                }
                catch
                {
                    continue;
                }
                foreach (Match m in ClassAttr.Matches(text))
                {
                    string full = m.Groups[1].Value;
                    if (full.StartsWith(prefix, StringComparison.Ordinal))
                        needed.Add(full);
                }
            }
            return needed;
        }

        static void CheckDllHasTypes(
            string folder, string label, string dllPath, HashSet<string> needed, List<string> errors)
        {
            HashSet<string> types;
            try
            {
                types = ReadPublicTypeNames(dllPath);
            }
            catch (Exception ex)
            {
                errors.Add(folder + " " + label + " DLL unreadable: " + ex.Message);
                return;
            }

            foreach (string full in needed.OrderBy(s => s, StringComparer.Ordinal))
            {
                if (!types.Contains(full))
                    errors.Add(folder + " " + label + " DLL missing XML Class type: " + full);
            }
        }

        static void CheckSourceHasTypes(
            string modRoot, string assembly, HashSet<string> needed, List<string> errors)
        {
            string src = Path.Combine(modRoot, "Source");
            if (!Directory.Exists(src))
            {
                errors.Add(Path.GetFileName(modRoot) + ": no Assemblies DLL and no Source/");
                return;
            }

            string joined = string.Join("\n",
                Directory.GetFiles(src, "*.cs", SearchOption.AllDirectories)
                    .Select(File.ReadAllText));

            foreach (string full in needed.OrderBy(s => s, StringComparer.Ordinal))
            {
                string simple = full.Substring(assembly.Length + 1);
                // Nested types use + in IL; XML almost always names top-level types.
                bool found = joined.Contains("class " + simple)
                    || joined.Contains("struct " + simple);
                if (!found)
                    errors.Add(Path.GetFileName(modRoot)
                        + " Source missing type for XML Class: " + full);
            }
        }

        static HashSet<string> ReadPublicTypeNames(string dllPath)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            using (var asm = AssemblyDefinition.ReadAssembly(dllPath))
            {
                foreach (TypeDefinition t in asm.MainModule.Types)
                {
                    if (t.Name == "<Module>" || !t.IsPublic)
                        continue;
                    names.Add(t.FullName.Replace('/', '+'));
                    names.Add(t.FullName.Replace('+', '/'));
                    names.Add(t.FullName);
                }
            }
            return names;
        }

        static bool FilesEqual(string a, string b)
        {
            var fa = new FileInfo(a);
            var fb = new FileInfo(b);
            if (fa.Length != fb.Length)
                return false;
            using (var ha = SHA256.Create())
            using (var hb = SHA256.Create())
            using (FileStream sa = File.OpenRead(a))
            using (FileStream sb = File.OpenRead(b))
            {
                return ha.ComputeHash(sa).SequenceEqual(hb.ComputeHash(sb));
            }
        }

        static string FindRepoRoot()
        {
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 10 && !string.IsNullOrEmpty(dir); i++)
            {
                if (File.Exists(Path.Combine(dir, "Common", "SafePatchAll.cs")))
                    return dir;
                dir = Directory.GetParent(dir)?.FullName;
            }
            string cwd = Directory.GetCurrentDirectory();
            return File.Exists(Path.Combine(cwd, "Common", "SafePatchAll.cs")) ? cwd : null;
        }
    }
}
