namespace Domain
{
    public enum TestEnum { One, Two }

    public class TestMessage
    {
        public string? String { get; set; }
        public int Int { get; set; }
        public int? Optional { get; set; }
        public int NonOptional { get; set; }
        public long OneOfA { get; set; }
        public long OneOfB { get; set; }
        public DateTime DateTime { get; set; }
        public Guid Guid { get; set; }
        public TestEnum TestEnum { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public byte[]? OptionalData { get; set; }
    }
}
