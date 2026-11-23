using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Per-colonist job and stair-relay trace for chasing stair loops.
    // Off unless Strata's "Verbose logging" setting is on; then every job
    // start/end and relay decision for player colonists goes to Player.log.
    public static class StrataTrace
    {
        public static bool On => StrataLog.VerboseEnabled;

        public static bool Wants(Pawn pawn)
        {
            return pawn != null && pawn.RaceProps != null && pawn.RaceProps.Humanlike
                && pawn.Faction != null && pawn.Faction.IsPlayer;
        }

        public static void Log(Pawn pawn, string text)
        {
            if (!On || !Wants(pawn))
            {
                return;
            }
            int tick = Current.Game?.tickManager?.TicksGame ?? -1;
            string map = pawn.MapHeld != null ? pawn.MapHeld.uniqueID.ToString() : "-";
            Write("t" + tick + " " + pawn.LabelShort + "@m" + map + " " + pawn.Position + ": " + text);
        }

        private static StreamWriter writer;
        private static bool writerFailed;

        // Own file next to the saves folder: Player.log stops taking messages
        // after its limit, and a stair loop fills that fast.
        internal static void WriteRaw(string line)
        {
            if (Current.Game == null)
            {
                return;
            }
            Write("t" + (Current.Game?.tickManager?.TicksGame ?? -1) + " " + line);
        }

        private static void Write(string line)
        {
            if (writer == null && !writerFailed)
            {
                try
                {
                    string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "Logs", "StrataTrace.log");
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    writer = new StreamWriter(path, append: false) { AutoFlush = true };
                    writer.WriteLine("Strata trace " + StrataBuildInfo.BuildStamp + " started " + DateTime.Now);
                    Verse.Log.Message("[Strata] Verbose logging is on: job trace goes to " + path);
                }
                catch (Exception e)
                {
                    writerFailed = true;
                    Verse.Log.Message("[Strata] Could not open the trace file: " + e.Message);
                }
            }
            if (writer == null)
            {
                return;
            }
            try
            {
                writer.WriteLine(line);
            }
            catch (Exception)
            {
                writerFailed = true;
                writer = null;
            }
        }

        public static string Target(LocalTargetInfo t)
        {
            if (!t.IsValid)
            {
                return "-";
            }
            if (t.HasThing)
            {
                Thing thing = t.Thing;
                string map = thing.MapHeld != null ? "m" + thing.MapHeld.uniqueID : "m-";
                return thing.def.defName + "#" + thing.thingIDNumber + "@" + map + thing.PositionHeld;
            }
            return t.Cell.ToString();
        }

        public static string DescribeJob(Job job)
        {
            if (job == null)
            {
                return "null";
            }
            var sb = new StringBuilder();
            sb.Append(job.def?.defName ?? "?");
            sb.Append(" A=").Append(Target(job.targetA));
            if (job.targetB.IsValid)
            {
                sb.Append(" B=").Append(Target(job.targetB));
            }
            if (job.count > 0)
            {
                sb.Append(" n=").Append(job.count);
            }
            if (job.workGiverDef != null)
            {
                sb.Append(" wg=").Append(job.workGiverDef.defName);
            }
            if (job.playerForced)
            {
                sb.Append(" forced");
            }
            return sb.ToString();
        }

        // Short Strata-only call chain, so a relay log shows who asked for it.
        public static string Caller(int skip = 2)
        {
            var frames = new StackTrace(skip, false).GetFrames();
            if (frames == null)
            {
                return "?";
            }
            var sb = new StringBuilder();
            int n = 0;
            foreach (StackFrame frame in frames)
            {
                var method = frame.GetMethod();
                if (method?.DeclaringType == null)
                {
                    continue;
                }
                string ns = method.DeclaringType.Namespace ?? "";
                if (!ns.StartsWith("Strata"))
                {
                    continue;
                }
                if (n > 0)
                {
                    sb.Append(" < ");
                }
                sb.Append(method.DeclaringType.Name).Append('.').Append(method.Name);
                if (++n >= 4)
                {
                    break;
                }
            }
            return n == 0 ? "vanilla" : sb.ToString();
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    public static class Patch_StrataTrace_StartJob
    {
        public static void Prefix(Pawn ___pawn, Job newJob, ThinkNode jobGiver)
        {
            if (!StrataTrace.On || !StrataTrace.Wants(___pawn))
            {
                return;
            }
            StrataTrace.Log(___pawn, "start " + StrataTrace.DescribeJob(newJob)
                + (jobGiver != null ? " giver=" + jobGiver.GetType().Name : "")
                + " via " + StrataTrace.Caller(2));
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.EndCurrentJob))]
    public static class Patch_StrataTrace_EndJob
    {
        public static void Prefix(Pawn ___pawn, Job ___curJob, JobCondition condition)
        {
            if (!StrataTrace.On || !StrataTrace.Wants(___pawn) || ___curJob == null)
            {
                return;
            }
            StrataTrace.Log(___pawn, "end " + ___curJob.def?.defName + " " + condition);
        }
    }
}
