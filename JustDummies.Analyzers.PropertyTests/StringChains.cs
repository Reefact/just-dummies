using FsCheck;
using FsCheck.Fluent;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     The FsCheck generator of <see cref="StringChain" />s: arity-bounded sequences of <c>Any.String()</c> calls with
///     constant arguments, drawn from a small vocabulary so the calls meet one another often.
/// </summary>
/// <remarks>
///     <para>
///         Every argument is a constant and the chain is one expression. <see cref="Arbitrary" /> holds the chains to
///         JD015's stated scope: each once-per-generator slot holds one call, or that call repeated. A second, different
///         family, prefix, length, casing or value set is a conflict of declaration, which is JD034's to report (issue
///         #274), and <see cref="Unscoped" /> lets it through. An exclusion appears only beside a value set: on a
///         shaped string it is met by a bounded redraw at the draw (ADR-0012), which no build-time rule judges.
///     </para>
///     <para>
///         Chains stay within the arity cap of eight (ADR-0005). The generator is this suite's own, independent of
///         the model-based one in <c>JustDummies.PropertyTests</c>: the two judge against different things — a runtime
///         oracle here, a symbolic model there — and neither should inherit the other's blind spots.
///     </para>
/// </remarks>
internal static class StringChains {

    private const int MaximumArity  = 8;
    private const int MaximumLength = 6;

    private static readonly string[] Literals = ["a", "ab", "AB", "1", " ", "x-", "é"];

    private static readonly string[] Pools = ["ab", "AB1", " a", "-_", "é", "\t "];

    private static readonly string[] Values = ["", "a", "ab", "AB", "a1", " ", "ab-", "1", "abc", "A b"];

    private static readonly string[] Families = ["Alpha", "Numeric", "AlphaNumeric", "Punctuation", "Printable", "NonPrintable", "Whitespaces", "Hexadecimal"];

    #region Statics members declarations

    /// <summary>The chains, with the shrinker that drops one call at a time.</summary>
    public static Arbitrary<StringChain> Arbitrary() {
        return Arb.From(Chain(OneCallPerSlot), chain => chain.Shrink());
    }

    /// <summary>
    ///     The same chains with the slot filter off, the conflicting declarations included — the ones JD034 reports and
    ///     JD015 leaves to it.
    /// </summary>
    public static Arbitrary<StringChain> Unscoped() {
        return Arb.From(Chain(calls => new StringChain(calls.ToList())), chain => chain.Shrink());
    }

    private static Gen<StringChain> Chain(Func<IEnumerable<StringChainCall>, StringChain> scope) {
        Gen<StringChain> shaped = from count in Gen.Choose(1, MaximumArity)
                                  from drawn in Shape().ListOf(count)
                                  from calls in Repeating(drawn, MaximumArity)
                                  select scope(calls);

        Gen<StringChain> pooled = from count in Gen.Choose(0, MaximumArity - 1)
                                  from drawn in Gen.Frequency((3, Shape()), (1, Exclusion())).ListOf(count)
                                  from calls in Repeating(drawn, MaximumArity - 1)
                                  from values in ValueSetValues()
                                  from position in Gen.Choose(0, calls.Count)
                                  from placed in Redeclaring(calls.Take(position).Append(OneOf(values)).Concat(calls.Skip(position)).ToList(), position, values)
                                  select scope(placed);

        return Gen.OneOf(shaped, pooled);
    }

    /// <summary>
    ///     Keeps the first call of each once-per-generator slot and drops a later, different one, and keeps a casing and
    ///     a <c>WithChars</c> pool apart — the pool "cannot combine" with a casing, another declaration conflict outside
    ///     the rule's scope. The same call repeated stays: the run time ignores the repetition, so it is in scope, and a
    ///     rule that miscounted it as a second declaration would stand down where it should not.
    /// </summary>
    private static StringChain OneCallPerSlot(IEnumerable<StringChainCall> calls) {
        Dictionary<StringChainSlot, string> taken = [];
        bool                                pool  = false;
        List<StringChainCall>               kept  = [];
        foreach (StringChainCall call in calls) {
            bool isPool = call.Source.StartsWith("WithChars(", StringComparison.Ordinal);
            if (call.Slot is StringChainSlot.None or StringChainSlot.Exclusion) { kept.Add(call); continue; }
            if (taken.TryGetValue(call.Slot, out string? holder)) {
                if (holder == call.Source) { kept.Add(call); }

                continue;
            }
            if ((isPool && taken.ContainsKey(StringChainSlot.Casing)) || (call.Slot == StringChainSlot.Casing && pool)) { continue; }

            taken.Add(call.Slot, call.Source);
            pool |= isPool;
            kept.Add(call);
        }

        return new StringChain(kept);
    }

