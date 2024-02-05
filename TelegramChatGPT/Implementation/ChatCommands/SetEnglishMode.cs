using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation.ChatCommands
{
    internal sealed class SetEnglishMode(IChatModeLoader modeLoader) : IChatCommand
    {
        const string EnglishTeacherMode = "EnglishTeacherMode";

        string IChatCommand.Name => "english";
        bool IChatCommand.IsAdminOnlyCommand => false;

        readonly IChatModeLoader modeLoader = modeLoader;

        public async Task Execute(IChat chat, IChatMessage message, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var mode = await modeLoader.GetChatMode(EnglishTeacherMode, cancellationToken).ConfigureAwait(false);
            chat.SetMode(mode);
            await chat.SendSystemMessage(Strings.EnglishModeNow, cancellationToken).ConfigureAwait(false);
        }
    }
}