using Google.Protobuf;

using StonyGrey.ObjectMapper;

//namespace StonyGrey.ObjectMapper.Tests;
namespace Tests;

public class ScalarsMessage
{
    public double DoubleMember { get; set; }
    public float FloatMember { get; set; }
    public int Int32Member { get; set; }
    public long Int64Member { get; set; }
    public uint Uint32Member { get; set; }
    public ulong Uint64Member { get; set; }
    public int Sint32Member { get; set; }
    public long Sint64Member { get; set; }
    public uint Fixed32Member { get; set; }
    public ulong Fixed64Member { get; set; }
    public int Sfixed32Member { get; set; }
    public long Sfixed64Member { get; set; }
    public bool BoolMember { get; set; }
    public string StringMember { get; set; }
    public byte[] BytesMember { get; set; }
}
