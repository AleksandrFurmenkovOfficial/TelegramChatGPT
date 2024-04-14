using System.Net.Mime;
using System.Text;
using System.Text.Json.Serialization;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation
{
    internal sealed class AnthropicClient : IAiAgent, IDisposable
    {
        private const string _model = "claude-3-opus-20240229";

        private readonly Uri _endpoint = new("https://api.anthropic.com/v1/messages");
        private readonly HttpClient _httpClient;
        private readonly string _aiName;
        private readonly string _systemMessage;
        private readonly long _max_tokens = 1024;
        private readonly string _tools;

        public string AiName => _aiName;

        public AnthropicClient(string aiName, string systemMessage, string apiKey)
        {
            _aiName = aiName;
            _systemMessage = systemMessage;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            _tools = "";
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private static string GetEventType(string data)
        {
            var eventType = System.Text.Json.JsonSerializer.Deserialize<EventType>(data);
            return eventType?.Type ?? "";
        }

        private static IEnumerable<IChatMessage> GetFilteredMessages(IEnumerable<IChatMessage> messages)
        {
            var filteredAndMergedMessages = messages
            .Where(m => (m.Role == "user" || m.Role == "assistant") && !string.IsNullOrWhiteSpace(m.Content))
            .Aggregate(new List<IChatMessage>(), (acc, m) =>
            {
                if (acc.Count > 0 && acc.Last().Role == m.Role)
                {
                    var lastMessage = acc.Last();
                    lastMessage.Content += " " + m.Content;
                }
                else
                {
                    acc.Add(m);
                }

                return acc;
            });

            return filteredAndMergedMessages;
        }

        private string GetJsonPayload(IEnumerable<IChatMessage> messages, string systemMessage, bool useStream)
        {
            messages = GetFilteredMessages(messages);
            var requestMessages = new List<object>();
            foreach (var message in messages)
            {
                if (message.ImagesInBase64 != null && message.ImagesInBase64.Count > 0)
                {
                    List<object> values = [
                        new
                        {
                            type = "text",
                            text = !string.IsNullOrEmpty(message.Content) ? message.Content : Strings.WhatIsOnTheImage
                        }];

                    foreach (var image in message.ImagesInBase64)
                    {
                        values.Add(new
                        {
                            type = "image",
                            source = new
                            {
                                type = "base64",
                                media_type = "image/jpeg",
                                data = image,
                            },
                        });
                    }

                    var json = new
                    {
                        role = message.Role,
                        content = values
                    };

                    requestMessages.Add(json);
                }
                else if (!string.IsNullOrWhiteSpace(message.Content))
                {
                    var json = new
                    {
                        role = message.Role,
                        content = message.Content
                    };

                    requestMessages.Add(json);
                }
            }

            var requestBody = new
            {
                model = _model,
                system = systemMessage,
                messages = requestMessages,
                max_tokens = _max_tokens,
                temperature = Utils.GetRandTemperature(),
                // tools = _tools, // TODO:
                stream = useStream
            };

            var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
            return jsonPayload;
        }

        public async Task GetResponse(string chatId,
            IEnumerable<IChatMessage> messages,
            Func<ResponseStreamChunk, Task<bool>> responseStreamChunkGetter,
            CancellationToken cancellationToken = default)
        {
            bool isCancelled = false;

            try
            {
                using var content = new StringContent(GetJsonPayload(messages, _systemMessage, useStream: true), Encoding.UTF8, MediaTypeNames.Application.Json);
                using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint) { Content = content };
                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (!line.StartsWith("data:", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    var data = line["data:".Length..].Trim();
                    var eventType = GetEventType(data);
                    if (eventType != "content_block_delta")
                        continue;

                    var delta = System.Text.Json.JsonSerializer.Deserialize<ContentBlockDelta>(data);
                    isCancelled = await responseStreamChunkGetter(new ResponseStreamChunk(delta?.Delta?.Text ?? "")).ConfigureAwait(false);
                    if (isCancelled)
                        break;
                }
            }
            finally
            {
                if (!isCancelled)
                {
                    await responseStreamChunkGetter(new LastResponseStreamChunk()).ConfigureAwait(false);
                }
            }
        }

        public async Task<string?> GetResponse(string setting, string question, string? data, CancellationToken cancellationToken = default)
        {
            var messages = new List<IChatMessage> { new ChatMessage { Role = "user", Content = $"{question}\n{data}" } };
            using var content = new StringContent(GetJsonPayload(messages, setting, useStream: false), Encoding.UTF8, MediaTypeNames.Application.Json);
            using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint) { Content = content };
            using (var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                {
                    return default;
                }

                string result = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return result; // TODO:
            }
        }

        public Task<Uri?> GetImage(string imageDescription, string userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string?> GetImageDescription(Uri image, string question, string? systemMessage = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        private class EventType
        {
            [JsonPropertyName("type")]
            public string? Type { get; set; }
        }

        private class ContentBlockDelta
        {
            [JsonPropertyName("delta")]
            public TextDelta? Delta { get; set; }
        }

        private class TextDelta
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }
    }
}