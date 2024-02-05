using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation.ChatMessageActions
{
    internal sealed class CancelAction : IChatMessageAction
    {
        public static ActionId Id => new("Cancel");

        public ActionId GetId => Id;

        public Task Run(IChat chat, CancellationToken cancellationToken = default)
        {
            return chat.RemoveResponse(default);
        }
    }
}