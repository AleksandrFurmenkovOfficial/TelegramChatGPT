namespace TelegramChatGPT.Interfaces
{
    internal interface IChat
    {
        string Id { get; }

        Task DoResponseToMessage(IChatMessage message, CancellationToken cancellationToken = default);

        void SetMode(ChatMode mode);
        Task SendSomethingGoesWrong(CancellationToken cancellationToken = default);
        Task SendSystemMessage(string content, CancellationToken cancellationToken = default);
        Task RemoveResponse(CancellationToken cancellationToken = default);
        Task Reset(CancellationToken cancellationToken = default);
        Task RegenerateLastResponse(CancellationToken cancellationToken = default);
        Task ContinueLastResponse(CancellationToken cancellationToken = default);
    }
}