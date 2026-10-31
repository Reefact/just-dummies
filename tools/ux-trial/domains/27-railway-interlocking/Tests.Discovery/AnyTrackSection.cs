// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TrackSection --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RailwayInterlocking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TrackSection" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTrackSection : IAny<TrackSection> {

    private readonly IAny<SectionId> _id;
    private readonly IAny<double>    _lengthMetres;
    private readonly IAny<double>    _lineSpeedKmh;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTrackSection()
        : this(id:           AnyValidId(),
               lengthMetres: AnyValidLengthMetres(),
               lineSpeedKmh: AnyValidLineSpeedKmh()) { }

    private static AnySectionId AnyValidId() {
        return new AnySectionId();
    }

    private static AnyDouble AnyValidLengthMetres() {
        // TrackSection.cs: length in [MinimumLengthMetres, MaximumLengthMetres].
        return Any.Double().Between(TrackSection.MinimumLengthMetres, TrackSection.MaximumLengthMetres);
    }

    private static AnyDouble AnyValidLineSpeedKmh() {
        // TrackSection.cs: line speed in (0, 300] km/h.
        return Any.Double().GreaterThan(0).LessThanOrEqualTo(300);
    }

    private AnyTrackSection(IAny<SectionId> id,
                            IAny<double>    lengthMetres,
                            IAny<double>    lineSpeedKmh) {
        _id           = id;
        _lengthMetres = lengthMetres;
        _lineSpeedKmh = lineSpeedKmh;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnyTrackSection WithId(SectionId value) {
        return new AnyTrackSection(new FixedValue<SectionId>(value), _lengthMetres, _lineSpeedKmh);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTrackSection WithId(Func<AnySectionId, AnySectionId> configure) {
        return new AnyTrackSection(configure(AnyValidId()), _lengthMetres, _lineSpeedKmh);
    }

    /// <summary>Pins <c>lengthMetres</c> to a fixed value.</summary>
    public AnyTrackSection WithLengthMetres(double value) {
        return new AnyTrackSection(_id, new FixedValue<double>(value), _lineSpeedKmh);
    }

    /// <summary>Draws <c>lengthMetres</c> from <c>AnyValidLengthMetres()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTrackSection WithLengthMetres(Func<AnyDouble, AnyDouble> configure) {
        return new AnyTrackSection(_id, configure(AnyValidLengthMetres()), _lineSpeedKmh);
    }

    /// <summary>Pins <c>lineSpeedKmh</c> to a fixed value.</summary>
    public AnyTrackSection WithLineSpeedKmh(double value) {
        return new AnyTrackSection(_id, _lengthMetres, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>lineSpeedKmh</c> from <c>AnyValidLineSpeedKmh()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTrackSection WithLineSpeedKmh(Func<AnyDouble, AnyDouble> configure) {
        return new AnyTrackSection(_id, _lengthMetres, configure(AnyValidLineSpeedKmh()));
    }

    /// <summary>Produces one arbitrary <see cref="TrackSection" />.</summary>
    public TrackSection Generate() {
        return new TrackSection(_id.Generate(),
                                _lengthMetres.Generate(),
                                _lineSpeedKmh.Generate());
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
