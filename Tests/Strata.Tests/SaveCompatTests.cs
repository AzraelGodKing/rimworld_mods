using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace Strata.Tests
{
    // A Strata 3.8.x save (the last 1.6 release before Strata 4) must load in
    // 4.0 on 1.7. Saves find their data by field name and by class name, so
    // every name 3.8.3 wrote must still be read somewhere in the source.
    public class SaveCompatTests
    {
        // Names that were renamed or dropped on purpose, each with code that
        // still reads the old name (or ignores it safely). Empty so far.
        private static readonly HashSet<string> Migrated = new HashSet<string>();

        private static readonly Regex ScribeKey =
            new Regex(@"Scribe_\w+\.Look\w*\(\s*(?:ref\s+)?[^,]+,\s*""([^""]+)""");

        private static readonly Regex SavedClass =
            new Regex(@"class\s+(\w+)\s*:\s*(?:MapComponent|GameComponent|WorldComponent|IExposable)\b");

        [Fact]
        public void Every383SavedNameIsStillRead()
        {
            string testDir = TestDir();
            string[] baseline = File.ReadAllLines(Path.Combine(testDir, "SavedKeys-3.8.3.txt"));
            var keys = new HashSet<string>();
            var classes = new HashSet<string>();
            foreach (string file in Directory.EnumerateFiles(
                Path.Combine(testDir, "..", "..", "Strata", "Source"), "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
                {
                    continue;
                }
                string src = File.ReadAllText(file);
                foreach (Match m in ScribeKey.Matches(src))
                {
                    keys.Add(m.Groups[1].Value);
                }
                foreach (Match m in SavedClass.Matches(src))
                {
                    classes.Add(m.Groups[1].Value);
                }
            }

            var missing = new List<string>();
            foreach (string line in baseline.Where(l => l.Length > 0 && l[0] != '#'))
            {
                string[] parts = line.Split(' ');
                bool present = parts[0] == "key" ? keys.Contains(parts[1]) : classes.Contains(parts[1]);
                if (!present && !Migrated.Contains(line))
                {
                    missing.Add(line);
                }
            }
            Assert.True(missing.Count == 0,
                "3.8.3 saves use these names, which the source no longer reads: " + string.Join(", ", missing));
        }

        private static string TestDir([CallerFilePath] string path = "")
        {
            return Path.GetDirectoryName(path);
        }
    }
}
