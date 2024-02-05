namespace TelegramChatGPT.Interfaces
{
    internal interface IChatProcessor
    {
        Task Run(CancellationToken cancellationToken = default);
    }
}