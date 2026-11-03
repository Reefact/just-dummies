using System;
using System.Linq;

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>Hand-written: a bare language and one to five distinct articles, some of them elided (<c>l'</c>).</summary>
public sealed partial class AnyArticleRule : IAny<ArticleRule> {

    private readonly string? _language;

    public AnyArticleRule()
        : this(null) { }

    private AnyArticleRule(string? language) {
        _language = language;
    }

    public AnyArticleRule InLanguage(string language) {
        return new AnyArticleRule(language);
    }

    public ArticleRule Generate() {
        LanguageTag language = _language is null ? new AnyLanguageTag().Bare().Generate() : new LanguageTag(_language);

        return new ArticleRule(language, Any.SetOf(Any.StringMatching("[a-z]{1,6}'?")).WithCountBetween(1, 5).Generate());
    }

}
