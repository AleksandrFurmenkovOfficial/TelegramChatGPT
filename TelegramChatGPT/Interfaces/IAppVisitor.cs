namespace TelegramChatGPT.Interfaces
{
    internal interface IAppVisitor
    {
        string Name { get; }
        bool Access { get; set; }
    }
}