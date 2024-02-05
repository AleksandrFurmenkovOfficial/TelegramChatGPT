using System.Collections.Concurrent;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation.ChatCommands
{
    internal sealed class DelAccess(ConcurrentDictionary<string, IAppVisitor> visitors) : IChatCommand
    {
        string IChatCommand.Name => "del";
        bool IChatCommand.IsAdminOnlyCommand => true;

        public Task Execute(IChat chat, IChatMessage message, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            _ = visitors.AddOrUpdate(chat.Id, _ =>
            {
                var arg = new AppVisitor(false, Strings.Unknown);
                return arg;
            }, (_, arg) =>
            {
                arg.Access = false;
                return arg;
            });

            var showVisitorsCommand = new ShowVisitors(visitors);
            return showVisitorsCommand.Execute(chat, message, cancellationToken);
        }
    }
}