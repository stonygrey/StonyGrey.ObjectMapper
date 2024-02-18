using NUnit.Framework;

namespace StonyGrey.ObjectMapper.Tests;

public static class HelpUrlBuilderTests
{
   [Test]
   public static void Create() =>
	   Assert.That(HelpUrlBuilder.Build("a", "b"),
		   Is.EqualTo("https://github.com/stonygrey/StonyGrey.ObjectMapper/tree/main/docs/a-b.md"));
}