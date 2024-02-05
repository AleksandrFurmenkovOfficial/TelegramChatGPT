namespace TelegramChatGPT.Interfaces
{
    internal class ChatMode
    {
        public ChatMode()
        {
            AiName = "";
            EnableFunctions = false;
            AiSettings = "";
            Messages = [];
        }

        public string AiName { get; set; }
        public bool EnableFunctions { get; set; }
        public string AiSettings { get; set; }
        public List<List<IChatMessage>> Messages { get; set; }
    }
}