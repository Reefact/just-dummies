using System.Globalization;

namespace InternationalCatalogue.Domain;

/// <summary>
/// How a listing is sorted: a culture's collation (its <see cref="CompareInfo"/>, ignoring case) applied to
/// each record's filing title — the title, bidirectional controls removed, without the leading article that
/// the rule for the record's language names. Ties fall to the subtitle, the year, the edition, then the id.
/// </summary>
public sealed class ListingCollation : IComparer<BibliographicRecord>
{
    private const CompareOptions Options = CompareOptions.IgnoreCase;

    private readonly Dictionary<string, ArticleRule> _rules;

    public ListingCollation(CultureInfo culture, IEnumerable<ArticleRule> articleRules)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(articleRules);
        Dictionary<string, ArticleRule> rules = new(StringComparer.Ordinal);
        foreach (ArticleRule? rule in articleRules)
        {
            if (rule is null)
            {
                throw new ArgumentException("An article rule cannot be null.", nameof(articleRules));
            }

            if (!rules.TryAdd(rule.Language.Language, rule))
            {
                throw new ArgumentException($"Two article rules are given for language '{rule.Language}'.", nameof(articleRules));
            }
        }

        Culture = culture;
        _rules = rules;
    }

    public CultureInfo Culture { get; }

    public IReadOnlyCollection<ArticleRule> ArticleRules => _rules.Values;

    /// <summary>The text the record files under: the rule of its primary language strips the leading article.</summary>
    public string FilingTitle(BibliographicRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        string title = UnicodeText.WithoutBidiControls(record.Title.Value);
        return _rules.TryGetValue(record.Language.Language, out ArticleRule? rule) ? rule.Strip(title) : title;
    }

    public SortKey SortKeyOf(BibliographicRecord record) => Culture.CompareInfo.GetSortKey(FilingTitle(record), Options);

    public int Compare(BibliographicRecord? x, BibliographicRecord? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        CompareInfo compare = Culture.CompareInfo;
        int byTitle = compare.Compare(FilingTitle(x), FilingTitle(y), Options);
        if (byTitle != 0)
        {
            return byTitle;
        }

        int bySubtitle = (x.Subtitle, y.Subtitle) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            _ => compare.Compare(UnicodeText.WithoutBidiControls(x.Subtitle.Value), UnicodeText.WithoutBidiControls(y.Subtitle.Value), Options),
        };
        if (bySubtitle != 0)
        {
            return bySubtitle;
        }

        int byYear = x.PublicationYear.CompareTo(y.PublicationYear);
        if (byYear != 0)
        {
            return byYear;
        }

        int byEdition = x.Edition.CompareTo(y.Edition);
        return byEdition != 0 ? byEdition : string.CompareOrdinal(x.Id.Value, y.Id.Value);
    }
}
