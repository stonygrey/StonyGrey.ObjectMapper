using AutoBogus;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StonyGrey.ObjectMapper;

using System.Diagnostics;
using System.Text.Json;

using Tests;

using static ProtobufDotNet.OneOfMessage;

[assembly: Map(typeof(ScalarsMessage), typeof(ProtobufDotNet.ScalarsMessage))]
[assembly: Map(typeof(ProtobufDotNet.ScalarsMessage), typeof(ScalarsMessage), ContainingNamespaceKind.Destination)]

[assembly: Map(typeof(EnumerationsMessage), typeof(ProtobufDotNet.EnumerationsMessage))]
[assembly: Map(typeof(ProtobufDotNet.EnumerationsMessage), typeof(EnumerationsMessage), ContainingNamespaceKind.Destination)]

[assembly: Map(typeof(SubMessage), typeof(ProtobufDotNet.SubMessage))]
[assembly: Map(typeof(ProtobufDotNet.SubMessage), typeof(SubMessage), ContainingNamespaceKind.Destination)]
[assembly: Map(typeof(SubMessage1), typeof(ProtobufDotNet.SubMessage1))]
[assembly: Map(typeof(ProtobufDotNet.SubMessage1), typeof(SubMessage1), ContainingNamespaceKind.Destination)]
[assembly: Map(typeof(SubMessage2), typeof(ProtobufDotNet.SubMessage2))]
[assembly: Map(typeof(ProtobufDotNet.SubMessage2), typeof(SubMessage2), ContainingNamespaceKind.Destination)]

[assembly: Map(typeof(OptionalMessage), typeof(ProtobufDotNet.OptionalMessage))]
[assembly: Map(typeof(ProtobufDotNet.OptionalMessage), typeof(OptionalMessage), ContainingNamespaceKind.Destination)]

[assembly: Map(typeof(OneOfMessage), typeof(ProtobufDotNet.OneOfMessage))]
[assembly: Map(typeof(ProtobufDotNet.OneOfMessage), typeof(OneOfMessage), ContainingNamespaceKind.Destination)]

//namespace StonyGrey.ObjectMapper.Tests
namespace StonyGrey.ObjectMapper.Tests
{
    [TestClass]
    public class MappingTests
    {
        [ClassInitialize]
        public static async Task ClassInitialize(TestContext context)
        {
        }

        [ClassCleanup]
        public static async Task ClassCleanup()
        {
        }

        [TestMethod]
        public void TestScalars()
        {
            var obj1 = new AutoFaker<ScalarsMessage>()
              .RuleFor(f => f.BytesMember, f => new byte[] { 1, 2, 3 })
              .Generate();

            var pbObj1 = obj1.MapToProtobuf();

            var obj2 = pbObj1.MapFromProtobuf();

            var json1 = JsonSerializer.Serialize(obj1);
            var json2 = JsonSerializer.Serialize(obj2);

            Debug.WriteLine(json1);
            Debug.WriteLine(json2);

            Assert.AreEqual(json1, json2);
        }

        [TestMethod]
        public void TestEnumerations()
        {
            var obj1 = new AutoFaker<EnumerationsMessage>()
              .Generate();

            var pbObj1 = obj1.MapToProtobuf();

            var obj2 = pbObj1.MapFromProtobuf();

            var json1 = JsonSerializer.Serialize(obj1);
            var json2 = JsonSerializer.Serialize(obj2);

            Debug.WriteLine(json1);
            Debug.WriteLine(json2);

            Assert.AreEqual(json1, json2);
        }

        [TestMethod]
        public void TestSubMessage1()
        {
            var obj1 = new AutoFaker<SubMessage>()
              .RuleFor(f => f.SubMessage1, f => new AutoFaker<SubMessage1>()
                .RuleFor(f => f.SubMessage2, f => new AutoFaker<SubMessage2>()))
              .Generate();

            var pbObj1 = obj1.MapToProtobuf();

            var obj2 = pbObj1.MapFromProtobuf();

            var json1 = JsonSerializer.Serialize(obj1);
            var json2 = JsonSerializer.Serialize(obj2);

            Debug.WriteLine(json1);
            Debug.WriteLine(json2);

            Assert.AreEqual(json1, json2);
        }

        [TestMethod]
        public void TestOptional()
        {
            OptionalMessage? obj1 = new();

            Assert.IsFalse(obj1.OptionalInt32Member.HasValue);
            Assert.IsNull(obj1.OptionalStringMember);
            Assert.IsNull(obj1.OptionalBytesMember);

            var pbObj1 = obj1.MapToProtobuf();

            Assert.IsFalse(pbObj1.HasOptionalInt32Member);
            Assert.IsFalse(pbObj1.HasOptionalStringMember);
            Assert.IsFalse(pbObj1.HasOptionalBytesMember);

            OptionalMessage? obj2 = new()
            {
                OptionalInt32Member = 1,
                OptionalStringMember = "abc",
                OptionalBytesMember = new byte[] { 1, 2, 3 }
            };

            Assert.IsNotNull(obj2.OptionalInt32Member.Value);
            Assert.IsNotNull(obj2.OptionalStringMember);
            Assert.IsNotNull(obj2.OptionalBytesMember);

            var pbObj2 = obj2.MapToProtobuf();

            Assert.IsTrue(pbObj2.HasOptionalInt32Member);
            Assert.IsTrue(pbObj2.HasOptionalStringMember);
            Assert.IsTrue(pbObj2.HasOptionalBytesMember);

            var obj3 = pbObj2.MapFromProtobuf();

            var json2 = JsonSerializer.Serialize(obj2);
            var json3 = JsonSerializer.Serialize(obj3);

            Debug.WriteLine(json2);
            Debug.WriteLine(json3);

            Assert.AreEqual(json2, json3);
        }

        [TestMethod]
        public void TestOneOf()
        {
            OneOfMessage obj1 = new();

            Assert.IsTrue(obj1.OneOfAMember == default);
            Assert.IsTrue(obj1.OneOfBMember == default);

            var pbObj1 = obj1.MapToProtobuf();

            Assert.IsTrue(pbObj1.OneOfMemberCase == OneOfMemberOneofCase.None);

            OneOfMessage obj2 = new() { OneOfAMember = 1 };

            var pbObj2 = obj2.MapToProtobuf();

            Assert.IsTrue(pbObj2.OneOfMemberCase == OneOfMemberOneofCase.OneOfAMember);

            OneOfMessage obj3 = new() { OneOfBMember = 1 };

            var pbObj3 = obj3.MapToProtobuf();

            Assert.IsTrue(pbObj3.OneOfMemberCase == OneOfMemberOneofCase.OneOfBMember);

            var obj4 = pbObj3.MapFromProtobuf();

            var json3 = JsonSerializer.Serialize(obj3);
            var json4 = JsonSerializer.Serialize(obj4);

            Debug.WriteLine(json3);
            Debug.WriteLine(json4);

            Assert.AreEqual(json3, json4);
        }
    }
}
