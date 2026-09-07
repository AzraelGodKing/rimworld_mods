using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Mono.Cecil;
using Xunit;

namespace Azrael.ModChecks
{
    public class Suite
    {
        [Fact]
        public void AllChecksPass()
        {
            var errors = new List<string>();
            string repo = FindRepoRoot();
            Assert.False(string.IsNullOrEmpty(repo), "Could not find repo root (no Common/SafePatchAll.cs).");

            CheckNoDuplicateBootstrap(repo, errors);
            var xmlDefNames = CollectDefNames(repo);
            CheckDefOfsFromSource(repo, xmlDefNames, errors);
            CheckDefNameLiterals(repo, xmlDefNames, errors);
            CheckSeriesRosterFromSource(repo, errors);
            CheckScribeParity(repo, errors);
            CheckKeyedLiterals(repo, errors);

            using (RefPack pack = RefPack.Load(repo))
            {
                CheckHarmonyStringTargets(repo, pack, errors);
                DefXml.CheckFields(repo, pack, errors);
            }

            foreach (string e in errors)
                Console.WriteLine("  " + e);
            Assert.True(errors.Count == 0, errors.Count == 0
                ? ""
                : errors.Count + " ModChecks failure(s):\n  " + string.Join("\n  ", errors));
        }

        public static readonly string[] ModFolders =
        {
            "Azrael", "DateNight", "Deep Colony", "Homesteader", "LivingWorld",
            "Nemesis", "Niceties", "Stormproof", "Strata",
        };

        static readonly string[] OurDefPrefixes =
        {
            "Strata_", "DC_", "HS_", "Stormproof_", "Nemesis_", "DateNight_",
            "LivingWorld_", "Niceties_", "Azrael_", "Homesteader_",
        };

        static readonly Regex HarmonyTyped = new Regex(
            @"HarmonyPatch\s*\(\s*typeof\s*\(\s*([A-Za-z0-9_]+)\s*\)\s*,\s*(?:nameof\s*\(|""([^""]+)"")",
            RegexOptions.Compiled);

        static readonly Regex KeyedLiteral = new Regex(
            @"""([A-Za-z][A-Za-z0-9_]*)""\s*\)?\s*\.Translate\s*\(",
            RegexOptions.Compiled);

        static readonly Regex ConstDefName = new Regex(
            @"const string \w*DefName\w* = ""((?:Strata_|DC_|HS_|Stormproof_|Nemesis_|DateNight_|LivingWorld_|Niceties_|Azrael_|Homesteader_)[A-Za-z0-9_]+)""",
            RegexOptions.Compiled);

        static readonly Regex GetNamedStrict = new Regex(
            @"GetNamed\s*\(\s*""((?:Strata_|DC_|HS_|Stormproof_|Nemesis_|DateNight_|LivingWorld_|Niceties_|Azrael_|Homesteader_)[A-Za-z0-9_]+)""",
            RegexOptions.Compiled);

        static readonly Regex QuotedOurDef = new Regex(
            @"""((?:Strata_|DC_|HS_|Stormproof_|Nemesis_|DateNight_|LivingWorld_|Niceties_|Azrael_|Homesteader_)[A-Za-z0-9_]+)""",
            RegexOptions.Compiled);

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

        static void CheckNoDuplicateBootstrap(string repo, List<string> errors)
        {
            foreach (string pattern in new[] { "HarmonyPatchAll.cs", "SafePatchAll.cs" })
            {
                foreach (string file in Directory.GetFiles(repo, pattern, SearchOption.AllDirectories))
                {
                    string rel = Rel(repo, file).Replace('\\', '/');
                    if (rel.Contains("/obj/") || rel.Contains("/bin/"))
                        continue;
                    if (rel == "Common/SafePatchAll.cs")
                        continue;
                    errors.Add("duplicate patch bootstrap: " + rel);
                }
            }
        }

