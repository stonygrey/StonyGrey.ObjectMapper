using StonyGrey.ObjectMapper.Configuration;
using StonyGrey.ObjectMapper.Extensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Text;

namespace StonyGrey.ObjectMapper;

internal sealed class MappingBuilder
{
    private static readonly SymbolDisplayFormat _format = new SymbolDisplayFormat(
               typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
               genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints | SymbolDisplayGenericsOptions.IncludeVariance
           );

    internal bool IsProtobufTarget { get; private set; }

    public MappingBuilder(INamedTypeSymbol source, INamedTypeSymbol destination, ImmutableArray<IPropertySymbol> destinationProperties,
        MappingContext context, Compilation compilation, ConfigurationValues configurationValues) =>
        this.Text = this.Build(source, destination, destinationProperties, context, compilation, configurationValues);

    private SourceText Build(INamedTypeSymbol source, INamedTypeSymbol destination, ImmutableArray<IPropertySymbol> destinationProperties,
        MappingContext context, Compilation compilation, ConfigurationValues configurationValues)
    {
        using var writer = new StringWriter();
        using var indentWriter = new IndentedTextWriter(writer,
            configurationValues.IndentStyle == IndentStyle.Tab ? "\t" : new string(' ', (int)configurationValues.IndentSize));

        var namespaces = new NamespaceGatherer();
        var emittedNamespace = false;

        if (context.ContainingNamespaceKind != ContainingNamespaceKind.Global)
        {
            if (context.ContainingNamespaceKind == ContainingNamespaceKind.Source)
            {
                if (source.ContainingNamespace.IsGlobalNamespace ||
                    !source.ContainingNamespace.Contains(destination.ContainingNamespace))
                {
                    namespaces.Add(destination.ContainingNamespace);
                }

                if (!source.ContainingNamespace.IsGlobalNamespace)
                {
                    indentWriter.WriteLine($"namespace {source.ContainingNamespace.ToDisplayString()}");
                    indentWriter.WriteLine("{");
                    indentWriter.Indent++;
                    emittedNamespace = true;
                }
            }
            else if (context.ContainingNamespaceKind == ContainingNamespaceKind.Destination)
            {
                if (destination.ContainingNamespace.IsGlobalNamespace ||
                    !destination.ContainingNamespace.Contains(source.ContainingNamespace))
                {
                    namespaces.Add(source.ContainingNamespace);
                }

                if (!destination.ContainingNamespace.IsGlobalNamespace)
                {
                    indentWriter.WriteLine($"namespace {destination.ContainingNamespace.ToDisplayString()}");
                    indentWriter.WriteLine("{");
                    indentWriter.Indent++;
                    emittedNamespace = true;
                }
            }
        }
        else
        {
            namespaces.Add(source.ContainingNamespace);
            namespaces.Add(destination.ContainingNamespace);
        }

        this.BuildType(source, destination, destinationProperties, context, compilation, indentWriter, namespaces);

        if (emittedNamespace)
        {
            indentWriter.Indent--;
            indentWriter.WriteLine("}");
        }
        indentWriter.WriteLine("");

        var append = string.Join(Environment.NewLine, string.Join(Environment.NewLine, context.OtherNamespaces.Select(e => $"using {e};").AsEnumerable()), $"{Environment.NewLine}#nullable enable{Environment.NewLine}");

        var code = namespaces.Values.Count > 0 ?
            string.Join(Environment.NewLine,
                string.Join(Environment.NewLine, namespaces.Values.Select(_ => $"using {_};")),
                string.Empty, append, string.Empty, writer.ToString()) :
            string.Join(Environment.NewLine, append, string.Empty, writer.ToString());

        return SourceText.From(code, Encoding.UTF8);
    }

