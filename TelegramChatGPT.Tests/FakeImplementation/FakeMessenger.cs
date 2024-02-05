using System.Collections.Concurrent;
using System.Globalization;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Tests.FakeImplementation
{
    internal sealed class FakeMessenger(bool exceptionThrower = false) : IMessenger
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, FakeChatMessageInfo>> messages =
            new();

        private long counter;

        public Task<string> SendMessage(string chatId, IChatMessage message,
            IEnumerable<ActionId>? messageActionIds = null, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<string>(cancellationToken);
            }

            if (exceptionThrower)
            {
                return Task.FromException<string>(new InvalidOperationException());
            }

            var chatMessages = messages.GetOrAdd(chatId, _ => new ConcurrentDictionary<string, FakeChatMessageInfo>());
            var newMessageId = Interlocked.Increment(ref counter).ToString(CultureInfo.InvariantCulture);
            _ = chatMessages.TryAdd(newMessageId, new FakeChatMessageInfo(message, true, messageActionIds));
            return Task.FromResult(newMessageId);
        }

        public Task<string> SendPhotoMessage(string chatId, Uri image, string? caption = null,
            IEnumerable<ActionId>? messageActionIds = null, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<string>(cancellationToken);
            }

            if (exceptionThrower)
            {
                return Task.FromException<string>(new InvalidOperationException());
            }

            var chatMessages = messages.GetOrAdd(chatId, _ => new ConcurrentDictionary<string, FakeChatMessageInfo>());
            var newMessageId = Interlocked.Increment(ref counter).ToString(CultureInfo.InvariantCulture);
            _ = chatMessages.TryAdd(newMessageId,
                new FakeChatMessageInfo(null, false, messageActionIds, caption, image));
            return Task.FromResult(newMessageId);
        }

        public Task EditTextMessage(string chatId, MessageId messageId, string? content,
            IEnumerable<ActionId>? messageActionIds = null, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            if (exceptionThrower)
            {
                return Task.FromException(new InvalidOperationException());
            }

            if (!messages.TryGetValue(chatId, out var chatMessages))
            {
                throw new InvalidOperationException($"chat {chatId} doesn't exist");
            }

            if (!chatMessages.TryGetValue(messageId.Value, out var fakeMessageInfo))
            {
                throw new InvalidOperationException($"message {messageId.Value} doesn't exist");
            }

            if (!fakeMessageInfo.IsTextOnly)
            {
                throw new InvalidOperationException($"attempt to edit non text-only message {messageId.Value}!");
            }

            if (fakeMessageInfo.Message != null)
            {
                fakeMessageInfo.Message.Content = content;
            }

            fakeMessageInfo.Actions = messageActionIds?.ToList();

            return Task.CompletedTask;
        }

        public Task EditMessageCaption(string chatId, MessageId messageId, string? caption = null,
            IEnumerable<ActionId>? messageActionIds = null, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            if (exceptionThrower)
            {
                return Task.FromException(new InvalidOperationException());
            }

            if (!messages.TryGetValue(chatId, out var chatMessages))
            {
                throw new InvalidOperationException($"chat {chatId} doesn't exist");
            }

            if (!chatMessages.TryGetValue(messageId.Value, out var fakeMessageInfo))
            {
                throw new InvalidOperationException($"message {messageId.Value} doesn't exist");
            }

            if (fakeMessageInfo.IsTextOnly)
            {
                throw new InvalidOperationException($"attempt to edit text-only message {messageId.Value}!");
            }

            if (fakeMessageInfo.Message != null)
            {
                fakeMessageInfo.Caption = caption;
            }

            fakeMessageInfo.Actions = messageActionIds?.ToList();

            return Task.CompletedTask;
        }

        public Task<bool> DeleteMessage(string chatId, MessageId messageId,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<bool>(cancellationToken);
            }

            if (exceptionThrower)
            {
                return Task.FromException<bool>(new InvalidOperationException());
            }

            if (!messages.TryGetValue(chatId, out var chatMessages))
            {
                throw new InvalidOperationException($"chat {chatId} doesn't exist");
            }

            if (!chatMessages.TryRemove(messageId.Value, out _))
            {
                throw new InvalidOperationException($"message {messageId.Value} doesn't exist");
            }

            return Task.FromResult(true);
        }

        public sealed class FakeChatMessageInfo(
            IChatMessage? message,
            bool isTextOnly,
            IEnumerable<ActionId>? actions,
            string? caption = null,
            Uri? image = null)
        {
            public readonly bool IsTextOnly = isTextOnly;
            public readonly IChatMessage? Message = message;
            public IEnumerable<ActionId>? Actions = actions;
            public string? Caption = caption;
            public Uri? Image = image;
        }
    }
}