using Microsoft.CodeAnalysis;

using StonyGrey.ObjectMapper.Diagnostics;
using StonyGrey.ObjectMapper.Extensions;

using System.Collections.Immutable;

namespace StonyGrey.ObjectMapper;

internal sealed class MappingInformation
{
    public MappingInformation(SyntaxNode currentNode, INamedTypeSymbol source, INamedTypeSymbol destination,
        MappingContext context, Compilation compilation)
    {
        ValidatePairs(currentNode, source, destination, context, compilation);

        (Node, Source, Destination) = (currentNode, source, destination);
    }

    private void ValidatePairs(SyntaxNode currentNode, INamedTypeSymbol source, INamedTypeSymbol destination,
        MappingContext context, Compilation compilation)
    {
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        if (!destination.Constructors.Any(_ => _.DeclaredAccessibility == Accessibility.Public ||
            (destination.ContainingAssembly.ExposesInternalsTo(compilation.Assembly) && _.DeclaredAccessibility == Accessibility.Friend)))
        {
            diagnostics.Add(NoAccessibleConstructorsDiagnostic.Create(currentNode));
        }

        var filteredDestinationProperties = ImmutableArray.CreateBuilder<IPropertySymbol>();

        FilterProperties(source, destination, context, compilation, filteredDestinationProperties, diagnostics);

        if (filteredDestinationProperties.Count == 0)
        {
            diagnostics.Add(NoPropertyMapsFoundDiagnostic.Create(currentNode));
        }

        Diagnostics = diagnostics.ToImmutable();
        MappedProperties = filteredDestinationProperties.ToImmutable();
    }

    private static void FilterProperties(INamedTypeSymbol source, INamedTypeSymbol destination, MappingContext context, Compilation compilation, ImmutableArray<IPropertySymbol>.Builder filteredDestinationProperties, ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        var sourceProperties = source.GetBaseTypesAndThis().SelectMany(n => n.GetMembers().OfType<IPropertySymbol>())
            .Where(_ => _.GetMethod is not null)
            .ToList();

        var destinationProperties = destination.GetBaseTypesAndThis().SelectMany(n => n.GetMembers().OfType<IPropertySymbol>())
            .Where(_ => _.SetMethod is not null &&
                (_.SetMethod.DeclaredAccessibility != Accessibility.Private ||
                (destination.ContainingAssembly.ExposesInternalsTo(compilation.Assembly) && _.SetMethod.DeclaredAccessibility == Accessibility.Friend))
                || _.IsEnumerableCollection())
            .ToList();

        var imessage = compilation.GetTypeByMetadataName("Google.Protobuf.IMessage");
        var isProtobufSource = imessage != null && compilation.ClassifyCommonConversion(source, imessage).IsImplicit;
        var isProtobufTarget = imessage != null && compilation.ClassifyCommonConversion(destination, imessage).IsImplicit;

        var byteString = compilation.GetTypeByMetadataName("Google.Protobuf.ByteString");

        foreach (var sourceProperty in sourceProperties)
        {
            var destinationProperty = destinationProperties.FirstOrDefault(_ => _.Name == sourceProperty.Name);
            
            if (destinationProperty is not null)
            {
                filteredDestinationProperties.Add(destinationProperty);
                _ = destinationProperties.Remove(destinationProperty);
            }
            else
            {
                diagnostics.Add(NoMatchDiagnostic.Create(sourceProperty, "source", source));
            }
        }

        foreach (var remainingDestinationProperty in destinationProperties)
        {
            diagnostics.Add(NoMatchDiagnostic.Create(remainingDestinationProperty, "destination", destination));
        }
    }

    public INamedTypeSymbol Destination { get; }
    public ImmutableArray<Diagnostic> Diagnostics { get; private set; }
    public SyntaxNode Node { get; }
    public ImmutableArray<IPropertySymbol> MappedProperties { get; private set; }
    public INamedTypeSymbol Source { get; }
}
