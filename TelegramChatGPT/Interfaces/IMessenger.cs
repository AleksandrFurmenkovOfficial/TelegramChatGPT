namespace TelegramChatGPT.Interfaces
{
    internal interface IMessenger
    {
        const int MaxTextLen = 2048;
        const int MaxCaptionLen = 1024;

        Task<string> SendMessage(string chatId, IChatMessage message, IEnumerable<ActionId>? messageActionIds = null,
            CancellationToken cancellationToken = default);

        Task<string> SendPhotoMessage(string chatId, Uri image, string? caption = null,
            IEnumerable<ActionId>? messageActionIds = null, CancellationToken cancellationToken = default);

        Task EditTextMessage(string chatId, MessageId messageId, string? content,
            IEnumerable<ActionId>? messageActionIds = null, CancellationToken cancellationToken = default);

        Task EditMessageCaption(string chatId, MessageId messageId, string? caption = null,
            IEnumerable<ActionId>? messageActionIds = null, CancellationToken cancellationToken = default);

        Task<bool> DeleteMessage(string chatId, MessageId messageId, CancellationToken cancellationToken = default);
    }
}