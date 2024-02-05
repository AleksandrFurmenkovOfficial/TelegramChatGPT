using System.Collections.Concurrent;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation
{
    internal sealed class ChatContext(IChat chat) : IDisposable
    {
        private readonly SemaphoreSlim cancellationGuard = new(2, 2);
        private readonly ConcurrentDictionary<string, Task> taskChain = new();
        private readonly SemaphoreSlim taskGuard = new(1, 1);
        private CancellationTokenSource? cancellationTokenSource;

        public IChat Chat { get; } = chat;

        public void Dispose()
        {
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;

            cancellationGuard.Dispose();
            taskGuard.Dispose();
        }

        public async Task ExecuteNoThrow(Func<CancellationToken, Task> chatTask)
        {
            await cancellationGuard.WaitAsync().ConfigureAwait(false);

            var newCancellationTokenSource = new CancellationTokenSource();
            var oldCancellationTokenSource =
                Interlocked.Exchange(ref cancellationTokenSource, newCancellationTokenSource);
            if (oldCancellationTokenSource != null)
            {
                await oldCancellationTokenSource.CancelAsync().ConfigureAwait(false);
            }

            var taskToken = Guid.NewGuid().ToString();
            try
            {
                await taskGuard.WaitAsync((CancellationToken)default).ConfigureAwait(false);
                if (newCancellationTokenSource.Token.IsCancellationRequested)
                {
                    return;
                }

                await Task.WhenAll(taskChain.Values).ConfigureAwait(false);
                if (newCancellationTokenSource.Token.IsCancellationRequested)
                {
                    return;
                }

                var task = chatTask(newCancellationTokenSource.Token);
                taskChain.TryAdd(taskToken, task);
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException operationCanceledException)
            {
                AppLogger.LogException(operationCanceledException);
            }
            catch (Exception e)
            {
                AppLogger.LogInfoMessage($"[ExecuteNoThrow]\nException: {e.Message}\n{e.StackTrace}");
                _ = Chat.SendSomethingGoesWrong(default);
            }
            finally
            {
                oldCancellationTokenSource?.Dispose();
                taskChain.TryRemove(taskToken, out _);
                taskGuard.Release();
                cancellationGuard.Release();
            }
        }
    }
}