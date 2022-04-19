using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StonyGrey.ObjectMapper.Configuration;

internal sealed class ConfigurationValues
{
    private const string IndentSizeKey = "indent_size";
    private const uint IndentSizeDefaultValue = 4u;
    private const string IndentStyleKey = "indent_style";
    private const IndentStyle IndentStyleDefaultValue = IndentStyle.Space;

    public ConfigurationValues(AnalyzerConfigOptionsProvider optionsProvider, SyntaxTree tree)
    {
        var options = optionsProvider.GetOptions(tree);
        IndentStyle = options.TryGetValue(ConfigurationValues.IndentStyleKey, out var indentStyle) ?
            (Enum.TryParse<IndentStyle>(indentStyle, out var indentStyleValue) ? indentStyleValue : ConfigurationValues.IndentStyleDefaultValue) :
            ConfigurationValues.IndentStyleDefaultValue;
        IndentSize = options.TryGetValue(ConfigurationValues.IndentSizeKey, out var indentSize) ?
            (uint.TryParse(indentSize, out var indentSizeValue) ? indentSizeValue : ConfigurationValues.IndentSizeDefaultValue) :
            ConfigurationValues.IndentSizeDefaultValue;
    }

    internal IndentStyle IndentStyle { get; }
    internal uint IndentSize { get; }
}