    private void BuildType(INamedTypeSymbol source, INamedTypeSymbol destination, ImmutableArray<IPropertySymbol> destinationProperties,
        MappingContext context, Compilation compilation, IndentedTextWriter indentWriter, NamespaceGatherer namespaces)
    {
        indentWriter.WriteLine($"public static partial class {source.Name}MappingExtensions");
        indentWriter.WriteLine("{");
        indentWriter.Indent++;

        var constructors = destination.Constructors.Where(_ => _.DeclaredAccessibility == Accessibility.Public ||
            destination.ContainingAssembly.ExposesInternalsTo(compilation.Assembly) && _.DeclaredAccessibility == Accessibility.Friend).ToArray();

        if (context.IsProtobuf)
        {
            var imessage = compilation.GetTypeByMetadataName("Google.Protobuf.IMessage");
            this.IsProtobufTarget = imessage != null && compilation.ClassifyCommonConversion(destination, imessage).IsImplicit;
        }

        for (var i = 0; i < constructors.Length; i++)
        {
            var constructor = constructors[i];

            if (context.IsProtobuf)
            {
                if (this.IsProtobufTarget)
                {
                    if (constructor.Parameters.Length == 0)
                    {
                        MappingBuilder.BuildMapToProtobufExtensionMethod(source, destination, destinationProperties, constructor, namespaces, indentWriter);
                    }
                }
                else
                {
                    MappingBuilder.BuildMapFromProtobufExtensionMethod(source, destination, destinationProperties, constructor, namespaces, indentWriter);
                }
            }
            else
            {
                MappingBuilder.BuildMapExtensionMethod(source, destination, destinationProperties.Select(e => e.Name).ToImmutableArray(), constructor, namespaces, indentWriter);
            }
            
            if (i < constructors.Length - 1)
            {
                indentWriter.WriteLine();
            }
        }

        if (context.IsProtobuf && !this.IsProtobufTarget)
        {
            MappingBuilder.BuildMapFromProtobufExtensionMethodWithTarget(source, destination, destinationProperties, namespaces, indentWriter);
        }

        indentWriter.Indent--;
        indentWriter.WriteLine("}");
    }

    private static void BuildMapExtensionMethod(ITypeSymbol source, ITypeSymbol destination, ImmutableArray<string> propertyNames,
        IMethodSymbol constructor, NamespaceGatherer namespaces, IndentedTextWriter indentWriter)
    {
        var parameters = new string[constructor.Parameters.Length + 1];
        parameters[0] = $"this {source.Name} self";

        for (var i = 0; i < constructor.Parameters.Length; i++)
        {
            var parameter = constructor.Parameters[i];
            namespaces.Add(parameter.Type.ContainingNamespace);
            var nullableAnnotation = parameter.NullableAnnotation == NullableAnnotation.Annotated ? "?" : string.Empty;
            var optionalValue = parameter.HasExplicitDefaultValue ? $" = {parameter.ExplicitDefaultValue.GetDefaultValue()}" : string.Empty;
            parameters[i + 1] = $"{parameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}{nullableAnnotation} {parameter.Name}{optionalValue}";
        }

        indentWriter.WriteLine($"public static {destination.Name} MapTo{destination.Name}({string.Join(", ", parameters)}) =>");
        indentWriter.Indent++;

        if (!source.IsValueType)
        {
            indentWriter.WriteLine("self is null ? throw new ArgumentNullException(nameof(self)) :");
            namespaces.Add(typeof(ArgumentNullException));
            indentWriter.Indent++;
        }

        if (constructor.Parameters.Length == 0)
        {
            indentWriter.WriteLine($"new {destination.Name}");
        }
        else
        {
            indentWriter.WriteLine(
                $"new {destination.Name}({string.Join(", ", constructor.Parameters.Select(_ => _.Name))})");
        }

        indentWriter.WriteLine("{");
        indentWriter.Indent++;

        foreach (var propertyName in propertyNames)
        {
            indentWriter.WriteLine($"{propertyName} = self.{propertyName},");
        }

        indentWriter.Indent--;
        indentWriter.WriteLine("};");

        if (!source.IsValueType)
        {
            indentWriter.Indent--;
        }

        indentWriter.Indent--;
    }

