using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation.ChatMessageActions
{
    internal class RegenerateAction : IChatMessageAction
    {
        public static ActionId Id => new("Regenerate");

        public virtual ActionId GetId => Id;

        public virtual Task Run(IChat chat, CancellationToken cancellationToken = default)
        {
            return chat.RegenerateLastResponse(cancellationToken);
        }
    }
}