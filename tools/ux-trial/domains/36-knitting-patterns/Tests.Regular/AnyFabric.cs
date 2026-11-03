// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Fabric --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Fabric" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyFabric : IAny<Fabric> {

    private IAny<int> _castOnCount;

    private AnyInt32 _castOnCountRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyFabric()
        : this(castOnCount: AnyValidCastOnCount()) { }

    private static AnyInt32 AnyValidCastOnCount() {
        return Any.Int32().Positive().LessThanOrEqualTo(KnittingPattern.MaxCastOn);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyFabric(AnyInt32 castOnCount) {
        _castOnCount       = castOnCount;
        _castOnCountRecipe = castOnCount;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyFabric(AnyFabric from) {
        _castOnCount       = from._castOnCount;
        _castOnCountRecipe = from._castOnCountRecipe;
    }

    /// <summary>Pins <c>castOnCount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFabric WithCastOnCount(int value) {
        AnyFabric copy = new AnyFabric(this);

        copy._castOnCount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>castOnCount</c> is drawn from — <c>AnyValidCastOnCount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFabric WithCastOnCount(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFabric copy = new AnyFabric(this);

        copy._castOnCountRecipe = configure(_castOnCountRecipe);
        copy._castOnCount       = copy._castOnCountRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Fabric" />.</summary>
    public Fabric Generate() {
        return new Fabric(_castOnCount.Generate());
    }

}
