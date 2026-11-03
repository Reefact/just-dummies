using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>
///     Hand-written: any of a few cultures, and article rules for distinct languages (a collation refuses
///     two rules for one language, which independent draws of <c>AnyArticleRule</c> would produce).
/// </summary>
public sealed partial class AnyListingCollation : IAny<ListingCollation> {

    private static readonly string[] CultureNames = ["en-US", "fr-FR", "de-DE", "sv-SE", "es-ES", "ja-JP", "ru-RU", "he-IL"];

    private readonly CultureInfo?                _culture;
    private readonly IEnumerable<ArticleRule>?   _rules;

    public AnyListingCollation()
        : this(null, null) { }

    private AnyListingCollation(CultureInfo? culture, IEnumerable<ArticleRule>? rules) {
        _culture = culture;
        _rules   = rules;
    }

    public AnyListingCollation WithCulture(CultureInfo culture) {
        return new AnyListingCollation(culture, _rules);
    }

    public AnyListingCollation WithArticleRules(params ArticleRule[] rules) {
        return new AnyListingCollation(_culture, rules);
    }

    public ListingCollation Generate() {
        CultureInfo culture = _culture ?? new CultureInfo(Any.ElementOf(CultureNames).Generate());
        IEnumerable<ArticleRule> rules = _rules
                                         ?? Any.SetOf(Any.StringMatching("[a-z]{2,3}")).WithCountBetween(0, 3).Generate()
                                               .Select(language => new AnyArticleRule().InLanguage(language).Generate())
                                               .ToList();

        return new ListingCollation(culture, rules);
    }

}
