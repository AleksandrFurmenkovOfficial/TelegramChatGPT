using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation
{
    internal sealed class AntrophicAgentFactory(
        string antrophicAiApiKey) : IAiAgentFactory
    {
        public IAiAgent CreateAiAgent(
            string aiName,
            string systemMessage,
            bool enableFunctions)
        {
            return new AnthropicClient(aiName, systemMessage, antrophicAiApiKey);
        }
    }
}