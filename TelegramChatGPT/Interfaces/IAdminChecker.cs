namespace TelegramChatGPT.Interfaces
{
    internal interface IAdminChecker
    {
        bool IsAdmin(string userId);
    }
}