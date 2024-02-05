namespace TelegramChatGPT.Interfaces
{
    internal interface IChatMessage
    {
        public static readonly MessageId InternalMessageId = new("");
        MessageId MessageId { get; set; }
        string? Name { get; set; }
        string? Content { get; set; }
        string? Role { get; set; }
        public Uri? ImageUrl { get; set; }
    }
}