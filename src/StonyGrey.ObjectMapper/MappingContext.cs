namespace StonyGrey.ObjectMapper;

public sealed class MappingContext
{
    public MappingContext(ContainingNamespaceKind containingNamespaceKind) =>
        ContainingNamespaceKind = containingNamespaceKind;

    public ContainingNamespaceKind ContainingNamespaceKind { get; }
}
