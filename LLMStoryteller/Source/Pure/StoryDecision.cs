using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LLMStoryteller
{
    /// <summary>One incident the model may pick, as offered in the prompt.</summary>
    public sealed class MenuOption
    {
        public string DefName;
        public string Label;
        public string Category;

        public MenuOption(string defName, string label, string category)
        {
            DefName = defName;
            Label = label;
            Category = category;
        }
    }

    /// <summary>What the model chose. <see cref="Option"/> is null for a quiet day.</summary>
    public sealed class StoryDecision
    {
        public MenuOption Option;
        public float Intensity = 1f;
        public string Reason = "";

        public bool Quiet => Option == null;

        public const string QuietWord = "none";

        static readonly Regex ThinkBlock = new Regex(@"<think>.*?</think>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        /// <summary>
        /// Reads the model's answer. Reasoning blocks (&lt;think&gt;) and code fences are skipped; the first
        /// JSON object in the text is used. The incident must be one of <paramref name="menu"/>, matched by
        /// defName or label, or "none". Intensity is clamped to [minIntensity, maxIntensity].
        /// </summary>
        public static bool TryParse(string content, IList<MenuOption> menu, float minIntensity, float maxIntensity,
            out StoryDecision decision, out string error)
        {
            decision = null;
            error = null;
            if (string.IsNullOrWhiteSpace(content))
            {
                error = "The model sent an empty answer.";
                return false;
            }
            string text = ThinkBlock.Replace(content, "");
            if (!TryFindObject(text, out Dictionary<string, object> obj))
            {
                error = "No JSON object in the model's answer.";
                return false;
            }

            string pick = ReadString(obj, "incident") ?? ReadString(obj, "event");
            if (string.IsNullOrWhiteSpace(pick))
            {
                error = "The answer has no \"incident\".";
                return false;
            }
            pick = pick.Trim();

            var d = new StoryDecision { Reason = (ReadString(obj, "reason") ?? "").Trim() };
            if (!IsQuietWord(pick))
            {
                d.Option = Find(menu, pick);
                if (d.Option == null)
                {
                    error = "\"" + pick + "\" is not on the list of allowed incidents.";
                    return false;
                }
            }
            d.Intensity = Clamp(ReadNumber(obj, "intensity") ?? 1f, minIntensity, maxIntensity);
            decision = d;
            return true;
        }

        static bool IsQuietWord(string s) =>
            s.Equals(QuietWord, StringComparison.OrdinalIgnoreCase)
            || s.Equals("nothing", StringComparison.OrdinalIgnoreCase)
            || s.Equals("quiet", StringComparison.OrdinalIgnoreCase)
            || s.Equals("null", StringComparison.OrdinalIgnoreCase);

        static MenuOption Find(IList<MenuOption> menu, string pick)
        {
            foreach (MenuOption o in menu)
                if (o.DefName.Equals(pick, StringComparison.OrdinalIgnoreCase)) return o;
            foreach (MenuOption o in menu)
                if (o.Label.Equals(pick, StringComparison.OrdinalIgnoreCase)) return o;
            return null;
        }

        static bool TryFindObject(string text, out Dictionary<string, object> obj)
        {
            obj = null;
            for (int i = text.IndexOf('{'); i >= 0; i = text.IndexOf('{', i + 1))
            {
                int pos = i;
                if (MiniJson.TryParsePrefix(text, ref pos, out object value) && value is Dictionary<string, object> d)
                {
                    obj = d;
                    return true;
                }
            }
            return false;
        }

        static string ReadString(Dictionary<string, object> obj, string key) =>
            obj.TryGetValue(key, out object v) ? v as string : null;

        static float? ReadNumber(Dictionary<string, object> obj, string key)
        {
            if (!obj.TryGetValue(key, out object v)) return null;
            if (v is double d) return (float)d;
            if (v is string s && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)) return f;
            return null;
        }

        static float Clamp(float v, float min, float max)
        {
            if (float.IsNaN(v)) v = 1f;
            return v < min ? min : v > max ? max : v;
        }
    }
}
