using Rystem.OpenAi;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation
{
    internal sealed class AiAgentFactory(
        string openAiApiKey,
        IAiImagePainter aiImagePainter,
        IAiImageDescriptor aiGetImageDescription) : IAiAgentFactory
    {
        public IAiAgent CreateAiAgent(
            string aiName,
            string systemMessage,
            bool enableFunctions)
        {
            _ = OpenAiService.Instance.AddOpenAi(settings => { settings.ApiKey = openAiApiKey; }, "NoDi");
            var openAiApi = OpenAiService.Factory.Create("NoDi");
            return new OpenAiAgent(aiName, systemMessage, enableFunctions, openAiApi, aiImagePainter,
                aiGetImageDescription);
        }
    }
}