using RxTelegram.Bot;
using RxTelegram.Bot.Interface.BaseTypes;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation
{
    internal sealed partial class ChatMessageConverter(
        string telegramBotKey,
        ITelegramBotSource botSource) : IChatMessageConverter
    {
        private const string TelegramBotFile = "https://api.telegram.org/file/bot";
        private static readonly Regex WrongNameSymbolsRegExp = WrongNameSymbolsRegexpCreator();
        private ITelegramBot Bot => (ITelegramBot)botSource.TelegramBot();

        public async Task<IChatMessage> ConvertToChatMessage(object rawMessage, CancellationToken cancellationToken = default)
        {
            if (rawMessage is not Message castedMessage)
            {
                throw new InvalidCastException(nameof(rawMessage));
            }

            List<string> images = [];
            string forwardedFrom = "";
            string forwardedMessageContent = string.Empty;

            if (castedMessage.ForwardOrigin != null)
            {
                switch (castedMessage.ForwardOrigin)
                {
                    case MessageOriginChannel messageOriginChannel:
                        forwardedFrom = $"Channel \"{messageOriginChannel.Chat.Title}\"(@{messageOriginChannel.Chat.Username})";
                        break;

                    case MessageOriginChat messageOriginChat:
                        forwardedFrom = $"Chat \"{messageOriginChat.SenderChat.Title}\"(@{messageOriginChat.SenderChat.Username})";
                        break;

                    case MessageOriginHiddenUser messageOriginHiddenUser:
                        forwardedFrom = $"Hidden user \"{messageOriginHiddenUser.SenderUserName}\"";
                        break;

                    case MessageOriginUser messageOriginUser:
                        forwardedFrom = $"User \"{CompoundUserName(messageOriginUser.SenderUser)}\"";
                        break;
                }

                string replyToPhotoLink = castedMessage.ReplyToMessage?.Photo != null
                    ? await PhotoToLink(castedMessage.ReplyToMessage.Photo, cancellationToken).ConfigureAwait(false)
                    : string.Empty;

                var replyTo = castedMessage!.ReplyToMessage?.Text ?? castedMessage!.ReplyToMessage?.Caption;
                if (!string.IsNullOrEmpty(replyTo))
                {
                    forwardedMessageContent = replyTo;
                }

                if (!string.IsNullOrEmpty(replyToPhotoLink))
                {
                    images.Add(await Utils.EncodeImageToBase64(new Uri(replyToPhotoLink), cancellationToken).ConfigureAwait(false));
                }
            }

            string userPhotoLink = castedMessage!.Photo != null
                ? await PhotoToLink(castedMessage.Photo, cancellationToken).ConfigureAwait(false)
                : string.Empty;

            if (!string.IsNullOrEmpty(userPhotoLink))
            {
                images.Add(await Utils.EncodeImageToBase64(new Uri(userPhotoLink), cancellationToken).ConfigureAwait(false));
            }

            string userContent = (castedMessage!.Text ?? castedMessage!.Caption ?? string.Empty);

            var fromUser = CompoundUserName(castedMessage.From);
            forwardedFrom = string.IsNullOrEmpty(forwardedFrom) ? "" : "\nUser \"" + fromUser + "\" forwarded message from " + forwardedFrom + ".";
            if (!string.IsNullOrEmpty(forwardedFrom) && string.IsNullOrEmpty(forwardedMessageContent))
            {
                forwardedMessageContent = userContent;
                userContent = "";
            }

            if (!string.IsNullOrEmpty(forwardedMessageContent))
            {
                forwardedMessageContent = "\nForwardedContent: \"" + forwardedMessageContent + "\"";
            }

            var resultMessage = new ChatMessage
            {
                MessageId = new MessageId(castedMessage!.MessageId.ToString(CultureInfo.InvariantCulture)),
                Name = fromUser,
                Role = Strings.RoleUser,
                Content = userContent + forwardedFrom + forwardedMessageContent,
                ImagesInBase64 = images
            };

            return resultMessage;
        }

        private static string CompoundUserName(User user)
        {
            string input = RemoveWrongSymbols($"{user.FirstName}_{user.LastName}_{user.Username}");
            var result = WrongNameSymbolsRegExp.Replace(input, string.Empty).Replace(' ', '_').TrimStart('_')
                .TrimEnd('_');
            if (result.Replace("_", string.Empty, StringComparison.InvariantCultureIgnoreCase).Length == 0)
            {
                result = $"User{user.Id}";
            }

            return result.Replace("__", "_", StringComparison.InvariantCultureIgnoreCase);

            static string RemoveWrongSymbols(string input)
            {
                var cleanText = new StringBuilder();
                foreach (var ch in input.Where(ch =>
                             char.IsAscii(ch) && !char.IsSurrogate(ch) && !IsEmoji(ch) &&
                             (ch == '_' || char.IsAsciiLetterOrDigit(ch))))
                {
                    cleanText.Append(ch);
                }

                return cleanText.ToString();

                static bool IsEmoji(char ch)
                {
                    var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(ch);
                    return unicodeCategory == UnicodeCategory.OtherSymbol;
                }
            }
        }

        private async Task<string> PhotoToLink(IEnumerable<PhotoSize> photos,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return await Task.FromCanceled<string>(cancellationToken).ConfigureAwait(false);
            }

            var photoSize = photos.Last();
            var file = await Bot.GetFile(photoSize.FileId, cancellationToken).ConfigureAwait(false);
            return $"{new Uri(new Uri($"{TelegramBotFile}{telegramBotKey}/"), file.FilePath)}";
        }

        [GeneratedRegex("[^a-zA-Z0-9_\\s-]")]
        private static partial Regex WrongNameSymbolsRegexpCreator();
    }
}