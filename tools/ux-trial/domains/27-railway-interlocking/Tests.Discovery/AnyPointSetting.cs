// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PointSetting --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RailwayInterlocking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PointSetting" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPointSetting : IAny<PointSetting> {

    private readonly IAny<string>        _pointName;
    private readonly IAny<PointPosition> _required;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPointSetting()
        : this(pointName: AnyValidPointName(),
               required:  AnyValidRequired()) { }

    private static AnyString AnyValidPointName() {
        return Any.String().NotBlank();
    }

    private static AnyEnum<PointPosition> AnyValidRequired() {
        return Any.Enum<PointPosition>();
    }

    private AnyPointSetting(IAny<string>        pointName,
                            IAny<PointPosition> required) {
        _pointName = pointName;
        _required  = required;
    }

    /// <summary>Pins <c>pointName</c> to a fixed value.</summary>
    public AnyPointSetting WithPointName(string value) {
        return new AnyPointSetting(new FixedValue<string>(value), _required);
    }

    /// <summary>Draws <c>pointName</c> from <c>AnyValidPointName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPointSetting WithPointName(Func<AnyString, AnyString> configure) {
        return new AnyPointSetting(configure(AnyValidPointName()), _required);
    }

    /// <summary>Pins <c>required</c> to a fixed value.</summary>
    public AnyPointSetting WithRequired(PointPosition value) {
        return new AnyPointSetting(_pointName, new FixedValue<PointPosition>(value));
    }

    /// <summary>Draws <c>required</c> from <c>AnyValidRequired()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPointSetting WithRequired(Func<AnyEnum<PointPosition>, AnyEnum<PointPosition>> configure) {
        return new AnyPointSetting(_pointName, configure(AnyValidRequired()));
    }

    /// <summary>Produces one arbitrary <see cref="PointSetting" />.</summary>
    public PointSetting Generate() {
        return new PointSetting(_pointName.Generate(),
                                _required.Generate());
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
