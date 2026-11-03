// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Latency --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Latency" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLatency : IAny<Latency> {

    private IAny<int> _roundTripMilliseconds;

    private AnyInt32 _roundTripMillisecondsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLatency()
        : this(roundTripMilliseconds: AnyValidRoundTripMilliseconds()) { }

    private static AnyInt32 AnyValidRoundTripMilliseconds() {
        return Any.Int32().Between(0, Latency.MaxMilliseconds);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyLatency(AnyInt32 roundTripMilliseconds) {
        _roundTripMilliseconds       = roundTripMilliseconds;
        _roundTripMillisecondsRecipe = roundTripMilliseconds;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyLatency(AnyLatency from) {
        _roundTripMilliseconds       = from._roundTripMilliseconds;
        _roundTripMillisecondsRecipe = from._roundTripMillisecondsRecipe;
    }

    /// <summary>Pins <c>roundTripMilliseconds</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLatency WithRoundTripMilliseconds(int value) {
        AnyLatency copy = new AnyLatency(this);

        copy._roundTripMilliseconds = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>roundTripMilliseconds</c> is drawn from — <c>AnyValidRoundTripMilliseconds()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLatency WithRoundTripMilliseconds(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLatency copy = new AnyLatency(this);

        copy._roundTripMillisecondsRecipe = configure(_roundTripMillisecondsRecipe);
        copy._roundTripMilliseconds       = copy._roundTripMillisecondsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Latency" />.</summary>
    public Latency Generate() {
        return new Latency(_roundTripMilliseconds.Generate());
    }

}
