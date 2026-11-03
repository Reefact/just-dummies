using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace InternationalCatalogue.Domain.Tests.Discovery;

/// <summary>
/// What a reviewer would expect of the catalogue on real-world text, and what the model's tables do not allow.
/// None of these tests draws a value: the skip reason says so where the usual form would give a seed.
/// </summary>
[Reproducible]
public sealed class DomainDefectTests {

    [Theory(Skip = "domain defect: U+3005 (the iteration mark, a letter) lies outside every CJK range of the model, so common Japanese titles read as an unsupported script; deterministic, no seed")]
    [InlineData("人々")]
    [InlineData("様々な出会い")]
    [InlineData("時々")]
    public void A_japanese_title_with_the_iteration_mark_belongs_to_the_cjk_script(string text) {
        // Act
        Title title = new Title(text);

        // Assert
        Assert.False(title.HasLettersOfUnsupportedScript());
        Assert.Equal([WritingScript.Cjk], title.LetterScripts());
    }

    [Fact(Skip = "domain defect: U+3005 (the iteration mark) is refused as 'no supported script', so the common surname 佐々木 cannot be recorded; deterministic, no seed")]
    public void A_japanese_family_name_with_the_iteration_mark_is_accepted() {
        // Act
        PersonName name = new PersonName("佐々木", "太郎");

        // Assert
        Assert.Equal(WritingScript.Cjk, name.Script);
    }

    [Theory(Skip = "domain defect: full-width Latin letters, half-width katakana and Hangul compatibility jamo are letters outside every range of the model, so they read as an unsupported script; deterministic, no seed")]
    [InlineData("ＡＩ入門")]
    [InlineData("ｱｲｳ")]
    [InlineData("ㅋㅋㅋ")]
    public void A_cjk_title_with_full_width_or_half_width_forms_belongs_to_the_cjk_script(string text) {
        Assert.False(new Title(text).HasLettersOfUnsupportedScript());
    }

    [Theory(Skip = "domain defect: a title made only of invisible characters is neither empty nor blank to Trim(), yet displays as nothing; deterministic, no seed")]
    [InlineData("​")]
    [InlineData("‍‌")]
    [InlineData("﻿")]
    public void A_title_that_displays_as_nothing_is_refused(string invisible) {
        Assert.Throws<ArgumentException>(() => new Title(invisible));
    }

    [Fact(Skip = "domain defect: the zero-width non-joiner required to spell Persian names is not among the characters a name may hold, so they cannot be recorded; deterministic, no seed")]
    public void A_persian_name_with_a_zero_width_non_joiner_is_accepted() {
        Assert.Equal(WritingScript.Arabic, new PersonName("عبد‌الله").Script);
    }

}
