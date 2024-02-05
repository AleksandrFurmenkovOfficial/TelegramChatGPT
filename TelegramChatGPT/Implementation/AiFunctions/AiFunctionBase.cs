using System.Text.Json.Serialization;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation.AiFunctions
{
    internal abstract class AiFunctionBase : IAiFunction
    {
        public virtual string Name => throw new NotImplementedException();

        public virtual Task<AiFunctionResult> Call(IAiAgent api, string parameters, string userId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public virtual JsonFunction Description()
        {
            throw new NotImplementedException();
        }

        protected static string GetPathToUserAssociatedMemories(string aiName, string userId)
        {
            string directory = AppContext.BaseDirectory;
            return $"{directory}/../AiMemory/{aiName}_{userId}.txt";
        }
    }
}