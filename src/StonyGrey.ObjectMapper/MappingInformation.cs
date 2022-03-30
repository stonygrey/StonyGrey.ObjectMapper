using StonyGrey.ObjectMapper.Diagnostics;
using StonyGrey.ObjectMapper.Extensions;

using Microsoft.CodeAnalysis;

using System.Collections.Immutable;

namespace StonyGrey.ObjectMapper;

internal sealed class MappingInformation
{
    public MappingInformation(SyntaxNode currentNode, INamedTypeSymbol source, INamedTypeSymbol destination,
        MappingContext context, Compilation compilation)
    {
        this.ValidatePairs(
            currentNode, source, destination, context, compilation);
        (this.Node, this.Source, this.Destination) = (currentNode, source, destination);
    }

    private void ValidatePairs(SyntaxNode currentNode, INamedTypeSymbol source, INamedTypeSymbol destination,
        MappingContext context, Compilation compilation)
    {
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        if (!destination.Constructors.Any(_ => _.DeclaredAccessibility == Accessibility.Public ||
            destination.ContainingAssembly.ExposesInternalsTo(compilation.Assembly) && _.DeclaredAccessibility == Accessibility.Friend))
        {
            diagnostics.Add(NoAccessibleConstructorsDiagnostic.Create(currentNode));
        }

        var filteredDestinationProperties = ImmutableArray.CreateBuilder<IPropertySymbol>();

        if (context.IsProtobuf)
        {
            FilterProtobufProperties(source, destination, context, compilation, filteredDestinationProperties, diagnostics);
        }
        else
        {
            FilterProperties(source, destination, context, compilation, filteredDestinationProperties, diagnostics);
        }

        if (filteredDestinationProperties.Count == 0)
        {
            diagnostics.Add(NoPropertyMapsFoundDiagnostic.Create(currentNode));
        }

        this.Diagnostics = diagnostics.ToImmutable();
        this.MappedProperties = filteredDestinationProperties.ToImmutable();
    }

    private static void FilterProperties(INamedTypeSymbol source, INamedTypeSymbol destination, MappingContext context, Compilation compilation, ImmutableArray<IPropertySymbol>.Builder filteredDestinationProperties, ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        var destinationProperties = destination.GetMembers().OfType<IPropertySymbol>()
            .Where(_ => _.SetMethod is not null &&
                (_.SetMethod.DeclaredAccessibility == Accessibility.Public ||
                (destination.ContainingAssembly.ExposesInternalsTo(compilation.Assembly) && _.SetMethod.DeclaredAccessibility == Accessibility.Friend)))
            .ToList();

        foreach (var sourceProperty in source.GetMembers().OfType<IPropertySymbol>()
            .Where(_ => _.GetMethod is not null &&
                (_.GetMethod.DeclaredAccessibility == Accessibility.Public ||
                (source.ContainingAssembly.ExposesInternalsTo(compilation.Assembly) && _.GetMethod.DeclaredAccessibility == Accessibility.Friend))))
        {
            var destinationProperty = destinationProperties.FirstOrDefault(
                _ => _.Name == sourceProperty.Name &&
                    context.MatchingPropertyTypeKind switch
                    {
                        MatchingPropertyTypeKind.Exact => _.Type.Equals(sourceProperty.Type, SymbolEqualityComparer.Default),
                        _ => compilation.ClassifyCommonConversion(sourceProperty.Type, _.Type).IsImplicit
                    } &&
                    (sourceProperty.NullableAnnotation != NullableAnnotation.Annotated ||
                        sourceProperty.NullableAnnotation == NullableAnnotation.Annotated && _.NullableAnnotation == NullableAnnotation.Annotated));

            if (destinationProperty is not null)
            {
                filteredDestinationProperties.Add(destinationProperty);
                destinationProperties.Remove(destinationProperty);
            }
            else
            {
                diagnostics.Add(NoMatchDiagnostic.Create(sourceProperty, "source", source));
            }
        }
    }

