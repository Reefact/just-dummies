namespace JustDummies.PropertyTests;

/// <summary>
///     An independent, symbolic model of what an <c>Any.String()</c> chain declares, built from the documented
///     semantics of each call — the strings guide and the XML documentation of <see cref="AnyString" /> — and never
///     from <c>StringSpec</c>. It answers the two questions <see cref="StringModelProperties" /> puts to the runtime:
///     does the chain contradict itself, and if not, may this value be drawn?
/// </summary>
/// <remarks>
///     <para>
///         The model is worth exactly its independence. One that mirrored the implementation's reasoning could only
///         confirm the implementation agrees with itself, so every rule below cites the documented behaviour it
///         restates rather than the code that applies it.
///     </para>
///     <para>
///         It is bounded to what the library itself claims to detect (ADR-0046). Fragments are laid out side by side
///         and their lengths summed, because that is the documented layout — not because a true string search would
///         find no overlap. And an exclusion on a shaped string is met by a bounded redraw that answers at
///         <see cref="AnyString.Generate" /> (ADR-0012): whether such a redraw exhausts is a probabilistic question
///         this model does not ask, which is why the chains it judges carry exclusions only beside a value set.
///     </para>
/// </remarks>
internal sealed class StringModel {

    private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const string Digits  = "0123456789";

    #region Statics members declarations

    // "The universe is the whole of ASCII, and every constraint below narrows it."
    private static readonly char[] Ascii = Enumerable.Range(0, 128).Select(code => (char)code).ToArray();

    /// <summary>The documented membership test of each named character family, from the strings guide's table.</summary>
    public static bool IsInFamily(string family, char character) {
        return family switch {
            "Alpha"        => IsLetter(character),
            "Numeric"      => IsDigit(character),
            "AlphaNumeric" => IsLetter(character) || IsDigit(character),
            "Printable"    => IsPrintable(character),
            "NonPrintable" => character < 0x20 || character == 0x7F,
            "Punctuation"  => IsPrintable(character) && !IsLetter(character) && !IsDigit(character) && character != ' ',
            "Whitespaces"  => character is ' ' or '\t',
            "Hexadecimal"  => IsDigit(character) || character is >= 'A' and <= 'F' or >= 'a' and <= 'f',
            _              => throw new ArgumentOutOfRangeException(nameof(family), family, "Not a documented family.")
        };
    }

    private static bool IsLetter(char character) {
        return Letters.IndexOf(character) >= 0;
    }

    private static bool IsDigit(char character) {
        return Digits.IndexOf(character) >= 0;
    }

    private static bool IsPrintable(char character) {
        return character is >= (char)0x20 and <= (char)0x7E;
    }

    #endregion

    private readonly HashSet<string> _excluded  = new(StringComparer.Ordinal);
    private readonly List<string>    _fragments = [];

    private string[]? _allowed;
    private bool?     _casingUpper;
    private int?      _exact;
    private string?   _family;
    private string?   _familySlot;
    private int?      _maximum;
    private int       _minimum;
    private bool      _notBlank;
    private string?   _pool;
    private string?   _prefix;
    private string?   _suffix;
    private bool      _withoutAlpha;
    private bool      _withoutNumeric;

    // ----- The calls. Each answers whether the chain is still free of contradiction once it is applied. -----

    /// <summary>"Requires at least one character": a floor of one.</summary>
    public bool NonEmpty() {
        _minimum = Math.Max(_minimum, 1);

        return IsConsistent();
    }

    /// <summary>A non-whitespace character, "and with it the floor of one character NonEmpty gives".</summary>
    public bool NotBlank() {
        _notBlank = true;

        return NonEmpty();
    }

    /// <summary>"Fixes the exact length. Declared once per generator."</summary>
    public bool WithLength(int length) {
        if (_exact is int declared) { return declared == length && IsConsistent(); }

        _exact = length;

        return IsConsistent();
    }

    /// <summary>A floor; a looser one than in force narrows nothing.</summary>
    public bool WithMinLength(int length) {
        _minimum = Math.Max(_minimum, length);

        return IsConsistent();
    }

    /// <summary>A ceiling; a looser one than in force narrows nothing.</summary>
    public bool WithMaxLength(int length) {
        _maximum = _maximum is int declared ? Math.Min(declared, length) : length;

        return IsConsistent();
    }

    /// <summary>"Exactly its two bounds declared separately."</summary>
    public bool WithLengthBetween(int minimum, int maximum) {
        _minimum = Math.Max(_minimum, minimum);
        _maximum = _maximum is int declared ? Math.Min(declared, maximum) : maximum;

        return IsConsistent();
    }

    /// <summary>"Declared once per generator"; the same literal again asks for nothing new.</summary>
    public bool StartingWith(string prefix) {
        if (_prefix is not null) { return _prefix == prefix && IsConsistent(); }

        _prefix = prefix;

        return IsConsistent();
    }

