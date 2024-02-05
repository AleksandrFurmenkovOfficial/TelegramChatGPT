using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation.ChatCommands
{
    internal sealed class ChatCommandProcessor : IChatCommandProcessor
    {
        private readonly IAdminChecker adminChecker;
        private readonly Dictionary<string, IChatCommand> commands = [];

        public ChatCommandProcessor(
            IEnumerable<IChatCommand> commands,
            IAdminChecker adminChecker)
        {
            this.adminChecker = adminChecker;
            foreach (var command in commands)
            {
                this.commands.Add($"/{command.Name}", command);
            }
        }

        public async Task<bool> ExecuteIfChatCommand(IChat chat, IChatMessage message,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return await Task.FromCanceled<bool>(cancellationToken).ConfigureAwait(false);
            }

            if (string.IsNullOrEmpty(message.Content))
            {
                return false;
            }

            var text = message.Content;
            foreach ((string commandName, IChatCommand command) in commands.Where(value =>
                         text.Trim().Contains(value.Key, StringComparison.InvariantCultureIgnoreCase)))
            {
                if (command.IsAdminOnlyCommand && !adminChecker.IsAdmin(chat.Id))
                {
                    return false;
                }

                message.Content = text[commandName.Length..];
                await command.Execute(chat, message, cancellationToken).ConfigureAwait(false);
                return true;
            }

            return false;
        }
    }
}