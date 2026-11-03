using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace InternationalCatalogue.Domain.Tests.Discovery;

[Reproducible]
public sealed class ArticleRuleTests {

    private static ArticleRule English => new ArticleRule(new LanguageTag("en"), ["the", "a", "an"]);

    private static ArticleRule French => new ArticleRule(new LanguageTag("fr"), ["le", "la", "les", "l'", "un", "une"]);

    [Fact]
    public void A_rule_keeps_its_articles_longest_first() {
        // Act
        ArticleRule rule = new ArticleRule(new LanguageTag("en"), ["a", "the", "an"]);

        // Assert
        Assert.Equal(["the", "an", "a"], rule.Articles);
    }

    [Fact]
    public void A_rule_writes_the_apostrophe_of_an_elided_article_straight() {
        // Act
        ArticleRule rule = new ArticleRule(new LanguageTag("fr"), ["l’"]);

        // Assert
        Assert.Equal(["l'"], rule.Articles);
    }

    [Fact]
    public void A_rule_belongs_to_a_bare_language() {
        // Arrange
        IEnumerable<string> anyArticles = Any.ArticleRule().Generate().Articles;

        // Act
        ArticleRule rule = new ArticleRule(new LanguageTag("fra"), anyArticles);

        // Assert
        Assert.Equal("fra", rule.Language.Value);
    }

    [Theory]
    [InlineData("sr-Latn")]
    [InlineData("pt-BR")]
    [InlineData("zh-Hant-TW")]
    [InlineData("es-419")]
    public void A_rule_refuses_a_language_with_a_script_or_a_region(string tag) {
        // Arrange
        IEnumerable<string> anyArticles = Any.ArticleRule().Generate().Articles;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ArticleRule(new LanguageTag(tag), anyArticles));
    }

    [Fact]
    public void A_rule_needs_a_language_and_a_list_of_articles() {
        // Arrange
        LanguageTag anyLanguage = Any.LanguageTag().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ArticleRule(null!, ["the"]));
        Assert.Throws<ArgumentNullException>(() => new ArticleRule(anyLanguage, null!));
    }

    [Fact]
    public void A_rule_lists_at_least_one_article() {
        // Arrange
        LanguageTag bareLanguage = new LanguageTag("en");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ArticleRule(bareLanguage, []));
    }

    [Fact]
    public void A_rule_lists_at_most_twenty_articles() {
        // Arrange
        IEnumerable<string> twenty = Enumerable.Range(0, 20).Select(i => "art" + (char)('a' + i));
        IEnumerable<string> twentyOne = Enumerable.Range(0, 21).Select(i => "art" + (char)('a' + i));

        // Act
        ArticleRule rule = new ArticleRule(new LanguageTag("xx"), twenty);

        // Assert
        Assert.Equal(20, rule.Articles.Count);
        Assert.Throws<ArgumentException>(() => new ArticleRule(new LanguageTag("xx"), twentyOne));
    }

    [Fact]
    public void A_rule_refuses_a_null_article() {
        Assert.Throws<ArgumentException>(() => new ArticleRule(new LanguageTag("en"), ["the", null!]));
    }

    [Theory]
    [InlineData("the3")]
    [InlineData("the-")]
    [InlineData("t.he")]
    [InlineData("d'un")]
    [InlineData("l''")]
    [InlineData("'")]
    [InlineData("''")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("the the")]
    public void An_article_is_letters_optionally_followed_by_one_apostrophe(string article) {
        Assert.Throws<ArgumentException>(() => new ArticleRule(new LanguageTag("en"), [article]));
    }

    [Fact]
    public void An_article_may_hold_twelve_grapheme_clusters_but_not_thirteen() {
        // Arrange
        string twelve = new string('a', 12);
        string thirteen = new string('a', 13);

        // Act
        ArticleRule rule = new ArticleRule(new LanguageTag("xx"), [twelve]);

        // Assert
        Assert.Equal([twelve], rule.Articles);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArticleRule(new LanguageTag("xx"), [thirteen]));
    }

    [Theory]
    [InlineData("The", "the")]
    [InlineData("der", "DER")]
    [InlineData("l'", "L’")]
    [InlineData("é", "É")]
    [InlineData("á", "á")]
    public void Two_articles_equal_under_case_folding_are_a_duplicate(string first, string second) {
        Assert.Throws<ArgumentException>(() => new ArticleRule(new LanguageTag("xx"), [first, second]));
    }

    [Theory]
    [InlineData("The Hobbit", "Hobbit")]
    [InlineData("THE HOBBIT", "HOBBIT")]
    [InlineData("the hobbit", "hobbit")]
    [InlineData("An Education", "Education")]
    [InlineData("A Tale of Two Cities", "Tale of Two Cities")]
    [InlineData("The  Hobbit", "Hobbit")]
    [InlineData("The The Band", "The Band")]
    public void A_plain_article_followed_by_a_space_is_skipped(string title, string expected) {
        Assert.Equal(expected, English.Strip(title));
    }

    [Theory]
    [InlineData("Theory of Everything")]
    [InlineData("Another Day")]
    [InlineData("Anthology")]
    [InlineData("The\tHobbit")]
    [InlineData("Hobbit, The")]
    [InlineData("Hobbit")]
    [InlineData("")]
    public void A_title_that_does_not_start_with_an_article_and_a_space_is_kept(string title) {
        Assert.Equal(title, English.Strip(title));
    }

    [Theory]
    [InlineData("The")]
    [InlineData("The ")]
    [InlineData("A   ")]
    public void A_title_that_would_become_empty_is_kept_whole(string title) {
        Assert.Equal(title, English.Strip(title));
    }

    [Theory]
    [InlineData("L'étranger", "étranger")]
    [InlineData("l'étranger", "étranger")]
    [InlineData("L’étranger", "étranger")]
    [InlineData("L'Oréal", "Oréal")]
    [InlineData("L' étranger", "étranger")]
    public void An_elided_article_is_skipped_without_a_space(string title, string expected) {
        Assert.Equal(expected, French.Strip(title));
    }

    [Theory]
    [InlineData("L'", "L'")]
    [InlineData("L’", "L’")]
    [InlineData("L'  ", "L'  ")]
    public void An_elided_article_alone_is_kept(string title, string expected) {
        Assert.Equal(expected, French.Strip(title));
    }

    [Theory]
    [InlineData("Les Misérables", "Misérables")]
    [InlineData("Le Rouge et le Noir", "Rouge et le Noir")]
    [InlineData("La Peste", "Peste")]
    [InlineData("Lettres de mon moulin", "Lettres de mon moulin")]
    [InlineData("Lesage", "Lesage")]
    [InlineData("Un amour de Swann", "amour de Swann")]
    public void The_longest_matching_article_is_the_one_skipped(string title, string expected) {
        Assert.Equal(expected, French.Strip(title));
    }

    [Fact]
    public void Only_the_first_article_is_skipped() {
        Assert.Equal("Les Halles", French.Strip("Le Les Halles"));
    }

    [Fact]
    public void An_article_of_another_script_is_matched_the_same_way() {
        // Arrange
        ArticleRule greek = new ArticleRule(new LanguageTag("el"), ["ο", "η", "το"]);

        // Act & Assert
        Assert.Equal("Οδύσσεια", greek.Strip("Η Οδύσσεια"));
        Assert.Equal("ΚΗΠΟΣ", greek.Strip("ΤΟ ΚΗΠΟΣ"));
    }

    [Fact]
    public void Stripping_a_null_title_is_refused() {
        Assert.Throws<ArgumentNullException>(() => English.Strip(null!));
    }

}
