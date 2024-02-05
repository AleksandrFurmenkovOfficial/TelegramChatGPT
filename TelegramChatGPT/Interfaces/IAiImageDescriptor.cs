namespace TelegramChatGPT.Interfaces
{
    internal interface IAiImageDescriptor
    {
        Task<string?> GetImageDescription(Uri image, string question, string? systemMessage = null,
            CancellationToken cancellationToken = default);
    }
}