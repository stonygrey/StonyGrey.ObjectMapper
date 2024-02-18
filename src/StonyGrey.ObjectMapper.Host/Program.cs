﻿using StonyGrey.ObjectMapper;
using Domain;

using System.Text.Json;
using System.Diagnostics;

[assembly: MapProtobuf(typeof(Domain.TestMessage), typeof(Protobuf.TestMessage))]
[assembly: MapProtobuf(typeof(Protobuf.TestMessage), typeof(Domain.TestMessage), ContainingNamespaceKind.Destination)]

var obj1 = new TestMessage()
{
    String = "abc",
    Int = 11,
    Optional = 22,
    OneOfA = 33,
    NonOptional = 44,
    DateTime = DateTime.Now,
    Guid = Guid.NewGuid(),
    TestEnum = TestEnum.Two,
    Data = new byte[] { 1, 2, 3, 4, 5, 6 },
    OptionalData = new byte[] { 7, 8 },
};

// MapToProtobuf & MapFromProtobuf were generated by the StonyGrey.ObjectMapper source generator.
// Look for them in StonyGrey.ObjectMapper.Host::Dependencies::Analyzers::StonyGrey.ObjectMapper::StonyGrey.ObjectMapper.MapGenerator
var pbObj1 = obj1.MapToProtobuf();

var obj2 = pbObj1.MapFromProtobuf();

var pbObj2 = obj2.MapToProtobuf();

Console.WriteLine($"{nameof(pbObj1)}: {JsonSerializer.Serialize(pbObj1)}");
Console.WriteLine($"{nameof(obj1)}: {JsonSerializer.Serialize(obj1)}");
Console.WriteLine($"{nameof(pbObj2)}: {JsonSerializer.Serialize(pbObj2)}");
Console.WriteLine($"{nameof(obj2)}: {JsonSerializer.Serialize(obj2)}");

Debug.Assert(string.Equals($"{JsonSerializer.Serialize(pbObj1)}", $"{JsonSerializer.Serialize(pbObj2)}", StringComparison.Ordinal));
Debug.Assert(string.Equals($"{JsonSerializer.Serialize(obj1)}", $"{JsonSerializer.Serialize(obj2)}", StringComparison.Ordinal));
