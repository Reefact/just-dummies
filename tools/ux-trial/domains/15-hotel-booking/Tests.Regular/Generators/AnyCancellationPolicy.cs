// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CancellationPolicy --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Booking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CancellationPolicy" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCancellationPolicy : IAny<CancellationPolicy> {

    private IAny<int>     _freeCancellationDays;
    private IAny<decimal> _penaltyRate;

    private AnyInt32   _freeCancellationDaysRecipe;
    private AnyDecimal _penaltyRateRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCancellationPolicy()
        : this(freeCancellationDays: AnyValidFreeCancellationDays(),
               penaltyRate:          AnyValidPenaltyRate()) { }

    private static AnyInt32 AnyValidFreeCancellationDays() {
        return Any.Int32().Between(0, 365);
    }

    private static AnyDecimal AnyValidPenaltyRate() {
        return Any.Decimal().Between(0m, 1m);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCancellationPolicy(AnyInt32   freeCancellationDays,
                                  AnyDecimal penaltyRate) {
        _freeCancellationDays       = freeCancellationDays;
        _freeCancellationDaysRecipe = freeCancellationDays;
        _penaltyRate                = penaltyRate;
        _penaltyRateRecipe          = penaltyRate;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCancellationPolicy(AnyCancellationPolicy from) {
        _freeCancellationDays       = from._freeCancellationDays;
        _freeCancellationDaysRecipe = from._freeCancellationDaysRecipe;
        _penaltyRate                = from._penaltyRate;
        _penaltyRateRecipe          = from._penaltyRateRecipe;
    }

    /// <summary>Pins <c>freeCancellationDays</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCancellationPolicy WithFreeCancellationDays(int value) {
        AnyCancellationPolicy copy = new AnyCancellationPolicy(this);

        copy._freeCancellationDays = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>freeCancellationDays</c> is drawn from — <c>AnyValidFreeCancellationDays()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCancellationPolicy WithFreeCancellationDays(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCancellationPolicy copy = new AnyCancellationPolicy(this);

        copy._freeCancellationDaysRecipe = configure(_freeCancellationDaysRecipe);
        copy._freeCancellationDays       = copy._freeCancellationDaysRecipe;

        return copy;
    }

    /// <summary>Pins <c>penaltyRate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCancellationPolicy WithPenaltyRate(decimal value) {
        AnyCancellationPolicy copy = new AnyCancellationPolicy(this);

        copy._penaltyRate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>penaltyRate</c> is drawn from — <c>AnyValidPenaltyRate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCancellationPolicy WithPenaltyRate(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCancellationPolicy copy = new AnyCancellationPolicy(this);

        copy._penaltyRateRecipe = configure(_penaltyRateRecipe);
        copy._penaltyRate       = copy._penaltyRateRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CancellationPolicy" />.</summary>
    public CancellationPolicy Generate() {
        return new CancellationPolicy(_freeCancellationDays.Generate(),
                                      _penaltyRate.Generate());
    }

}
