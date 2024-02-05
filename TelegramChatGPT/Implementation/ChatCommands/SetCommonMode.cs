using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation.ChatCommands
{
    internal sealed class SetCommonMode(IChatModeLoader modeLoader) : IChatCommand
    {
        string IChatCommand.Name => "common";
        bool IChatCommand.IsAdminOnlyCommand => false;

        readonly IChatModeLoader modeLoader = modeLoader;

        public async Task Execute(IChat chat, IChatMessage message, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var mode = await modeLoader.GetChatMode(IChatModeLoader.CommonMode, cancellationToken).ConfigureAwait(false);
            chat.SetMode(mode);
            await chat.SendSystemMessage(Strings.CommonModeNow, cancellationToken).ConfigureAwait(false);
        }
    }
}