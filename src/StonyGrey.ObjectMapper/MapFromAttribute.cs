namespace StonyGrey.ObjectMapper;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class MapFromAttribute
     : Attribute
{
    public MapFromAttribute(Type source,
        ContainingNamespaceKind containingNamespaceKind = ContainingNamespaceKind.Source) =>
            (Source, ContainingNamespaceKind) = (source, containingNamespaceKind);

    public ContainingNamespaceKind ContainingNamespaceKind { get; }
    public Type Source { get; }
}
