namespace StonyGrey.ObjectMapper;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class MapFromAttribute
     : Attribute
{
    public MapFromAttribute(Type source,
        ContainingNamespaceKind containingNamespaceKind = ContainingNamespaceKind.Source, bool longName = false) =>
            (Source, ContainingNamespaceKind, LongName) = (source, containingNamespaceKind, longName);

    public Type Source { get; }
    public ContainingNamespaceKind ContainingNamespaceKind { get; }
    public bool LongName { get; }
}