        static void CheckDefOfsFromSource(string repo, HashSet<string> xmlDefNames, List<string> errors)
        {
            foreach (string folder in ModFolders)
            {
                string src = Path.Combine(repo, folder, "Source");
                if (!Directory.Exists(src))
                    continue;
                foreach (string file in Directory.GetFiles(src, "*.cs", SearchOption.AllDirectories))
                {
                    string text = File.ReadAllText(file);
                    if (!text.Contains("[DefOf]"))
                        continue;
                    foreach (string line in text.Split(new[] { '\n' }, StringSplitOptions.None))
                    {
                        string t = line.Trim();
                        if (!t.StartsWith("public static ", StringComparison.Ordinal) || !t.Contains(";") || t.Contains("("))
                            continue;
                        string left = t.TrimEnd(';');
                        string[] parts = left.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 4)
                            continue;
                        string name = parts[parts.Length - 1];
                        if (xmlDefNames.Contains(name))
                            continue;
                        if (OurDefPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal)))
                            errors.Add("DefOf missing XML defName: " + Rel(repo, file) + " " + name);
                    }
                }
            }
        }

        static void CheckDefNameLiterals(string repo, HashSet<string> xmlDefNames, List<string> errors)
        {
            foreach (string folder in ModFolders)
            {
                string src = Path.Combine(repo, folder, "Source");
                if (!Directory.Exists(src))
                    continue;
                foreach (string file in Directory.GetFiles(src, "*.cs", SearchOption.AllDirectories))
                {
                    if (file.Replace('\\', '/').Contains("/obj/"))
                        continue;
                    string text = File.ReadAllText(file);
                    var seen = new HashSet<string>(StringComparer.Ordinal);
                    foreach (Match m in ConstDefName.Matches(text))
                        seen.Add(m.Groups[1].Value);
                    foreach (Match m in GetNamedStrict.Matches(text))
                        seen.Add(m.Groups[1].Value);
                    int skipAt = text.IndexOf("SkipDefNames", StringComparison.Ordinal);
                    if (skipAt >= 0)
                    {
                        int brace = text.IndexOf('{', skipAt);
                        int end = brace >= 0 ? MatchingBrace(text, brace) : -1;
                        if (end > brace)
                        {
                            foreach (Match m in QuotedOurDef.Matches(text.Substring(brace, end - brace)))
                                seen.Add(m.Groups[1].Value);
                        }
                    }
                    foreach (string name in seen)
                    {
                        if (!xmlDefNames.Contains(name))
                            errors.Add("defName literal missing XML: " + name + " (" + Rel(repo, file) + ")");
                    }
                }
            }
        }

        static void CheckSeriesRosterFromSource(string repo, List<string> errors)
        {
            string file = Path.Combine(repo, "Azrael", "Source", "SeriesHub.cs");
            if (!File.Exists(file))
            {
                errors.Add("missing Azrael/Source/SeriesHub.cs");
                return;
            }
            string text = File.ReadAllText(file);
            foreach (string folder in ModFolders)
            {
                string display = folder == "DateNight" ? "Date Night"
                    : folder == "LivingWorld" ? "Living World"
                    : folder;
                if (text.IndexOf("\"" + display + "\"", StringComparison.Ordinal) < 0
                    && text.IndexOf("\"" + folder + "\"", StringComparison.Ordinal) < 0)
                    errors.Add("SeriesHub missing " + folder);
            }
        }

        static void CheckHarmonyStringTargets(string repo, RefPack pack, List<string> errors)
        {
            foreach (string folder in ModFolders)
            {
                string src = Path.Combine(repo, folder, "Source");
                if (!Directory.Exists(src))
                    continue;
                foreach (string file in Directory.GetFiles(src, "*.cs", SearchOption.AllDirectories))
                {
                    if (file.Replace('\\', '/').Contains("/obj/"))
                        continue;
                    string text = File.ReadAllText(file);
                    foreach (Match m in HarmonyTyped.Matches(text))
                    {
                        if (!m.Groups[2].Success)
                            continue;
                        string typeName = m.Groups[1].Value;
                        string method = m.Groups[2].Value;
                        if (string.IsNullOrWhiteSpace(method))
                        {
                            errors.Add("empty HarmonyPatch method name in " + Rel(repo, file));
                            continue;
                        }
                        if (ClassAllowsSoftTarget(text, m.Index))
                            continue;

                        string look = method;
                        int after = m.Index + m.Length;
                        string window = after < text.Length
                            ? text.Substring(after, Math.Min(80, text.Length - after))
                            : "";
                        if (window.Contains("MethodType.Getter") && !look.StartsWith("get_", StringComparison.Ordinal))
                            look = "get_" + look;
                        else if (window.Contains("MethodType.Setter") && !look.StartsWith("set_", StringComparison.Ordinal))
                            look = "set_" + look;
                        else if (window.Contains("MethodType.Constructor"))
                            look = ".ctor";

                        TypeDefinition type = pack.FindType(typeName);
                        if (type == null)
                        {
                            errors.Add("Harmony type not in ref pack: " + typeName + " (" + Rel(repo, file) + ")");
                            continue;
                        }
                        if (!pack.HasMethod(type, look))
                            errors.Add("Harmony method missing: " + typeName + "." + look + " (" + Rel(repo, file) + ")");
                    }
                }
            }
        }

        static bool ClassAllowsSoftTarget(string text, int index)
        {
            int bestStart = -1;
            int bestEnd = -1;
            var classRx = new Regex(@"\bclass\s+[A-Za-z0-9_]+");
            foreach (Match cm in classRx.Matches(text))
            {
                int brace = text.IndexOf('{', cm.Index);
                if (brace < 0)
                    continue;
                int end = MatchingBrace(text, brace);
                if (end < 0 || index < brace || index > end)
                    continue;
                if (cm.Index > bestStart)
                {
                    bestStart = brace;
                    bestEnd = end;
                }
            }
            if (bestStart < 0)
                return false;
            string body = text.Substring(bestStart, bestEnd - bestStart);
            return body.Contains("TargetMethod(")
                || body.Contains("TargetMethods(")
                || body.Contains("bool Prepare(")
                || body.Contains("HarmonyTargetMethod");
        }

        static int MatchingBrace(string text, int open)
        {
            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '"')
                {
                    i++;
                    while (i < text.Length && text[i] != '"')
                    {
                        if (text[i] == '\\')
                            i++;
                        i++;
                    }
                    continue;
                }
                if (c == '{')
                    depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }
            return -1;
        }

        static void CheckScribeParity(string repo, List<string> errors)
        {
            foreach (string folder in ModFolders)
            {
                string src = Path.Combine(repo, folder, "Source");
                if (!Directory.Exists(src))
                    continue;
                foreach (string file in Directory.GetFiles(src, "*Settings*.cs", SearchOption.AllDirectories))
                {
                    string text = File.ReadAllText(file);
                    if (!text.Contains("ModSettings") || !text.Contains("ExposeData"))
                        continue;
                    foreach (string line in text.Split(new[] { '\n' }, StringSplitOptions.None))
                    {
                        string t = line.Trim();
                        if (!t.StartsWith("public ", StringComparison.Ordinal) || t.Contains("(")
                            || t.Contains("=>") || t.Contains("static ") || t.Contains(" const ")
                            || t.Contains(" event ") || t.Contains(" class "))
                            continue;
                        int eq = t.IndexOf('=');
                        int sc = t.IndexOf(';');
                        if (sc < 0)
                            continue;
                        string left = (eq > 0 && eq < sc ? t.Substring(0, eq) : t.Substring(0, sc)).Trim();
                        string[] parts = left.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 3)
                            continue;
                        string field = parts[parts.Length - 1];
                        if (field == "Settings" || field.Length < 2)
                            continue;
                        if (text.IndexOf("Look(ref " + field, StringComparison.Ordinal) < 0
                            && text.IndexOf("Look(ref this." + field, StringComparison.Ordinal) < 0)
                            errors.Add("Scribe missing Look(ref " + field + ") in " + Rel(repo, file));
                    }
                }
            }
        }

        static void CheckKeyedLiterals(string repo, List<string> errors)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (string folder in ModFolders)
            {
                string keyed = Path.Combine(repo, folder, "Languages", "English", "Keyed");
                if (!Directory.Exists(keyed))
                    continue;
                foreach (string xml in Directory.GetFiles(keyed, "*.xml", SearchOption.AllDirectories))
                {
                    try
                    {
                        foreach (XElement el in XDocument.Load(xml).Root.Elements())
                            keys.Add(el.Name.LocalName);
                    }
                    catch { /* validate_mods.py */ }
                }
            }

            var allow = new HashSet<string>(StringComparer.Ordinal)
            {
                "Attack", "Cancel", "CannotGoNoPath", "OK", "Confirm", "Yes", "No",
                "Report", "Copy", "Close", "Accept", "Reject", "None", "Default",
                "Enabled", "Disabled", "Settings", "Reset", "Save", "Load",
            };

            foreach (string folder in ModFolders)
            {
                string src = Path.Combine(repo, folder, "Source");
                if (!Directory.Exists(src))
                    continue;
                foreach (string file in Directory.GetFiles(src, "*.cs", SearchOption.AllDirectories))
                {
                    if (file.Replace('\\', '/').Contains("/obj/"))
                        continue;
                    string text = File.ReadAllText(file);
                    foreach (Match m in KeyedLiteral.Matches(text))
                    {
                        string key = m.Groups[1].Value;
                        if (key.Length > 2 && key.IndexOf('_') >= 0
                            && !keys.Contains(key) && !allow.Contains(key))
                            errors.Add("Translate() key missing in English Keyed: " + key
                                + " (" + Rel(repo, file) + ")");
                    }
                }
            }
        }

        public static IEnumerable<string> DefFolders(string repo, string folder)
        {
            foreach (string name in new[]
            {
                "Defs", "Odyssey/Defs", "Biotech/Defs", "Royalty/Defs",
                "Ideology/Defs", "Anomaly/Defs",
            })
            {
                string path = Path.Combine(repo, folder, name.Replace('/', Path.DirectorySeparatorChar));
                if (Directory.Exists(path))
                    yield return path;
            }
        }

        static HashSet<string> CollectDefNames(string repo)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (string folder in ModFolders)
            {
                foreach (string defs in DefFolders(repo, folder))
                {
                    foreach (string file in Directory.GetFiles(defs, "*.xml", SearchOption.AllDirectories))
                    {
                        try
                        {
                            XDocument doc = XDocument.Load(file);
                            IEnumerable<XElement> els = doc.Root?.Name.LocalName == "Defs"
                                ? doc.Root.Elements()
                                : (doc.Root != null ? new[] { doc.Root } : Array.Empty<XElement>());
                            foreach (XElement el in els)
                            {
                                string n = (string)el.Element("defName");
                                if (!string.IsNullOrEmpty(n))
                                    names.Add(n);
                            }
                        }
                        catch { /* validate_mods.py */ }
                    }
                }
            }
            return names;
        }

        public static string Rel(string repo, string path)
        {
            return path.StartsWith(repo, StringComparison.OrdinalIgnoreCase)
                ? path.Substring(repo.Length).TrimStart('\\', '/')
                : path;
        }
    }
}
