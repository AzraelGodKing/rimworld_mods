using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using HarmonyLib;
using Verse;

namespace Strata
{
    // AZR-463 / AZR-471: opt-in timing of Strata's own code, so a baseline can
    // be taken on the same save on 1.6 and 1.7 without Dubs Performance
    // Analyzer. Started from Mod options > Diagnostics. While it runs, every
    // Strata component tick/update and every Strata Harmony prefix, postfix and
    // finalizer gets a timing prefix and postfix; nothing is patched while it
    // is off. Stopping writes a table to Player.log and Logs/StrataPerf.log.
    // Times are inclusive (a method that calls another timed one counts both).
    // Very small patch methods the JIT has already inlined into the patched
    // vanilla method may not show up.
    public static class StrataPerfProbe
    {
        private const string HarmonyId = "azraelgodking.strata.perfprobe";

        private static readonly string[] ComponentMethods =
        {
            "MapComponentTick", "MapComponentUpdate", "MapComponentOnGUI",
            "GameComponentTick", "GameComponentUpdate", "GameComponentOnGUI",
            "WorldComponentTick", "WorldComponentUpdate",
        };

        private sealed class Slot
        {
            public string label;
            public long calls;
            public long ticks;
            public long maxTicks;
        }

        private static Harmony harmony;
        private static Dictionary<MethodBase, Slot> slots;
        private static long startTimestamp;
        private static int startGameTick;
        private static int skipped;

        public static bool Running => harmony != null;

        public static int MethodCount => slots?.Count ?? 0;

