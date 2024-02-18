using Microsoft.CodeAnalysis;

using StonyGrey.ObjectMapper.Extensions;

using System.Collections.Immutable;

namespace StonyGrey.ObjectMapper;

internal sealed class NamespaceGatherer
{
    private readonly ImmutableHashSet<string>.Builder builder =
        ImmutableHashSet.CreateBuilder<string>();

    public void Add(INamespaceSymbol @namespace)
    {
        if (!@namespace.IsGlobalNamespace)
        {
            _ = builder.Add(@namespace.GetName());
        }
    }

    public void Add(Type type)
    {
        if (!string.IsNullOrWhiteSpace(type.Namespace))
        {
            _ = builder.Add(type.Namespace);
        }
    }

    public void AddRange(IEnumerable<INamespaceSymbol> namespaces)
    {
        foreach (var @namespace in namespaces)
        {
            Add(@namespace);
        }
    }

    public IImmutableSet<string> Values => builder.ToImmutableSortedSet();
}
