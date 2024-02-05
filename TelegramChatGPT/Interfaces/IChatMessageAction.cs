namespace TelegramChatGPT.Interfaces
{
    internal interface IChatMessageAction
    {
        ActionId GetId { get; }
        Task Run(IChat chat, CancellationToken cancellationToken = default);
    }
}