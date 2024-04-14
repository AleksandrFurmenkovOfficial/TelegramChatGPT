using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Tests.FakeImplementation
{
    internal sealed class FakeAiAgent(string aiName, string systemMessage, bool enableFunctions, bool cancelledTasks = false, bool exceptionThrower = false)
        : IAiAgent
    {
        public string AiName => aiName;
        public string SystemMessage => systemMessage;
        public bool EnableFunctions => enableFunctions;

        Task<Uri?> IAiImagePainter.GetImage(string imageDescription, string userId, CancellationToken cancellationToken)
        {
            if (cancelledTasks)
            {
                return Task.FromCanceled<Uri?>(cancellationToken);
            }

            if (exceptionThrower)
            {
                return Task.FromException<Uri?>(new InvalidOperationException());
            }

            throw new NotImplementedException();
        }

        Task IAiAgent.GetResponse(string chatId, IEnumerable<IChatMessage> messages, Func<ResponseStreamChunk, Task<bool>> responseStreamChunkGetter, CancellationToken cancellationToken)
        {
            if (cancelledTasks)
            {
                return Task.FromCanceled(cancellationToken);
            }

            if (exceptionThrower)
            {
                return Task.FromException(new InvalidOperationException());
            }

            throw new NotImplementedException();
        }

        Task<string?> IAiSimpleResponseGetter.GetResponse(string setting, string question, string? data, CancellationToken cancellationToken)
        {
            if (cancelledTasks)
            {
                return Task.FromCanceled<string?>(cancellationToken);
            }

            if (exceptionThrower)
            {
                return Task.FromException<string?>(new InvalidOperationException());
            }

            throw new NotImplementedException();
        }
    }
}