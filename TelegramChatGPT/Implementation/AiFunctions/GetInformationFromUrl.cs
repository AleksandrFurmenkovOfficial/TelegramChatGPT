using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation.AiFunctions
{
    internal sealed class GetInformationFromUrl : AiFunctionBase
    {
        public override string Name => nameof(GetInformationFromUrl);

        public override JsonFunction Description()
        {
            return new JsonFunction
            {
                Name = Name,
                Description =
                    "This function retrieves and analyzes the content of a specified webpage. It allows you to get actual weather, currency, news, etc.\n" +
                    "Your rating for the function: 8 out of 10.",

                Parameters = new JsonFunctionNonPrimitiveProperty()
                    .AddPrimitive("Url", new JsonFunctionProperty
                    {
                        Type = "string",
                        Description = "The URL of the webpage to be analyzed.\n" +
                                      "Prefer simple web pages, ideally with plain text data, over complex URLs loaded with scripts and other elements."
                    })
                    .AddRequired("Url")
                    .AddPrimitive("Question", new JsonFunctionProperty
                    {
                        Type = "string",
                        Description = "A question regarding the information to be extracted from the webpage."
                    })
                    .AddRequired("Question")
            };
        }

        private static async Task<string> GetTextContentOnly(Uri url, CancellationToken cancellationToken = default)
        {
            using (var httpClient = new HttpClient())
            {
                string responseBody = await httpClient.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(responseBody);
                return htmlDocument.DocumentNode.InnerText;
            }
        }

        public override async Task<AiFunctionResult> Call(IAiAgent api, string parameters, string userId,
            CancellationToken cancellationToken = default)
        {
            var deserializedParameters =
                JsonConvert.DeserializeObject<GetInformationFromUrlRequest>(parameters) ??
                throw new ArgumentNullException(nameof(parameters));
            string textContent =
                await GetTextContentOnly(new Uri(deserializedParameters.Url), cancellationToken).ConfigureAwait(false);
            return new AiFunctionResult((await api.GetResponse(
                "I am tasked with extracting facts from the given text.", deserializedParameters.Question,
                textContent, cancellationToken).ConfigureAwait(false))!);
        }

        private sealed class GetInformationFromUrlRequest(string url, string question)
        {
            [JsonProperty] public string Url { get; } = url;

            [JsonProperty] public string Question { get; } = question;
        }
    }
}