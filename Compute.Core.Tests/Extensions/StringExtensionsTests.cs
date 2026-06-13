using Compute.Core.Extensions;

namespace Compute.Core.Tests.Extensions
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("café", "cafe")]
        [InlineData("Köln", "Koln")]
        [InlineData("Açúcar", "Acucar")]
        [InlineData("São Paulo", "Sao Paulo")]
        [InlineData("naïve", "naive")]
        [InlineData("plain ascii", "plain ascii")]
        [InlineData("", "")]
        public void RemoveAccents_StripsDiacritics(string input, string expected)
        {
            Assert.Equal(expected, input.RemoveAccents());
        }

        [Theory]
        [InlineData("value", true)]
        [InlineData("  ", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsSet_IsTrue_OnlyForNonWhitespace(string? value, bool expected)
        {
            Assert.Equal(expected, value.IsSet());
        }

        [Theory]
        [InlineData("value", false)]
        [InlineData("  ", true)]
        [InlineData("", true)]
        public void IsNotSet_IsInverseOfIsSet(string value, bool expected)
        {
            Assert.Equal(expected, value.IsNotSet());
        }

        [Theory]
        [InlineData("value", false)]
        [InlineData("", true)]
        public void IsNullOrEmpty_DetectsEmpty(string value, bool expected)
        {
            Assert.Equal(expected, value.IsNullOrEmpty());
        }

        [Theory]
        [InlineData("value", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsNotNullOrEmpty_DetectsContent(string? value, bool expected)
        {
            Assert.Equal(expected, value.IsNotNullOrEmpty());
        }

        [Theory]
        [InlineData("Hello World", "WORLD", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello World", "world", StringComparison.Ordinal, false)]
        [InlineData("Hello World", "lo Wo", StringComparison.Ordinal, true)]
        [InlineData("Hello World", "missing", StringComparison.OrdinalIgnoreCase, false)]
        public void Contains_RespectsStringComparison(string source, string toCheck, StringComparison comparison, bool expected)
        {
            Assert.Equal(expected, source.Contains(toCheck, comparison));
        }
    }
}
