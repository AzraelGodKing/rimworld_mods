using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LLMStoryteller
{
    /// <summary>
    /// The OpenAI-compatible chat completions format that Ollama and LM Studio both serve on localhost.
    /// </summary>
    public static class ChatProtocol
    {
        /// <summary>
        /// Turns what the player typed into the full chat completions URL. Accepts a bare host
        /// ("http://localhost:11434"), the base URL ending in /v1, or the full /chat/completions URL.
        /// Returns null when it is not an http(s) URL.
        /// </summary>
        public static string ChatCompletionsUrl(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint)) return null;
            string e = endpoint.Trim();
            if (!e.Contains("://")) e = "http://" + e;
            if (!Uri.TryCreate(e, UriKind.Absolute, out Uri uri)) return null;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;
            string path = uri.AbsolutePath.TrimEnd('/');
            if (path.Length == 0) path = "/v1/chat/completions";
            else if (!path.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)) path += "/chat/completions";
            return uri.GetLeftPart(UriPartial.Authority) + path;
        }

        public static string BuildRequestBody(string model, string system, string user, float temperature, int maxTokens)
        {
            var sb = new StringBuilder();
            sb.Append("{\"model\":").Append(MiniJson.Quote(model ?? ""));
            sb.Append(",\"messages\":[");
            sb.Append("{\"role\":\"system\",\"content\":").Append(MiniJson.Quote(system ?? "")).Append("},");
            sb.Append("{\"role\":\"user\",\"content\":").Append(MiniJson.Quote(user ?? "")).Append("}");
            sb.Append("],\"temperature\":").Append(temperature.ToString("0.###", CultureInfo.InvariantCulture));
            sb.Append(",\"max_tokens\":").Append(maxTokens.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"stream\":false}");
            return sb.ToString();
        }

        /// <summary>Reads choices[0].message.content, or the server's error message.</summary>
        public static bool TryReadContent(string responseBody, out string content, out string error)
        {
            content = null;
            error = null;
            if (!MiniJson.TryParse(responseBody, out object root) || !(root is Dictionary<string, object> obj))
            {
                error = "The server's reply was not JSON.";
                return false;
            }
            if (obj.TryGetValue("error", out object err) && err != null)
            {
                error = err is Dictionary<string, object> e && e.TryGetValue("message", out object m) && m is string ms
                    ? ms
                    : err as string ?? "The server returned an error.";
                return false;
            }
            if (obj.TryGetValue("choices", out object ch) && ch is List<object> choices && choices.Count > 0
                && choices[0] is Dictionary<string, object> first
                && first.TryGetValue("message", out object msg) && msg is Dictionary<string, object> message
                && message.TryGetValue("content", out object c) && c is string text)
            {
                content = text;
                return true;
            }
            error = "The reply had no choices[0].message.content.";
            return false;
        }

        /// <summary>Model names from GET /v1/models ({"data":[{"id":...}]}), for the connection test.</summary>
        public static List<string> ReadModelIds(string responseBody)
        {
            var ids = new List<string>();
            if (MiniJson.TryParse(responseBody, out object root) && root is Dictionary<string, object> obj
                && obj.TryGetValue("data", out object d) && d is List<object> data)
            {
                foreach (object item in data)
                {
                    if (item is Dictionary<string, object> m && m.TryGetValue("id", out object id) && id is string s)
                        ids.Add(s);
                }
            }
            return ids;
        }

        /// <summary>The /v1/models URL next to a chat completions URL.</summary>
        public static string ModelsUrl(string chatCompletionsUrl)
        {
            const string tail = "/chat/completions";
            return chatCompletionsUrl.EndsWith(tail, StringComparison.OrdinalIgnoreCase)
                ? chatCompletionsUrl.Substring(0, chatCompletionsUrl.Length - tail.Length) + "/models"
                : chatCompletionsUrl;
        }
    }
}