    /// <summary>
    ///     The calls, one of them sometimes declared again later in the chain, within <paramref name="room" /> calls.
    /// </summary>
    /// <remarks>
    ///     The run time ignores an identical redeclaration, so a chain carrying one is in scope — and a rule that read
    ///     it as a second declaration would stand down where it should not. Drawn from the vocabulary alone, the same
    ///     call rarely meets itself, and the one that matters next to its subtraction almost never does.
    /// </remarks>
    private static Gen<List<StringChainCall>> Repeating(List<StringChainCall> calls, int room) {
        if (calls.Count == 0 || calls.Count >= room) { return Gen.Constant(calls); }

        Gen<List<StringChainCall>> repeated = from original in Gen.Choose(0, calls.Count - 1)
                                              from position in Gen.Choose(original + 1, calls.Count)
                                              select calls.Take(position).Append(calls[original]).Concat(calls.Skip(position)).ToList();

        return Gen.Frequency((2, Gen.Constant(calls)), (1, repeated));
    }

    private static Gen<StringChainCall> Shape() {
        Gen<int>    length  = Gen.Choose(0, MaximumLength);
        Gen<string> literal = Gen.Elements(Literals);

        return Gen.Frequency(
            (2, length.Select(n => Call($"WithLength({n})", g => g.WithLength(n), StringChainSlot.ExactLength))),
            (2, length.Select(n => Call($"WithMinLength({n})", g => g.WithMinLength(n)))),
            (2, length.Select(n => Call($"WithMaxLength({n})", g => g.WithMaxLength(n)))),
            (2, from low in length from high in length where low <= high select Call($"WithLengthBetween({low}, {high})", g => g.WithLengthBetween(low, high))),
            (1, Gen.Constant(Call("NonEmpty()", g => g.NonEmpty()))),
            (2, Gen.Constant(Call("NotBlank()", g => g.NotBlank()))),
            (2, literal.Select(s => Call($"StartingWith({CSharp(s)})", g => g.StartingWith(s), StringChainSlot.Prefix))),
            (2, literal.Select(s => Call($"EndingWith({CSharp(s)})", g => g.EndingWith(s), StringChainSlot.Suffix))),
            (2, literal.Select(s => Call($"Containing({CSharp(s)})", g => g.Containing(s)))),
            (2, Gen.Elements(Families).Select(Family)),
            (1, Gen.Elements(Pools).Select(p => Call($"WithChars({CSharp(p)})", g => g.WithChars(p), StringChainSlot.Family))),
            (1, Gen.Constant(Call("WithoutAlpha()", g => g.WithoutAlpha()))),
            (1, Gen.Constant(Call("WithoutNumeric()", g => g.WithoutNumeric()))),
            (1, Gen.Constant(Call("InUpperCase()", g => g.InUpperCase(), StringChainSlot.Casing))),
            (1, Gen.Constant(Call("InLowerCase()", g => g.InLowerCase(), StringChainSlot.Casing))));
    }

    private static Gen<string[]> ValueSetValues() {
        return Gen.SubListOf(Values).Where(values => values.Count > 0).Select(values => values.ToArray());
    }

    private static StringChainCall OneOf(string[] supplied) {
        return Call($"OneOf({Join(supplied)})", g => g.OneOf(supplied), StringChainSlot.ValueSet);
    }

    /// <summary>
    ///     The chain, sometimes with a second value set declared after the first, within the arity cap: the same values
    ///     reordered and repeated, which the run time ignores (ADR-0100), or another set, which it refuses.
    /// </summary>
    private static Gen<List<StringChainCall>> Redeclaring(List<StringChainCall> calls, int first, string[] values) {
        if (calls.Count >= MaximumArity) { return Gen.Constant(calls); }

        string[]                   same       = [.. Enumerable.Reverse(values), values[0]];
        Gen<StringChainCall>       second     = Gen.OneOf(Gen.Constant(OneOf(same)), ValueSetValues().Select(OneOf));
        Gen<List<StringChainCall>> redeclared = from call in second
                                                from position in Gen.Choose(first + 1, calls.Count)
                                                select calls.Take(position).Append(call).Concat(calls.Skip(position)).ToList();

        return Gen.Frequency((3, Gen.Constant(calls)), (1, redeclared));
    }

    private static Gen<StringChainCall> Exclusion() {
        Gen<StringChainCall> differentFrom = Gen.Elements(Values).Select(v => Call($"DifferentFrom({CSharp(v)})", g => g.DifferentFrom(v), StringChainSlot.Exclusion));
        Gen<StringChainCall> except = Gen.SubListOf(Values)
                                         .Where(values => values.Count is > 0 and <= 3)
                                         .Select(values => {
                                             string[] excluded = values.ToArray();

                                             return Call($"Except({Join(excluded)})", g => g.Except(excluded), StringChainSlot.Exclusion);
                                         });

        return Gen.OneOf(differentFrom, except);
    }

    private static StringChainCall Family(string family) {
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

        return Call($"{family}()", apply, StringChainSlot.Family);
    }

    private static StringChainCall Call(string source, Func<AnyString, AnyString> apply, StringChainSlot slot = StringChainSlot.None) {
        return new StringChainCall(source, apply, slot);
    }

    /// <summary>A string as a C# literal.</summary>
    private static string CSharp(string value) {
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\t", "\\t") + "\"";
    }

    private static string Join(IEnumerable<string> values) {
        return string.Join(", ", values.Select(CSharp));
    }

    #endregion

}
