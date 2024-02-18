using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StonyGrey.ObjectMapper.Extensions
{
    public static class SymbolExtensions
    {
        internal static ITypeSymbol StringTypeSymbol { get; private set; } = default!;
        internal static ITypeSymbol ByteTypeSymbol { get; private set; } = default!;
        internal static ITypeSymbol EnumerableTypeSymbol { get; private set; } = default!;
        internal static INamedTypeSymbol CollectionTypeSymbol { get; private set; } = default!;
        internal static INamedTypeSymbol GenericEnumerableTypeSymbol { get; private set; } = default!;

        internal static void LoadCommonSymbols()
        {
            ByteTypeSymbol ??= MapGenerator.Compilation.GetTypeByMetadataName(typeof(byte).FullName)!;
            StringTypeSymbol ??= MapGenerator.Compilation.GetTypeByMetadataName(typeof(string).FullName)!;
            EnumerableTypeSymbol ??= MapGenerator.Compilation.GetTypeByMetadataName(typeof(System.Collections.IEnumerable).FullName)!;
            CollectionTypeSymbol ??= MapGenerator.Compilation.GetTypeByMetadataName("System.Collections.Generic.ICollection`1")!;
            GenericEnumerableTypeSymbol ??= MapGenerator.Compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1")!;
        }

        internal static bool IsAssignableTo(this ITypeSymbol sourceType, ITypeSymbol targetType)
        {
            var conversion = MapGenerator.Compilation.ClassifyCommonConversion(sourceType, targetType);
            return conversion.IsIdentity || conversion.IsNumeric || conversion.IsReference || conversion.IsImplicit;
        }

        internal static string? GetConversionMethod(this IPropertySymbol source, IPropertySymbol target)
        {
            var classAndTree = MapGenerator.Compilation.SyntaxTrees
                .SelectMany(st => st.GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Select(e => new { Tree = st, Class = e })
                    .Where(r => r.Class.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Any(a => string.Equals(a.Name.GetText().ToString(), "MappingConversion", StringComparison.Ordinal) || string.Equals(a.Name.GetText().ToString(), "MappingConversionAttribute", StringComparison.Ordinal))))
                .FirstOrDefault();

            if (classAndTree == null)
            {
                return null;
            }

            var model = MapGenerator.Compilation.GetSemanticModel(classAndTree.Tree);

            var namedClassSymbol = model.GetDeclaredSymbol(classAndTree.Class);

            if (namedClassSymbol == null)
            {
                return null;
            }

            var memberSymbols = namedClassSymbol.GetMembers();

            foreach (var memberSymbol in memberSymbols)
            {
                if (memberSymbol is IMethodSymbol methodSymbol)
                {
                    if (methodSymbol.Parameters.Length == 1)
                    {
                        var compatibleParameter = SymbolEqualityComparer.Default.Equals(source.Type, methodSymbol.Parameters[0].Type);

                        if (!compatibleParameter)
                        {
                            if (!source.Type.IsValueType && methodSymbol.Parameters[0].Type.NullableAnnotation == NullableAnnotation.Annotated)
                            {
                                var n = methodSymbol.Parameters[0].Type as INamedTypeSymbol;
                                var p = n?.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
                                compatibleParameter = p != null && SymbolEqualityComparer.Default.Equals(source.Type, p);
                            }
                        }

                        if (compatibleParameter)
                        {
                            if (SymbolEqualityComparer.Default.Equals(target.Type, methodSymbol.ReturnType))
                            {
                                return methodSymbol.Name;
                            }

                            if (target.Type.NullableAnnotation == NullableAnnotation.Annotated)
                            {
                                var n = target.Type as INamedTypeSymbol;
                                var r = n!.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
                                if (r != null && SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType, r))
                                {
                                    return methodSymbol.Name;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        internal static string FullyQualifiedName(this ISymbol typeSymbol)
            => $"global::{typeSymbol.ContainingNamespace.ToDisplayString()}.{typeSymbol.Name}";

        public static string GenerateName(this ITypeSymbol typeSymbol)
            => typeSymbol.IsByteArrayType() ? "ByteArray" : typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        public static bool IsByteArrayType(this ITypeSymbol typeSymbol)
            => typeSymbol?.Kind == SymbolKind.ArrayType && typeSymbol is IArrayTypeSymbol symbol && SymbolEqualityComparer.Default.Equals(symbol.ElementType, ByteTypeSymbol);

        public static bool IsStringType(this ITypeSymbol typeSymbol)
            => SymbolEqualityComparer.Default.Equals(typeSymbol, StringTypeSymbol);

        internal static bool HasSetter(this IPropertySymbol propertySymbol)
            => propertySymbol.SetMethod is not null && propertySymbol.SetMethod.DeclaredAccessibility != Accessibility.Private;

        public static string? GetElementTypeDisplayString(this ITypeSymbol type)
        {
            INamedTypeSymbol? namedType = type as INamedTypeSymbol;
            var typeArgument = namedType?.TypeArguments.Length == 1 ? namedType.TypeArguments[0] : null;
            return typeArgument == null ? null : $"global::{typeArgument.ContainingNamespace.ToDisplayString()}.{typeArgument.Name}";
        }

        internal static bool IsEnumerableCollection(this IPropertySymbol propertySymbol)
            => MapGenerator.Compilation.ClassifyCommonConversion(propertySymbol.Type, EnumerableTypeSymbol!).IsImplicit
            && !propertySymbol.Type.IsByteArrayType()
            && !SymbolEqualityComparer.Default.Equals(propertySymbol.Type, StringTypeSymbol);

        internal static bool IsMutableCollection(this IPropertySymbol propertySymbol)
        {
            if (propertySymbol.Type is INamedTypeSymbol namedTypeSymbol)
            {
                // If the property is parameterized, grab the parameter type (e.g. string) and construct an ICollection<T> to compare against
                var typeArgument = namedTypeSymbol.TypeArguments.Length == 1 ? namedTypeSymbol.TypeArguments[0] : null;
                if (typeArgument != null) // e.g. string
                {
                    var collectionNamedTypeSymbol = CollectionTypeSymbol.Construct(typeArgument); // ICollection<string>
                    return MapGenerator.Compilation.ClassifyCommonConversion(propertySymbol.Type, collectionNamedTypeSymbol).IsImplicit;
                }
            }

            return false;
        }

        internal static ITypeSymbol? GetCollectionType(this IPropertySymbol propertySymbol)
        {
            if (propertySymbol.Type is INamedTypeSymbol namedTypeSymbol)
            {
                // If the property is parameterized, grab the parameter type (e.g. string) and construct an ICollection<T> to compare against
                var typeArgument = namedTypeSymbol.TypeArguments.Length == 1 ? namedTypeSymbol.TypeArguments[0] : null;
                if (typeArgument != null) // e.g. string
                {
                    var collectionNamedTypeSymbol = GenericEnumerableTypeSymbol.Construct(typeArgument); // IEnumerable<string>
                    if (MapGenerator.Compilation.ClassifyCommonConversion(propertySymbol.Type, collectionNamedTypeSymbol).IsImplicit)
                    {
                        return typeArgument;
                    }
                }
            }

            return null;
        }

        public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
        {
            var current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }
    }
}
