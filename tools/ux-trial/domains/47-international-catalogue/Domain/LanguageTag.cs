using System.Text.RegularExpressions;

namespace InternationalCatalogue.Domain;

/// <summary>
/// A language tag in a simple BCP 47 form: a 2–3 letter language, an optional 4-letter script and an
/// optional region (2 letters or 3 digits), e.g. <c>fr</c>, <c>zh-Hant</c>, <c>sr-Latn-RS</c>, <c>es-419</c>.
/// Accepted in any letter case and stored in canonical case (<c>sr-Latn-RS</c>).
/// </summary>
public sealed partial class LanguageTag : IEquatable<LanguageTag>
{
    public LanguageTag(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Match match = TagPattern().Match(value);
        if (!match.Success)
        {
            throw new FormatException($"'{value}' is not a language tag of the form language[-Script][-REGION].");
        }

        Language = match.Groups["language"].Value.ToLowerInvariant();
        Group script = match.Groups["script"];
        Script = script.Success ? char.ToUpperInvariant(script.Value[0]) + script.Value[1..].ToLowerInvariant() : null;
        Group region = match.Groups["region"];
        Region = region.Success ? region.Value.ToUpperInvariant() : null;
        Value = string.Join('-', new[] { Language, Script, Region }.Where(part => part is not null));
    }

    /// <summary>The canonical tag.</summary>
    public string Value { get; }

    /// <summary>The primary language subtag, lower case.</summary>
    public string Language { get; }

    /// <summary>The script subtag in title case, or <see langword="null"/>.</summary>
    public string? Script { get; }

    /// <summary>The region subtag in upper case, or <see langword="null"/>.</summary>
    public string? Region { get; }

    /// <summary>
    /// The catalogue script the script subtag designates; <see langword="null"/> when there is no script
    /// subtag or when it names a script the catalogue does not support.
    /// </summary>
    public WritingScript? DeclaredScript => Script switch
    {
        null => null,
        "Latn" => WritingScript.Latin,
        "Grek" => WritingScript.Greek,
        "Cyrl" => WritingScript.Cyrillic,
        "Arab" => WritingScript.Arabic,
        "Hebr" => WritingScript.Hebrew,
        "Deva" => WritingScript.Devanagari,
        "Hani" or "Hans" or "Hant" or "Jpan" or "Kore" or "Hira" or "Kana" or "Hang" => WritingScript.Cjk,
        _ => null,
    };

    [GeneratedRegex(@"^(?<language>[A-Za-z]{2,3})(?:-(?<script>[A-Za-z]{4}))?(?:-(?<region>[A-Za-z]{2}|[0-9]{3}))?\z")]
    private static partial Regex TagPattern();

    public bool Equals(LanguageTag? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as LanguageTag);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
