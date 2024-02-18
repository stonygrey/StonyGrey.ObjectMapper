using Google.Protobuf;

namespace Domain
{
    public enum TestEnum { One, Two, Three }

    public class TestSubSubMessage
    {
        public TestSubSubMessage(string readOnlyValue)
        {
            ReadOnlyStringMember = readOnlyValue;
        }

        public string ReadOnlyStringMember { get; }

        public string? StringMember { get; set; }
    }

    public class TestSubMessage1
    {
        public string? StringMember { get; set; }
        public TestSubSubMessage? TestSubSubMessageMember { get; set; }
    }

    public class TestSubMessage2
    {
        private List<string> _testSubMessage2StringsMember = new();

        public string? StringMember { get; set; }
        public IEnumerable<string> TestSubMessage2StringsMember => _testSubMessage2StringsMember;

        public void AddValue(string value)
            => _testSubMessage2StringsMember.Add(value);
    }

    public struct TestStruct
    {
        public string StringMember { get; set; }
    }

    public class TestMessage
    {
        public string? StringMember { get; set; }
        public int IntMember { get; set; }
        public int? OptionalMember { get; set; }
        public int NonOptionalMember { get; set; }
        public long OneOfAMember { get; set; }
        public long OneOfBMember { get; set; }
        public DateTime DateTimeMember { get; set; }
        public Guid GuidMember { get; set; }
        public TestEnum EnumMember { get; set; }
        public byte[] DataMember { get; set; } = Array.Empty<byte>();
        public byte[]? OptionalDataMember { get; set; }
        public TestSubMessage1? TestSubMessageMember { get; set; }
        public ICollection<string> StringsMember { get; set;  } = new List<string>();
        public ICollection<TestSubMessage1> TestSubMessagesMember { get; set; } = new List<TestSubMessage1>();

        public TestSubMessage2 TestSubMessage2Member { get; set; } = new TestSubMessage2();

        public List<TestEnum> EnumListMember { get; set; } = new List<TestEnum>();

        public TestEnum? NullableEnumMember { get; set; }

        public TestStruct TestStructMember { get; set; }
    }
}
