using System.Collections.Concurrent;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation.ChatCommands
{
    internal sealed class AddAccess(ConcurrentDictionary<string, IAppVisitor> visitors) : IChatCommand
    {
        string IChatCommand.Name => "add";
        bool IChatCommand.IsAdminOnlyCommand => true;

        public Task Execute(IChat chat, IChatMessage message, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            var id = message.Content!.Trim();
            _ = visitors.AddOrUpdate(id, _ =>
            {
                var arg = new AppVisitor(true, Strings.Unknown);
                return arg;
            }, (_, arg) =>
            {
                arg.Access = true;
                return arg;
            });

            var showVisitorsCommand = new ShowVisitors(visitors);
            return showVisitorsCommand.Execute(chat, message, cancellationToken);
        }
    }
}
