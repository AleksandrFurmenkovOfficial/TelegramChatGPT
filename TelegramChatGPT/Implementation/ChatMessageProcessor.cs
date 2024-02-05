using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation
{
    internal sealed class ChatMessageProcessor(IChatCommandProcessor chatCommandProcessor) : IChatMessageProcessor
    {
        public async Task HandleMessage(IChat chat, IChatMessage message, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            bool isCommandDone = await chatCommandProcessor.ExecuteIfChatCommand(chat, message, cancellationToken)
                .ConfigureAwait(false);
            if (isCommandDone)
            {
                return;
            }

            await chat.DoResponseToMessage(message, cancellationToken).ConfigureAwait(false);
        }
    }
}