namespace StonyGrey.ObjectMapper;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class MapToAttribute
     : Attribute
{
    public MapToAttribute(Type destination,
        ContainingNamespaceKind containingNamespaceKind = ContainingNamespaceKind.Source) =>
            (Destination, ContainingNamespaceKind) =
                (destination, containingNamespaceKind);

    public ContainingNamespaceKind ContainingNamespaceKind { get; }
    public Type Destination { get; }
}
