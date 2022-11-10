namespace StonyGrey.ObjectMapper;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class MapToAttribute
     : Attribute
{
    public MapToAttribute(Type destination,
        ContainingNamespaceKind containingNamespaceKind = ContainingNamespaceKind.Source, bool longName = false) =>
            (Destination, ContainingNamespaceKind, LongName) =
                (destination, containingNamespaceKind, longName);

    public Type Destination { get; }
    public ContainingNamespaceKind ContainingNamespaceKind { get; }
    public bool LongName { get; }
}
