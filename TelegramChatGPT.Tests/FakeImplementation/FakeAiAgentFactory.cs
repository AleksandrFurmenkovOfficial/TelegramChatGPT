using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Tests.FakeImplementation
{
    internal sealed class FakeAiAgentFactory() : IAiAgentFactory
    {
        public IAiAgent CreateAiAgent(
            string aiName,
            string systemMessage,
            bool enableFunctions)
        {
            return new FakeAiAgent(aiName, systemMessage, enableFunctions);
        }
    }
}