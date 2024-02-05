namespace TelegramChatGPT.Interfaces
{
    internal class ResponseStreamChunk
    {
        public readonly List<IChatMessage> Messages;

        public readonly string? TextDelta;

        public ResponseStreamChunk(string textDelta) : this(null, textDelta)
        {
            TextDelta = textDelta;
        }

        public ResponseStreamChunk(IEnumerable<IChatMessage>? messages, string? textDelta = null)
        {
            TextDelta = textDelta;
            Messages = [];

            if (messages != null)
            {
                Messages.AddRange(messages);
            }
        }
    }
}