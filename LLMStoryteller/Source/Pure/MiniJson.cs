using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LLMStoryteller
{
    /// <summary>
    /// Just enough JSON for the chat API: objects become Dictionary&lt;string, object&gt;, arrays
    /// List&lt;object&gt;, numbers double, plus string, bool and null. No game types, so it is tested
    /// headless. RimWorld ships no JSON library both 1.6 and 1.7 can use.
    /// </summary>
    public static class MiniJson
    {
        public static bool TryParse(string text, out object value)
        {
            value = null;
            if (text == null) return false;
            int pos = 0;
            if (!TryParsePrefix(text, ref pos, out value)) return false;
            SkipWhitespace(text, ref pos);
            return pos == text.Length;
        }

        /// <summary>Parses one value starting at <paramref name="pos"/> and leaves pos after it. Text after the value is ignored.</summary>
        public static bool TryParsePrefix(string text, ref int pos, out object value)
        {
            value = null;
            try
            {
                return ParseValue(text, ref pos, 0, out value);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string Quote(string s)
        {
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        const int MaxDepth = 64;

        static bool ParseValue(string s, ref int pos, int depth, out object value)
        {
            value = null;
            if (depth > MaxDepth) return false;
            SkipWhitespace(s, ref pos);
            if (pos >= s.Length) return false;
            char c = s[pos];
            switch (c)
            {
                case '{': return ParseObject(s, ref pos, depth, out value);
                case '[': return ParseArray(s, ref pos, depth, out value);
                case '"':
                    bool ok = ParseString(s, ref pos, out string str);
                    value = str;
                    return ok;
                case 't': value = true; return Literal(s, ref pos, "true");
                case 'f': value = false; return Literal(s, ref pos, "false");
                case 'n': value = null; return Literal(s, ref pos, "null");
                default:
                    if (c == '-' || (c >= '0' && c <= '9')) return ParseNumber(s, ref pos, out value);
                    return false;
            }
        }

        static bool ParseObject(string s, ref int pos, int depth, out object value)
        {
            value = null;
            var dict = new Dictionary<string, object>();
            pos++;
            SkipWhitespace(s, ref pos);
            if (pos < s.Length && s[pos] == '}') { pos++; value = dict; return true; }
            while (true)
            {
                SkipWhitespace(s, ref pos);
                if (pos >= s.Length || s[pos] != '"') return false;
                if (!ParseString(s, ref pos, out string key)) return false;
                SkipWhitespace(s, ref pos);
                if (pos >= s.Length || s[pos] != ':') return false;
                pos++;
                if (!ParseValue(s, ref pos, depth + 1, out object item)) return false;
                dict[key] = item;
                SkipWhitespace(s, ref pos);
                if (pos >= s.Length) return false;
                if (s[pos] == ',') { pos++; continue; }
                if (s[pos] == '}') { pos++; value = dict; return true; }
                return false;
            }
        }

        static bool ParseArray(string s, ref int pos, int depth, out object value)
        {
            value = null;
            var list = new List<object>();
            pos++;
            SkipWhitespace(s, ref pos);
            if (pos < s.Length && s[pos] == ']') { pos++; value = list; return true; }
            while (true)
            {
                if (!ParseValue(s, ref pos, depth + 1, out object item)) return false;
                list.Add(item);
                SkipWhitespace(s, ref pos);
                if (pos >= s.Length) return false;
                if (s[pos] == ',') { pos++; continue; }
                if (s[pos] == ']') { pos++; value = list; return true; }
                return false;
            }
        }

        static bool ParseString(string s, ref int pos, out string value)
        {
            value = null;
            var sb = new StringBuilder();
            pos++;
            while (pos < s.Length)
            {
                char c = s[pos++];
                if (c == '"') { value = sb.ToString(); return true; }
                if (c != '\\') { sb.Append(c); continue; }
                if (pos >= s.Length) return false;
                char e = s[pos++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (pos + 4 > s.Length) return false;
                        if (!int.TryParse(s.Substring(pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code)) return false;
                        sb.Append((char)code);
                        pos += 4;
                        break;
                    default: return false;
                }
            }
            return false;
        }

        static bool ParseNumber(string s, ref int pos, out object value)
        {
            int start = pos;
            while (pos < s.Length && "+-0123456789.eE".IndexOf(s[pos]) >= 0) pos++;
            bool ok = double.TryParse(s.Substring(start, pos - start), NumberStyles.Float, CultureInfo.InvariantCulture, out double d);
            value = d;
            return ok;
        }

        static bool Literal(string s, ref int pos, string word)
        {
            if (string.CompareOrdinal(s, pos, word, 0, word.Length) != 0) return false;
            pos += word.Length;
            return true;
        }

        static void SkipWhitespace(string s, ref int pos)
        {
            while (pos < s.Length && char.IsWhiteSpace(s[pos])) pos++;
        }
    }
}
