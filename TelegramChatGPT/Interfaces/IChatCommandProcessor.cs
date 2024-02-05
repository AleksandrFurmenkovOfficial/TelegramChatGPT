namespace TelegramChatGPT.Interfaces
{
    internal interface IChatCommandProcessor
    {
        Task<bool> ExecuteIfChatCommand(IChat chat, IChatMessage message,
            CancellationToken cancellationToken = default);
    }
}