#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
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
            public string? Stamp;
            /// <summary>Version folder(s) the game loaded for this mod, e.g. "1.7" ("/" = root only).</summary>
            public string? Folder;
            /// <summary>Why this build looks out of step with the series, or null.</summary>
            public string? Mismatch;
        }

        internal struct BridgeRow
        {
            public string LabelKey;
            public bool Live;
            public bool Degraded;
            public string Status;
        }

        internal struct ConflictRow
        {
            public string Name;
            public string Reason;
        }

        internal struct DlcRow
        {
            public string Name;
            public string PackageId;
            public bool Loaded;
        }

        internal readonly struct SeriesMod
        {
            public readonly string Display;
            public readonly string PackageId;

            public SeriesMod(string display, string packageId)
            {
                Display = display;
                PackageId = packageId;
            }
        }

        internal const string AzraelPackageId = "azraelgodking.Azrael";

        // Series versions this Azrael build was tested with (the 2026-11 release wave). An older
        // sister build gets a hub warning so mixed waves are spotted before a bug report.
        private static readonly Dictionary<string, string> TestedWith = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "AzraelGodKing.Homesteader", "2.0.0" },
            { "AzraelGodKing.Strata", "4.0.0" },
            { "AzraelGodKing.Stormproof", "2.0.0" },
            { "AzraelGodKing.Nemesis", "2.0.0" },
            { "azraelgodking.DeepColony", "2.0.0" },
            { "azraelgodking.livingworld", "1.0.0" },
            { "azraelgodking.DateNight", "2.0.0" },
            { "AzraelGodKing.Niceties", "2.0.0" },
        };

        internal static readonly SeriesMod[] Series =
        {
            new SeriesMod("Azrael", AzraelPackageId),
            new SeriesMod("Homesteader", "AzraelGodKing.Homesteader"),
            new SeriesMod("Strata", "AzraelGodKing.Strata"),
            new SeriesMod("Stormproof", "AzraelGodKing.Stormproof"),
            new SeriesMod("Nemesis", "AzraelGodKing.Nemesis"),
            new SeriesMod("Deep Colony", "azraelgodking.DeepColony"),
            new SeriesMod("Living World", "azraelgodking.livingworld"),
            new SeriesMod("Date Night", "azraelgodking.DateNight"),
            new SeriesMod("Niceties", "AzraelGodKing.Niceties"),
        };

        private const string SeriesIdPrefix = "azraelgodking.";

        // Standalone leftovers that the Conflicts section already names; not series rows.
        private static readonly HashSet<string> NotSeries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AzraelGodKing.Wellspring",
        };

        // Rebuilt each time Mod Options opens (AzraelMod calls InvalidateHub) and on Copy report.
        private static bool hubDirty = true;
        private static List<ModRow>? cachedMods;
        private static List<BridgeRow>? cachedBridges;
        private static List<ConflictRow>? cachedConflicts;
        private static List<DlcRow>? cachedDlc;
        private static List<string>? cachedFails;
        private static Roster? cachedRoster;

        internal static void InvalidateHub()
        {
            hubDirty = true;
            cachedRoster = null;
            cachedMods = null;
            cachedBridges = null;
            cachedConflicts = null;
            cachedDlc = null;
            cachedFails = null;
        }

        private static void EnsureHub()
        {
            if (!hubDirty)
            {
                return;
            }

            cachedRoster = BuildRoster();
            cachedMods = BuildMods();
            cachedBridges = BuildBridges();
            cachedConflicts = BuildConflicts();
            cachedDlc = BuildDlc();
            cachedFails = BuildHarmonyFailures();
            hubDirty = false;
        }

        internal static List<ModRow> Mods()
        {
            EnsureHub();
            return cachedMods!;
        }

        internal static List<BridgeRow> Bridges()
        {
            EnsureHub();
            return cachedBridges!;
        }

        internal static List<DlcRow> Dlc()
        {
            EnsureHub();
            return cachedDlc!;
        }

        internal static List<ConflictRow> Conflicts()
        {
            EnsureHub();
            return cachedConflicts!;
        }

        internal static List<string> HarmonyFailures()
        {
            EnsureHub();
            return cachedFails!;
        }

        /// <summary>Series roster with a case-insensitive packageId index.</summary>
        private sealed class Roster
        {
            internal readonly List<SeriesMod> Mods = new List<SeriesMod>();
            private readonly Dictionary<string, int> index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            internal bool Add(SeriesMod mod)
            {
                if (index.ContainsKey(mod.PackageId))
                {
                    return false;
                }
                index[mod.PackageId] = Mods.Count;
                Mods.Add(mod);
                return true;
            }

            internal int IndexOf(string? packageId)
            {
                return packageId != null && index.TryGetValue(packageId, out int i) ? i : -1;
            }
        }

        /// <summary>
        /// Canonical <see cref="Series"/> first (stable order, shows "not loaded" rows),
        /// then any other active azraelgodking.* mod so a new sister mod appears without a hub edit.
        /// </summary>
        private static Roster BuildRoster()
        {
            var roster = new Roster();
            foreach (SeriesMod mod in Series)
            {
                roster.Add(mod);
            }
            try
            {
                foreach (ModMetaData meta in ModsConfig.ActiveModsInLoadOrder)
                {
                    string? id = meta?.PackageIdPlayerFacing ?? meta?.PackageId;
                    if (meta == null
                        || id == null
                        || id.Length == 0
                        || !id.StartsWith(SeriesIdPrefix, StringComparison.OrdinalIgnoreCase)
                        || NotSeries.Contains(id))
                    {
                        continue;
                    }
                    string display = string.IsNullOrEmpty(meta.Name) ? id.Substring(SeriesIdPrefix.Length) : meta.Name;
                    roster.Add(new SeriesMod(display, id));
                }
            }
            catch
            {
                // Fail-open: canonical roster only.
            }
            return roster;
        }

        private static List<ModRow> BuildMods()
        {
            List<SeriesMod> roster = (cachedRoster ?? BuildRoster()).Mods;
            var rows = new List<ModRow>(roster.Count);
            foreach (SeriesMod mod in roster)
            {
                string display = mod.Display;
                string packageId = mod.PackageId;
                ModMetaData? meta = FindActive(packageId);
                rows.Add(new ModRow
                {
                    Display = display,
                    PackageId = packageId,
                    Loaded = meta != null,
                    Version = VersionOf(meta),
                    Stamp = meta != null ? StampOf(display) : null,
                    Folder = meta != null ? LoadFolderOf(packageId) : null,
                    Mismatch = meta != null ? MismatchOf(packageId, meta) : null,
                });
            }
            return rows;
        }

        private static List<BridgeRow> BuildBridges()
        {
            bool homesteader = IsLoaded("AzraelGodKing.Homesteader");
            bool strata = IsLoaded("AzraelGodKing.Strata");
            bool stormproof = IsLoaded("AzraelGodKing.Stormproof");
            bool nemesis = IsLoaded("AzraelGodKing.Nemesis");
            bool deepColony = IsLoaded("azraelgodking.DeepColony");
            bool livingWorld = IsLoaded("azraelgodking.livingworld");

            bool rootCellar = DefPresent("Homesteader_RootCellar");
            bool wells = DefPresent("Wellspring_HandDugWell");
            bool lwSignals = TypePresent("LivingWorld.LivingWorldSignals");
            // Deep Colony registers in its [StaticConstructorOnStartup]; never force it from here.
            bool dcConsumer = TypePresent("DeepColony.LivingWorldSoftCompat")
                && SisterContracts.LivingWorldHandlerRegistered("DeepColony.LivingWorldSoftCompat") != false;

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
                Degraded = livingWorld && deepColony && !(lwSignals && dcConsumer),
                Status = GoodwillStatus(livingWorld, deepColony, lwSignals, dcConsumer)
            });
            rows.Add(DubsBridge(
                "Azrael_Hub_Bridge_DubsHomestead",
                homesteader,
                "Homesteader",
                "DubsBadHygiene.CompProperties_Pipe"));
            rows.Add(DubsBridge(
                "Azrael_Hub_Bridge_DubsStrata",
                strata,
                "Strata",
                "DubsBadHygiene.PlumbingNet"));
            return rows;
        }

        private static List<DlcRow> BuildDlc()
        {
            return new List<DlcRow>
            {
                DlcItem("Royalty", "Ludeon.RimWorld.Royalty"),
                DlcItem("Ideology", "Ludeon.RimWorld.Ideology"),
                DlcItem("Biotech", "Ludeon.RimWorld.Biotech"),
                DlcItem("Anomaly", "Ludeon.RimWorld.Anomaly"),
                DlcItem("Odyssey", "Ludeon.RimWorld.Odyssey"),
            };
        }

        private static List<ConflictRow> BuildConflicts()
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

            TryLoadOrderConflict(rows);
            TryConflict(rows, "rwmt.Multiplayer", "Multiplayer", "Azrael_Hub_Conflict_Multiplayer");
            return rows;
        }

        private static List<string> BuildHarmonyFailures()
        {
            var hits = new List<string>();
            try
            {
                foreach (PatchFailureSink.Row row in PatchFailureSink.Snapshot())
                {
                    string line = (row.Prefix + " " + row.ClassName).Trim();
                    if (!string.IsNullOrEmpty(row.Reason))
                    {
                        line += " — " + row.Reason;
                    }
                    hits.Add(line);
                }
            }
            catch
            {
                // Fail-open: a missing sink is not a hub error.
            }

            return hits;
        }

        internal static string ClipboardReport()
        {
            // Fresh scan for support paste; GUI keeps the per-open cache.
            InvalidateHub();
            var sb = new StringBuilder();
            sb.AppendLine("Azrael series hub");
            sb.AppendLine("RimWorld " + RimWorldVersion());
            sb.AppendLine("Runtime " + RuntimeDescription());

            sb.AppendLine();
            sb.AppendLine("Mods");
            foreach (ModRow row in Mods())
            {
                sb.Append("- ").Append(row.Display).Append(": ");
                sb.Append(row.Loaded ? "loaded" : "not loaded");
                if (row.Loaded)
                {
                    sb.Append(" v").Append(row.Version);
                    if (!string.IsNullOrEmpty(row.Stamp))
                    {
                        sb.Append(" [").Append(row.Stamp).Append("]");
                    }
                    if (!string.IsNullOrEmpty(row.Folder))
                    {
                        sb.Append(" folder ").Append(row.Folder);
                    }
                }
                sb.Append(" (").Append(row.PackageId).AppendLine(")");
                if (row.Mismatch != null)
                {
                    sb.Append("  ! ").AppendLine(row.Mismatch);
                }
            }

            sb.AppendLine();
            sb.AppendLine("DLC");
            foreach (DlcRow row in Dlc())
            {
                sb.Append("- ").Append(row.Name).Append(": ");
                sb.AppendLine(row.Loaded ? "loaded" : "not loaded");
            }

            sb.AppendLine();
            sb.AppendLine("Bridges (" + BridgeSummary(Bridges()) + ")");
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
            sb.AppendLine("Failed patches");
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
                string value = VersionControl.CurrentVersionStringWithRev;
                return string.IsNullOrEmpty(value) ? "(version unread)" : value;
            }
            catch
            {
                return "(version unread)";
            }
        }

        /// <summary>CLR and engine, e.g. ".NET 10.0.0 (Waylith)" on 1.7 or "Mono ... (Unity 2022.3...)" on 1.6.</summary>
        internal static string RuntimeDescription()
        {
            string clr;
            try
            {
                clr = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            }
            catch
            {
                clr = Environment.Version.ToString();
            }
#if RIMWORLD_1_7
            const string engine = "Waylith";
#else
            string engine = "Unity " + Application.unityVersion;
#endif
            return clr + " (" + engine + ")";
        }

        /// <summary>"5 of 7 live" style count for the soft-compat section.</summary>
        internal static string BridgeSummary(List<BridgeRow> bridges)
        {
            int live = 0;
            int degraded = 0;
            foreach (BridgeRow row in bridges)
            {
                if (row.Live)
                {
                    live++;
                }
                else if (row.Degraded)
                {
                    degraded++;
                }
            }
            return degraded > 0
                ? "Azrael_Hub_BridgeSummaryBroken".Translate(live, bridges.Count, degraded)
                : "Azrael_Hub_BridgeSummary".Translate(live, bridges.Count);
        }

        private static ModContentPack? RunningPack(string packageId)
        {
            foreach (ModContentPack pack in LoadedModManager.RunningModsListForReading)
            {
                if (pack != null
                    && (string.Equals(pack.PackageId, packageId, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(pack.PackageIdPlayerFacing, packageId, StringComparison.OrdinalIgnoreCase)))
                {
                    return pack;
                }
            }
            return null;
        }

        /// <summary>Loaded folders other than the root, relative to the mod, e.g. "1.7"; "/" when only the root loads.</summary>
        private static string? LoadFolderOf(string packageId)
        {
            try
            {
                ModContentPack? pack = RunningPack(packageId);
                List<string>? folders = pack?.foldersToLoadDescendingOrder;
                if (pack == null || folders == null)
                {
                    return null;
                }
                string root = System.IO.Path.GetFullPath(pack.RootDir).TrimEnd('/', '\\');
                var names = new List<string>();
                foreach (string folder in folders)
                {
                    string full = System.IO.Path.GetFullPath(folder).TrimEnd('/', '\\');
                    if (full.Length > root.Length && full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    {
                        names.Add(full.Substring(root.Length + 1).Replace('\\', '/'));
                    }
                }
                return names.Count == 0 ? "/" : string.Join(", ", names);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Series-version mismatch (AZR-688): the mod does not declare the running game version,
        /// or it is older than the build this Azrael was tested with.
        /// </summary>
        private static string? MismatchOf(string packageId, ModMetaData meta)
        {
            try
            {
                var game = new Version(VersionControl.CurrentMajor, VersionControl.CurrentMinor);
                List<Version>? supported = meta.SupportedVersionsReadOnly;
                if (supported != null && supported.Count > 0 && !supported.Exists(v => v.Major == game.Major && v.Minor == game.Minor))
                {
                    return "Azrael_Hub_Mismatch_Game".Translate(game.ToString(2));
                }
                if (TestedWith.TryGetValue(packageId, out string? tested)
                    && TryParseVersion(meta.ModVersion, out Version? have)
                    && TryParseVersion(tested, out Version? want)
                    && have! < want!)
                {
                    return "Azrael_Hub_Mismatch_Older".Translate(tested);
                }
            }
            catch
            {
                // Fail-open: no warning.
            }
            return null;
        }

        private static bool TryParseVersion(string? text, out Version? version)
        {
            version = null;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            int cut = text!.IndexOfAny(new[] { '-', '+', ' ' });
            return Version.TryParse(cut > 0 ? text.Substring(0, cut) : text, out version);
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

            return new BridgeRow
            {
                LabelKey = labelKey,
                Live = live,
                Degraded = !live && leftLoaded && rightLoaded,
                Status = status
            };
        }

        private static BridgeRow NemesisDeepBridge(bool nemesis, bool deepColony)
        {
            // Live hook: Nemesis.SoftCompat.OfferEpitaphToDeepColony → DeepColony familyLetters.
            bool hook = MethodPresent("Nemesis.SoftCompat", "OfferEpitaphToDeepColony")
                && TypePresent("DeepColony.GameComp_DeepColony")
                && TypePresent("DeepColony.FamilyLetterEntry");
            return Bridge(
                "Azrael_Hub_Bridge_NemesisDeep",
                nemesis && deepColony && hook,
                nemesis,
                deepColony,
                "Nemesis",
                "Deep Colony");
        }

        private static void TryLoadOrderConflict(List<ConflictRow> rows)
        {
            int azraelIndex = -1;
            int lastSeriesAfter = -1;
            string? lastSeriesName = null;
            List<ModContentPack> running = LoadedModManager.RunningModsListForReading;
            for (int i = 0; i < running.Count; i++)
            {
                ModContentPack pack = running[i];
                if (pack == null)
                {
                    continue;
                }

                if (!IsSeriesPack(pack, out bool isAzrael, out string display))
                {
                    continue;
                }

                if (isAzrael)
                {
                    azraelIndex = i;
                }
                else if (azraelIndex >= 0 && i > azraelIndex)
                {
                    lastSeriesAfter = i;
                    lastSeriesName = display;
                }
            }

            if (azraelIndex >= 0 && lastSeriesAfter > azraelIndex)
            {
                rows.Add(new ConflictRow
                {
                    Name = "Azrael",
                    Reason = "Azrael_Hub_Conflict_LoadOrder".Translate(lastSeriesName ?? "series mod")
                });
            }
        }

        private static bool IsSeriesPack(ModContentPack pack, out bool isAzrael, out string display)
        {
            Roster roster = cachedRoster ?? BuildRoster();
            int i = roster.IndexOf(pack.PackageId);
            if (i < 0)
            {
                i = roster.IndexOf(pack.PackageIdPlayerFacing);
            }
            isAzrael = i == 0;
            display = i >= 0 ? roster.Mods[i].Display : pack.Name;
            return i >= 0;
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

        private static ModMetaData? FindActive(string packageId)
        {
            try
            {
                ModMetaData? meta = ModLister.GetActiveModWithIdentifier(packageId, ignorePostfix: true);
                if (meta != null)
                {
                    return meta;
                }
            }
            catch
            {
                // Older GetActiveModWithIdentifier overloads — fall through.
            }

            return ModsConfig.IsActive(packageId) ? RunningPack(packageId)?.ModMetaData : null;
        }

        private static string VersionOf(ModMetaData? meta)
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

        private static bool MethodPresent(string typeName, string methodName)
        {
            try
            {
                Type type = AccessTools.TypeByName(typeName);
                return type != null && AccessTools.Method(type, methodName) != null;
            }
            catch
            {
                return false;
            }
        }

        private static DlcRow DlcItem(string name, string packageId)
        {
            return new DlcRow
            {
                Name = name,
                PackageId = packageId,
                Loaded = IsLoaded(packageId)
            };
        }

        private static BridgeRow DubsBridge(string labelKey, bool seriesLoaded, string seriesName, string hookType)
        {
            bool dubs = IsLoaded("Dubwise.DubsBadHygiene");
            bool hook = TypePresent(hookType);
            bool live = seriesLoaded && dubs && hook;
            string status;
            if (live)
            {
                status = "Azrael_Hub_BridgeLive".Translate();
            }
            else if (!seriesLoaded)
            {
                status = "Azrael_Hub_BridgeWaiting".Translate(seriesName);
            }
            else if (!dubs)
            {
                status = "Azrael_Hub_BridgeWaiting".Translate("Dubs Bad Hygiene");
            }
            else
            {
                status = "Azrael_Hub_BridgeNoType".Translate(hookType);
            }

            return new BridgeRow
            {
                LabelKey = labelKey,
                Live = live,
                Degraded = seriesLoaded && dubs && !hook,
                Status = status
            };
        }

        /// <summary>
        /// Series convention: namespace X exposes X.XBuildInfo.BuildStamp (StrataBuildInfo,
        /// DeepColonyBuildInfo, AzraelBuildInfo). Mods without a typed stamp stay blank.
        /// </summary>
        private static string? StampOf(string display)
        {
            string ns = NamespaceFor(display);
            return PublicConst(ns + "." + ns + "BuildInfo", "BuildStamp");
        }

        private static string NamespaceFor(string display)
        {
            return (display ?? string.Empty).Replace(" ", string.Empty);
        }

        private static string? PublicConst(string typeName, string fieldName)
        {
            try
            {
                Type? type = AccessTools.TypeByName(typeName);
                FieldInfo? field = type == null
                    ? null
                    : AccessTools.Field(type, fieldName);
                return field?.GetValue(null) as string;
            }
            catch
            {
                return null;
            }
        }

        internal static Color StatusColor(bool ok)
        {
            return ok ? new Color(0.55f, 0.85f, 0.55f) : new Color(0.7f, 0.7f, 0.7f);
        }

        internal static Color ConflictColor => new Color(0.95f, 0.5f, 0.42f);

        internal static Color WaitingColor => new Color(0.9f, 0.78f, 0.45f);
    }
}
