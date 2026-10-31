// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate AnatomicalLandmark --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace PulmonaryNodules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="AnatomicalLandmark" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAnatomicalLandmark : IAny<AnatomicalLandmark> {

    private readonly IAny<LandmarkKind> _kind;
    private readonly IAny<SpatialPoint> _position;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAnatomicalLandmark()
        : this(kind:     AnyValidKind(),
               position: AnyValidPosition()) { }

    private static AnyEnum<LandmarkKind> AnyValidKind() {
        return Any.Enum<LandmarkKind>();
    }

    private static AnySpatialPoint AnyValidPosition() {
        return new AnySpatialPoint();
    }

    private AnyAnatomicalLandmark(IAny<LandmarkKind> kind,
                                  IAny<SpatialPoint> position) {
        _kind     = kind;
        _position = position;
    }

    /// <summary>Pins <c>kind</c> to a fixed value.</summary>
    public AnyAnatomicalLandmark WithKind(LandmarkKind value) {
        return new AnyAnatomicalLandmark(new FixedValue<LandmarkKind>(value), _position);
    }

    /// <summary>Draws <c>kind</c> from <c>AnyValidKind()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAnatomicalLandmark WithKind(Func<AnyEnum<LandmarkKind>, AnyEnum<LandmarkKind>> configure) {
        return new AnyAnatomicalLandmark(configure(AnyValidKind()), _position);
    }

    /// <summary>Pins <c>position</c> to a fixed value.</summary>
    public AnyAnatomicalLandmark WithPosition(SpatialPoint value) {
        return new AnyAnatomicalLandmark(_kind, new FixedValue<SpatialPoint>(value));
    }

    /// <summary>Draws <c>position</c> from <c>AnyValidPosition()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAnatomicalLandmark WithPosition(Func<AnySpatialPoint, AnySpatialPoint> configure) {
        return new AnyAnatomicalLandmark(_kind, configure(AnyValidPosition()));
    }

    /// <summary>Produces one arbitrary <see cref="AnatomicalLandmark" />.</summary>
    public AnatomicalLandmark Generate() {
        return new AnatomicalLandmark(_kind.Generate(),
                                      _position.Generate());
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
