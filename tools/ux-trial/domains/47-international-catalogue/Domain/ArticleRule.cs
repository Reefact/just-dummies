using System.Globalization;
using System.Text;

namespace InternationalCatalogue.Domain;

/// <summary>
/// The leading articles a language skips when a title is filed ("The", "Le", "L'", "Der"…). A plain article
/// is skipped when a space follows it; an elided article ends with an apostrophe (' or ’) and is skipped
/// directly. Matching ignores case (simple case folding) and treats ' and ’ alike.
/// </summary>
public sealed class ArticleRule
{
    public const int MaxArticles = 20;
    public const int MaxArticleLength = 12;

    private readonly List<string> _articles;

    public ArticleRule(LanguageTag language, IEnumerable<string> articles)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(articles);
        if (language.Script is not null || language.Region is not null)
        {
            throw new ArgumentException($"Article rules apply to a bare language such as '{language.Language}', not '{language}'.", nameof(language));
        }

        List<string> list = [];
        HashSet<string> keys = [];
        foreach (string? raw in articles)
        {
            if (raw is null)
            {
                throw new ArgumentException("An article cannot be null.", nameof(articles));
            }

            string article = UnicodeText.ToNfc(raw, nameof(articles)).Trim().Replace('’', '\'');
            CheckArticle(article, nameof(articles));
            if (!keys.Add(UnicodeText.MatchKey(article)))
            {
                throw new ArgumentException($"Article '{article}' is listed twice.", nameof(articles));
            }

            list.Add(article);
        }

        if (list.Count == 0 || list.Count > MaxArticles)
        {
            throw new ArgumentException($"A rule lists between 1 and {MaxArticles} articles, not {list.Count}.", nameof(articles));
        }

        Language = language;
        _articles = list.OrderByDescending(a => a.Length).ToList();
    }

    public LanguageTag Language { get; }

    /// <summary>The articles, longest first, apostrophes written '.</summary>
    public IReadOnlyList<string> Articles => _articles.AsReadOnly();

    /// <summary>The title without its leading article; a title made of an article alone is kept whole.</summary>
    public string Strip(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        string comparable = title.Replace('’', '\'');
        foreach (string article in _articles)
        {
            if (!comparable.StartsWith(article, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string rest;
            if (article.EndsWith('\''))
            {
                rest = title[article.Length..];
            }
            else if (title.Length > article.Length && title[article.Length] == ' ')
            {
                rest = title[(article.Length + 1)..];
            }
            else
            {
                continue;
            }

            rest = rest.TrimStart();
            if (rest.Length > 0)
            {
                return rest;
            }
        }

        return title;
    }

    private static void CheckArticle(string article, string paramName)
    {
        string letters = article.EndsWith('\'') ? article[..^1] : article;
        if (letters.Length == 0)
        {
            throw new ArgumentException("An article must contain letters.", paramName);
        }

        foreach (Rune rune in letters.EnumerateRunes())
        {
            if (!Rune.IsLetter(rune) && Rune.GetUnicodeCategory(rune) is not (UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark))
            {
                throw new ArgumentException($"Article '{article}' may only hold letters, optionally followed by one apostrophe.", paramName);
            }
        }

        int length = UnicodeText.GraphemeCount(article);
        if (length > MaxArticleLength)
        {
            throw new ArgumentOutOfRangeException(paramName, length, $"An article has at most {MaxArticleLength} grapheme clusters.");
        }
    }
}
