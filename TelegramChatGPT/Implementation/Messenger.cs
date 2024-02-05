using RxTelegram.Bot;
using RxTelegram.Bot.Interface.BaseTypes;
using RxTelegram.Bot.Interface.BaseTypes.Enums;
using RxTelegram.Bot.Interface.BaseTypes.Requests.Attachments;
using RxTelegram.Bot.Interface.BaseTypes.Requests.Messages;
using System.Collections.Concurrent;
using System.Globalization;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation
{
    internal sealed class Messenger(
        ConcurrentDictionary<string, ConcurrentDictionary<string, ActionId>> actionsMapping,
        ITelegramBotSource telegramBotSource)
        : IMessenger
    {
        private const ParseMode MainParseMode = ParseMode.Markdown;
        private const ParseMode FallbackParseMode = ParseMode.HTML;

        private ITelegramBot Bot => (ITelegramBot)telegramBotSource.TelegramBot();

        public Task<bool> DeleteMessage(string chatId, MessageId messageId,
            CancellationToken cancellationToken = default)
        {
            return Bot.DeleteMessage(new DeleteMessage
            {
                ChatId = Utils.StrToLong(chatId),
                MessageId = Utils.MessageIdToInt(messageId)
            }, cancellationToken);
        }

        public async Task<string> SendMessage(string chatId, IChatMessage message,
            IEnumerable<ActionId>? messageActionIds = null, CancellationToken cancellationToken = default)
        {
            return await ReTry().ConfigureAwait(false);

            async Task<string> SendMessageInternal(ParseMode parseMode)
            {
                var sendMessageRequest = new SendMessage
                {
                    ChatId = Utils.StrToLong(chatId),
                    Text = message.Content,
                    ReplyMarkup = GetInlineKeyboardMarkup(chatId, messageActionIds),
                    ParseMode = parseMode
                };
                var sentMessage = await Bot.SendMessage(sendMessageRequest, cancellationToken).ConfigureAwait(false);
                return sentMessage.MessageId.ToString(CultureInfo.InvariantCulture);
            }

            async Task<string> ReTry(int tryCount = 3)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return await Task.FromCanceled<string>(cancellationToken).ConfigureAwait(false);
                }

                try
                {
                    return await SendMessageInternal(MainParseMode).ConfigureAwait(false);
                }
                catch
                {
                    try
                    {
                        return await SendMessageInternal(FallbackParseMode).ConfigureAwait(false);
                    }
                    catch
                    {
                        if (tryCount > 0)
                        {
                            return await ReTry(tryCount - 1).ConfigureAwait(false);
                        }

                        throw;
                    }
                }
            }
        }

        public async Task EditTextMessage(string chatId, MessageId messageId, string? newContent,
            IEnumerable<ActionId>? messageActionIds = null, CancellationToken cancellationToken = default)
        {
            await ReTry().ConfigureAwait(false);
            return;

            async Task ReTry(int tryCount = 3)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    await EditTextMessageInternal(MainParseMode).ConfigureAwait(false);
                }
                catch
                {
                    try
                    {
                        await EditTextMessageInternal(FallbackParseMode).ConfigureAwait(false);
                    }
                    catch
                    {
                        if (tryCount > 0)
                        {
                            await ReTry(tryCount - 1).ConfigureAwait(false);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            async Task EditTextMessageInternal(ParseMode parseMode)
            {
                var editMessageRequest = new EditMessageText
                {
                    ChatId = Utils.StrToLong(chatId),
                    MessageId = Utils.MessageIdToInt(messageId),
                    Text = newContent,
                    ReplyMarkup = GetInlineKeyboardMarkup(chatId, messageActionIds),
                    ParseMode = parseMode
                };

                await Bot.EditMessageText(editMessageRequest, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<string> SendPhotoMessage(string chatId, Uri imageUrl, string? caption,
            IEnumerable<ActionId>? messageActionIds = null, CancellationToken cancellationToken = default)
        {
            return await ReTry().ConfigureAwait(false);

            async Task<string> ReTry(int tryCount = 3)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return await Task.FromCanceled<string>(cancellationToken).ConfigureAwait(false);
                }

                try
                {
                    var imageStream = await Utils.GetStreamFromUrlAsync(imageUrl, cancellationToken)
                        .ConfigureAwait(false);
                    await using (imageStream.ConfigureAwait(false))
                    {
                        return await SendPhotoMessageInternal(MainParseMode, imageStream).ConfigureAwait(false);
                    }
                }
                catch
                {
                    try
                    {
                        var imageStream = await Utils.GetStreamFromUrlAsync(imageUrl, cancellationToken)
                            .ConfigureAwait(false);
                        await using (imageStream.ConfigureAwait(false))
                        {
                            return await SendPhotoMessageInternal(FallbackParseMode, imageStream).ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                        if (tryCount > 0)
                        {
                            return await ReTry(tryCount - 1).ConfigureAwait(false);
                        }

                        throw;
                    }
                }
            }

            async Task<string> SendPhotoMessageInternal(ParseMode parseMode, Stream imageStream)
            {
                var sendPhotoMessage = new SendPhoto
                {
                    ChatId = Utils.StrToLong(chatId),
                    Photo = new InputFile(imageStream),
                    Caption = caption,
                    ReplyMarkup = GetInlineKeyboardMarkup(chatId, messageActionIds),
                    ParseMode = parseMode
                };

                return (await Bot.SendPhoto(sendPhotoMessage, cancellationToken).ConfigureAwait(false)).MessageId
                    .ToString(CultureInfo
                        .InvariantCulture);
            }
        }

        public async Task EditMessageCaption(string chatId, MessageId messageId, string? caption,
            IEnumerable<ActionId>? messageActionIds = null, CancellationToken cancellationToken = default)
        {
            await ReTry().ConfigureAwait(false);
            return;

            async Task ReTry(int tryCount = 3)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    await EditMessageCaptionInternal(MainParseMode).ConfigureAwait(false);
                }
                catch
                {
                    try
                    {
                        await EditMessageCaptionInternal(FallbackParseMode).ConfigureAwait(false);
                    }
                    catch
                    {
                        if (tryCount > 0)
                        {
                            await ReTry(tryCount - 1).ConfigureAwait(false);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            async Task EditMessageCaptionInternal(ParseMode parseMode)
            {
                var editCaptionRequest = new EditMessageCaption
                {
                    ChatId = Utils.StrToLong(chatId),
                    MessageId = Utils.MessageIdToInt(messageId),
                    Caption = caption,
                    ReplyMarkup = GetInlineKeyboardMarkup(chatId, messageActionIds),
                    ParseMode = parseMode
                };

                await Bot.EditMessageCaption(editCaptionRequest, cancellationToken).ConfigureAwait(false);
            }
        }

        private InlineKeyboardMarkup? GetInlineKeyboardMarkup(string chatId, IEnumerable<ActionId>? messageActionIds)
        {
            var messageActionIdsList = messageActionIds?.ToList();
            if (messageActionIdsList == null || messageActionIdsList.Count == 0)
            {
                return null;
            }

            var mapping = actionsMapping.GetOrAdd(chatId, _ => []);
            mapping.Clear();
            var buttons = new List<InlineKeyboardButton>();
            foreach (var callbackId in messageActionIdsList)
            {
                var token = Guid.NewGuid().ToString();
                _ = mapping.TryAdd(token, callbackId);
                buttons.Add(new InlineKeyboardButton
                {
                    Text = callbackId.Name,
                    CallbackData = token
                });
            }

            return new InlineKeyboardMarkup
            {
                InlineKeyboard = new[] { buttons }
            };
        }
    }
}