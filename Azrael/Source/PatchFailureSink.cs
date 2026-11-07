#nullable enable
using System.Collections.Generic;

namespace Azrael
{
    /// <summary>
    /// Cross-mod sink for SafePatchAll. Sister assemblies report via TypeByName
    /// after startup so Azrael does not have to load first.
    /// Class name, namespace and Record(string, string, string) are a cross-mod contract.
    /// </summary>
    public static class PatchFailureSink
    {
        public readonly struct Row
        {
            public readonly string Prefix;
            public readonly string ClassName;
            public readonly string Reason;

            public Row(string? prefix, string? className, string? reason)
            {
                Prefix = prefix ?? "";
                ClassName = className ?? "";
                Reason = reason ?? "";
            }
        }

#if NET9_0_OR_GREATER
        private static readonly System.Threading.Lock gate = new System.Threading.Lock();
#else
        private static readonly object gate = new object();
#endif
        private static readonly List<Row> rows = new List<Row>();

        public static void Record(string? prefix, string? className, string? reason)
        {
            lock (gate)
            {
                rows.Add(new Row(prefix, className, reason));
            }
        }

        public static List<Row> Snapshot()
        {
            lock (gate)
            {
                return new List<Row>(rows);
            }
        }
    }
}
