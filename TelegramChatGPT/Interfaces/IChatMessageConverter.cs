namespace TelegramChatGPT.Interfaces
{
    internal interface IChatMessageConverter
    {
        public Task<IChatMessage>
            ConvertToChatMessage(object rawMessage, CancellationToken cancellationToken = default);
    }
}