using Microsoft.CodeAnalysis;

namespace StonyGrey.ObjectMapper.Extensions
{
    public static class OtherExtensions
    {
        public static bool IsByteArrayType(this ITypeSymbol typeSymbol)
            => typeSymbol?.Kind == SymbolKind.ArrayType && typeSymbol is IArrayTypeSymbol symbol && SymbolEqualityComparer.Default.Equals(symbol.ElementType, MappingContext.ByteTypeSymbol);

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
