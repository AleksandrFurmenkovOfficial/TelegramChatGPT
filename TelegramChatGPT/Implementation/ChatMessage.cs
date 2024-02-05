using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation
{
    internal sealed class ChatMessage(
        MessageId messageId,
        string? content = null,
        string? role = null,
        string? name = null,
        Uri? imageUrl = null)
        : IChatMessage
    {
        public ChatMessage(string content, string? role = null, string? name = null, Uri? imageUrl = null) : this(
            IChatMessage.InternalMessageId, content, role, name, imageUrl)
        {
        }

        public ChatMessage(MessageId? messageId = null) : this(messageId ?? IChatMessage.InternalMessageId)
        {
        }

        public MessageId MessageId { get; set; } = messageId;
        public string? Content { get; set; } = content;
        public string? Role { get; set; } = role;
        public string? Name { get; set; } = name;
        public Uri? ImageUrl { get; set; } = imageUrl;
    }
}