using System.Text.Json.Serialization;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation.AiFunctions
{
    internal sealed class GetLastEntriesFromMyDiary : AiFunctionBase
    {
        public override string Name => nameof(GetLastEntriesFromMyDiary);

        public override JsonFunction Description()
        {
            return new JsonFunction
            {
                Name = Name,
                Description =
                    "This must be the first function called in a new dialogue! Why? It enables you to read and recall the last nine entries from your diary.\n" +
                    "Your rating for the function: 10 out of 10.",

                Parameters = new JsonFunctionNonPrimitiveProperty()
            };
        }

        public override async Task<AiFunctionResult> Call(IAiAgent api, string parameters, string userId,
            CancellationToken cancellationToken = default)
        {
            string path = GetPathToUserAssociatedMemories(api.AiName, userId);
            if (!File.Exists(path))
            {
                return new AiFunctionResult("There are no records in the long-term memory associated with this user.");
            }

            string data = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            string firstNineRecordsAsString = string.Join(Environment.NewLine, data.Split(Environment.NewLine).Take(9));
            return new AiFunctionResult(firstNineRecordsAsString);
        }
    }
}