using JustDummies;

using Xunit;

namespace InternationalCatalogue.Domain.Tests;

public sealed class ArticleRuleTests {

    private static ArticleRule English(params string[] articles) {
        return new ArticleRule(new LanguageTag("en"), articles);
    }

    [Fact]
    public void A_rule_applies_to_a_bare_language() {
        // Arrange
        LanguageTag french = new LanguageTag("fr");

        // Act
        ArticleRule rule = new ArticleRule(french, ["le"]);

        // Assert
        Assert.Equal(french, rule.Language);
    }

    [Theory]
    [InlineData("sr-Latn")]
    [InlineData("fr-CA")]
    [InlineData("sr-Latn-RS")]
    public void A_rule_cannot_apply_to_a_tag_with_a_script_or_a_region(string notBare) {
        Assert.Throws<ArgumentException>(() => new ArticleRule(new LanguageTag(notBare), ["le"]));
    }

    [Fact]
    public void A_rule_needs_a_language_and_articles() {
        // Arrange
        LanguageTag anyLanguage = new AnyLanguageTag().Bare().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new ArticleRule(null!, ["the"]));
        Assert.Throws<ArgumentNullException>(() => new ArticleRule(anyLanguage, null!));
    }

    [Fact]
    public void A_rule_lists_at_least_one_article() {
        Assert.Throws<ArgumentException>(() => English());
    }

    [Fact]
    public void A_rule_lists_at_most_twenty_articles() {
        // Arrange
        string[] twenty         = Enumerable.Range(0, 20).Select(index => "a" + (char)('a' + index)).ToArray();
        string[] twentyOne      = twenty.Append("zz").ToArray();

        // Act
        ArticleRule accepted = English(twenty);

        // Assert
        Assert.Equal(20, accepted.Articles.Count);
        Assert.Throws<ArgumentException>(() => English(twentyOne));
    }

    [Fact]
    public void An_article_cannot_be_null() {
        Assert.Throws<ArgumentException>(() => English("the", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("'")]
    [InlineData("’")]
    public void An_article_must_contain_letters(string withoutLetters) {
        Assert.Throws<ArgumentException>(() => English(withoutLetters));
    }

    [Theory]
    [InlineData("a1")]
    [InlineData("l''")]
    [InlineData("l'a")]
    [InlineData("'l")]
    [InlineData("la le")]
    [InlineData("the.")]
    [InlineData("the-")]
    [InlineData("\U0001F600")]
    public void An_article_holds_letters_optionally_followed_by_one_apostrophe(string malformed) {
        Assert.Throws<ArgumentException>(() => English(malformed));
    }

    [Fact]
    public void An_article_may_have_twelve_grapheme_clusters_but_not_thirteen() {
        // Act
        ArticleRule accepted = English(new string('a', ArticleRule.MaxArticleLength));

        // Assert
        Assert.Single(accepted.Articles);
        Assert.Throws<ArgumentOutOfRangeException>(() => English(new string('a', 13)));
    }

    [Fact]
    public void Articles_are_trimmed_and_the_typographic_apostrophe_is_stored_as_the_plain_one() {
        // Act
        ArticleRule rule = new ArticleRule(new LanguageTag("fr"), ["  L’  "]);

        // Assert
        Assert.Equal(["L'"], rule.Articles);
    }

    [Theory]
    [InlineData("the", "THE")]
    [InlineData("l'", "L’")]
    [InlineData("él", "ÉL")]
    public void An_article_cannot_be_listed_twice_under_case_folding(string first, string second) {
        Assert.Throws<ArgumentException>(() => English(first, second));
    }

    [Fact]
    public void Articles_are_kept_longest_first() {
        // Act
        ArticleRule rule = English("a", "the", "an");

        // Assert
        Assert.Equal(["the", "an", "a"], rule.Articles);
    }

    [Theory]
    [InlineData("The Hobbit", "Hobbit")]
    [InlineData("THE HOBBIT", "HOBBIT")]
    [InlineData("the hobbit", "hobbit")]
    [InlineData("The  Hobbit", "Hobbit")]
    [InlineData("An Instinct for Dragons", "Instinct for Dragons")]
    [InlineData("A Tale of Two Cities", "Tale of Two Cities")]
    [InlineData("The The Band", "The Band")]
    public void A_plain_article_followed_by_a_space_is_skipped(string title, string filed) {
        Assert.Equal(filed, English("a", "an", "the").Strip(title));
    }

    [Theory]
    [InlineData("Theory of Games")]
    [InlineData("Another Country")]
    [InlineData("Annie")]
    [InlineData("The\tHobbit")]
    [InlineData("Hobbit, The")]
    public void A_plain_article_is_skipped_only_as_a_whole_word_followed_by_a_space(string title) {
        Assert.Equal(title, English("a", "an", "the").Strip(title));
    }

    [Theory]
    [InlineData("A", "A")]
    [InlineData("The", "The")]
    [InlineData("The ", "The ")]
    [InlineData("The   ", "The   ")]
    public void A_title_made_of_an_article_alone_is_kept_whole(string title, string filed) {
        Assert.Equal(filed, English("a", "an", "the").Strip(title));
    }

    [Theory]
    [InlineData("L'étranger", "étranger")]
    [InlineData("L’Étranger", "Étranger")]
    [InlineData("l'Étranger", "Étranger")]
    [InlineData("L'", "L'")]
    [InlineData("L’", "L’")]
    public void An_elided_article_is_skipped_without_a_space(string title, string filed) {
        // Arrange
        ArticleRule french = new ArticleRule(new LanguageTag("fr"), ["le", "la", "les", "l'"]);

        // Assert
        Assert.Equal(filed, french.Strip(title));
    }

    [Theory]
    [InlineData("Le Rouge et le Noir", "Rouge et le Noir")]
    [InlineData("Les Misérables", "Misérables")]
    [InlineData("La Peste", "Peste")]
    [InlineData("Lemonde", "Lemonde")]
    public void The_longest_matching_article_wins(string title, string filed) {
        // Arrange
        ArticleRule french = new ArticleRule(new LanguageTag("fr"), ["le", "la", "les", "l'"]);

        // Assert
        Assert.Equal(filed, french.Strip(title));
    }

    [Fact]
    public void A_title_is_required_to_strip() {
        Assert.Throws<ArgumentNullException>(() => English("the").Strip(null!));
    }

    [Fact]
    public void An_article_matches_without_regard_to_the_case_of_non_ascii_letters() {
        // Arrange
        ArticleRule spanish = new ArticleRule(new LanguageTag("es"), ["él"]);

        // Assert
        Assert.Equal("Libro", spanish.Strip("ÉL Libro"));
    }

}
