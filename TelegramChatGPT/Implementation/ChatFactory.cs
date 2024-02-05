using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation
{
    internal sealed class ChatFactory(IChatModeLoader modeLoader, IAiAgentFactory aIAgentFactory, IMessenger messenger) : IChatFactory
    {
        public async Task<IChat> CreateChat(string chatId)
        {
            var chat = new Chat(chatId, aIAgentFactory, messenger);
            var mode = await modeLoader.GetChatMode(IChatModeLoader.CommonMode).ConfigureAwait(false);
            chat.SetMode(mode);
            return chat;
        }
    }
}