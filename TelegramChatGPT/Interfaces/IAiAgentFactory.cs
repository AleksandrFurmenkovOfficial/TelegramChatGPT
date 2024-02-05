namespace TelegramChatGPT.Interfaces
{
    internal interface IAiAgentFactory
    {
        IAiAgent CreateAiAgent(
            string aiName,
            string systemMessage,
            bool enableFunctions);
    }
}