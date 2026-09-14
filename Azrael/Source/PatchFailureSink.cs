using System.Collections.Generic;

namespace Azrael
{
    /// <summary>
    /// Cross-mod sink for SafePatchAll. Sister assemblies report via TypeByName
    /// after startup so Azrael does not have to load first.
    /// </summary>
    public static class PatchFailureSink
    {
        public struct Row
        {
            public string Prefix;
            public string ClassName;
            public string Reason;
        }

        private static readonly List<Row> rows = new List<Row>();

        public static void Record(string prefix, string className, string reason)
        {
            lock (rows)
            {
                rows.Add(new Row
                {
                    Prefix = prefix ?? "",
                    ClassName = className ?? "",
                    Reason = reason ?? ""
                });
            }
        }

        public static List<Row> Snapshot()
        {
            lock (rows)
            {
                return new List<Row>(rows);
            }
        }
    }
}
