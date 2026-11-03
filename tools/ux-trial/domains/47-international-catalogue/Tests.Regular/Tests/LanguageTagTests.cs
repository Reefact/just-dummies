using InternationalCatalogue.Domain;

using JustDummies;

using Xunit;

namespace InternationalCatalogue.Tests;

public sealed class LanguageTagTests {

    [Theory]
    [InlineData("fr", "fr")]
    [InlineData("FR", "fr")]
    [InlineData("fra", "fra")]
    [InlineData("EN-gb", "en-GB")]
    [InlineData("SR-latn-rs", "sr-Latn-RS")]
    [InlineData("zh-HANT", "zh-Hant")]
    [InlineData("es-419", "es-419")]
    [InlineData("ES-Latn-419", "es-Latn-419")]
    public void A_tag_is_accepted_in_any_case_and_stored_canonically(string written, string canonical) {
        // Act
        LanguageTag tag = new LanguageTag(written);

        // Assert
        Assert.Equal(canonical, tag.Value);
        Assert.Equal(canonical, tag.ToString());
    }

    [Fact]
    public void A_tag_exposes_its_subtags() {
        // Act
        LanguageTag full     = new LanguageTag("sr-latn-rs");
        LanguageTag language = new LanguageTag("FR");
        LanguageTag region   = new LanguageTag("es-419");

        // Assert
        Assert.Equal("sr", full.Language);
        Assert.Equal("Latn", full.Script);
        Assert.Equal("RS", full.Region);
        Assert.Equal("fr", language.Language);
        Assert.Null(language.Script);
        Assert.Null(language.Region);
        Assert.Null(region.Script);
        Assert.Equal("419", region.Region);
    }

    [Theory]
    [InlineData("")]
    [InlineData("f")]
    [InlineData("french")]
    [InlineData("fr_FR")]
    [InlineData("fr-")]
    [InlineData("-fr")]
    [InlineData(" fr")]
    [InlineData("fr ")]
    [InlineData("fr\n")]
    [InlineData("fr-F")]
    [InlineData("fr-FRA")]
    [InlineData("fr-12")]
    [InlineData("fr-1234")]
    [InlineData("fr-Latn-Cyrl")]
    [InlineData("fr-FR-Latn")]
    [InlineData("fr-FR-x")]
    [InlineData("fr-Lat")]
    [InlineData("f1")]
    [InlineData("é")]
    [InlineData("fr-٤١٩")]
    public void A_malformed_tag_is_refused(string written) {
        Assert.Throws<FormatException>(() => new LanguageTag(written));
    }

    [Fact]
    public void A_null_tag_is_refused() {
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
    public void The_script_subtag_designates_a_catalogue_script(string written, WritingScript expected) {
        // Act
        LanguageTag tag = new LanguageTag(written);

        // Assert
        Assert.Equal(expected, tag.DeclaredScript);
    }

    [Theory]
    [InlineData("th-Thai")]
    [InlineData("ka-Geor")]
    [InlineData("xx-Qaaa")]
    public void A_script_subtag_the_catalogue_does_not_support_designates_nothing(string written) {
        // Act
        LanguageTag tag = new LanguageTag(written);

        // Assert
        Assert.NotNull(tag.Script);
        Assert.Null(tag.DeclaredScript);
    }

    [Fact]
    public void A_tag_without_a_script_subtag_declares_no_script() {
        // Arrange
        string anyLanguage = Any.StringMatching("[a-z]{2,3}").Generate();
        string anyRegion   = Any.StringMatching("[A-Z]{2}").Generate();

        // Act
        LanguageTag tag = new LanguageTag(anyLanguage + "-" + anyRegion);

        // Assert
        Assert.Null(tag.DeclaredScript);
    }

    [Fact]
    public void Tags_are_equal_when_their_canonical_forms_are() {
        // Assert
        Assert.Equal(new LanguageTag("sr-latn-rs"), new LanguageTag("SR-LATN-RS"));
        Assert.Equal(new LanguageTag("sr-latn-rs").GetHashCode(), new LanguageTag("SR-LATN-RS").GetHashCode());
        Assert.NotEqual(new LanguageTag("fr"), new LanguageTag("fr-FR"));
    }

}
