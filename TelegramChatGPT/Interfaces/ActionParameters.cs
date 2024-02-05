namespace TelegramChatGPT.Interfaces
{
    internal readonly struct ActionParameters(ActionId actionId, string messageId)
    {
        public readonly string MessageId = messageId;
        public readonly ActionId ActionId = actionId;
    }
}