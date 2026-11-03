// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TokenAmount --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Numerics;

using JustDummies;

namespace TokenLedger.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TokenAmount" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTokenAmount : IAny<TokenAmount> {

    private IAny<BigInteger> _baseUnits;

    private AnyBigInteger _baseUnitsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTokenAmount()
        : this(baseUnits: AnyValidBaseUnits()) { }

    private static AnyBigInteger AnyValidBaseUnits() {
        return new AnyBigInteger().Between(0, TokenAmount.MaxBaseUnits);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTokenAmount(AnyBigInteger baseUnits) {
        _baseUnits       = baseUnits;
        _baseUnitsRecipe = baseUnits;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTokenAmount(AnyTokenAmount from) {
        _baseUnits       = from._baseUnits;
        _baseUnitsRecipe = from._baseUnitsRecipe;
    }

    /// <summary>Pins <c>baseUnits</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTokenAmount WithBaseUnits(BigInteger value) {
        AnyTokenAmount copy = new AnyTokenAmount(this);

        copy._baseUnits = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>baseUnits</c> is drawn from — <c>AnyValidBaseUnits()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBigInteger, AnyBigInteger&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTokenAmount WithBaseUnits(Func<AnyBigInteger, AnyBigInteger> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTokenAmount copy = new AnyTokenAmount(this);

        copy._baseUnitsRecipe = configure(_baseUnitsRecipe);
        copy._baseUnits       = copy._baseUnitsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TokenAmount" />.</summary>
    public TokenAmount Generate() {
        return new TokenAmount(_baseUnits.Generate());
    }

}
