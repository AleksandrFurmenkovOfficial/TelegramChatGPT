using TelegramChatGPT.Implementation.ChatMessageActions;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation
{
    internal sealed class Chat(string chatId, IAiAgentFactory aiAgentFactory, IMessenger messenger) : IChat
    {
        private const int MessageUpdateStepInCharsCount = 42;

        private readonly List<List<IChatMessage>> messages = [];

        private readonly IAiAgentFactory aiAgentFactory = aiAgentFactory;
        private readonly IMessenger messenger = messenger;

        private IAiAgent? aiAgent;

        public string Id { get; } = chatId;

        public async Task SendSomethingGoesWrong(CancellationToken cancellationToken)
        {
            var message = new ChatMessage(Strings.SomethingGoesWrong);
            var messageId = await messenger.SendMessage(Id, message, new List<ActionId> { RetryAction.Id }, cancellationToken).ConfigureAwait(false);
            message.MessageId = new MessageId(messageId);

            AddAnswerMessage(message);
        }

        public async Task DoResponseToMessage(IChatMessage message, CancellationToken cancellationToken)
        {
            await UpdateLastMessageButtons(default).ConfigureAwait(false);

            AddNewUsersMessage(message);
            await DoStreamResponseToLastMessage(null, cancellationToken).ConfigureAwait(false);
        }

        public async Task Reset(CancellationToken cancellationToken)
        {
            await UpdateLastMessageButtons(default).ConfigureAwait(false);

            messages.Clear();
        }

        public Task SendSystemMessage(string content, CancellationToken cancellationToken = default)
        {
            return messenger.SendMessage(Id, new ChatMessage
            {
                Content = content
            }, null, cancellationToken);
        }

        public async Task RegenerateLastResponse(CancellationToken cancellationToken)
        {
            await RemoveResponse(default).ConfigureAwait(false);
            await DoStreamResponseToLastMessage(null, cancellationToken).ConfigureAwait(false);
        }

        public async Task ContinueLastResponse(CancellationToken cancellationToken)
        {
            await DoResponseToMessage(new ChatMessage(IChatMessage.InternalMessageId, Strings.Continue,
                Strings.RoleUser, Strings.RoleSystem), cancellationToken).ConfigureAwait(false);
        }

        public async Task RemoveResponse(CancellationToken cancellationToken)
        {
            var lastPack = messages.Last();
            var initialUserInput = lastPack.First();
            foreach (var message in lastPack.Where(message =>
                         !message.MessageId.Equals(IChatMessage.InternalMessageId) &&
                         message != initialUserInput))
            {
                await messenger.DeleteMessage(Id, message.MessageId, cancellationToken).ConfigureAwait(false);
            }

            lastPack.RemoveRange(1, lastPack.Count - 1);
        }

        private async Task UpdateLastMessageButtons(CancellationToken cancellationToken)
        {
            if (messages.Count == 0)
            {
                return;
            }

            var lastMessages = messages.Last();
            var lastMessage = lastMessages.Last();

            if (lastMessage.Role != Strings.RoleAssistant)
            {
                return;
            }

            if (lastMessage.MessageId.Equals(IChatMessage.InternalMessageId))
            {
                return;
            }

            var content = (lastMessage.Content?.Length ?? 0) > 0 ? lastMessage.Content : Strings.InitAnswerTemplate;
            await UpdateMessage(lastMessage, content, null, cancellationToken).ConfigureAwait(false);
        }

        private static bool IsMediaMessage(IChatMessage message)
        {
            return message.ImageUrl != null;
        }

        private async Task UpdateMessage(IChatMessage message, string? newContent,
            IEnumerable<ActionId>? newActions = null, CancellationToken cancellationToken = default)
        {
            if (IsMediaMessage(message))
            {
                await messenger.EditMessageCaption(Id, message.MessageId, newContent, newActions, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await messenger.EditTextMessage(Id, message.MessageId, newContent, newActions, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private IChatMessage CreateInitMessage()
        {
            return new ChatMessage
            {
                Role = Strings.RoleAssistant,
                Name = aiAgent?.AiName ?? Strings.DefaultName,
                Content = Strings.InitAnswerTemplate
            };
        }

        private void AddNewUsersMessage(IChatMessage newMessageFromUser)
        {
            messages.Add([newMessageFromUser]);
        }

        private void AddAnswerMessage(IChatMessage responseTargetMessage)
        {
            messages.Last()?.Add(responseTargetMessage);
        }

        private async Task DoStreamResponseToLastMessage(IChatMessage? responseTargetMessage = null,
            CancellationToken cancellationToken = default)
        {
            responseTargetMessage ??= await SendResponseTargetMessage(default).ConfigureAwait(false);

            await aiAgent!.GetResponse(Id, messages.SelectMany(subList => subList.ToList()),
                    Task<bool> (contentDelta) =>
                        ProcessAsyncResponse(responseTargetMessage, contentDelta, cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<bool> ProcessAsyncResponse(
            IChatMessage responseTargetMessage,
            ResponseStreamChunk contentDelta,
            CancellationToken cancellationToken = default)
        {
            bool textStreamUpdate = contentDelta.Messages.Count == 0;
            var finalUpdate = contentDelta is LastResponseStreamChunk || cancellationToken.IsCancellationRequested;

            if (textStreamUpdate || finalUpdate)
            {
                bool res = await UpdateTargetMessage(responseTargetMessage, contentDelta.TextDelta ?? "", finalUpdate, default).ConfigureAwait(false);
                finalUpdate |= !res;
                if (!finalUpdate)
                {
                    return false;
                }

                AddAnswerMessage(responseTargetMessage);
                return true;
            }

            await ProcessFunctionResult(responseTargetMessage, contentDelta, cancellationToken).ConfigureAwait(false);
            return true;
        }

        private async Task ProcessFunctionResult(IChatMessage responseTargetMessage, ResponseStreamChunk contentDelta,
            CancellationToken cancellationToken = default)
        {
            var functionCallMessage = contentDelta.Messages.First();
            AddAnswerMessage(functionCallMessage);

            var functionResultMessage = contentDelta.Messages.Last();
            AddAnswerMessage(functionResultMessage);

            bool imageMessage = functionResultMessage.ImageUrl != null;
            if (imageMessage)
            {
                await messenger.DeleteMessage(Id, responseTargetMessage.MessageId, default)
                    .ConfigureAwait(false);
                var newMessageId = await messenger.SendPhotoMessage(Id, functionResultMessage.ImageUrl!,
                    Strings.InitAnswerTemplate, [StopAction.Id], cancellationToken).ConfigureAwait(false);
                var responseTargetMessageNew = new ChatMessage(IChatMessage.InternalMessageId,
                    Strings.InitAnswerTemplate,
                    Strings.RoleAssistant, aiAgent?.AiName ?? Strings.DefaultName, functionResultMessage.ImageUrl)
                {
                    Content = "",
                    MessageId = new MessageId(newMessageId)
                };
                await DoStreamResponseToLastMessage(responseTargetMessageNew, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await DoStreamResponseToLastMessage(responseTargetMessage, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<bool> UpdateTargetMessage(
            IChatMessage responseTargetMessage,
            string textContentDelta,
            bool finalUpdate,
            CancellationToken cancellationToken = default)
        {
            responseTargetMessage.Content += textContentDelta;
            if (responseTargetMessage.Content.Length % MessageUpdateStepInCharsCount != 1 && !finalUpdate)
            {
                return true;
            }

            bool hasContent = responseTargetMessage.Content.Length > 0;
            bool hasMedia = responseTargetMessage.ImageUrl != null;
            switch (hasMedia)
            {
                case true when responseTargetMessage.Content.Length > IMessenger.MaxCaptionLen:
                    responseTargetMessage.Content = responseTargetMessage.Content[..IMessenger.MaxCaptionLen];
                    finalUpdate = true;
                    break;
                case false when responseTargetMessage.Content.Length > IMessenger.MaxTextLen:
                    responseTargetMessage.Content = responseTargetMessage.Content[..IMessenger.MaxTextLen];
                    finalUpdate = true;
                    break;
            }

            var newContent = hasContent ? responseTargetMessage.Content : Strings.InitAnswerTemplate;
            List<ActionId> actions = finalUpdate
                ? hasContent ? [ContinueAction.Id, RegenerateAction.Id] : [RetryAction.Id]
                : [StopAction.Id];
            await UpdateMessage(responseTargetMessage, newContent, actions, cancellationToken).ConfigureAwait(false);

            return !finalUpdate;
        }

        private async Task<IChatMessage> SendResponseTargetMessage(CancellationToken cancellationToken)
        {
            var responseTargetMessage = CreateInitMessage();
            responseTargetMessage.MessageId = new MessageId(await messenger
                .SendMessage(Id, responseTargetMessage, new List<ActionId> { CancelAction.Id }, cancellationToken)
                .ConfigureAwait(false));
            responseTargetMessage.Content = string.Empty;
            return responseTargetMessage;
        }

        public void SetMode(ChatMode mode)
        {
            aiAgent = aiAgentFactory.CreateAiAgent(mode.AiName, mode.AiSettings, mode.EnableFunctions);
            messages.Clear();
            messages.AddRange(mode.Messages);
        }
    }
}