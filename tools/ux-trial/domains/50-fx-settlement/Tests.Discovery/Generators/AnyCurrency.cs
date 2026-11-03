// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Currency --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace FxSettlement.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Currency" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCurrency : IAny<Currency> {

    private IAny<string> _code;
    private IAny<int>    _minorUnits;

    private AnyPattern _codeRecipe;
    private AnyInt32  _minorUnitsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCurrency()
        : this(code:       AnyValidCode(),
               minorUnits: AnyValidMinorUnits()) { }

    private static AnyPattern AnyValidCode() {
        // Three upper-case letters, not starting with X. A code of the desk's ISO table takes its ISO minor units in Generate().
        return Any.StringMatching("[A-WYZ][A-Z]{2}");
    }

    private static AnyInt32 AnyValidMinorUnits() {
        // Only used for a code outside the ISO table; see Generate().
        return Any.Int32().Between(0, Currency.MaxMinorUnits);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCurrency(AnyPattern code,
                        AnyInt32   minorUnits) {
        _code             = code;
        _codeRecipe       = code;
        _minorUnits       = minorUnits;
        _minorUnitsRecipe = minorUnits;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCurrency(AnyCurrency from) {
        _code             = from._code;
        _codeRecipe       = from._codeRecipe;
        _minorUnits       = from._minorUnits;
        _minorUnitsRecipe = from._minorUnitsRecipe;
    }

    /// <summary>Pins <c>code</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCurrency WithCode(string value) {
        AnyCurrency copy = new AnyCurrency(this);

        copy._code = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>code</c> is drawn from — <c>AnyValidCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCurrency WithCode(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCurrency copy = new AnyCurrency(this);

        copy._codeRecipe = configure(_codeRecipe);
        copy._code       = copy._codeRecipe;

        return copy;
    }

    /// <summary>Pins <c>minorUnits</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCurrency WithMinorUnits(int value) {
        AnyCurrency copy = new AnyCurrency(this);

        copy._minorUnits = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>minorUnits</c> is drawn from — <c>AnyValidMinorUnits()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCurrency WithMinorUnits(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCurrency copy = new AnyCurrency(this);

        copy._minorUnitsRecipe = configure(_minorUnitsRecipe);
        copy._minorUnits       = copy._minorUnitsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Currency" />.</summary>
    public Currency Generate() {
        string code = _code.Generate();

        // A code of the ISO table must carry its ISO minor units; any other code takes the drawn ones.
        int minorUnits = IsoMinorUnitsOf(code) ?? _minorUnits.Generate();

        return new Currency(code, minorUnits);
    }

    private static int? IsoMinorUnitsOf(string code) {
        try {
            return Currency.Iso(code).MinorUnits;
        } catch (ArgumentException) {
            return null;
        }
    }

}
