using System;
using System.IO;
using System.Reflection;
using System.Text;
using RimWorld;
using Verse;

namespace AzraelCommon
{
    /// <summary>
    /// Version letter from the running pack's About.xml + matching changelog.txt.
    /// Always uses this DLL's ModContentPack so a Workshop copy of the same
    /// packageId cannot supply an older version.
    /// </summary>
    internal static class UpdateNews
    {
        internal static void TrySend(
            ref string lastAnnouncedVersion,
            string packageId,
            string changelogUrl,
            string labelKey,
            string fallbackKey,
            string footerKey)
        {
            if (Find.LetterStack == null)
            {
                return;
            }

            ModContentPack pack = PackForExecutingAssembly() ?? PackForPackageId(packageId);
            if (pack?.ModMetaData == null)
            {
                return;
            }

            string version = pack.ModMetaData.ModVersion;
            if (version.NullOrEmpty())
            {
                return;
            }

            if (string.Equals(lastAnnouncedVersion, version, StringComparison.Ordinal))
            {
                return;
            }

            lastAnnouncedVersion = version;

            string raw = ReadMatchingBlock(pack, version);
            string url = changelogUrl;
            string body;
            if (raw.NullOrEmpty())
            {
                body = fallbackKey.Translate(version, url);
            }
            else
            {
                body = StripBbCode(raw);
                if (body.IndexOf("http", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    body = body + "\n\n" + footerKey.Translate(url);
                }
            }

            Find.LetterStack.ReceiveLetter(
                labelKey.Translate(version),
                body,
                LetterDefOf.PositiveEvent);
        }

        internal static ModContentPack PackForExecutingAssembly()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            foreach (ModContentPack pack in LoadedModManager.RunningModsListForReading)
            {
                if (pack.assemblies?.loadedAssemblies == null)
                {
                    continue;
                }

                if (pack.assemblies.loadedAssemblies.Contains(asm))
                {
                    return pack;
                }
            }

            return null;
        }

        private static ModContentPack PackForPackageId(string packageId)
        {
            if (packageId.NullOrEmpty())
            {
                return null;
            }

            foreach (ModContentPack pack in LoadedModManager.RunningModsListForReading)
            {
                ModMetaData meta = pack.ModMetaData;
                if (meta == null)
                {
                    continue;
                }

                if (string.Equals(meta.PackageId, packageId, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(meta.PackageIdNonUnique, packageId, StringComparison.OrdinalIgnoreCase))
                {
                    return pack;
                }
            }

            return null;
        }

        private static string ReadMatchingBlock(ModContentPack pack, string version)
        {
            try
            {
                string path = Path.Combine(pack.RootDir, "About", "changelog.txt");
                if (!File.Exists(path))
                {
                    return null;
                }

                string[] lines = File.ReadAllLines(path);
                string want = version.Trim();
                string unmatched = null;
                bool unmatchedHasVersion = false;
                int i = 0;
                while (i < lines.Length)
                {
                    while (i < lines.Length && lines[i].Trim().Length == 0)
                    {
                        i++;
                    }

                    if (i >= lines.Length)
                    {
                        break;
                    }

                    string header = lines[i].Trim();
                    bool versionHeader = IsVersionLine(header);
                    if (versionHeader)
                    {
                        i++;
                    }

                    StringBuilder sb = new StringBuilder();
                    while (i < lines.Length && !IsVersionLine(lines[i]))
                    {
                        string line = lines[i];
                        i++;
                        if (line.IndexOf("Build stamp", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            continue;
                        }

                        sb.AppendLine(line);
                    }

                    string text = sb.ToString().Trim();
                    if (versionHeader && string.Equals(header, want, StringComparison.Ordinal))
                    {
                        return text.Length == 0 ? null : text;
                    }

                    if (!versionHeader && unmatched == null)
                    {
                        unmatched = text;
                    }
                    else if (versionHeader)
                    {
                        unmatchedHasVersion = true;
                    }
                }

                if (!unmatchedHasVersion && !unmatched.NullOrEmpty())
                {
                    return unmatched;
                }

                return null;
            }
            catch (Exception e)
            {
                Log.Warning("[UpdateNews] could not read changelog.txt: " + e.Message);
                return null;
            }
        }

        private static bool IsVersionLine(string line)
        {
            string t = line.Trim();
            if (t.Length == 0 || t.IndexOf('.') < 0)
            {
                return false;
            }

            for (int i = 0; i < t.Length; i++)
            {
                char c = t[i];
                if (!(char.IsDigit(c) || c == '.'))
                {
                    return false;
                }
            }

            return true;
        }

        private static string StripBbCode(string text)
        {
            string s = text;
            s = s.Replace("[list]", "").Replace("[/list]", "");
            s = s.Replace("[*]", "• ");
            s = s.Replace("[b]", "").Replace("[/b]", "");
            s = s.Replace("[i]", "").Replace("[/i]", "");
            s = s.Replace("[h1]", "").Replace("[/h1]", "");
            s = s.Replace("[h2]", "").Replace("[/h2]", "");
            while (s.Contains("\r\n\r\n\r\n"))
            {
                s = s.Replace("\r\n\r\n\r\n", "\r\n\r\n");
            }

            while (s.Contains("\n\n\n"))
            {
                s = s.Replace("\n\n\n", "\n\n");
            }

            return s.Trim();
        }
    }
}
