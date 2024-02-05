using Newtonsoft.Json;
using System.Globalization;
using System.Text.Json.Serialization;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation.AiFunctions
{
    internal sealed class SaveEntryToMyDiary : AiFunctionBase
    {
        public override string Name => nameof(SaveEntryToMyDiary);

        public override JsonFunction Description()
        {
            return new JsonFunction
            {
                Name = Name,
                Description = "This function enables you to create a new entry in your diary.\n" +
                              "Your rating for the function: 10 out of 10.",

                Parameters = new JsonFunctionNonPrimitiveProperty()
                    .AddPrimitive("DiaryEntry", new JsonFunctionProperty
                    {
                        Type = "string",
                        Description =
                            "The diary entry to be recorded, encompassing your plans, facts, thoughts, reasoning, conjectures, and impressions."
                    })
                    .AddRequired("DiaryEntry")
            };
        }

        public override async Task<AiFunctionResult> Call(IAiAgent api, string parameters, string userId,
            CancellationToken cancellationToken = default)
        {
            string path = GetPathToUserAssociatedMemories(api.AiName, userId);
            string directory = Path.GetDirectoryName(path) ?? "";

            if (!Directory.Exists(directory))
            {
                _ = Directory.CreateDirectory(directory);
            }

            var deserializedParameters =
                JsonConvert.DeserializeObject<SaveEntryToMyDiaryRequest>(parameters) ??
                throw new ArgumentNullException(nameof(parameters));
            string timestamp = DateTime.Now.ToString("[dd/MM/yyyy|HH:mm]", CultureInfo.InvariantCulture);
            string line = $"{timestamp}|{deserializedParameters.DiaryEntry}";

            await File.AppendAllTextAsync(path, line + Environment.NewLine, cancellationToken).ConfigureAwait(false);
            return new AiFunctionResult("The diary entry has been successfully recorded.");
        }

        private sealed class SaveEntryToMyDiaryRequest(string diaryEntry)
        {
            [JsonProperty] public string DiaryEntry { get; } = diaryEntry;
        }
    }
}