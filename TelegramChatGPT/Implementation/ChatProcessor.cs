using Microsoft.Azure.Cosmos.Core;
using RxTelegram.Bot;
using RxTelegram.Bot.Interface.BaseTypes;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using TelegramChatGPT.Implementation.ChatCommands;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation
{
    internal sealed class ChatProcessor(
        ConcurrentDictionary<string, IAppVisitor> visitorByChatId,
        ConcurrentDictionary<string, ConcurrentDictionary<string, ActionId>> actionsMappingByChat,
        IAdminChecker adminChecker,
        IChatFactory chatFactory,
        IChatMessageProcessor chatMessageProcessor,
        IChatMessageActionProcessor chatMessageActionProcessor,
        IChatMessageConverter chatMessageConverter,
        ITelegramBotSource botSource)
        : IChatProcessor, IDisposable
    {
        private const long MaxUniqueVisitors = 2;
        private static readonly CompositeFormat HasStarted = CompositeFormat.Parse(Strings.HasStarted);

        private readonly ConcurrentDictionary<string, ChatContext> chatContextById = [];
        private AsyncQueue<Tuple<ChatContext, Func<CancellationToken, Task>>>? events;

        private CancellationTokenSource? subscriptionCancellationTokenSource;

        private long brokenBot = 1;
        private readonly SemaphoreSlim botGuard = new(1, 1);

        public async Task Run(CancellationToken cancellationToken = default)
        {
            events = new AsyncQueue<Tuple<ChatContext, Func<CancellationToken, Task>>>(cancellationToken);
            await ReCreateTelegramListener().ConfigureAwait(false);

            using (var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount))
            {
                var activeTasks = new ConcurrentDictionary<string, Task>();
                while (!cancellationToken.IsCancellationRequested)
                {
                    await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                    var chatEvent = await events.DequeueAsync().ConfigureAwait(false);
                    var taskGuid = Guid.NewGuid().ToString();
                    activeTasks.TryAdd(taskGuid, Task.Run(async () =>
                    {
                        await chatEvent.Item1.ExecuteNoThrow(chatEvent.Item2).ConfigureAwait(false);
                        semaphore.Release();
                        activeTasks.TryRemove(taskGuid, out _);
                    }, cancellationToken));
                }

                await Task.WhenAll(activeTasks.Values).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            subscriptionCancellationTokenSource?.Dispose();
            subscriptionCancellationTokenSource = null;

            foreach (var chatContext in chatContextById.Values)
            {
                chatContext.Dispose();
            }

            events?.Dispose();
            events = null;

            botGuard.Dispose();
        }

        private async Task ReCreateTelegramListener()
        {
            await botGuard.WaitAsync().ConfigureAwait(false);

            try
            {
                var isBrokenBot = Interlocked.CompareExchange(ref brokenBot, 1, 1);
                if (isBrokenBot == 0)
                    return;

                var newTokenSource = new CancellationTokenSource();
                var oldTokenSource = Interlocked.CompareExchange(ref subscriptionCancellationTokenSource,
                    newTokenSource,
                    subscriptionCancellationTokenSource);
                if (oldTokenSource != null)
                {
                    await oldTokenSource.CancelAsync().ConfigureAwait(false);
                }

                if (botSource.NewTelegramBot() is not ITelegramBot bot)
                {
                    throw new InvalidCastException(nameof(bot));
                }

                Interlocked.Exchange(ref brokenBot, 0);
                bot.Updates.Message.Subscribe(OnNextMessageNoThrow, OnError, newTokenSource.Token);
                bot.Updates.CallbackQuery.Subscribe(OnNextCallbackQueryNoThrow, OnError, newTokenSource.Token);

                var hi = string.Format(CultureInfo.InvariantCulture, HasStarted,
                    (await bot.GetMe(newTokenSource.Token).ConfigureAwait(false)).Username);
                AppLogger.LogInfoMessage(hi);
            }
            finally
            {
                botGuard.Release();
            }
        }

        private void OnError(Exception e)
        {
            Interlocked.Exchange(ref brokenBot, 1);
            AppLogger.LogInfoMessage($"[OnError]\nException: {e.Message}\n{e.StackTrace}");
            ReCreateTelegramListener().Wait();
        }

        private async void OnNextMessageNoThrow(Message rawMessage)
        {
            if (events == null)
            {
                return;
            }

            try
            {
                bool isOneToOneChat = rawMessage.From.Id == rawMessage.Chat.Id;
                if (!isOneToOneChat)
                {
                    return;
                }

                bool isPhoto = rawMessage.Photo != null || rawMessage.ReplyToMessage?.Photo != null;
                bool isText =
                    rawMessage.Text?.Length > 0 ||
                    rawMessage.Caption?.Length > 0 ||
                    rawMessage.ReplyToMessage?.Text?.Length > 0 ||
                    rawMessage.ReplyToMessage?.Caption?.Length > 0;

                if (!isText && !isPhoto)
                {
                    return;
                }

                var chatId = rawMessage.Chat.Id.ToString(CultureInfo.InvariantCulture);
                var chatContext =
                    chatContextById.GetOrAdd(chatId, _ => new ChatContext(chatFactory.CreateChat(chatId).Result));
                if (!HasAccess(chatId, rawMessage.From))
                {
                    await chatContext.Chat.SendSystemMessage(Strings.NoAccess).ConfigureAwait(false);
                    chatContextById.Remove(chatId, out _);
                    return;
                }

                var message = await chatMessageConverter
                    .ConvertToChatMessage(rawMessage)
                    .ConfigureAwait(false);

                Func<CancellationToken, Task> chatEvent = cancellationToken =>
                    chatMessageProcessor.HandleMessage(chatContext.Chat, message, cancellationToken);
                events.TryEnqueue(new Tuple<ChatContext, Func<CancellationToken, Task>>(chatContext, chatEvent));
            }
            catch (Exception e)
            {
                AppLogger.LogInfoMessage($"[OnNextMessageNoThrow]\nException: {e.Message}\n{e.StackTrace}");
            }

            return;

            bool HasAccess(string chatId, User user)
            {
                return visitorByChatId.GetOrAdd(chatId, id =>
                {
                    bool accessByDefault = visitorByChatId.Count <= MaxUniqueVisitors || adminChecker.IsAdmin(chatId);
                    var arg = new AppVisitor(accessByDefault, $"{user.FirstName}_{user.LastName}_{user.Username}");
                    return arg;
                }).Access;
            }
        }

        private void OnNextCallbackQueryNoThrow(CallbackQuery callbackQuery)
        {
            if (events == null)
            {
                return;
            }

            try
            {
                var chatId = callbackQuery.From?.Id.ToString(CultureInfo.InvariantCulture);
                if (chatId == null)
                {
                    return;
                }

                var mapping = actionsMappingByChat.GetOrAdd(chatId, _ => []);
                if (!mapping.Remove(callbackQuery.Data, out var callbackId))
                {
                    return;
                }

                if (!chatContextById.TryGetValue(chatId, out var chatContext))
                {
                    return;
                }

                Func<CancellationToken, Task> chatEvent = cancellationToken =>
                    chatMessageActionProcessor.HandleMessageAction(
                        chatContext.Chat,
                        new ActionParameters(
                            callbackId,
                            callbackQuery.Message.MessageId.ToString(CultureInfo.InvariantCulture)), cancellationToken);

                events.TryEnqueue(new Tuple<ChatContext, Func<CancellationToken, Task>>(chatContext, chatEvent));
            }
            catch (Exception e)
            {
                AppLogger.LogInfoMessage($"[OnNextCallbackQueryNoThrow]\nException: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}