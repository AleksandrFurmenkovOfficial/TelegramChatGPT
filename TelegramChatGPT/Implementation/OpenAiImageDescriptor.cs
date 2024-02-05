using System.Text.Json.Serialization;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation
{
    internal sealed class OpenAiImageDescriptor(string apiKey) : IAiImageDescriptor
    {
        private const string ApiHost = "https://api.openai.com/v1";
        private const string GetImageDescriptionModel = "gpt-4-vision-preview";

        public async Task<string?> GetImageDescription(Uri imageUrl, string question, string? systemMessage = null,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return await Task.FromCanceled<string>(cancellationToken).ConfigureAwait(false);
            }

            var imageDescriptionResponse = await Utils.DoRequest<ChatCompletion>(new
            {
                model = GetImageDescriptionModel,
                messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = new object[]
                            {
                                new { type = "text", text = systemMessage ?? "" }
                            }
                        },
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new
                                {
                                    type = "text",
                                    text = string.IsNullOrEmpty(question) ? question : Strings.WhatIsOnTheImage
                                },
                                new
                                {
                                    type = "image_url",
                                    image_url = new
                                    {
                                        url =
                                            $"data:image/jpeg;base64,{await Utils.EncodeImageToBase64(imageUrl, cancellationToken).ConfigureAwait(false)}"
                                    }
                                }
                            }
                        }
                    },
                max_tokens = 512
            }, $"{ApiHost}/chat/completions", apiKey, cancellationToken).ConfigureAwait(false);

            return imageDescriptionResponse?.Choices?[0]?.Message?.Content;
        }

        private sealed class ChatCompletion(List<Choice> choices)
        {
            [JsonPropertyName("id")] public string? Id { get; set; }

            [JsonPropertyName("object")] public string? Object { get; set; }

            [JsonPropertyName("created")] public long Created { get; set; }

            [JsonPropertyName("model")] public string? Model { get; set; }

            [JsonPropertyName("usage")] public Usage? Usage { get; set; }

            [JsonPropertyName("choices")] public List<Choice> Choices { get; } = choices;
        }

        private sealed class Usage
        {
            [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }

            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }

            [JsonPropertyName("total_tokens")] public int TotalTokens { get; set; }
        }

        private sealed class Choice(Message message)
        {
            [JsonPropertyName("message")] public Message Message { get; } = message;

            [JsonPropertyName("finish_reason")] public string? FinishReason { get; set; }

            [JsonPropertyName("index")] public int Index { get; set; }
        }

        private sealed class Message(string content)
        {
            [JsonPropertyName("role")] public string? Role { get; set; }

            [JsonPropertyName("content")] public string Content { get; } = content;
        }
    }
}