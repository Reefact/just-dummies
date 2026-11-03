using System;
using System.Collections.Generic;

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ArticleRule" /> values: a bare language and one to five distinct
///     lower-case articles, some of them elided (ending with an apostrophe).
/// </summary>
public sealed partial class AnyArticleRule : IAny<ArticleRule> {

    private IAny<LanguageTag>         _language;
    private IAny<IEnumerable<string>> _articles;

    public AnyArticleRule()
        : this(language: Any.StringMatching("[a-z]{2,3}").As(tag => new LanguageTag(tag)),
               articles: Any.SetOf(Any.StringMatching("[a-z]{1,8}'?")).WithCountBetween(1, 5).As(set => (IEnumerable<string>)set)) { }

    private AnyArticleRule(IAny<LanguageTag> language, IAny<IEnumerable<string>> articles) {
        _language = language;
        _articles = articles;
    }

    private AnyArticleRule(AnyArticleRule from)
        : this(from._language, from._articles) { }

    public AnyArticleRule WithLanguage(LanguageTag value) {
        AnyArticleRule copy = new AnyArticleRule(this);

        copy._language = Any.Fixed(value);

        return copy;
    }

    public AnyArticleRule WithArticles(IEnumerable<string> value) {
        AnyArticleRule copy = new AnyArticleRule(this);

        copy._articles = Any.Fixed(value);

        return copy;
    }

    public ArticleRule Generate() {
        return new ArticleRule(_language.Generate(), _articles.Generate());
    }

}
