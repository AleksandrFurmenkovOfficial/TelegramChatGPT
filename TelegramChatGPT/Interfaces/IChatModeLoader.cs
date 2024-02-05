namespace TelegramChatGPT.Interfaces
{
    internal interface IChatModeLoader
    {
        const string CommonMode = "CommonMode";

        Task<ChatMode> GetChatMode(string modeDescriptionFilename, CancellationToken cancellationToken = default);
    }
}