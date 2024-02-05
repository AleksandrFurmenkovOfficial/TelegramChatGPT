namespace TelegramChatGPT.Interfaces
{
    internal interface IAiImagePainter
    {
        Task<Uri?> GetImage(string imageDescription, string userId, CancellationToken cancellationToken = default);
    }
}