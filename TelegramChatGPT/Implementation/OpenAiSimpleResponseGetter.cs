using Rystem.OpenAi;
using Rystem.OpenAi.Chat;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation
{
    internal sealed class OpenAiSimpleResponseGetter(IOpenAi openAiApi, string model, double temperature = 0.0)
        : IAiSimpleResponseGetter
    {
        public async Task<string?> GetResponse(
            string setting,
            string question,
            string? data,
            CancellationToken cancellationToken = default)
        {
            var request = openAiApi.Chat.Request(new Rystem.OpenAi.Chat.ChatMessage
            { Role = ChatRole.System, Content = setting })
                .WithModel(model)
                .WithTemperature(temperature);

            if (!string.IsNullOrWhiteSpace(data))
            {
                _ = request.AddMessage(new Rystem.OpenAi.Chat.ChatMessage
                { Role = ChatRole.User, Content = $"{Strings.Text}:\n{data}" });
            }

            _ = request.AddMessage(new Rystem.OpenAi.Chat.ChatMessage
            { Role = ChatRole.User, Content = $"{Strings.Question}:\n{question}" });

            var chatChoices =
                (await request.ExecuteAsync(false, cancellationToken).ConfigureAwait(false)).Choices;

            return chatChoices?[0].Message?.Content?.ToString() ?? "";
        }
    }
}