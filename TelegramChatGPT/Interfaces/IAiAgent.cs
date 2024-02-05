namespace TelegramChatGPT.Interfaces
{
    internal interface IAiAgent : IAiSimpleResponseGetter, IAiImagePainter, IAiImageDescriptor
    {
        string AiName { get; }

        Task GetResponse(string chatId, IEnumerable<IChatMessage> messages,
            Func<ResponseStreamChunk, Task<bool>> responseStreamChunkGetter,
            CancellationToken cancellationToken = default);
    }
}