using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace InternationalCatalogue.Domain.Tests.Discovery;

[Reproducible]
public sealed class LanguageTagTests {

    [Theory]
    [InlineData("fr", "fr")]
    [InlineData("FR", "fr")]
    [InlineData("fil", "fil")]
    [InlineData("SR-latn-rs", "sr-Latn-RS")]
    [InlineData("sr-LATN-rs", "sr-Latn-RS")]
    [InlineData("es-419", "es-419")]
    [InlineData("zh-HANT", "zh-Hant")]
    [InlineData("pt-br", "pt-BR")]
    [InlineData("zh-hans-cn", "zh-Hans-CN")]
    public void A_language_tag_is_stored_in_canonical_case(string text, string canonical) {
        // Act
        LanguageTag tag = new LanguageTag(text);

        // Assert
        Assert.Equal(canonical, tag.Value);
        Assert.Equal(canonical, tag.ToString());
    }

    [Fact]
    public void A_language_tag_splits_into_language_script_and_region() {
        // Act
        LanguageTag full = new LanguageTag("sr-Latn-RS");
        LanguageTag onlyRegion = new LanguageTag("es-419");
        LanguageTag onlyScript = new LanguageTag("zh-Hant");
        LanguageTag bare = new LanguageTag("fr");

        // Assert
        Assert.Equal(("sr", "Latn", "RS"), (full.Language, full.Script, full.Region));
        Assert.Equal(("es", null, "419"), (onlyRegion.Language, onlyRegion.Script, onlyRegion.Region));
        Assert.Equal(("zh", "Hant", null), (onlyScript.Language, onlyScript.Script, onlyScript.Region));
        Assert.Equal(("fr", null, null), (bare.Language, bare.Script, bare.Region));
    }

    [Theory]
    [InlineData("")]
    [InlineData("f")]
    [InlineData("fran")]
    [InlineData("fr_FR")]
    [InlineData("fr-")]
    [InlineData("-fr")]
    [InlineData("fr-F")]
    [InlineData("fr-FRA")]
    [InlineData("fr-12")]
    [InlineData("fr-1234")]
    [InlineData("fr-Latn-RS-extra")]
    [InlineData("fr-RS-Latn")]
    [InlineData("en-US-POSIX")]
    [InlineData("f1")]
    [InlineData("éé")]
    [InlineData("fr ")]
    [InlineData(" fr")]
    [InlineData("fr\n")]
    [InlineData("fr-٤١٩")]
    public void A_text_outside_the_simple_bcp47_form_is_refused(string text) {
        Assert.Throws<FormatException>(() => new LanguageTag(text));
    }

    [Fact]
    public void A_null_language_tag_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new LanguageTag(null!));
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
    [InlineData("zh-hant-tw", WritingScript.Cjk)]
    public void The_script_subtag_designates_a_catalogue_script(string text, WritingScript expected) {
        Assert.Equal(expected, new LanguageTag(text).DeclaredScript);
    }

    [Theory]
    [InlineData("th-Thai")]
    [InlineData("ka-Geor")]
    [InlineData("fr")]
    [InlineData("fr-FR")]
    [InlineData("es-419")]
    [InlineData("xx-Qaaa")]
    public void A_tag_without_a_script_subtag_or_with_an_unsupported_one_designates_no_script(string text) {
        Assert.Null(new LanguageTag(text).DeclaredScript);
    }

    [Fact]
    public void Two_tags_are_equal_once_in_canonical_case() {
        Assert.Equal(new LanguageTag("SR-LATN-rs"), new LanguageTag("sr-Latn-RS"));
        Assert.NotEqual(new LanguageTag("sr-Latn"), new LanguageTag("sr"));
        Assert.Equal(new LanguageTag("PT-br").GetHashCode(), new LanguageTag("pt-BR").GetHashCode());
    }

}
