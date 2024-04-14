using Newtonsoft.Json;
using Rystem.OpenAi;
using Rystem.OpenAi.Chat;
using TelegramChatGPT.Implementation.AiFunctions;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation
{
    internal sealed class OpenAiAgent(
        string aiName,
        string systemMessage,
        bool enableFunctions,
        IOpenAi openAiApi,
        IAiImagePainter aiImagePainter) : IAiAgent, IAiImagePainter
    {
        private const string GptModel = "gpt-4-turbo-2024-04-09";
        private readonly IDictionary<string, IAiFunction> functions = GetAiFunctions();

        public string AiName => aiName;

        public Task GetResponse(
            string chatId,
            IEnumerable<IChatMessage> messages,
            Func<ResponseStreamChunk, Task<bool>> streamGetter,
            CancellationToken cancellationToken = default)
        {
            return GetAiResponseImpl(ConvertMessages(messages), streamGetter, chatId, cancellationToken);
        }

        public Task<string?> GetResponse(string setting,
            string question,
            string? data,
            CancellationToken cancellationToken = default)
        {
            return new OpenAiSimpleResponseGetter(openAiApi, GptModel, Utils.GetRandTemperature()).GetResponse(GptModel, question,
                data, cancellationToken);
        }

        public Task<Uri?> GetImage(string imageDescription, string userId,
            CancellationToken cancellationToken = default)
        {
            return aiImagePainter.GetImage(imageDescription, userId, cancellationToken);
        }

        private static IDictionary<string, IAiFunction> GetAiFunctions()
        {
            var functions = new Dictionary<string, IAiFunction>();

            AddFunction(new GetImageByDescription());

            AddFunction(new GetLastEntriesFromMyDiary());
            AddFunction(new GetAnswerFromDiaryAboutUser());
            AddFunction(new SaveEntryToMyDiary());

            AddFunction(new GetInformationFromUrl());

            return functions;

            void AddFunction(IAiFunction function)
            {
                functions.Add(function.Name, function);
            }
        }

        private static string CallInfo(string function, string parameters)
        {
            return $"{{\"thought\": I called function \"{function}\" with arguments \"{parameters}\".}}";
        }

        private IEnumerable<ChatMessage> CreateFunctionResultMessages(
            string functionName,
            string parameters,
            AiFunctionResult result)
        {
            var callMessage = new ChatMessage
            {
                Role = Strings.RoleAssistant,
                Content = CallInfo(functionName, parameters),
                Name = aiName
            };

            var resultMessage = new ChatMessage
            {
                Role = Strings.RoleAssistant,
                Content = $"{{\"result\": {JsonConvert.SerializeObject(result.Result)}}}",
                Name = functionName,
                ImageUrl = result.ImageUrl
            };

            return [callMessage, resultMessage];
        }

        private async Task CallFunction(string functionName,
            string functionArguments,
            string userId,
            Func<ResponseStreamChunk, Task<bool>> streamGetter,
            CancellationToken cancellationToken = default)
        {
            var resultMessages = new List<IChatMessage>();
            try
            {
                AppLogger.LogDebugMessage($"{aiName} calls function {functionName}({functionArguments})");
                var result = await functions[functionName].Call(this, functionArguments, userId, cancellationToken)
                    .ConfigureAwait(false);
                resultMessages.AddRange(CreateFunctionResultMessages(functionName, functionArguments, result));
            }
            catch (Exception e)
            {
                resultMessages.AddRange(CreateFunctionResultMessages(functionName, functionArguments,
                    new AiFunctionResult("Exception: Can't call function " + functionName + " (" + functionArguments +
                                         "); Possible issues:\n1. function Name is incorrect\n2. wrong arguments are provided\n3. internal function error\nException message: " +
                                         e.Message)));
            }

            await streamGetter(new ResponseStreamChunk(resultMessages)).ConfigureAwait(false);
        }

        private static IEnumerable<Rystem.OpenAi.Chat.ChatMessage> ConvertMessages(IEnumerable<IChatMessage> messages)
        {
            var result = new List<Rystem.OpenAi.Chat.ChatMessage>();

            foreach (var message in messages)
            {
                if (message.ImagesInBase64 != null && message.ImagesInBase64.Count > 0)
                {
                    List<object> values = [];

                    if (!string.IsNullOrWhiteSpace(message.Content))
                    {
                        values.Add(new
                        {
                            type = "text",
                            text = !string.IsNullOrEmpty(message.Content) ? message.Content : Strings.WhatIsOnTheImage
                        });
                    }

                    foreach (var image in message.ImagesInBase64)
                    {
                        values.Add(new
                        {
                            type = "image_url",
                            image_url = new
                            {
                                url = $"data:image/jpeg;base64,{image}"
                            }
                        });
                    }

                    result.Add(new Rystem.OpenAi.Chat.ChatMessage
                    {
                        StringableRole = message.Role ?? Strings.RoleAssistant,
                        Content = values
                    });
                }
                else if (!string.IsNullOrWhiteSpace(message.Content))
                {
                    result.Add(new Rystem.OpenAi.Chat.ChatMessage
                    {
                        StringableRole = message.Role ?? Strings.RoleAssistant,
                        Content = message.Content
                    });
                }
            }

            return result;
        }

        private void AddFunctions(ChatRequestBuilder builder)
        {
            foreach (var function in functions)
            {
                _ = builder.WithFunction(function.Value.Description());
            }
        }

        private async Task GetAiResponseImpl(
            IEnumerable<Rystem.OpenAi.Chat.ChatMessage> convertedMessages,
            Func<ResponseStreamChunk, Task<bool>> streamGetter,
            string userId,
            CancellationToken cancellationToken = default)
        {
            bool isFunctionCall = false;
            bool isCancelled = false;
            try
            {
                var messageBuilder = openAiApi.Chat.Request(new Rystem.OpenAi.Chat.ChatMessage
                { Role = ChatRole.System, Content = systemMessage })
                    .WithModel(GptModel)
                    .WithTemperature(Utils.GetRandTemperature());

                if (enableFunctions)
                {
                    AddFunctions(messageBuilder);
                }

                foreach (var message in convertedMessages)
                {
                    _ = messageBuilder.AddMessage(message);
                }

                string functionName = "";
                string functionArgs = "";
                const string functionCallReason = "tool_calls";
                await foreach (var streamingChatResult in messageBuilder.ExecuteAsStreamAsync(false, cancellationToken)
                                   .ConfigureAwait(false))
                {
                    var responseDelta = streamingChatResult.LastChunk.Choices?.ElementAt(0);
                    var messageDelta = responseDelta?.Delta?.Content?.ToString();
                    if (!string.IsNullOrEmpty(messageDelta))
                    {
                        isCancelled = await streamGetter(new ResponseStreamChunk(messageDelta)).ConfigureAwait(false);
                        if (isCancelled)
                        {
                            return;
                        }
                    }
                    else if (responseDelta?.FinishReason != functionCallReason)
                    {
                        var functionCall = responseDelta?.Delta?.ToolCalls?[0]?.Function;
                        if (functionCall != null)
                        {
                            isFunctionCall = true;
                            if (!string.IsNullOrWhiteSpace(functionCall.Name))
                            {
                                functionName = functionCall.Name ?? "";
                            }

                            functionArgs += functionCall.Arguments ?? "";
                        }
                    }
                    else if (isFunctionCall && !string.IsNullOrEmpty(functionName))
                    {
                        await CallFunction(functionName, functionArgs, userId, streamGetter, cancellationToken).ConfigureAwait(false);
                        return;
                    }
                }
            }
            finally
            {
                if (!isFunctionCall && !isCancelled)
                {
                    await streamGetter(new LastResponseStreamChunk()).ConfigureAwait(false);
                }
            }
        }
    }
}