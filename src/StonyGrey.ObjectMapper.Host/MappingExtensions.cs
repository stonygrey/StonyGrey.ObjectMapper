using Google.Protobuf;

using StonyGrey.ObjectMapper;

namespace Domain
{
    [MappingConversion]
    public static partial class MappingExtensions
    {
        public static ByteString MapToByteString(this Guid value)
            => ByteString.CopyFrom(value.ToByteArray());

        public static byte[] MapToByteArray(this ByteString value)
            => value == null ? Array.Empty<byte>() : value.ToByteArray();

        public static Guid MapToGuid(this ByteString value)
            => value == null ? Guid.Empty : new Guid(value.ToArray());

        public static DateTime MapToDateTime(this long value)
            => DateTime.FromBinary(value);

        public static long MapToLong(this DateTime value)
            => value.ToBinary();

        public static T MapToEnum<T>(this Enum e) where T : Enum
            => (T)e;

        public static ProtobufDotNet.Testing MapToTesting(this Domain.Testing? e)
            => e.HasValue ? (ProtobufDotNet.Testing)e.Value : default(ProtobufDotNet.Testing);

        public static Domain.Testing? MapToTesting(this ProtobufDotNet.Testing e)
            => (Domain.Testing)e;

        public static ByteString MapToByteString(this byte[] value)
            => value == null ? ByteString.Empty : ByteString.CopyFrom(value);

        public static void MapCollection<T>(this IEnumerable<T> source, ICollection<T> target)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            foreach (var element in source)
            {
                target.Add(element);
            }
        }

        public static void MapCollection(this IEnumerable<Domain.TestSubMessage1> source, ICollection<ProtobufDotNet.TestSubMessage1> target)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            foreach (var element in source)
            {
                target.Add(element.MapToProtobuf());
            }
        }

        public static void MapCollection(this IEnumerable<ProtobufDotNet.TestSubMessage1> source, ICollection<Domain.TestSubMessage1> target)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            foreach (var element in source)
            {
                target.Add(element.MapFromProtobuf());
            }
        }

        public static Domain.TestSubMessage2 Map(this ProtobufDotNet.TestSubMessage2 source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var target = source.MapFromProtobuf();

            foreach (var element in source.TestSubMessage2StringsMember)
            {
                target.AddValue(element);
            }

            return target;
        }

        public static TestSubSubMessage? MapToTestSubSubMessage(this ProtobufDotNet.TestSubSubMessage? source)
            => source == null ? null : source.MapFromProtobuf(source.ReadOnlyStringMember);
    }
}
