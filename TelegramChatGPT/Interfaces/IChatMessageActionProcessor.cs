namespace TelegramChatGPT.Interfaces
{
    internal interface IChatMessageActionProcessor
    {
        Task HandleMessageAction(IChat chat, ActionParameters actionCallParameters,
            CancellationToken cancellationToken = default);
    }
}