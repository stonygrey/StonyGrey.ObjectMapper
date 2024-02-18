namespace StonyGrey.ObjectMapper;

internal static class HelpUrlBuilder
{
   internal static string Build(string identifier, string title) =>
	 $"https://github.com/stonygrey/StonyGrey.ObjectMapper/tree/main/docs/{identifier}-{title}.md";
}