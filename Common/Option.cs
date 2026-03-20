namespace SevenBoldPencil.Common
{
    public readonly struct Option<T>
    {
        public readonly T Value;
        public readonly bool HasValue;

        public Option(T value)
        {
            Value = value;
            HasValue = true;
        }

        public bool Some(out T value)
        {
            value = Value;
            return HasValue;
        }
    }
}
