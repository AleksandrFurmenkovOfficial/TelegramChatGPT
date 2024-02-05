namespace TelegramChatGPT.Interfaces
{
    internal interface IChatMessageProcessor
    {
        Task HandleMessage(IChat chat, IChatMessage message, CancellationToken cancellationToken = default);
    }
}