using System.Text.RegularExpressions;
using JustDummies;
using Xunit;

namespace DocsMisc.Tests;

public sealed class DocsMiscProbes {
    private readonly ITestOutputHelper _output;

    public DocsMiscProbes(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public void Item2_NonEmpty_raises_the_ceiling_too() {
        int longest = 0;
        for (int draw = 0; draw < 20000; draw++) {
            longest = Math.Max(longest, Any.String().NonEmpty().Generate().Length);
        }
        _output.WriteLine($"longest NonEmpty draw: {longest}");
        Assert.True(longest > 1024);
    }

    [Fact]
    public void Item4_hex_and_unicode_escapes_are_supported() {
        _output.WriteLine(Any.StringMatching(@"\x41B").Generate());
        Assert.Equal("AB", Any.StringMatching(@"\x41B").Generate());
    }

    [Theory]
    [InlineData(@"\bab")]
    [InlineData(@"\p{L}")]
    [InlineData(@"[a-z-[aeiou]]")]
    [InlineData(@"(?>ab)")]
    public void Item4_refused_constructs(string pattern) {
        UnsupportedRegexException exception = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(pattern));
        _output.WriteLine(exception.Message);
    }

    [Fact]
    public void Item4_ignore_pattern_whitespace_is_a_plain_argument_exception() {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Any.StringMatching(new Regex("a b", RegexOptions.IgnorePatternWhitespace)));
        _output.WriteLine(exception.GetType().FullName + ": " + exception.Message);
    }

    [Fact]
    public void Item5_a_lambda_ignoring_its_argument_widens_the_base() {
        AnyCourse widened = new AnyCourse().WithCredits(_ => Any.Int32());
        ArgumentOutOfRangeException exception = Assert.ThrowsAny<ArgumentOutOfRangeException>(() => {
            for (int draw = 0; draw < 100; draw++) {
                widened.Generate();
            }
        });
        _output.WriteLine(exception.Message);
    }

    [Fact]
    public void Item6_datetime_kind_and_unconstrained_range() {
        DateTime min = DateTime.MaxValue;
        DateTime max = DateTime.MinValue;
        for (int draw = 0; draw < 2000; draw++) {
            DateTime value = Any.DateTime().Generate();
            Assert.Equal(DateTimeKind.Utc, value.Kind);
            if (value < min) { min = value; }
            if (value > max) { max = value; }
        }
        _output.WriteLine($"kind Utc; min {min:O}; max {max:O}");
    }
}
