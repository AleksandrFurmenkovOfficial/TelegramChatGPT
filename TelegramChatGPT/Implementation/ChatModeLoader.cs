using Newtonsoft.Json;
using System.Globalization;
using System.Text;
using TelegramChatGPT.Interfaces;
using File = System.IO.File;

namespace TelegramChatGPT.Implementation
{
    internal sealed class ChatModeLoader : IChatModeLoader
    {
        private static readonly CompositeFormat DefaultDescription = CompositeFormat.Parse(Strings.DefaultDescription);

        public async Task<ChatMode> GetChatMode(string modeDescriptionFilename, CancellationToken cancellationToken = default)
        {
            var result = new ChatMode();

            string jsonString = await File.ReadAllTextAsync(GetPath(modeDescriptionFilename), cancellationToken).ConfigureAwait(false);
            var modeDescription = JsonConvert.DeserializeObject<ModeDescription>(jsonString);

            result.EnableFunctions = modeDescription?.EnableFunctions ?? false;

            var name = modeDescription?.AiName?.Trim();
            result.AiName = string.IsNullOrEmpty(name) ? Strings.DefaultName : name;

            var settings = modeDescription?.AiSettings?.Trim();
            result.AiSettings = string.IsNullOrEmpty(settings)
                ? string.Format(CultureInfo.InvariantCulture, DefaultDescription,
                    result!.AiName ?? Strings.DefaultName)
                : settings;

            if (modeDescription?.Messages == null)
            {
                return result!;
            }

            foreach (var example in modeDescription.Messages.Where(example =>
                         example is { Role: not null, Content: not null }))
            {
                AddMessageExample(example.Name, example.Role!, example.Content!);
            }

            return result!;

            void AddMessageExample(string? name, string messageRole, string message)
            {
                if (result!.Messages.Count == 0)
                {
                    result!.Messages.Add([]);
                }

                result!.Messages.Last().Add(new ChatMessage
                {
                    Role = messageRole,
                    Name = messageRole == Strings.RoleUser
                        ? name
                        : result.AiName,
                    Content = message
                });
            }
        }

        private static string GetPath(string mode)
        {
            string directory = AppContext.BaseDirectory;
            return $"{directory}/Modes/{mode}.json";
        }

        private sealed class ModeDescription
        {
            [JsonProperty("enableFunctions")] public bool EnableFunctions { get; set; }
            [JsonProperty("aiName")] public string? AiName { get; set; }
            [JsonProperty("aiSettings")] public string? AiSettings { get; set; }
            [JsonProperty("messages")] public List<Example>? Messages { get; set; }
        }

        private sealed class Example
        {
            [JsonProperty("name")] public string? Name { get; set; }
            [JsonProperty("role")] public string? Role { get; set; }
            [JsonProperty("content")] public string? Content { get; set; }
        }
    }
}