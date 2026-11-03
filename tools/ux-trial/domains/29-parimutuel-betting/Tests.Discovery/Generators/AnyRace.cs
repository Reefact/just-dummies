// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Race --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Race" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRace : IAny<Race> {

    private IAny<string> _code;
    private IAny<int>    _fieldSize;

    private AnyPattern _codeRecipe;
    private AnyInt32  _fieldSizeRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRace()
        : this(code:      AnyValidCode(),
               fieldSize: AnyValidFieldSize()) { }

    private static AnyPattern AnyValidCode() {
        return Any.StringMatching("[A-Z]{3}-R([1-9]|1[0-6])");
    }

    private static AnyInt32 AnyValidFieldSize() {
        return Any.Int32().Between(Race.MinimumFieldSize, HorseNumber.Maximum);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRace(AnyPattern code,
                    AnyInt32  fieldSize) {
        _code            = code;
        _codeRecipe      = code;
        _fieldSize       = fieldSize;
        _fieldSizeRecipe = fieldSize;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRace(AnyRace from) {
        _code            = from._code;
        _codeRecipe      = from._codeRecipe;
        _fieldSize       = from._fieldSize;
        _fieldSizeRecipe = from._fieldSizeRecipe;
    }

    /// <summary>Pins <c>code</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRace WithCode(string value) {
        AnyRace copy = new AnyRace(this);

        copy._code = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>code</c> is drawn from — <c>AnyValidCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRace WithCode(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRace copy = new AnyRace(this);

        copy._codeRecipe = configure(_codeRecipe);
        copy._code       = copy._codeRecipe;

        return copy;
    }

    /// <summary>Pins <c>fieldSize</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRace WithFieldSize(int value) {
        AnyRace copy = new AnyRace(this);

        copy._fieldSize = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>fieldSize</c> is drawn from — <c>AnyValidFieldSize()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRace WithFieldSize(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRace copy = new AnyRace(this);

        copy._fieldSizeRecipe = configure(_fieldSizeRecipe);
        copy._fieldSize       = copy._fieldSizeRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Race" />.</summary>
    public Race Generate() {
        return new Race(_code.Generate(),
                        _fieldSize.Generate());
    }

}
