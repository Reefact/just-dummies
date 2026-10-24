#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     The FsCheck generator of <see cref="StringModelChain" />s: arity-bounded sequences of <c>Any.String()</c> calls
///     over a deliberately small vocabulary, so that the calls meet one another often — a length beside a literal, a
///     family beside a subtraction, a value set beside both.
/// </summary>
/// <remarks>
///     <para>
///         Chains stay within the arity cap of eight (ADR-0005), which keeps the space tractable without a bound of
///         the suite's own. Half of them carry one value set, placed anywhere in the chain, together with the
///         exclusions; the other half are shaped strings with none — an exclusion on a shaped string is answered by a
///         bounded redraw at the draw, which the model does not reason about (ADR-0012, ADR-0046).
///     </para>
///     <para>
///         The literals mix letters of both cases, a digit, a space, a hyphen and a non-ASCII letter, so that each
///         family, casing, subtraction and <c>NotBlank()</c> meets a literal it would refuse to draw — the exemption
///         ADR-0079 grants a literal is then exercised rather than assumed.
///     </para>
/// </remarks>
internal static class StringModelChains {

    private const int MaximumArity  = 8;
    private const int MaximumLength = 6;

    private static readonly string[] Literals = ["a", "ab", "AB", "1", " ", "x-", "é"];

    private static readonly string[] Pools = ["ab", "AB1", " a", "-_", "é", "\t "];

    private static readonly string[] Values = ["", "a", "ab", "AB", "a1", " ", "ab-", "1", "abc", "A b"];

    private static readonly string[] Families = ["Alpha", "Numeric", "AlphaNumeric", "Punctuation", "Printable", "NonPrintable", "Whitespaces", "Hexadecimal"];

    #region Statics members declarations

    /// <summary>The chains, with the shrinker that drops one call at a time.</summary>
    public static Arbitrary<StringModelChain> Arbitrary() {
        return Arb.From(Chain(), chain => chain.Shrink());
    }

    private static Gen<StringModelChain> Chain() {
        Gen<StringModelChain> shaped = from count in Gen.Choose(1, MaximumArity)
                                       from commands in Shape().ListOf(count)
                                       select new StringModelChain(commands.ToList());

        Gen<StringModelChain> pooled = from count in Gen.Choose(0, MaximumArity - 2)
                                       from commands in Gen.Frequency((3, Shape()), (1, Exclusion())).ListOf(count)
                                       from supplied in Supplied()
                                       from redeclared in Redeclaration(supplied)
                                       from position in Gen.Choose(0, count)
                                       from later in Gen.Choose(position, count)
                                       select new StringModelChain(Insert(Insert(commands.ToList(), position, OneOf(supplied)), later + 1, redeclared));

        return Gen.OneOf(shaped, pooled);
    }

    private static Gen<StringModelCommand> Shape() {
        Gen<int> length = Gen.Choose(0, MaximumLength);
        Gen<string> literal = Gen.Elements(Literals);

        return Gen.Frequency(
            (2, length.Select(n => new StringModelCommand($"WithLength({n})", g => g.WithLength(n), m => m.WithLength(n)))),
            (2, length.Select(n => new StringModelCommand($"WithMinLength({n})", g => g.WithMinLength(n), m => m.WithMinLength(n)))),
            (2, length.Select(n => new StringModelCommand($"WithMaxLength({n})", g => g.WithMaxLength(n), m => m.WithMaxLength(n)))),
            (2, Generators.OrderedPair(length).Select(b => new StringModelCommand($"WithLengthBetween({b.Min}, {b.Max})", g => g.WithLengthBetween(b.Min, b.Max), m => m.WithLengthBetween(b.Min, b.Max)))),
            (1, Gen.Constant(new StringModelCommand("NonEmpty()", g => g.NonEmpty(), m => m.NonEmpty()))),
            (2, Gen.Constant(new StringModelCommand("NotBlank()", g => g.NotBlank(), m => m.NotBlank()))),
            (2, literal.Select(s => new StringModelCommand($"StartingWith({Quote(s)})", g => g.StartingWith(s), m => m.StartingWith(s)))),
            (2, literal.Select(s => new StringModelCommand($"EndingWith({Quote(s)})", g => g.EndingWith(s), m => m.EndingWith(s)))),
            (2, literal.Select(s => new StringModelCommand($"Containing({Quote(s)})", g => g.Containing(s), m => m.Containing(s)))),
            (2, Gen.Elements(Families).Select(Family)),
            (1, Gen.Elements(Pools).Select(p => new StringModelCommand($"WithChars({Quote(p)})", g => g.WithChars(p), m => m.WithChars(p)))),
            (1, Gen.Constant(new StringModelCommand("WithoutAlpha()", g => g.WithoutAlpha(), m => m.WithoutAlpha()))),
            (1, Gen.Constant(new StringModelCommand("WithoutNumeric()", g => g.WithoutNumeric(), m => m.WithoutNumeric()))),
            (1, Gen.Constant(new StringModelCommand("InUpperCase()", g => g.InUpperCase(), m => m.Casing(upper: true)))),
            (1, Gen.Constant(new StringModelCommand("InLowerCase()", g => g.InLowerCase(), m => m.Casing(upper: false)))));
    }

