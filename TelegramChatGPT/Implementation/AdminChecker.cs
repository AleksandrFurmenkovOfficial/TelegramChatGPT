using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation
{
    internal sealed class AdminChecker(string adminUserId) : IAdminChecker
    {
        public bool IsAdmin(string userId)
        {
            return string.Equals(userId, adminUserId, StringComparison.OrdinalIgnoreCase);
        }
    }
}