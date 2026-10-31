// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Tempo --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace MusicScore.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Tempo" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTempo : IAny<Tempo> {

    private readonly IAny<double>   _beatsPerMinute;
    private readonly IAny<Duration> _beatUnit;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTempo()
        : this(beatsPerMinute: AnyValidBeatsPerMinute(),
               beatUnit:       AnyValidBeatUnit()) { }

    private static AnyDouble AnyValidBeatsPerMinute() {
        return Any.Double().Between(20, 400);
    }

    private static AnyDuration AnyValidBeatUnit() {
        // Positive and at most a whole note: one note value (whole, half, quarter...) by construction.
        return new AnyDuration().WithNumerator(1).WithDenominator(d => d.OneOf(1, 2, 4, 8, 16));
    }

    private AnyTempo(IAny<double>   beatsPerMinute,
                     IAny<Duration> beatUnit) {
        _beatsPerMinute = beatsPerMinute;
        _beatUnit       = beatUnit;
    }

    /// <summary>Pins <c>beatsPerMinute</c> to a fixed value.</summary>
    public AnyTempo WithBeatsPerMinute(double value) {
        return new AnyTempo(new FixedValue<double>(value), _beatUnit);
    }

    /// <summary>Draws <c>beatsPerMinute</c> from <c>AnyValidBeatsPerMinute()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTempo WithBeatsPerMinute(Func<AnyDouble, AnyDouble> configure) {
        return new AnyTempo(configure(AnyValidBeatsPerMinute()), _beatUnit);
    }

    /// <summary>Pins <c>beatUnit</c> to a fixed value.</summary>
    public AnyTempo WithBeatUnit(Duration value) {
        return new AnyTempo(_beatsPerMinute, new FixedValue<Duration>(value));
    }

    /// <summary>Draws <c>beatUnit</c> from <c>AnyValidBeatUnit()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTempo WithBeatUnit(Func<AnyDuration, AnyDuration> configure) {
        return new AnyTempo(_beatsPerMinute, configure(AnyValidBeatUnit()));
    }

    /// <summary>Produces one arbitrary <see cref="Tempo" />.</summary>
    public Tempo Generate() {
        return new Tempo(_beatsPerMinute.Generate(),
                         _beatUnit.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
