namespace TelegramChatGPT.Interfaces
{
    internal readonly struct ActionId(string name)
    {
        public readonly string Name = name;
    }
}