namespace StonyGrey.ObjectMapper;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class MapProtobufAttribute : Attribute
{
    public MapProtobufAttribute(Type source, Type destination,
        ContainingNamespaceKind containingNamespaceKind = ContainingNamespaceKind.Source,
        MatchingPropertyTypeKind matchingPropertyTypeKind = MatchingPropertyTypeKind.Implicit, string otherNamespaces = "") =>
        (this.Source, this.Destination, this.ContainingNamespaceKind, this.MatchingPropertyTypeKind, this.OtherNamespaces) =
            (source, destination, containingNamespaceKind, matchingPropertyTypeKind, otherNamespaces);

    public Type Destination { get; }
    public ContainingNamespaceKind ContainingNamespaceKind { get; }
    public MatchingPropertyTypeKind MatchingPropertyTypeKind { get; }
    public Type Source { get; }

    public string OtherNamespaces { get; }
}