    private static void FilterProtobufProperties(INamedTypeSymbol source, INamedTypeSymbol destination, MappingContext context, Compilation compilation, ImmutableArray<IPropertySymbol>.Builder filteredDestinationProperties, ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        var sourceProperties = source.GetBaseTypesAndThis().SelectMany(n => n.GetMembers().OfType<IPropertySymbol>())
              .Where(_ => _.GetMethod is not null &&
                  (_.GetMethod.DeclaredAccessibility == Accessibility.Public ||
                  (source.ContainingAssembly.ExposesInternalsTo(compilation.Assembly) && _.GetMethod.DeclaredAccessibility == Accessibility.Friend)))
              .ToList();

        var destinationProperties = destination.GetBaseTypesAndThis().SelectMany(n => n.GetMembers().OfType<IPropertySymbol>())
            .Where(_ => _.SetMethod is not null &&
                (_.SetMethod.DeclaredAccessibility == Accessibility.Public ||
                (destination.ContainingAssembly.ExposesInternalsTo(compilation.Assembly) && _.SetMethod.DeclaredAccessibility == Accessibility.Friend)))
            .ToList();

        MappingContext.ByteTypeSymbol ??= compilation.GetTypeByMetadataName($"{typeof(byte)}");
        MappingContext.Int64TypeSymbol ??= compilation.GetTypeByMetadataName($"{typeof(long)}");
        MappingContext.DateTimeTypeSymbol ??= compilation.GetTypeByMetadataName($"{typeof(DateTime)}");
        MappingContext.GuidTypeSymbol ??= compilation.GetTypeByMetadataName($"{typeof(Guid)}");
        MappingContext.ByteStringTypeSymbol ??= compilation.GetTypeByMetadataName("Google.Protobuf.ByteString");
        MappingContext.StringTypeSymbol ??= compilation.GetTypeByMetadataName($"{typeof(string)}");

        var imessage = compilation.GetTypeByMetadataName("Google.Protobuf.IMessage");
        var isProtobufTarget = imessage != null && compilation.ClassifyCommonConversion(destination, imessage).IsImplicit;

        foreach (var sourceProperty in sourceProperties)
        {
            IPropertySymbol? destinationProperty = null;

            if (isProtobufTarget)
            {
                destinationProperty = destinationProperties.FirstOrDefault(
                    _ => _.Name == sourceProperty.Name &&
                        (context.MatchingPropertyTypeKind switch
                            {
                                MatchingPropertyTypeKind.Exact => _.Type.Equals(sourceProperty.Type, SymbolEqualityComparer.Default),
                                _ => compilation.ClassifyCommonConversion(sourceProperty.Type, _.Type).IsImplicit,
                            }
                            || (_.Type.TypeKind == TypeKind.Enum && sourceProperty.Type.TypeKind == TypeKind.Enum)
                            || (SymbolEqualityComparer.Default.Equals(_.Type, MappingContext.Int64TypeSymbol) && SymbolEqualityComparer.Default.Equals(sourceProperty.Type, MappingContext.DateTimeTypeSymbol))
                            || (SymbolEqualityComparer.Default.Equals(_.Type, MappingContext.DateTimeTypeSymbol) && SymbolEqualityComparer.Default.Equals(sourceProperty.Type, MappingContext.Int64TypeSymbol))
                            || (SymbolEqualityComparer.Default.Equals(_.Type, MappingContext.ByteStringTypeSymbol) && SymbolEqualityComparer.Default.Equals(sourceProperty.Type, MappingContext.GuidTypeSymbol))
                            || (SymbolEqualityComparer.Default.Equals(_.Type, MappingContext.GuidTypeSymbol) && SymbolEqualityComparer.Default.Equals(sourceProperty.Type, MappingContext.ByteStringTypeSymbol))
                            || (SymbolEqualityComparer.Default.Equals(_.Type, MappingContext.ByteStringTypeSymbol) && sourceProperty.Type.IsByteArrayType())
                            || (SymbolEqualityComparer.Default.Equals(sourceProperty.Type, MappingContext.ByteStringTypeSymbol) && _.Type.IsByteArrayType())
                        )
                        || sourceProperty.NullableAnnotation == NullableAnnotation.Annotated
                    );
            }
            else
            {
                destinationProperty = destinationProperties.FirstOrDefault(_ => _.Name == sourceProperty.Name);
            }

            if (destinationProperty is not null)
            {
                filteredDestinationProperties.Add(destinationProperty);
                destinationProperties.Remove(destinationProperty);
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