    private static void BuildMapToProtobufExtensionMethod(ITypeSymbol source, ITypeSymbol destination, ImmutableArray<IPropertySymbol> destinationProperties, IMethodSymbol constructor, NamespaceGatherer namespaces, IndentedTextWriter indentWriter)
    {
        var fullyQualifiedSource = $"global::{source.ContainingNamespace.Name}.{source.Name}";
        var fullyQualifiedDestination = $"global::{destination.ContainingNamespace.Name}.{destination.Name}";

        var parameters = new string[constructor.Parameters.Length + 1];
        parameters[0] = $"this {fullyQualifiedSource} self";

        for (var i = 0; i < constructor.Parameters.Length; i++)
        {
            var parameter = constructor.Parameters[i];
            namespaces.Add(parameter.Type.ContainingNamespace);
            var nullableAnnotation = parameter.NullableAnnotation == NullableAnnotation.Annotated ? "?" : string.Empty;
            var optionalValue = parameter.HasExplicitDefaultValue ? $" = {parameter.ExplicitDefaultValue.GetDefaultValue()}" : string.Empty;
            parameters[i + 1] = $"{parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}{nullableAnnotation} {parameter.Name}{optionalValue}";
        }

        indentWriter.WriteLine($"public static {fullyQualifiedDestination} MapToProtobuf({string.Join(", ", parameters)})");
        indentWriter.WriteLine("{");
        indentWriter.Indent++;

        if (!source.IsValueType)
        {
            indentWriter.WriteLine("var mapped = self is null ? throw new ArgumentNullException(nameof(self)) :");
            namespaces.Add(typeof(ArgumentNullException));
            indentWriter.Indent++;
        }

        if (constructor.Parameters.Length == 0)
        {
            indentWriter.WriteLine($"new {fullyQualifiedDestination}();");
        }
        else
        {
            indentWriter.WriteLine(
                $"new {fullyQualifiedDestination}({string.Join(", ", constructor.Parameters.Select(_ => _.Name))});");
        }

        indentWriter.WriteLine();
        indentWriter.Indent--;

        var remaining = new List<IPropertySymbol>();

        foreach (var destinationProperty in destinationProperties)
        {
            if (source.GetBaseTypesAndThis().SelectMany(n => n.GetMembers(destinationProperty.Name).OfType<IPropertySymbol>()).SingleOrDefault() is not IPropertySymbol sourceProperty)
            {
                // TODO: diagnostics
                continue;
            }

            //
            // Set the property only if it's not already set to the Protobuf default.
            // This ensures proper treatment of 'oneof' fields.
            //
            // https://developers.google.com/protocol-buffers/docs/proto3#default
            // https://developers.google.com/protocol-buffers/docs/proto3#oneof
            //

            if (sourceProperty.NullableAnnotation == NullableAnnotation.Annotated && destinationProperty.NullableAnnotation != NullableAnnotation.Annotated && sourceProperty.Type.IsValueType)
            {
                remaining.Add(destinationProperty);
            }
            else if (SymbolEqualityComparer.Default.Equals(sourceProperty.Type, MappingContext.DateTimeTypeSymbol) && SymbolEqualityComparer.Default.Equals(destinationProperty.Type, MappingContext.Int64TypeSymbol))
            {
                indentWriter.WriteLine($"if (self.{destinationProperty.Name} != default(DateTime))");
                indentWriter.WriteLine("{");
                indentWriter.Indent++;
                indentWriter.WriteLine($"mapped.{destinationProperty.Name} = self.{destinationProperty.Name}.ToBinary();");
                indentWriter.Indent--;
                indentWriter.WriteLine($"}}{Environment.NewLine}");
            }
            else if (SymbolEqualityComparer.Default.Equals(sourceProperty.Type, MappingContext.GuidTypeSymbol) && SymbolEqualityComparer.Default.Equals(destinationProperty.Type, MappingContext.ByteStringTypeSymbol))
            {
                indentWriter.WriteLine($"if (self.{destinationProperty.Name} != default(Guid))");
                indentWriter.WriteLine("{");
                indentWriter.Indent++;
                indentWriter.WriteLine($"mapped.{destinationProperty.Name} = ByteString.CopyFrom(self.{destinationProperty.Name}.ToByteArray());");
                indentWriter.Indent--;
                indentWriter.WriteLine($"}}{Environment.NewLine}");
            }
            else if (sourceProperty.Type.IsByteArrayType())
            {
                indentWriter.WriteLine($"if (self.{destinationProperty.Name}?.Length > 0)");
                indentWriter.WriteLine("{");
                indentWriter.Indent++;
                indentWriter.WriteLine($"mapped.{destinationProperty.Name} = ByteString.CopyFrom(self.{destinationProperty.Name}.ToArray());");
                indentWriter.Indent--;
                indentWriter.WriteLine($"}}{Environment.NewLine}");
            }
            else if (destinationProperty.Type.TypeKind == TypeKind.Enum)
            {
                indentWriter.WriteLine($"if (self.{destinationProperty.Name} != default)");
                indentWriter.WriteLine("{");
                indentWriter.Indent++;
                indentWriter.WriteLine($"mapped.{destinationProperty.Name} = (global::{destination.ContainingNamespace.Name}.{destinationProperty.Type.Name})self.{destinationProperty.Name};");
                indentWriter.Indent--;
                indentWriter.WriteLine($"}}{Environment.NewLine}");
            }
            else
            {
                var destinationType = Type.GetType(destinationProperty.Type.ToDisplayString(_format));
                var sourceType = Type.GetType(sourceProperty.Type.ToDisplayString(_format));

                if (destinationType?.IsAssignableFrom(sourceType) == true)
                {
                    if (SymbolEqualityComparer.Default.Equals(destinationProperty.Type, MappingContext.StringTypeSymbol))
                    {
                        indentWriter.WriteLine($"if (!string.IsNullOrWhiteSpace(self.{destinationProperty.Name}))");
                        indentWriter.WriteLine("{");
                        indentWriter.Indent++;
                        indentWriter.WriteLine($"mapped.{destinationProperty.Name} = self.{destinationProperty.Name};");
                        indentWriter.Indent--;
                        indentWriter.WriteLine($"}}{Environment.NewLine}");
                    }
                    else if (sourceProperty.Type.IsByteArrayType())
                    {
                        indentWriter.WriteLine($"if (self.{destinationProperty.Name}?.Length > 0)");
                        indentWriter.WriteLine("{");
                        indentWriter.Indent++;
                        indentWriter.WriteLine($"mapped.{destinationProperty.Name} = self.{destinationProperty.Name};");
                        indentWriter.Indent--;
                        indentWriter.WriteLine($"}}{Environment.NewLine}");
                    }
                    else
                    {
                        indentWriter.WriteLine($"if (self.{destinationProperty.Name} != default)");
                        indentWriter.WriteLine("{");
                        indentWriter.Indent++;
                        indentWriter.WriteLine($"mapped.{destinationProperty.Name} = self.{destinationProperty.Name};");
                        indentWriter.Indent--;
                        indentWriter.WriteLine($"}}{Environment.NewLine}");
                    }
                }
                else
                {
                    // TODO: diagnostics
                }
            }
        }

        foreach (var property in remaining)
        {
            //
            // Ensure that 'optional' fields are set only when needed
            //
            indentWriter.WriteLine($"if (self.{property.Name}.HasValue)");
            indentWriter.WriteLine("{");
            indentWriter.Indent++;
            indentWriter.WriteLine($"mapped.{property.Name} = self.{property.Name}.Value;");
            indentWriter.Indent--;
            indentWriter.WriteLine($"}}{Environment.NewLine}");
        }

        indentWriter.WriteLine("return mapped;");

        indentWriter.Indent--;
        indentWriter.WriteLine("}");
    }

