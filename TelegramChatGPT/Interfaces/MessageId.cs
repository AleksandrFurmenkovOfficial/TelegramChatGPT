namespace TelegramChatGPT.Interfaces
{
    internal readonly struct MessageId(string value) : IEquatable<MessageId>
    {
        public readonly string Value = value;

        public bool Equals(MessageId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            return obj is MessageId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : Value.GetHashCode(StringComparison.InvariantCulture);
        }
    }
}