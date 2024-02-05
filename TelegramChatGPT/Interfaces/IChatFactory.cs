namespace TelegramChatGPT.Interfaces
{
    internal interface IChatFactory
    {
        Task<IChat> CreateChat(string chatId);
    }
}