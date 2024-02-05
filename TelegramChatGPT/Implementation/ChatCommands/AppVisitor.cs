using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation.ChatCommands
{
    internal sealed class AppVisitor(bool access, string name) : IAppVisitor
    {
        public string Name { get; } = name;
        public bool Access { get; set; } = access;
    }
}