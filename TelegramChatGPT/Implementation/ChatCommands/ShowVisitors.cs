using System.Collections.Concurrent;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation.ChatCommands
{
    internal sealed class ShowVisitors(ConcurrentDictionary<string, IAppVisitor> visitors) : IChatCommand
    {
        string IChatCommand.Name => "vis";
        bool IChatCommand.IsAdminOnlyCommand => true;

        public Task Execute(IChat chat, IChatMessage message, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            string vis = visitors.Aggregate("Visitors:\n",
                (current, item) => current + $"{item.Key} - {item.Value.Name}:{item.Value.Access}\n");
            return chat.SendSystemMessage(vis, cancellationToken);
        }
    }
}