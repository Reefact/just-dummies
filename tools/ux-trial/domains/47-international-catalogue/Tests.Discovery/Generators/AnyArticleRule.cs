// Scaffolded by `any`, then completed by hand: the language must be bare (no script, no region) and the
// articles distinct under case folding, which the scaffolder reads as `to verify`.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace InternationalCatalogue.Domain;

public sealed partial class AnyArticleRule : IAny<ArticleRule> {

    private IAny<LanguageTag>         _language;
    private IAny<IEnumerable<string>> _articles;

    public AnyArticleRule()
        : this(language: Any.StringMatching("[a-z]{2,3}").As(tag => new LanguageTag(tag)),
               articles: Any.SetOf(Any.StringMatching("[a-z]{1,8}'?")).WithCountBetween(1, ArticleRule.MaxArticles).As(set => (IEnumerable<string>)set)) { }

    private AnyArticleRule(IAny<LanguageTag> language, IAny<IEnumerable<string>> articles) {
        _language = language;
        _articles = articles;
    }

    private AnyArticleRule(AnyArticleRule from) {
        _language = from._language;
        _articles = from._articles;
    }

    public AnyArticleRule WithLanguage(LanguageTag language) {
        AnyArticleRule copy = new AnyArticleRule(this);

        copy._language = Any.Fixed(language);

        return copy;
    }

    public AnyArticleRule WithArticles(params string[] articles) {
        AnyArticleRule copy = new AnyArticleRule(this);

        copy._articles = Any.Fixed<IEnumerable<string>>(articles);

        return copy;
    }

    public ArticleRule Generate() {
        return new ArticleRule(_language.Generate(), _articles.Generate());
    }

}
