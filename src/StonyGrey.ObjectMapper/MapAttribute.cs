namespace StonyGrey.ObjectMapper;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class MapAttribute
     : Attribute
{
    public MapAttribute(Type source, Type destination,
        ContainingNamespaceKind containingNamespaceKind = ContainingNamespaceKind.Source) =>
        (Source, Destination, ContainingNamespaceKind) =
            (source, destination, containingNamespaceKind);

    public Type Destination { get; }
    public ContainingNamespaceKind ContainingNamespaceKind { get; }
    public Type Source { get; }
}
