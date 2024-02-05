namespace TelegramChatGPT.Interfaces
{
    internal interface IAiSimpleResponseGetter
    {
        Task<string?> GetResponse(
            string setting,
            string question,
            string? data,
            CancellationToken cancellationToken = default);
    }
}