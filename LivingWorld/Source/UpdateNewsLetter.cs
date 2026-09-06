using System;
using System.IO;
using System.Text;
using RimWorld;
using Verse;

namespace LivingWorld
{
    internal static class UpdateNewsLetter
    {
        private const string PackageId = "azraelgodking.livingworld";
        private const string ChangelogUrl =
            "https://github.com/AzraelGodKing/rimworld_mods/blob/main/site/src/data/changelogs/living-world.md";

        internal static void TrySend(ref string lastAnnouncedVersion)
        {
            UpdateNewsCore.TrySend(
                ref lastAnnouncedVersion,
                PackageId,
                ChangelogUrl,
                "LivingWorld_UpdateLetterLabel",
                "LivingWorld_UpdateLetterFallback",
                "LivingWorld_UpdateLetterFooter");
        }
    }

    internal static class UpdateNewsCore
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

            ModMetaData meta = ModLister.GetActiveModWithIdentifier(packageId, ignorePostfix: true);
            if (meta == null)
            {
                return;
            }

            string version = meta.ModVersion;
            if (version.NullOrEmpty())
            {
                return;
            }

            if (string.Equals(lastAnnouncedVersion, version, StringComparison.Ordinal))
            {
                return;
            }

            lastAnnouncedVersion = version;

            string raw = ReadLatestBlock(meta);
            string url = FindUrl(raw) ?? changelogUrl;
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

        private static string ReadLatestBlock(ModMetaData meta)
        {
            try
            {
                string path = Path.Combine(meta.RootDir.FullName, "About", "changelog.txt");
                if (!File.Exists(path))
                {
                    return null;
                }

                string[] lines = File.ReadAllLines(path);
                StringBuilder sb = new StringBuilder();
                bool started = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (!started)
                    {
                        if (line.Trim().Length == 0)
                        {
                            continue;
                        }

                        started = true;
                        if (IsVersionLine(line))
                        {
                            continue;
                        }
                    }
                    else if (IsVersionLine(line))
                    {
                        break;
                    }

                    if (line.IndexOf("Build stamp", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        continue;
                    }

                    sb.AppendLine(line);
                }

                string text = sb.ToString().Trim();
                return text.Length == 0 ? null : text;
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

        private static string FindUrl(string text)
        {
            if (text.NullOrEmpty())
            {
                return null;
            }

            int i = text.IndexOf("https://", StringComparison.OrdinalIgnoreCase);
            if (i < 0)
            {
                return null;
            }

            int end = i;
            while (end < text.Length)
            {
                char c = text[end];
                if (char.IsWhiteSpace(c) || c == '[' || c == ']')
                {
                    break;
                }

                end++;
            }

            return text.Substring(i, end - i).TrimEnd('.', ',');
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
