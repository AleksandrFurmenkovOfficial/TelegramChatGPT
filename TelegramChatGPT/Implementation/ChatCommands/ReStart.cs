using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation.ChatCommands
{
    internal sealed class ReStart : IChatCommand
    {
        string IChatCommand.Name => "start";
        bool IChatCommand.IsAdminOnlyCommand => false;

        public Task Execute(IChat chat, IChatMessage message, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            chat.Reset(cancellationToken);
            return chat.SendSystemMessage(Strings.StartWarning, cancellationToken);
        }
    }
}