using Microsoft.CodeAnalysis;

namespace StonyGrey.ObjectMapper;

public sealed class MappingContext
{
    internal static ITypeSymbol? Int64TypeSymbol;
    internal static ITypeSymbol? DateTimeTypeSymbol;
    internal static ITypeSymbol? GuidTypeSymbol;
    internal static ITypeSymbol? ByteStringTypeSymbol;
    internal static ITypeSymbol? StringTypeSymbol;
    internal static ITypeSymbol? ByteTypeSymbol;

    private readonly char[] Split = { ',', ' ' };
    private readonly string[] DefaultNamespaces = { "Google.Protobuf" };

   public MappingContext(ContainingNamespaceKind containingNamespaceKind,
	   MatchingPropertyTypeKind matchingPropertyTypeKind) =>
	   (this.ContainingNamespaceKind, this.MatchingPropertyTypeKind) =
		   (containingNamespaceKind, matchingPropertyTypeKind);

    public MappingContext(ContainingNamespaceKind containingNamespaceKind,
       MatchingPropertyTypeKind matchingPropertyTypeKind, string otherNamespaces = "")
    {
        this.ContainingNamespaceKind = containingNamespaceKind;
        this.MatchingPropertyTypeKind = matchingPropertyTypeKind;
        this.OtherNamespaces = string.IsNullOrWhiteSpace(otherNamespaces) ? this.DefaultNamespaces : otherNamespaces.Split(this.Split, StringSplitOptions.RemoveEmptyEntries);
        this.IsProtobuf = true;
    }

    public ContainingNamespaceKind ContainingNamespaceKind { get; }
    public MatchingPropertyTypeKind MatchingPropertyTypeKind { get; }

    public bool IsProtobuf { get; }

    public IEnumerable<string> OtherNamespaces { get; } = Array.Empty<string>();
}
