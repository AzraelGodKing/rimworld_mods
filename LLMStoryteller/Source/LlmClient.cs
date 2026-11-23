using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Verse;

namespace LLMStoryteller
{
    /// <summary>The model's raw answer, or why there is none.</summary>
    public sealed class LlmResult
    {
        public string Content;
        public string Error;
        public bool Ok => Error == null;
    }

    /// <summary>
    /// Talks to the local server off the main thread. Nothing here touches game state: callers pass
    /// plain strings in and poll the returned task from the main thread.
    /// </summary>
    public static class LlmClient
    {
        const int MaxTokens = 400;

        static readonly HttpClient Http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

        public static Task<LlmResult> Ask(LLMStorytellerSettings s, string system, string user)
        {
            string url = ChatProtocol.ChatCompletionsUrl(s.endpoint);
            if (url == null)
                return Task.FromResult(new LlmResult { Error = "LLMStoryteller_BadEndpoint".Translate(s.endpoint).ToString() });
            string body = ChatProtocol.BuildRequestBody(s.model, system, user, s.temperature, MaxTokens);
            string key = s.apiKey;
            var timeout = TimeSpan.FromSeconds(s.timeoutSeconds);
            return Task.Run(() => Post(url, key, body, timeout));
        }

        static async Task<LlmResult> Post(string url, string apiKey, string body, TimeSpan timeout)
        {
            try
            {
                using (var cts = new CancellationTokenSource(timeout))
                using (var req = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    req.Content = new StringContent(body, Encoding.UTF8, "application/json");
                    if (!string.IsNullOrEmpty(apiKey)) req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + apiKey);
                    using (HttpResponseMessage resp = await Http.SendAsync(req, cts.Token).ConfigureAwait(false))
                    {
                        string text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (ChatProtocol.TryReadContent(text, out string content, out string error))
                            return new LlmResult { Content = content };
                        return new LlmResult { Error = "HTTP " + (int)resp.StatusCode + ": " + error };
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return new LlmResult { Error = "timed out after " + (int)timeout.TotalSeconds + "s" };
            }
            catch (Exception e)
            {
                return new LlmResult { Error = Innermost(e).Message };
            }
        }

        static Exception Innermost(Exception e)
        {
            while (e.InnerException != null) e = e.InnerException;
            return e;
        }

        // ------------------------------------------------------------ connection test (Mod Options)

        static volatile string testStatus = "";

        public static string ConnectionTestStatus => testStatus;

        public static void StartConnectionTest(LLMStorytellerSettings s)
        {
            string url = ChatProtocol.ChatCompletionsUrl(s.endpoint);
            if (url == null)
            {
                testStatus = "LLMStoryteller_BadEndpoint".Translate(s.endpoint);
                return;
            }
            string modelsUrl = ChatProtocol.ModelsUrl(url);
            string model = s.model;
            string key = s.apiKey;
            string found = "LLMStoryteller_TestFound".Translate(model);
            string missing = "LLMStoryteller_TestMissing".Translate(model);
            string failed = "LLMStoryteller_TestFailed".Translate();
            testStatus = "LLMStoryteller_Testing".Translate(modelsUrl);
            Task.Run(async () =>
            {
                try
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    using (var req = new HttpRequestMessage(HttpMethod.Get, modelsUrl))
                    {
                        if (!string.IsNullOrEmpty(key)) req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + key);
                        using (HttpResponseMessage resp = await Http.SendAsync(req, cts.Token).ConfigureAwait(false))
                        {
                            string text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                            List<string> ids = ChatProtocol.ReadModelIds(text);
                            testStatus = ids.Contains(model) ? found : missing + " " + string.Join(", ", ids.ToArray());
                        }
                    }
                }
                catch (Exception e)
                {
                    testStatus = failed + " " + Innermost(e).Message;
                }
            });
        }
    }
}
