namespace TelegramChatGPT.Interfaces
{
    internal sealed class AiFunctionResult(string result, Uri? imageUrl = null)
    {
        public readonly Uri? ImageUrl = imageUrl;

        public readonly string Result = result;
    }
}