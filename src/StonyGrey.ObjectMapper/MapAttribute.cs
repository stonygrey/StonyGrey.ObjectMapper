namespace StonyGrey.ObjectMapper;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class MapAttribute
     : Attribute
{
    public MapAttribute(Type source, Type destination,
        ContainingNamespaceKind containingNamespaceKind = ContainingNamespaceKind.Source, bool longName = false) =>
        (Source, Destination, ContainingNamespaceKind, LongName) =
            (source, destination, containingNamespaceKind, longName);

    public Type Source { get; }
    public Type Destination { get; }
    public ContainingNamespaceKind ContainingNamespaceKind { get; }
    public bool LongName { get; }
}
