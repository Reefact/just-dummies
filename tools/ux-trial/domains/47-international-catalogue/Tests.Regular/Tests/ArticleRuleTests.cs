using InternationalCatalogue.Domain;

using JustDummies;

using Xunit;

namespace InternationalCatalogue.Tests;

public sealed class ArticleRuleTests {

    private static ArticleRule English() => new ArticleRule(new LanguageTag("en"), ["the", "a", "an"]);

    private static ArticleRule French() => new ArticleRule(new LanguageTag("fr"), ["le", "la", "les", "l'"]);

    [Theory]
    [InlineData("The Hobbit", "Hobbit")]
    [InlineData("THE HOBBIT", "HOBBIT")]
    [InlineData("the hobbit", "hobbit")]
    [InlineData("A Tale of Two Cities", "Tale of Two Cities")]
    [InlineData("An Inspector Calls", "Inspector Calls")]
    [InlineData("The   Hobbit", "Hobbit")]
    public void A_leading_plain_article_followed_by_a_space_is_skipped_whatever_its_case(string title, string expected) {
        Assert.Equal(expected, English().Strip(title));
    }

    [Theory]
    [InlineData("Theory of Everything")]
    [InlineData("Another Country")]
    [InlineData("Anna Karenina")]
    [InlineData("A")]
    [InlineData("Hobbit, The")]
    [InlineData("The-Hobbit")]
    [InlineData("The\tHobbit")]
    [InlineData("")]
    public void A_title_that_does_not_start_with_an_article_and_a_space_is_kept(string title) {
        Assert.Equal(title, English().Strip(title));
    }

    [Theory]
    [InlineData("The")]
    [InlineData("A ")]
    [InlineData("the   ")]
    public void A_title_that_would_become_empty_is_kept_whole(string title) {
        Assert.Equal(title, English().Strip(title));
    }

    [Theory]
    [InlineData("L'étranger", "étranger")]
    [InlineData("L’étranger", "étranger")]
    [InlineData("l'ÉTRANGER", "ÉTRANGER")]
    [InlineData("L' étranger", "étranger")]
    [InlineData("Le Petit Prince", "Petit Prince")]
    [InlineData("Les Misérables", "Misérables")]
    [InlineData("La Peste", "Peste")]
    public void An_elided_article_is_skipped_directly_and_both_apostrophes_are_alike(string title, string expected) {
        Assert.Equal(expected, French().Strip(title));
    }

    [Theory]
    [InlineData("Lemonade")]
    [InlineData("Lait")]
    [InlineData("Lesbos")]
    [InlineData("L'")]
    [InlineData("L’")]
    public void An_article_inside_a_longer_word_is_not_an_article(string title) {
        Assert.Equal(title, French().Strip(title));
    }

    [Fact]
    public void The_longest_article_is_tried_first() {
        // Arrange
        ArticleRule rule = new ArticleRule(new LanguageTag("fr"), ["l'", "le", "les"]);

        // Act
        string stripped = rule.Strip("Les chats");

        // Assert
        Assert.Equal("chats", stripped);
        Assert.Equal(["les", "l'", "le"], rule.Articles);   // longest first; equal lengths keep the order given
    }

    [Fact]
    public void Articles_are_stored_with_a_straight_apostrophe() {
        // Act
        ArticleRule rule = new ArticleRule(new LanguageTag("it"), ["l’", "gli", "il", "lo"]);

        // Assert
        Assert.Equal(["gli", "l'", "il", "lo"], rule.Articles);
        Assert.Equal("it", rule.Language.Value);
    }

    [Fact]
    public void A_null_title_is_refused() {
        Assert.Throws<ArgumentNullException>(() => English().Strip(null!));
    }

    [Theory]
    [InlineData("sr-Latn")]
    [InlineData("fr-FR")]
    [InlineData("es-419")]
    [InlineData("zh-Hant-TW")]
    public void The_language_must_be_bare(string tag) {
        // Arrange
        IEnumerable<string> anyArticles = Any.SetOf(Any.StringMatching("[a-z]{2,6}")).WithCountBetween(1, 5).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ArticleRule(new LanguageTag(tag), anyArticles));

