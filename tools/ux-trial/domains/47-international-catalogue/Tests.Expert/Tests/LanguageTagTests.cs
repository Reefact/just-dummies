using Xunit;

namespace InternationalCatalogue.Domain.Tests;

public sealed class LanguageTagTests {

    [Theory]
    [InlineData("fr", "fr", null, null)]
    [InlineData("FR", "fr", null, null)]
    [InlineData("fil", "fil", null, null)]
    [InlineData("fr-ca", "fr-CA", null, "CA")]
    [InlineData("ES-419", "es-419", null, "419")]
    [InlineData("zh-hant", "zh-Hant", "Hant", null)]
    [InlineData("SR-LATN-rs", "sr-Latn-RS", "Latn", "RS")]
    [InlineData("sr-Cyrl-RS", "sr-Cyrl-RS", "Cyrl", "RS")]
    public void A_tag_is_stored_in_canonical_case(string written, string canonical, string? script, string? region) {
        // Act
        LanguageTag tag = new LanguageTag(written);

        // Assert
        Assert.Equal(canonical, tag.Value);
        Assert.Equal(canonical, tag.ToString());
        Assert.Equal(canonical.Split('-')[0], tag.Language);
        Assert.Equal(script, tag.Script);
        Assert.Equal(region, tag.Region);
    }

    [Fact]
    public void A_tag_is_required() {
        Assert.Throws<ArgumentNullException>(() => new LanguageTag(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("e")]
    [InlineData("engl")]
    [InlineData("en_US")]
    [InlineData("en-U")]
    [InlineData("en-USA")]
    [InlineData("en-12")]
    [InlineData("en-1234")]
    [InlineData("en-Latn-")]
    [InlineData("en-Latn-US-x")]
    [InlineData("en-US-Latn")]
    [InlineData("en-Latn-Latn")]
    [InlineData("fr-")]
    [InlineData("-fr")]
    [InlineData(" fr")]
    [InlineData("fr ")]
    [InlineData("fr\n")]
    [InlineData("fé")]
    [InlineData("1r")]
    public void A_malformed_tag_is_refused(string malformed) {
        Assert.Throws<FormatException>(() => new LanguageTag(malformed));
    }

    [Theory]
    [InlineData("sr-Latn", WritingScript.Latin)]
    [InlineData("el-Grek", WritingScript.Greek)]
    [InlineData("sr-Cyrl", WritingScript.Cyrillic)]
    [InlineData("ar-Arab", WritingScript.Arabic)]
    [InlineData("he-Hebr", WritingScript.Hebrew)]
    [InlineData("hi-Deva", WritingScript.Devanagari)]
    [InlineData("zh-Hani", WritingScript.Cjk)]
    [InlineData("zh-Hans", WritingScript.Cjk)]
    [InlineData("zh-Hant", WritingScript.Cjk)]
    [InlineData("ja-Jpan", WritingScript.Cjk)]
    [InlineData("ko-Kore", WritingScript.Cjk)]
    [InlineData("ja-Hira", WritingScript.Cjk)]
    [InlineData("ja-Kana", WritingScript.Cjk)]
    [InlineData("ko-Hang", WritingScript.Cjk)]
    public void A_script_subtag_designates_a_catalogue_script(string written, WritingScript expected) {
        Assert.Equal(expected, new LanguageTag(written).DeclaredScript);
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("fr-CA")]
    [InlineData("th-Thai")]
    [InlineData("ka-Geor")]
    [InlineData("und-Zzzz")]
    public void A_tag_designates_no_script_without_a_script_subtag_or_with_an_unsupported_one(string written) {
        Assert.Null(new LanguageTag(written).DeclaredScript);
    }

    [Fact]
    public void Tags_are_equal_whatever_the_case_they_were_written_in() {
        Assert.Equal(new LanguageTag("fr-CA"), new LanguageTag("FR-ca"));
        Assert.Equal(new LanguageTag("fr-CA").GetHashCode(), new LanguageTag("FR-ca").GetHashCode());
        Assert.NotEqual(new LanguageTag("fr"), new LanguageTag("fr-CA"));
    }

}
