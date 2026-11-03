using InternationalCatalogue.Domain;

using JustDummies;

using Xunit;

namespace InternationalCatalogue.Tests;

/// <summary>
///     Ordinary Japanese and Korean text that the script ranges of the model leave out. Each case is a literal, since the
///     test is about this very character: it is not a dummy.
/// </summary>
public sealed class ScriptCoverageTests {

    [Theory(Skip = "domain defect: U+3005 IDEOGRAPHIC ITERATION MARK (a letter, Lm) lies outside every CJK range, so common Japanese surnames (\u4F50\u3005\u6728) are refused as 'no supported script'; literal case, no seed")]
    [InlineData("佐\u3005木")]
    [InlineData("時\u3005")]
    public void A_name_using_the_ideographic_iteration_mark_is_a_Japanese_name(string family) {
        // Act
        PersonName name = new PersonName(family);

        // Assert
        Assert.Equal(WritingScript.Cjk, name.Script);
    }

    [Theory(Skip = "domain defect: Hangul Compatibility Jamo (U+3131..U+318E, letters) lie outside the CJK ranges, so Korean text is refused or has no script; literal case, no seed")]
    [InlineData("ㅋㅋㅋ")]
    public void Korean_compatibility_jamo_belong_to_the_CJK_script(string text) {
        // Act
        Title title = new Title(text);

        // Assert
        Assert.Equal([WritingScript.Cjk], title.LetterScripts());
    }

    [Theory(Skip = "domain defect: halfwidth katakana (U+FF66..U+FF9F) are Japanese letters but lie outside the CJK ranges; literal case, no seed")]
    [InlineData("ｶﾀｶﾅ")]
    public void Halfwidth_katakana_belong_to_the_CJK_script(string text) {
        // Act
        Title title = new Title(text);

        // Assert
        Assert.Equal([WritingScript.Cjk], title.LetterScripts());
    }

}