        public static void Start()
        {
            if (Running)
            {
                return;
            }
            var targets = new Dictionary<MethodBase, Slot>();
            Assembly strata = typeof(StrataMod).Assembly;
            foreach (Type type in AccessTools.GetTypesFromAssembly(strata))
            {
                if (type.IsAbstract || type.ContainsGenericParameters)
                {
                    continue;
                }
                for (int i = 0; i < ComponentMethods.Length; i++)
                {
                    MethodInfo m = type.GetMethod(
                        ComponentMethods[i],
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                    if (m != null && m.GetMethodBody() != null)
                    {
                        targets[m] = new Slot { label = type.Name + "." + m.Name };
                    }
                }
            }

            foreach (MethodBase original in Harmony.GetAllPatchedMethods())
            {
                Patches info = Harmony.GetPatchInfo(original);
                if (info == null)
                {
                    continue;
                }
                AddPatchMethods(targets, info.Prefixes, original, "prefix");
                AddPatchMethods(targets, info.Postfixes, original, "postfix");
                AddPatchMethods(targets, info.Finalizers, original, "finalizer");
            }

            slots = targets;
            skipped = 0;
            harmony = new Harmony(HarmonyId);
            var prefix = new HarmonyMethod(typeof(StrataPerfProbe), nameof(Before)) { priority = Priority.First };
            var postfix = new HarmonyMethod(typeof(StrataPerfProbe), nameof(After)) { priority = Priority.Last };
            foreach (MethodBase m in targets.Keys)
            {
                try
                {
                    harmony.Patch(m, prefix: prefix, postfix: postfix);
                }
                catch (Exception ex)
                {
                    skipped++;
                    StrataLog.Verbose("[Strata] Perf probe skipped " + m.DeclaringType?.Name + "." + m.Name + ": " + ex.Message);
                }
            }
            startTimestamp = Stopwatch.GetTimestamp();
            startGameTick = Find.TickManager?.TicksGame ?? 0;
            Log.Message("[Strata] Perf probe started on " + (targets.Count - skipped) + " methods.");
        }

        public static void StopAndReport()
        {
            if (!Running)
            {
                return;
            }
            harmony.UnpatchAll(HarmonyId);
            harmony = null;
            string report = BuildReport();
            slots = null;
            Log.Message(report);
            try
            {
                string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "Logs", "StrataPerf.log");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.AppendAllText(path, report + Environment.NewLine + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Log.Warning("[Strata] Could not write StrataPerf.log: " + ex.Message);
            }
        }

        // A probe left running into another save would mix two games: report
        // what it has and stop before the new one loads.
        internal static void ResetSession()
        {
            StopAndReport();
        }

        private static void AddPatchMethods(
            Dictionary<MethodBase, Slot> targets,
            IEnumerable<Patch> patches,
            MethodBase original,
            string kind)
        {
            foreach (Patch p in patches)
            {
                if (p?.PatchMethod == null || p.owner == HarmonyId || !IsStrataOwner(p.owner))
                {
                    continue;
                }
                MethodInfo m = p.PatchMethod;
                if (m.ContainsGenericParameters || targets.ContainsKey(m))
                {
                    continue;
                }
                targets[m] = new Slot
                {
                    label = original.DeclaringType?.Name + "." + original.Name + " " + kind
                        + " (" + m.DeclaringType?.Name + ")",
                };
            }
        }

        private static bool IsStrataOwner(string owner)
        {
            return owner != null
                && (owner.StartsWith("azraelgodking.strata", StringComparison.Ordinal)
                    || owner.StartsWith("azrael.strata", StringComparison.Ordinal));
        }

        public static void Before(out long __state)
        {
            __state = Stopwatch.GetTimestamp();
        }

        public static void After(MethodBase __originalMethod, long __state)
        {
            long elapsed = Stopwatch.GetTimestamp() - __state;
            Dictionary<MethodBase, Slot> map = slots;
            if (map == null || __originalMethod == null || !map.TryGetValue(__originalMethod, out Slot slot))
            {
                return;
            }
            // Some patched methods run on render or pathfinding workers.
            Interlocked.Increment(ref slot.calls);
            Interlocked.Add(ref slot.ticks, elapsed);
            long max = Interlocked.Read(ref slot.maxTicks);
            while (elapsed > max)
            {
                long seen = Interlocked.CompareExchange(ref slot.maxTicks, elapsed, max);
                if (seen == max)
                {
                    break;
                }
                max = seen;
            }
        }

        private static string BuildReport()
        {
            double secs = (Stopwatch.GetTimestamp() - startTimestamp) / (double)Stopwatch.Frequency;
            int gameTicks = Math.Max(0, (Find.TickManager?.TicksGame ?? 0) - startGameTick);
            var rows = new List<Slot>();
            long total = 0;
            foreach (Slot s in slots.Values)
            {
                if (s.calls > 0)
                {
                    rows.Add(s);
                    total += s.ticks;
                }
            }
            rows.Sort((a, b) => b.ticks.CompareTo(a.ticks));
            double toMs = 1000.0 / Stopwatch.Frequency;
            var sb = new StringBuilder();
            sb.AppendLine("[Strata] Perf probe report " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                + " (" + StrataBuildInfo.BuildStamp + ", " + Environment.Version + ")");
            sb.AppendLine("Span: " + secs.ToString("0.0") + " s real, " + gameTicks + " game ticks. "
                + "Methods timed: " + slots.Count + (skipped > 0 ? " (" + skipped + " could not be patched)" : "")
                + ". Inclusive Strata time: " + (total * toMs).ToString("0.0") + " ms"
                + (gameTicks > 0 ? " = " + (total * toMs / gameTicks).ToString("0.000") + " ms per game tick" : "")
                + (secs > 0 ? ", " + (total * toMs / secs / 10.0).ToString("0.00") + "% of real time" : "") + ".");
            sb.AppendLine("total ms | calls | avg us | max us | method");
            int shown = Math.Min(rows.Count, 40);
            for (int i = 0; i < shown; i++)
            {
                Slot s = rows[i];
                sb.Append((s.ticks * toMs).ToString("0.00").PadLeft(8)).Append(" | ")
                    .Append(s.calls.ToString().PadLeft(7)).Append(" | ")
                    .Append((s.ticks * toMs * 1000.0 / s.calls).ToString("0.0").PadLeft(7)).Append(" | ")
                    .Append((s.maxTicks * toMs * 1000.0).ToString("0").PadLeft(6)).Append(" | ")
                    .AppendLine(s.label);
            }
            if (rows.Count > shown)
            {
                sb.AppendLine("(" + (rows.Count - shown) + " more methods with calls not shown)");
            }
            return sb.ToString().TrimEnd();
        }
    }
}