        // Assert
        Assert.Equal("language", refusal.ParamName);
    }

    [Fact]
    public void A_null_language_or_null_list_is_refused() {
        // Arrange
        LanguageTag anyLanguage = Any.LanguageTag().Bare().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ArticleRule(null!, ["the"]));
        Assert.Throws<ArgumentNullException>(() => new ArticleRule(anyLanguage, null!));
    }

    [Fact]
    public void A_rule_needs_at_least_one_article() {
        // Arrange
        LanguageTag anyLanguage = Any.LanguageTag().Bare().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ArticleRule(anyLanguage, []));
    }

    [Fact]
    public void Twenty_articles_are_accepted_and_twenty_one_are_refused() {
        // Arrange
        LanguageTag    anyLanguage = Any.LanguageTag().Bare().Generate();
        List<string>   twenty      = Any.SetOf(Any.StringMatching("[a-z]{3,8}")).WithCount(ArticleRule.MaxArticles).Generate().ToList();
        List<string>   twentyOne   = [.. twenty, "zzzzzzzzzz"];

        // Act
        ArticleRule accepted = new ArticleRule(anyLanguage, twenty);
        Exception?  refusal  = Record.Exception(() => new ArticleRule(anyLanguage, twentyOne));

        // Assert
        Assert.Equal(20, accepted.Articles.Count);
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_null_article_is_refused() {
        // Arrange
        LanguageTag anyLanguage = Any.LanguageTag().Bare().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ArticleRule(anyLanguage, ["the", null!]));
    }

    [Theory]
    [InlineData("the", "THE")]
    [InlineData("the", " the ")]
    [InlineData("l'", "L’")]
    [InlineData("é", "É")]
    public void Two_articles_equal_under_case_folding_are_refused(string first, string second) {
        // Arrange
        LanguageTag anyLanguage = Any.LanguageTag().Bare().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ArticleRule(anyLanguage, [first, second]));

        // Assert
        Assert.Contains("twice", refusal.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("'")]
    [InlineData("’")]
    [InlineData("l''")]
    [InlineData("l'a")]
    [InlineData("two words")]
    [InlineData("the1")]
    [InlineData("l-")]
    public void An_article_holds_letters_and_at_most_one_final_apostrophe(string article) {
        // Arrange
        LanguageTag anyLanguage = Any.LanguageTag().Bare().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ArticleRule(anyLanguage, [article]));
    }

    [Fact]
    public void An_article_of_twelve_clusters_is_accepted_and_thirteen_are_refused() {
        // Arrange
        LanguageTag anyLanguage = Any.LanguageTag().Bare().Generate();

        // Act
        ArticleRule accepted = new ArticleRule(anyLanguage, ["abcdefghijkl"]);
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new ArticleRule(anyLanguage, ["abcdefghijklm"]));

        // Assert
        Assert.Equal("abcdefghijkl", accepted.Articles.Single());
        Assert.Equal("articles", refusal.ParamName);
    }

    [Fact]
    public void An_unpaired_surrogate_in_an_article_is_refused() {
        // Arrange
        LanguageTag anyLanguage = Any.LanguageTag().Bare().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ArticleRule(anyLanguage, ["ab\uD800"]));
    }

    [Fact]
    public void The_articles_of_the_rule_cannot_be_changed_through_the_list_it_was_built_from() {
        // Arrange
        List<string> source = ["the", "a"];
        ArticleRule rule = new ArticleRule(new LanguageTag("en"), source);

        // Act
        source.Add("an");

        // Assert
        Assert.Equal(["the", "a"], rule.Articles);
    }

    [Fact]
    public void A_non_Latin_article_is_matched_ignoring_case() {
        // Arrange
        ArticleRule rule = new ArticleRule(new LanguageTag("el"), ["ο", "η", "το"]);

        // Assert
        Assert.Equal("Παιδί", rule.Strip("Το Παιδί"));
        Assert.Equal("Παιδί", rule.Strip("ΤΟ Παιδί"));
    }

}
