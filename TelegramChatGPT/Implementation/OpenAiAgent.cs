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
        IAiImagePainter aiImagePainter,
        IAiImageDescriptor aiGetImageDescription) : IAiAgent
    {
        private const string GptModel = "gpt-4-0125-preview";
        private static readonly ThreadLocal<Random> Random = new(() => new Random(Guid.NewGuid().GetHashCode()));
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
            return new OpenAiSimpleResponseGetter(openAiApi, GptModel, GetTemperature()).GetResponse(GptModel, question,
                data, cancellationToken);
        }

        public Task<Uri?> GetImage(string imageDescription, string userId,
            CancellationToken cancellationToken = default)
        {
            return aiImagePainter.GetImage(imageDescription, userId, cancellationToken);
        }

        public Task<string?> GetImageDescription(Uri image, string question, string? overridenSystemMessage = null,
            CancellationToken cancellationToken = default)
        {
            return aiGetImageDescription.GetImageDescription(image, question,
                string.IsNullOrEmpty(systemMessage) ? systemMessage : overridenSystemMessage, cancellationToken);
        }

        private static IDictionary<string, IAiFunction> GetAiFunctions()
        {
            var functions = new Dictionary<string, IAiFunction>();

            AddFunction(new GetImageByDescription());
            AddFunction(new GetImageDescription());

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
                Role = Strings.RoleFunction,
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
            return messages.Select(message => new Rystem.OpenAi.Chat.ChatMessage
            {
                StringableRole = message.Role ?? "",
                Content = message.Content,
                Name = message.Name
            });
        }

        private void AddFunctions(ChatRequestBuilder builder)
        {
            foreach (var function in functions)
            {
                _ = builder.WithFunction(function.Value.Description());
            }
        }

        private static double GetTemperature()
        {
            const double minTemperature = 0.333;
            const double oneThird = 1.0 / 3.0;
            if (Random.Value != null)
            {
                return minTemperature + (Random.Value.NextDouble() * oneThird);
            }

            return minTemperature;
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
                    .WithTemperature(GetTemperature());

                if (enableFunctions)
                {
                    AddFunctions(messageBuilder);
                }

                foreach (var message in convertedMessages)
                {
                    _ = messageBuilder.AddMessage(message);
                }

                string currentFunction = "";
                string currentFunctionArguments = "";
                const string functionCallReason = "function_call";
                await foreach (var streamingChatResult in messageBuilder.ExecuteAsStreamAsync(false, cancellationToken)
                                   .ConfigureAwait(false))
                {
                    var newPartOfResponse = streamingChatResult.LastChunk.Choices?.ElementAt(0);
                    var messageDelta = newPartOfResponse?.Delta?.Content ?? "";
                    var functionDelta = newPartOfResponse?.Delta?.Function;

                    if (!string.IsNullOrEmpty(messageDelta))
                    {
                        isCancelled = await streamGetter(new ResponseStreamChunk(messageDelta)).ConfigureAwait(false);
                        if (isCancelled)
                        {
                            return;
                        }
                    }
                    else if (functionDelta != null)
                    {
                        if (functionDelta.Name != null)
                        {
                            currentFunction = functionDelta.Name;
                        }
                        else
                        {
                            currentFunctionArguments += functionDelta.Arguments;
                        }
                    }
                    else if (newPartOfResponse?.FinishReason == functionCallReason)
                    {
                        isFunctionCall = true;
                        await CallFunction(currentFunction, currentFunctionArguments, userId, streamGetter,
                                cancellationToken)
                            .ConfigureAwait(false);
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