    /// <summary>"Declared once per generator"; the same literal again asks for nothing new.</summary>
    public bool EndingWith(string suffix) {
        if (_suffix is not null) { return _suffix == suffix && IsConsistent(); }

        _suffix = suffix;

        return IsConsistent();
    }

    /// <summary>"May be declared several times"; "redeclaring the exact SAME value is a no-op".</summary>
    public bool Containing(string fragment) {
        if (!_fragments.Contains(fragment)) { _fragments.Add(fragment); }

        return IsConsistent();
    }

    /// <summary>A named family: "a family occupies one slot: declaring a second one contradicts the first".</summary>
    public bool Family(string family) {
        string call = family + "()";
        if (_familySlot is not null) { return _familySlot == call && IsConsistent(); }

        _familySlot = call;
        _family     = family;

        return IsConsistent();
    }

    /// <summary>
    ///     A custom pool: "it occupies the same character-family slot as the named sets, and because the pool is the
    ///     whole character definition it cannot combine with InLowerCase/InUpperCase".
    /// </summary>
    public bool WithChars(string pool) {
        string call = $"WithChars(\"{pool}\")";
        if (_familySlot is not null) { return _familySlot == call && IsConsistent(); }
        if (_casingUpper is not null) { return false; }

        _familySlot = call;
        _pool       = pool;

        return IsConsistent();
    }

    /// <summary>A casing, "declared once per generator", and never beside a custom pool.</summary>
    public bool Casing(bool upper) {
        if (_casingUpper is bool declared) { return declared == upper && IsConsistent(); }
        if (_pool is not null) { return false; }

        _casingUpper = upper;

        return IsConsistent();
    }

    /// <summary>A subtraction: "they subtract and accumulate".</summary>
    public bool WithoutAlpha() {
        _withoutAlpha = true;

        return IsConsistent();
    }

    /// <summary>A subtraction: "they subtract and accumulate".</summary>
    public bool WithoutNumeric() {
        _withoutNumeric = true;

        return IsConsistent();
    }

    /// <summary>
    ///     A value set, "declared once per generator": it "replaces the layout rather than shaping it", and every
    ///     other constraint narrows the set instead.
    /// </summary>
    public bool OneOf(IReadOnlyCollection<string> values) {
        string[] distinct = values.Distinct(StringComparer.Ordinal).ToArray();
        if (_allowed is not null) { return new HashSet<string>(_allowed, StringComparer.Ordinal).SetEquals(distinct) && IsConsistent(); }

        _allowed = distinct;

        return IsConsistent();
    }

    /// <summary>
    ///     Exclusions "accumulate". Beside a value set "the excluded values are removed from the set at once, and
    ///     removing all of them is a conflict here and now"; on a shaped string they wait for the bounded redraw.
    /// </summary>
    public bool Except(IEnumerable<string> values) {
        _excluded.UnionWith(values);

        return IsConsistent();
    }

    // ----- What the model concludes. -----

    /// <summary>
    ///     Whether the constraints declared so far contradict one another — the question the run time answers at every
    ///     call, so a chain is refused at the first call that makes it unsatisfiable.
    /// </summary>
    private bool IsConsistent() {
        if (_exact is int exact && (exact < _minimum || (_maximum is int capped && exact > capped))) { return false; }
        if (_maximum is int maximum && _minimum > maximum) { return false; }

        if (_allowed is not null) { return _allowed.Any(AdmitsSupplied); }

        // A shaped string must have a character to draw, and room for its literals laid side by side.
        if (!FillerAlphabet().Any()) { return false; }

        int required = Required();

        return !(_exact is int fixedAt && required > fixedAt) && !(_maximum is int ceiling && required > ceiling);
    }

    /// <summary>
    ///     Whether a finished, consistent chain can actually be drawn. The one check the run time defers to the draw:
    ///     <c>NotBlank()</c> needs a non-whitespace character, and on a shaped string where no literal already carries
    ///     one, the filler has to supply it — so it needs a position of its own and a character to put there. An
    ///     anchor declared after the pair settles it, which is why the question is asked of the finished chain only.
    /// </summary>
    public bool IsDrawable() {
        if (_allowed is not null || !_notBlank) { return true; }
        if (Literals().Any(literal => literal.Any(character => !char.IsWhiteSpace(character)))) { return true; }

        int ceiling = _exact ?? _maximum ?? int.MaxValue;

        return ceiling > Required() && FillerAlphabet().Any(character => !char.IsWhiteSpace(character));
    }

