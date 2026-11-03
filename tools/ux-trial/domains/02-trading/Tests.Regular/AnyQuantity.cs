// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Quantity --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Trading.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Quantity" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyQuantity : IAny<Quantity> {

    private IAny<int> _shares;

    private AnyInt32 _sharesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyQuantity()
        : this(shares: AnyValidShares()) { }

    private static AnyInt32 AnyValidShares() {
        return Any.Int32().Positive().LessThanOrEqualTo(Quantity.Maximum);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyQuantity(AnyInt32 shares) {
        _shares       = shares;
        _sharesRecipe = shares;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyQuantity(AnyQuantity from) {
        _shares       = from._shares;
        _sharesRecipe = from._sharesRecipe;
    }

    /// <summary>Pins <c>shares</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyQuantity WithShares(int value) {
        AnyQuantity copy = new AnyQuantity(this);

        copy._shares = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>shares</c> is drawn from — <c>AnyValidShares()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyQuantity WithShares(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyQuantity copy = new AnyQuantity(this);

        copy._sharesRecipe = configure(_sharesRecipe);
        copy._shares       = copy._sharesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Quantity" />.</summary>
    public Quantity Generate() {
        return new Quantity(_shares.Generate());
    }

}
