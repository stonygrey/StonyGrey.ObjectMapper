namespace StonyGrey.ObjectMapper;

public sealed class MappingContext
{
    public MappingContext(ContainingNamespaceKind containingNamespaceKind, bool longName) =>
        (ContainingNamespaceKind, LongName) = (containingNamespaceKind, longName);

    public ContainingNamespaceKind ContainingNamespaceKind { get; }
    public bool LongName { get; }
}
