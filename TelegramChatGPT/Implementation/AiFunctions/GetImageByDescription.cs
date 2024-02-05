using Newtonsoft.Json;
using System.Text.Json.Serialization;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation.AiFunctions
{
    internal sealed class GetImageByDescription : AiFunctionBase
    {
        public override string Name => nameof(GetImageByDescription);

        public override JsonFunction Description()
        {
            return new JsonFunction
            {
                Name = Name,
                Description =
                    "This function enables the creation of an image based on a detailed text description provided by the user.\n" +
                    "Your rating for the function: 9.5 out of 10.",

                Parameters = new JsonFunctionNonPrimitiveProperty()
                    .AddPrimitive("ImageDescription", new JsonFunctionProperty
                    {
                        Type = "string",
                        Description = "A detailed(!) English description of the image you wish to create."
                    })
                    .AddRequired("ImageDescription")
            };
        }

        public override async Task<AiFunctionResult> Call(IAiAgent api, string parameters, string userId,
            CancellationToken cancellationToken = default)
        {
            var deserializedParameters = JsonConvert.DeserializeObject<GetImageByDescriptionRequest>(parameters);
            var imageDescription = deserializedParameters?.ImageDescription ??
                                   throw new ArgumentNullException(nameof(parameters));
            var image = await api.GetImage(imageDescription, userId, cancellationToken).ConfigureAwait(false);
            if (image == null)
            {
                return new AiFunctionResult(
                    "An internal error occurred, and the image could not be created.\n" +
                    "Please report this issue and inquire if the user would like to try again.");
            }

            return new AiFunctionResult(
                $"The image has been successfully created. " +
                $"The user is currently viewing it. " +
                $"Now, you should briefly describe to the user what has been created.\n" +
                $"Url to the image: {image}",
                image);
        }

        private sealed class GetImageByDescriptionRequest(string imageDescription)
        {
            [JsonProperty] public string ImageDescription { get; } = imageDescription;
        }
    }
}