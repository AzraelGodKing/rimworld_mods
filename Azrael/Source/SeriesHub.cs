using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Azrael
{
    /// <summary>
    /// Read-only series inventory. Detects by packageId / defs; never hard-requires
    /// a sister assembly. Fail-open: missing mods show "not loaded".
    /// </summary>
    internal static class SeriesHub
    {
        internal struct ModRow
        {
            public string Display;
            public string PackageId;
            public bool Loaded;
            public string Version;
        }

        internal struct BridgeRow
        {
            public string LabelKey;
            public bool Live;
            public string Status;
        }

        internal struct ConflictRow
        {
            public string Name;
            public string Reason;
        }

        internal static readonly string[][] Series =
        {
            new[] { "Azrael", "azraelgodking.Azrael" },
            new[] { "Homesteader", "AzraelGodKing.Homesteader" },
            new[] { "Strata", "AzraelGodKing.Strata" },
            new[] { "Stormproof", "AzraelGodKing.Stormproof" },
            new[] { "Nemesis", "AzraelGodKing.Nemesis" },
            new[] { "Deep Colony", "azraelgodking.DeepColony" },
            new[] { "Living World", "azraelgodking.livingworld" },
            new[] { "Date Night", "azraelgodking.DateNight" },
            new[] { "Niceties", "AzraelGodKing.Niceties" },
        };

        internal static List<ModRow> Mods()
        {
            var rows = new List<ModRow>(Series.Length);
            for (int i = 0; i < Series.Length; i++)
            {
                string display = Series[i][0];
                string packageId = Series[i][1];
                ModMetaData meta = FindActive(packageId);
                rows.Add(new ModRow
                {
                    Display = display,
                    PackageId = packageId,
                    Loaded = meta != null,
                    Version = VersionOf(meta)
                });
            }
            return rows;
        }

        internal static List<BridgeRow> Bridges()
        {
            bool homesteader = IsLoaded("AzraelGodKing.Homesteader");
            bool strata = IsLoaded("AzraelGodKing.Strata");
            bool stormproof = IsLoaded("AzraelGodKing.Stormproof");
            bool nemesis = IsLoaded("AzraelGodKing.Nemesis");
            bool deepColony = IsLoaded("azraelgodking.DeepColony");
            bool livingWorld = IsLoaded("azraelgodking.livingworld");

            bool rootCellar = DefPresent("Homesteader_RootCellar");
            bool wells = DefPresent("Homesteader_HandDugWell") || DefPresent("Wellspring_HandDugWell");
            bool lwSignals = TypePresent("LivingWorld.LivingWorldSignals");
            bool dcConsumer = TypePresent("DeepColony.LivingWorldSoftCompat");

            var rows = new List<BridgeRow>();
            rows.Add(Bridge(
                "Azrael_Hub_Bridge_RootCellar",
                homesteader && strata && rootCellar,
                homesteader,
                strata,
                "Homesteader",
                "Strata"));
            rows.Add(Bridge(
                "Azrael_Hub_Bridge_Wells",
                homesteader && strata && wells,
                homesteader,
                strata,
                "Homesteader",
                "Strata"));
            rows.Add(Bridge(
                "Azrael_Hub_Bridge_StormRooms",
                strata && stormproof,
                strata,
                stormproof,
                "Strata",
                "Stormproof"));
            rows.Add(NemesisDeepBridge(nemesis, deepColony));
            rows.Add(new BridgeRow
            {
                LabelKey = "Azrael_Hub_Bridge_LwGoodwill",
                Live = livingWorld && deepColony && lwSignals && dcConsumer,
                Status = GoodwillStatus(livingWorld, deepColony, lwSignals, dcConsumer)
            });
            return rows;
        }

        internal static List<ConflictRow> Conflicts()
        {
            var rows = new List<ConflictRow>();
            bool strata = IsLoaded("AzraelGodKing.Strata");
            if (strata)
            {
                TryConflict(rows, "astryl.AsAboveSoBelow", "As Above, So Below", "Azrael_Hub_Conflict_Stack");
                TryConflict(rows, "astryl.AsAboveSoBelow2", "As above, So below 2", "Azrael_Hub_Conflict_Stack");
                TryConflict(rows, "telardo.MultiFloors", "MultiFloors", "Azrael_Hub_Conflict_Stack");
                TryConflict(rows, "telardo.MultiFloorsDev", "MultiFloors (dev)", "Azrael_Hub_Conflict_Stack");
            }

            if (IsLoaded("AzraelGodKing.Homesteader") && IsLoaded("AzraelGodKing.Wellspring"))
            {
                rows.Add(new ConflictRow
                {
                    Name = "Wellspring (standalone)",
                    Reason = "Azrael_Hub_Conflict_Wellspring".Translate()
                });
            }

            return rows;
        }

        internal static List<string> HarmonyFailures()
        {
            var hits = new List<string>();
            try
            {
                FieldInfo queueField = AccessTools.Field(typeof(Log), "messageQueue");
                object queue = queueField?.GetValue(null);
                IEnumerable messages = null;
                if (queue != null)
                {
                    messages = queue as IEnumerable;
                    if (messages == null)
                    {
                        FieldInfo listField = AccessTools.Field(queue.GetType(), "messages")
                            ?? AccessTools.Field(queue.GetType(), "Messages");
                        messages = listField?.GetValue(queue) as IEnumerable;
                    }
                }

                if (messages == null)
                {
                    PropertyInfo messagesProp = AccessTools.Property(typeof(Log), "Messages");
                    messages = messagesProp?.GetValue(null, null) as IEnumerable;
                }

                if (messages == null)
                {
                    return hits;
                }

                foreach (object item in messages)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    string text = MessageText(item);
                    if (string.IsNullOrEmpty(text))
                    {
                        continue;
                    }

                    if (text.IndexOf("Harmony patch class", StringComparison.Ordinal) < 0
                        || text.IndexOf(" failed", StringComparison.Ordinal) < 0)
                    {
                        continue;
                    }

                    if (!LooksLikeSeriesLog(text))
                    {
                        continue;
                    }

                    hits.Add(text);
                }
            }
            catch
            {
                // Fail-open: an inaccessible log is not a hub error.
            }

            return hits;
        }

        internal static string ClipboardReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Azrael series hub");
            sb.AppendLine("RimWorld " + RimWorldVersion());

            sb.AppendLine();
            sb.AppendLine("Mods");
            foreach (ModRow row in Mods())
            {
                sb.Append("- ").Append(row.Display).Append(": ");
                sb.Append(row.Loaded ? "loaded" : "not loaded");
                if (row.Loaded)
                {
                    sb.Append(" v").Append(row.Version);
                }
                sb.Append(" (").Append(row.PackageId).AppendLine(")");
            }

            sb.AppendLine();
            sb.AppendLine("Bridges");
            foreach (BridgeRow row in Bridges())
            {
                sb.Append("- ").Append(row.LabelKey.Translate()).Append(": ").AppendLine(row.Status);
            }

            sb.AppendLine();
            sb.AppendLine("Conflicts");
            List<ConflictRow> conflicts = Conflicts();
            if (conflicts.Count == 0)
            {
                sb.AppendLine("- none");
            }
            else
            {
                foreach (ConflictRow row in conflicts)
                {
                    sb.Append("- ").Append(row.Name).Append(": ").AppendLine(row.Reason);
                }
            }

            sb.AppendLine();
            sb.AppendLine("Harmony");
            List<string> fails = HarmonyFailures();
            if (fails.Count == 0)
            {
                sb.AppendLine("- none");
            }
            else
            {
                foreach (string line in fails)
                {
                    sb.Append("- ").AppendLine(line);
                }
            }

            return sb.ToString();
        }

        private static string RimWorldVersion()
        {
            try
            {
                Type type = AccessTools.TypeByName("Verse.VersionControl");
                if (type == null)
                {
                    return "(version unread)";
                }

                PropertyInfo prop = AccessTools.Property(type, "CurrentVersionStringWithRev")
                    ?? AccessTools.Property(type, "CurrentVersionString");
                string value = prop?.GetValue(null, null) as string;
                return string.IsNullOrEmpty(value) ? "(version unread)" : value;
            }
            catch
            {
                return "(version unread)";
            }
        }

        internal static bool IsLoaded(string packageId)
        {
            return FindActive(packageId) != null;
        }

        private static BridgeRow Bridge(
            string labelKey,
            bool live,
            bool leftLoaded,
            bool rightLoaded,
            string leftName,
            string rightName)
        {
            string status;
            if (live)
            {
                status = "Azrael_Hub_BridgeLive".Translate();
            }
            else if (!leftLoaded)
            {
                status = "Azrael_Hub_BridgeWaiting".Translate(leftName);
            }
            else if (!rightLoaded)
            {
                status = "Azrael_Hub_BridgeWaiting".Translate(rightName);
            }
            else
            {
                // Both mods present but the hook def/type is missing.
                status = "Azrael_Hub_BridgeNoHook".Translate();
            }

            return new BridgeRow { LabelKey = labelKey, Live = live, Status = status };
        }

        private static BridgeRow NemesisDeepBridge(bool nemesis, bool deepColony)
        {
            if (nemesis && deepColony)
            {
                return new BridgeRow
                {
                    LabelKey = "Azrael_Hub_Bridge_NemesisDeep",
                    Live = false,
                    Status = "Azrael_Hub_BridgeNoHook".Translate()
                };
            }

            return Bridge(
                "Azrael_Hub_Bridge_NemesisDeep",
                false,
                nemesis,
                deepColony,
                "Nemesis",
                "Deep Colony");
        }

        private static string GoodwillStatus(bool livingWorld, bool deepColony, bool signals, bool consumer)
        {
            if (livingWorld && deepColony && signals && consumer)
            {
                return "Azrael_Hub_BridgeLive".Translate();
            }
            if (!livingWorld)
            {
                return "Azrael_Hub_BridgeWaiting".Translate("Living World");
            }
            if (!deepColony)
            {
                return "Azrael_Hub_BridgeWaiting".Translate("Deep Colony");
            }
            return "Azrael_Hub_BridgeNoHook".Translate();
        }

        private static void TryConflict(List<ConflictRow> rows, string packageId, string name, string reasonKey)
        {
            if (!IsLoaded(packageId))
            {
                return;
            }
            rows.Add(new ConflictRow
            {
                Name = name,
                Reason = reasonKey.Translate()
            });
        }

        private static ModMetaData FindActive(string packageId)
        {
            try
            {
                ModMetaData meta = ModLister.GetActiveModWithIdentifier(packageId, ignorePostfix: true);
                if (meta != null)
                {
                    return meta;
                }
            }
            catch
            {
                // Older GetActiveModWithIdentifier overloads — fall through.
            }

            if (ModsConfig.IsActive(packageId))
            {
                foreach (ModContentPack pack in LoadedModManager.RunningModsListForReading)
                {
                    if (pack?.PackageId != null
                        && (string.Equals(pack.PackageId, packageId, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(pack.PackageIdPlayerFacing, packageId, StringComparison.OrdinalIgnoreCase)))
                    {
                        return pack.ModMetaData;
                    }
                }
            }

            return null;
        }

        private static string VersionOf(ModMetaData meta)
        {
            if (meta == null)
            {
                return "Azrael_Hub_VersionUnknown".Translate();
            }
            string v = meta.ModVersion;
            return string.IsNullOrEmpty(v) ? "Azrael_Hub_VersionUnknown".Translate() : v;
        }

        private static bool DefPresent(string defName)
        {
            return DefDatabase<ThingDef>.GetNamedSilentFail(defName) != null;
        }

        private static bool TypePresent(string fullName)
        {
            try
            {
                return AccessTools.TypeByName(fullName) != null;
            }
            catch
            {
                return false;
            }
        }

        private static string MessageText(object logMessage)
        {
            FieldInfo textField = AccessTools.Field(logMessage.GetType(), "text");
            if (textField != null)
            {
                return textField.GetValue(logMessage) as string;
            }
            PropertyInfo textProp = AccessTools.Property(logMessage.GetType(), "text")
                ?? AccessTools.Property(logMessage.GetType(), "Text");
            return textProp?.GetValue(logMessage, null) as string;
        }

        private static bool LooksLikeSeriesLog(string text)
        {
            return text.IndexOf("[Homesteader]", StringComparison.Ordinal) >= 0
                || text.IndexOf("[Strata]", StringComparison.Ordinal) >= 0
                || text.IndexOf("[Stormproof]", StringComparison.Ordinal) >= 0
                || text.IndexOf("[Nemesis]", StringComparison.Ordinal) >= 0
                || text.IndexOf("[DeepColony]", StringComparison.Ordinal) >= 0
                || text.IndexOf("[LivingWorld]", StringComparison.Ordinal) >= 0
                || text.IndexOf("[DateNight]", StringComparison.Ordinal) >= 0
                || text.IndexOf("[Azrael]", StringComparison.Ordinal) >= 0;
        }

        internal static Color StatusColor(bool ok)
        {
            return ok ? new Color(0.55f, 0.85f, 0.55f) : new Color(0.7f, 0.7f, 0.7f);
        }

        internal static Color ConflictColor => new Color(0.95f, 0.5f, 0.42f);

        internal static Color WaitingColor => new Color(0.9f, 0.78f, 0.45f);
    }
}
