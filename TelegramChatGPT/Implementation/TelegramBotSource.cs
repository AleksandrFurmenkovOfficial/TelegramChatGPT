using RxTelegram.Bot;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation
{
    internal sealed class TelegramBotSource(string telegramBotKey) : ITelegramBotSource
    {
        private ITelegramBot? bot;

        public object TelegramBot()
        {
            if (bot == null)
            {
                throw new ArgumentNullException(nameof(bot));
            }

            return bot;
        }

        public object NewTelegramBot()
        {
            Interlocked.CompareExchange(ref bot, new TelegramBot(telegramBotKey), bot);
            if (bot == null)
            {
                throw new ArgumentNullException(nameof(bot));
            }

            return bot;
        }
    }
}