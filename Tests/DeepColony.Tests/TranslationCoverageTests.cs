using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace DeepColony.Tests
{
    /// <summary>
    /// Every English Keyed key exists in Chinese and Russian (and no stale extras), and every
    /// def with a label has a translated label in Chinese and Russian DefInjected.
    /// </summary>
    public class TranslationCoverageTests
    {
        public static IEnumerable<object[]> Languages() =>
            new[] { "ChineseSimplified", "Russian" }.Select(l => new object[] { l });

        private static string ModDir
        {
            get
            {
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Deep Colony", "Languages")))
                    dir = dir.Parent;
                Assert.NotNull(dir);
                return Path.Combine(dir!.FullName, "Deep Colony");
            }
        }

        private static IEnumerable<XElement> Entries(string folder) =>
            Directory.Exists(folder)
                ? Directory.GetFiles(folder, "*.xml", SearchOption.AllDirectories)
                    .SelectMany(f => XDocument.Load(f).Root!.Elements())
                : Enumerable.Empty<XElement>();

        private static HashSet<string> Keyed(string lang) =>
            new HashSet<string>(Entries(Path.Combine(ModDir, "Languages", lang, "Keyed")).Select(e => e.Name.LocalName));

        [Theory]
        [MemberData(nameof(Languages))]
        public void KeyedMatchesEnglish(string lang)
        {
            HashSet<string> en = Keyed("English");
            HashSet<string> other = Keyed(lang);
            Assert.Empty(en.Except(other).OrderBy(k => k));
            Assert.Empty(other.Except(en).OrderBy(k => k));
        }

        [Theory]
        [MemberData(nameof(Languages))]
        public void EveryLabelledDefIsTranslated(string lang)
        {
            var labelled = Directory.GetFiles(Path.Combine(ModDir, "Defs"), "*.xml", SearchOption.AllDirectories)
                .SelectMany(f => XDocument.Load(f).Root!.Elements())
                .Where(d => (string?)d.Attribute("Abstract") != "True"
                    && d.Element("defName") != null && d.Element("label") != null)
                .Select(d => d.Element("defName")!.Value)
                .ToList();
            var translated = new HashSet<string>(
                Entries(Path.Combine(ModDir, "Languages", lang, "DefInjected"))
                    .Select(e => e.Name.LocalName)
                    .Where(n => n.EndsWith(".label", StringComparison.Ordinal) && n.Count(c => c == '.') == 1)
                    .Select(n => n.Substring(0, n.Length - ".label".Length)));
            Assert.NotEmpty(labelled);
            Assert.Empty(labelled.Where(d => !translated.Contains(d)).OrderBy(d => d));
        }
    }
}
