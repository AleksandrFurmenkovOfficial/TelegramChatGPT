using TelegramChatGPT.Implementation;
using TelegramChatGPT.Tests.FakeImplementation;

namespace TelegramChatGPT.Tests
{
    public class ChatTest
    {
        [Fact]
        public void ChatIdSameTest()
        {
            var messenger = new FakeMessenger();
            var aiAgentFactory = new FakeAiAgentFactory();

            var chatId = "123";
            var chat = new Chat(chatId, aiAgentFactory, messenger);

           Assert.Equal(chat.Id, chatId);
        }

        // TODO: 
    }
}
