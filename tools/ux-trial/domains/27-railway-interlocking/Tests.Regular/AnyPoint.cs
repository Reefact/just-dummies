// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RailwayInterlocking.Domain.Point --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RailwayInterlocking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Point" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPoint : IAny<Point> {

    private readonly IAny<string>        _name;
    private readonly IAny<SectionId>     _section;
    private readonly IAny<PointPosition> _initialPosition;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPoint()
        : this(name:            AnyValidName(),
               section:         AnyValidSection(),
               initialPosition: AnyValidInitialPosition()) { }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank();
    }

    private static AnySectionId AnyValidSection() {
        return new AnySectionId();
    }

    private static AnyEnum<PointPosition> AnyValidInitialPosition() {
        return Any.Enum<PointPosition>();
    }

    private AnyPoint(IAny<string>        name,
                     IAny<SectionId>     section,
                     IAny<PointPosition> initialPosition) {
        _name            = name;
        _section         = section;
        _initialPosition = initialPosition;
    }

    /// <summary>Pins <c>name</c> to a fixed value.</summary>
    public AnyPoint WithName(string value) {
        return new AnyPoint(new FixedValue<string>(value), _section, _initialPosition);
    }

    /// <summary>Draws <c>name</c> from <c>AnyValidName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPoint WithName(Func<AnyString, AnyString> configure) {
        return new AnyPoint(configure(AnyValidName()), _section, _initialPosition);
    }

    /// <summary>Pins <c>section</c> to a fixed value.</summary>
    public AnyPoint WithSection(SectionId value) {
        return new AnyPoint(_name, new FixedValue<SectionId>(value), _initialPosition);
    }

    /// <summary>Draws <c>section</c> from <c>AnyValidSection()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPoint WithSection(Func<AnySectionId, AnySectionId> configure) {
        return new AnyPoint(_name, configure(AnyValidSection()), _initialPosition);
    }

    /// <summary>Pins <c>initialPosition</c> to a fixed value.</summary>
    public AnyPoint WithInitialPosition(PointPosition value) {
        return new AnyPoint(_name, _section, new FixedValue<PointPosition>(value));
    }

    /// <summary>Draws <c>initialPosition</c> from <c>AnyValidInitialPosition()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPoint WithInitialPosition(Func<AnyEnum<PointPosition>, AnyEnum<PointPosition>> configure) {
        return new AnyPoint(_name, _section, configure(AnyValidInitialPosition()));
    }

    /// <summary>Produces one arbitrary <see cref="Point" />.</summary>
    public Point Generate() {
        return new Point(_name.Generate(),
                         _section.Generate(),
                         _initialPosition.Generate());
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