    private static void BuildMapFromProtobufExtensionMethod(ITypeSymbol source, ITypeSymbol destination, ImmutableArray<IPropertySymbol> destinationProperties, IMethodSymbol constructor, NamespaceGatherer namespaces, IndentedTextWriter indentWriter)
    {
        var fullyQualifiedSource = $"global::{source.ContainingNamespace.Name}.{source.Name}";
        var fullyQualifiedDestination = $"global::{destination.ContainingNamespace.Name}.{destination.Name}";

        var parameters = new string[constructor.Parameters.Length + 1];
        parameters[0] = $"this {fullyQualifiedSource} self";

        for (var i = 0; i < constructor.Parameters.Length; i++)
        {
            var parameter = constructor.Parameters[i];

            if (parameter.Type.ContainingNamespace != null)
            {
                namespaces.Add(parameter.Type.ContainingNamespace);
            }

            var nullableAnnotation = parameter.NullableAnnotation == NullableAnnotation.Annotated ? "?" : string.Empty;
            var optionalValue = parameter.HasExplicitDefaultValue ? $" = {parameter.ExplicitDefaultValue.GetDefaultValue()}" : string.Empty;
            parameters[i + 1] = $"{parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}{nullableAnnotation} {parameter.Name}{optionalValue}";
        }

        indentWriter.WriteLine($"public static {fullyQualifiedDestination} MapFromProtobuf({string.Join(", ", parameters)})");
        indentWriter.WriteLine("{");
        indentWriter.Indent++;

        if (!source.IsValueType)
        {
            indentWriter.WriteLine("var mapped = self is null ? throw new ArgumentNullException(nameof(self)) :");
            namespaces.Add(typeof(ArgumentNullException));
            indentWriter.Indent++;
        }

        if (constructor.Parameters.Length == 0)
        {
            indentWriter.WriteLine($"new {fullyQualifiedDestination}()");
        }
        else
        {
            indentWriter.WriteLine(
                $"new {fullyQualifiedDestination}({string.Join(", ", constructor.Parameters.Select(_ => _.Name))})");
        }

        indentWriter.WriteLine("{");
        indentWriter.Indent++;

        var remaining = new List<IPropertySymbol>();

        foreach (var destinationProperty in destinationProperties)
        {
            if (source.GetMembers(destinationProperty.Name).OfType<IPropertySymbol>().SingleOrDefault() is not IPropertySymbol sourceProperty)
            {
                // TODO: diagnostics
                continue;
            }

            if (destinationProperty.NullableAnnotation == NullableAnnotation.Annotated && sourceProperty.Type.IsValueType)
            {
                remaining.Add(destinationProperty);
            }
            else if (SymbolEqualityComparer.Default.Equals(sourceProperty.Type, MappingContext.Int64TypeSymbol) && SymbolEqualityComparer.Default.Equals(destinationProperty.Type, MappingContext.DateTimeTypeSymbol))
            {
                indentWriter.WriteLine($"{destinationProperty.Name} = DateTime.FromBinary(self.{destinationProperty.Name}),");
            }
            else if (SymbolEqualityComparer.Default.Equals(sourceProperty.Type, MappingContext.ByteStringTypeSymbol) && SymbolEqualityComparer.Default.Equals(destinationProperty.Type, MappingContext.GuidTypeSymbol))
            {
                indentWriter.WriteLine($"{destinationProperty.Name} = new Guid(self.{destinationProperty.Name}.ToByteArray()),");
            }
            else if (SymbolEqualityComparer.Default.Equals(sourceProperty.Type, MappingContext.ByteStringTypeSymbol) && destinationProperty.Type.Kind == SymbolKind.ArrayType && destinationProperty.Type.IsByteArrayType())
            {
                indentWriter.WriteLine($"{destinationProperty.Name} = self.{destinationProperty.Name}.ToByteArray(),");
            }
            else if (destinationProperty.Type.TypeKind == TypeKind.Enum)
            {
                var destinationType = Type.GetType(destinationProperty.Type.ToDisplayString(_format));
                indentWriter.WriteLine($"{destinationProperty.Name} = (global::{destinationProperty.Type})self.{destinationProperty.Name},");
            }
            else
            {
                var destinationType = Type.GetType(destinationProperty.Type.ToDisplayString(_format));
                var sourceType = Type.GetType(sourceProperty.Type.ToDisplayString(_format));
                if (destinationType?.IsAssignableFrom(sourceType) == true)
                {
                    indentWriter.WriteLine($"{destinationProperty.Name} = self.{destinationProperty.Name},");
                }
                else
                {
                    // TODO: diagnostics
                }
            }
        }

        indentWriter.Indent--;
        indentWriter.WriteLine($"}};{Environment.NewLine}");

        if (!source.IsValueType)
        {
            indentWriter.Indent--;
        }

        foreach (var destinationProperty in remaining)
        {
            var hasProperty = source.GetMembers($"Has{destinationProperty.Name}").OfType<IPropertySymbol>().SingleOrDefault();
            if (hasProperty != null)
            {
                indentWriter.WriteLine($"if (self.Has{destinationProperty.Name})");
                indentWriter.WriteLine("{");
                indentWriter.Indent++;
                indentWriter.WriteLine($"mapped.{destinationProperty.Name} = self.{destinationProperty.Name};");
                indentWriter.Indent--;
                indentWriter.WriteLine($"}}{Environment.NewLine}");
            }
            else
            {
                indentWriter.WriteLine($"mapped.{destinationProperty.Name} = self.{destinationProperty.Name};");
            }
        }

        indentWriter.WriteLine("return mapped;");

        indentWriter.Indent--;
        indentWriter.WriteLine("}");
    }

