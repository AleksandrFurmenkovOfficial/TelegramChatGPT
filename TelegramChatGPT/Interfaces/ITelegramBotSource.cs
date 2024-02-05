namespace TelegramChatGPT.Interfaces
{
    internal interface ITelegramBotSource
    {
        object NewTelegramBot();
        object TelegramBot();
    }
}