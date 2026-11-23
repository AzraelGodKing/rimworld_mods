using System.Collections.Generic;
using Xunit;

namespace LLMStoryteller.Tests
{
    public class ChatProtocolTests
    {
        [Theory]
        [InlineData("http://localhost:11434/v1", "http://localhost:11434/v1/chat/completions")]
        [InlineData("http://localhost:11434/v1/", "http://localhost:11434/v1/chat/completions")]
        [InlineData("http://localhost:1234", "http://localhost:1234/v1/chat/completions")]
        [InlineData("localhost:1234/v1", "http://localhost:1234/v1/chat/completions")]
        [InlineData("http://127.0.0.1:1234/v1/chat/completions", "http://127.0.0.1:1234/v1/chat/completions")]
        public void ChatCompletionsUrl_accepts_common_forms(string typed, string expected)
        {
            Assert.Equal(expected, ChatProtocol.ChatCompletionsUrl(typed));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("ftp://localhost/v1")]
        public void ChatCompletionsUrl_rejects_non_http(string typed)
        {
            Assert.Null(ChatProtocol.ChatCompletionsUrl(typed));
        }

        [Fact]
        public void ModelsUrl_sits_next_to_chat_completions()
        {
            Assert.Equal("http://localhost:1234/v1/models", ChatProtocol.ModelsUrl("http://localhost:1234/v1/chat/completions"));
        }

        [Fact]
        public void Request_body_is_valid_json_with_escaped_text()
        {
            string body = ChatProtocol.BuildRequestBody("llama3.1:8b", "sys \"quoted\"", "line1\nline2", 0.8f, 400);
            Assert.True(MiniJson.TryParse(body, out object root));
            var obj = (Dictionary<string, object>)root;
            Assert.Equal("llama3.1:8b", obj["model"]);
            Assert.Equal(0.8, (double)obj["temperature"], 3);
            Assert.Equal(false, obj["stream"]);
            var messages = (List<object>)obj["messages"];
            Assert.Equal("sys \"quoted\"", ((Dictionary<string, object>)messages[0])["content"]);
            Assert.Equal("line1\nline2", ((Dictionary<string, object>)messages[1])["content"]);
        }

        [Fact]
        public void Reads_message_content()
        {
            const string reply = "{\"id\":\"x\",\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":\"{\\\"incident\\\":\\\"none\\\"}\"}}]}";
            Assert.True(ChatProtocol.TryReadContent(reply, out string content, out _));
            Assert.Equal("{\"incident\":\"none\"}", content);
        }

        [Fact]
        public void Reads_server_error_message()
        {
            const string reply = "{\"error\":{\"message\":\"model \\\"foo\\\" not found\",\"type\":\"api_error\"}}";
            Assert.False(ChatProtocol.TryReadContent(reply, out _, out string error));
            Assert.Equal("model \"foo\" not found", error);
        }

        [Fact]
        public void Non_json_reply_is_an_error()
        {
            Assert.False(ChatProtocol.TryReadContent("<html>502</html>", out _, out string error));
            Assert.NotNull(error);
        }

        [Fact]
        public void Reads_model_ids()
        {
            var ids = ChatProtocol.ReadModelIds("{\"object\":\"list\",\"data\":[{\"id\":\"llama3.1:8b\"},{\"id\":\"qwen2.5:7b\"}]}");
            Assert.Equal(new[] { "llama3.1:8b", "qwen2.5:7b" }, ids);
        }
    }
}