    private static void BuildMapFromProtobufExtensionMethodWithTarget(ITypeSymbol source, ITypeSymbol destination, ImmutableArray<IPropertySymbol> destinationProperties, NamespaceGatherer namespaces, IndentedTextWriter indentWriter)
    {
        var fullyQualifiedSource = $"global::{source.ContainingNamespace.Name}.{source.Name}";
        var fullyQualifiedDestination = $"global::{destination.ContainingNamespace.Name}.{destination.Name}";

        var parameters = new string[1];
        parameters[0] = $"this {fullyQualifiedSource} self";

        indentWriter.WriteLine($"public static {fullyQualifiedDestination} MapFromProtobuf(this {fullyQualifiedSource} self, {fullyQualifiedDestination} target)");
        indentWriter.WriteLine("{");
        indentWriter.Indent++;

        if (!source.IsValueType)
        {
            indentWriter.WriteLine("if (target == null) throw new ArgumentNullException(nameof(target));");
        }

        var remaining = new List<IPropertySymbol>();

        foreach (var destinationProperty in destinationProperties)
        {
            if (source.GetMembers(destinationProperty.Name).OfType<IPropertySymbol>().SingleOrDefault() is not IPropertySymbol sourceProperty)
            {
                // TODO: diagnostics
                continue;
            }

            if (SymbolEqualityComparer.Default.Equals(sourceProperty.Type, MappingContext.Int64TypeSymbol) && SymbolEqualityComparer.Default.Equals(destinationProperty.Type, MappingContext.DateTimeTypeSymbol))
            {
                indentWriter.WriteLine($"target.{destinationProperty.Name} = DateTime.FromBinary(self.{destinationProperty.Name});");
            }
            else if (SymbolEqualityComparer.Default.Equals(sourceProperty.Type, MappingContext.ByteStringTypeSymbol) && SymbolEqualityComparer.Default.Equals(destinationProperty.Type, MappingContext.GuidTypeSymbol))
            {
                indentWriter.WriteLine($"target.{destinationProperty.Name} = new Guid(self.{destinationProperty.Name}.ToByteArray());");
            }
            else if (SymbolEqualityComparer.Default.Equals(sourceProperty.Type, MappingContext.ByteStringTypeSymbol) && destinationProperty.Type.Kind == SymbolKind.ArrayType && destinationProperty.Type.IsByteArrayType())
            {
                indentWriter.WriteLine($"target.{destinationProperty.Name} = self.{destinationProperty.Name}.ToByteArray();");
            }
            else if (destinationProperty.Type.TypeKind == TypeKind.Enum)
            {
                var destinationType = Type.GetType(destinationProperty.Type.ToDisplayString(_format));
                indentWriter.WriteLine($"target.{destinationProperty.Name} = (global::{destinationProperty.Type})self.{destinationProperty.Name};");
            }
            else
            {
                var destinationType = Type.GetType(destinationProperty.Type.ToDisplayString(_format));
                var sourceType = Type.GetType(sourceProperty.Type.ToDisplayString(_format));
                if (destinationType?.IsAssignableFrom(sourceType) == true)
                {
                    indentWriter.WriteLine($"target.{destinationProperty.Name} = self.{destinationProperty.Name};");
                }
                else
                {
                    // TODO: diagnostics
                }
            }
        }

        foreach (var destinationProperty in remaining)
        {
            var hasProperty = source.GetMembers($"Has{destinationProperty.Name}").OfType<IPropertySymbol>().SingleOrDefault();
            if (hasProperty != null)
            {
                indentWriter.WriteLine($"if (self.Has{destinationProperty.Name})");
                indentWriter.WriteLine("{");
                indentWriter.Indent++;
                indentWriter.WriteLine($"target.{destinationProperty.Name} = self.{destinationProperty.Name};");
                indentWriter.Indent--;
                indentWriter.WriteLine($"}}{Environment.NewLine}");
            }
            else
            {
                indentWriter.WriteLine($"target.{destinationProperty.Name} = self.{destinationProperty.Name};");
            }
        }

        indentWriter.WriteLine("return target;");

        indentWriter.Indent--;
        indentWriter.WriteLine("}");
    }

    public SourceText Text { get; private set; }
}