    /// <summary>Whether <paramref name="value" /> is one a finished, drawable chain may yield.</summary>
    public bool Accepts(string value) {
        if (_allowed is not null) { return _allowed.Contains(value, StringComparer.Ordinal) && AdmitsSupplied(value); }

        if (!HasDeclaredLength(value.Length) || value.Length < Required()) { return false; }
        if (_prefix is not null && !value.StartsWith(_prefix, StringComparison.Ordinal)) { return false; }
        if (_suffix is not null && !value.EndsWith(_suffix, StringComparison.Ordinal)) { return false; }
        if (_notBlank && string.IsNullOrWhiteSpace(value)) { return false; }

        return FitsTheLayout(value);
    }

    /// <summary>
    ///     A supplied value against every declared constraint: "on a value set nothing is drawn at all, so the
    ///     supplied values stay subject to them" (ADR-0079) — the characters included, and the literals matched
    ///     rather than laid out.
    /// </summary>
    private bool AdmitsSupplied(string value) {
        if (!HasDeclaredLength(value.Length)) { return false; }
        if (_prefix is not null && !value.StartsWith(_prefix, StringComparison.Ordinal)) { return false; }
        if (_suffix is not null && !value.EndsWith(_suffix, StringComparison.Ordinal)) { return false; }
        if (_fragments.Any(fragment => value.IndexOf(fragment, StringComparison.Ordinal) < 0)) { return false; }
        if (_notBlank && string.IsNullOrWhiteSpace(value)) { return false; }
        if (_excluded.Contains(value)) { return false; }

        return value.All(AdmitsCharacter);
    }

    private bool HasDeclaredLength(int length) {
        return length >= _minimum && (_exact is null || length == _exact) && (_maximum is null || length <= _maximum);
    }

    /// <summary>Whether a character satisfies the declared family or pool, casing and subtractions.</summary>
    private bool AdmitsCharacter(char character) {
        if (_pool is not null && _pool.IndexOf(character) < 0) { return false; }
        if (_family is not null && !IsInFamily(_family, character)) { return false; }
        if (_casingUpper == true && character is >= 'a' and <= 'z') { return false; }
        if (_casingUpper == false && character is >= 'A' and <= 'Z') { return false; }
        if (_withoutAlpha && IsLetter(character)) { return false; }

        return !(_withoutNumeric && IsDigit(character));
    }

    /// <summary>What the generator draws filler from: the universe, or the pool that replaces it, narrowed by every character constraint.</summary>
    private IEnumerable<char> FillerAlphabet() {
        return (_pool?.ToCharArray() ?? Ascii).Where(AdmitsCharacter);
    }

    /// <summary>"The length budget they need is the plain sum of their lengths."</summary>
    private int Required() {
        return Literals().Sum(literal => literal.Length);
    }

    private IEnumerable<string> Literals() {
        if (_prefix is not null) { yield return _prefix; }
        foreach (string fragment in _fragments) { yield return fragment; }
        if (_suffix is not null) { yield return _suffix; }
    }

    /// <summary>
    ///     Whether <paramref name="value" /> reads as the documented layout — "prefix + filler + contained values +
    ///     filler + suffix" — with every filler character drawn from the alphabet.
    /// </summary>
    /// <remarks>
    ///     "A character constraint governs the filler, not your literals": a character outside the alphabet may appear
    ///     only where a literal put it, and each literal is put there once. The same text appearing again elsewhere is
    ///     filler, and answers to the alphabet like any other — <c>WithLength(2).Containing("a").Numeric()</c> never
    ///     draws <c>"aa"</c>. The guide does not say in which order the contained values follow one another, so any
    ///     order is accepted.
    /// </remarks>
    private bool FitsTheLayout(string value) {
        HashSet<char> alphabet = new(FillerAlphabet());
        int           start    = _prefix?.Length ?? 0;
        string        middle   = value.Substring(start, value.Length - (_suffix?.Length ?? 0) - start);
        int           block    = _fragments.Sum(fragment => fragment.Length);

        for (int at = 0; at + block <= middle.Length; at++) {
            if (!middle.Take(at).All(alphabet.Contains)) { return false; }
            if (middle.Skip(at + block).All(alphabet.Contains) && IsLaidEndToEnd(middle.Substring(at, block), _fragments)) { return true; }
        }

        return false;
    }

    /// <summary>Whether <paramref name="text" /> is every one of <paramref name="fragments" /> laid end to end, in some order.</summary>
    private static bool IsLaidEndToEnd(string text, IReadOnlyList<string> fragments) {
        if (fragments.Count == 0) { return text.Length == 0; }

        // The contained values are distinct — an identical redeclaration asks for nothing new — so each is removed by value.
        return fragments.Where(fragment => text.StartsWith(fragment, StringComparison.Ordinal))
                        .Any(fragment => IsLaidEndToEnd(text.Substring(fragment.Length), fragments.Where(other => other != fragment).ToList()));
    }

}
