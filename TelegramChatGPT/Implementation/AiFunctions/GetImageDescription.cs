using Newtonsoft.Json;
using System.Text.Json.Serialization;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation.AiFunctions
{
    internal sealed class GetImageDescription : AiFunctionBase
    {
        public override string Name => nameof(GetImageDescription);

        public override JsonFunction Description()
        {
            return new JsonFunction
            {
                Name = Name,
                Description =
                    "This function enables detailed image descriptions by leveraging another GPT-Vision Ai.\n" +
                    "Your rating for the function: 9 out of 10.",

                Parameters = new JsonFunctionNonPrimitiveProperty()
                    .AddPrimitive("ImageUrlToDescribe", new JsonFunctionProperty
                    {
                        Type = "string",
                        Description = "A full path URL of the image for which a detailed description is sought."
                    })
                    .AddRequired("ImageUrlToDescribe")
                    .AddPrimitive("QuestionAboutImage", new JsonFunctionProperty
                    {
                        Type = "string",
                        Description = "An additional question regarding the image's content."
                    })
                    .AddRequired("QuestionAboutImage")
            };
        }

        public override async Task<AiFunctionResult> Call(IAiAgent api, string parameters, string userId,
            CancellationToken cancellationToken = default)
        {
            var deserializedParameters =
                JsonConvert.DeserializeObject<GetImageDescriptionRequest>(parameters) ??
                throw new ArgumentNullException(nameof(parameters));
            string description = (await api.GetImageDescription(new Uri(deserializedParameters.ImageUrlToDescribe),
                deserializedParameters.QuestionAboutImage, "", cancellationToken).ConfigureAwait(false))!;
            return new AiFunctionResult(description);
        }

        private sealed class GetImageDescriptionRequest(string imageUrlToDescribe, string questionAboutImage)
        {
            [JsonProperty] public string ImageUrlToDescribe { get; } = imageUrlToDescribe;

            [JsonProperty] public string QuestionAboutImage { get; } = questionAboutImage;
        }
    }
}