    private static Gen<string[]> Supplied() {
        return Gen.SubListOf(Values).Where(values => values.Count > 0).Select(values => values.ToArray());
    }

    /// <summary>
    ///     Most chains declare their value set once. Some declare it again — the same set reordered and with a
    ///     duplicate, which "asks for exactly what the first already guarantees", or another set, which contradicts it.
    /// </summary>
    private static Gen<StringModelCommand?> Redeclaration(string[] supplied) {
        return Gen.Frequency((4, Gen.Constant<StringModelCommand?>(null)),
                             (1, Gen.Constant<StringModelCommand?>(OneOf(Enumerable.Reverse(supplied).Append(supplied[0]).ToArray()))),
                             (1, Supplied().Select(other => (StringModelCommand?)OneOf(other))));
    }

    private static StringModelCommand OneOf(string[] supplied) {
        return new StringModelCommand($"OneOf({Join(supplied)})", g => g.OneOf(supplied), m => m.OneOf(supplied), StringModelCommandKind.ValueSet);
    }

    private static List<StringModelCommand> Insert(List<StringModelCommand> commands, int position, StringModelCommand? command) {
        if (command is not null) { commands.Insert(position, command); }

        return commands;
    }

    private static Gen<StringModelCommand> Exclusion() {
        Gen<StringModelCommand> differentFrom = Gen.Elements(Values)
                                                   .Select(v => new StringModelCommand($"DifferentFrom({Quote(v)})", g => g.DifferentFrom(v), m => m.Except([v]), StringModelCommandKind.Exclusion));
        Gen<StringModelCommand> except = Gen.SubListOf(Values)
                                            .Where(values => values.Count is > 0 and <= 3)
                                            .Select(values => {
                                                string[] excluded = values.ToArray();

                                                return new StringModelCommand($"Except({Join(excluded)})", g => g.Except(excluded), m => m.Except(excluded), StringModelCommandKind.Exclusion);
                                            });

        return Gen.OneOf(differentFrom, except);
    }

    private static StringModelCommand Family(string family) {
        Func<AnyString, AnyString> apply = family switch {
            "Alpha"        => g => g.Alpha(),
            "Numeric"      => g => g.Numeric(),
            "AlphaNumeric" => g => g.AlphaNumeric(),
            "Punctuation"  => g => g.Punctuation(),
            "Printable"    => g => g.Printable(),
            "NonPrintable" => g => g.NonPrintable(),
            "Whitespaces"  => g => g.Whitespaces(),
            "Hexadecimal"  => g => g.Hexadecimal(),
            _              => throw new ArgumentOutOfRangeException(nameof(family), family, "Not a documented family.")
        };

        return new StringModelCommand($"{family}()", apply, m => m.Family(family));
    }

    private static string Quote(string value) {
        return "\"" + value.Replace("\t", "\\t") + "\"";
    }

    private static string Join(IEnumerable<string> values) {
        return string.Join(", ", values.Select(Quote));
    }

    #endregion

}
