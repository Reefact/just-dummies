// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate IsoTolerance --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Drafting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="IsoTolerance" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyIsoTolerance : IAny<IsoTolerance> {

    private readonly IAny<string> _zone;
    private readonly IAny<int>    _grade;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyIsoTolerance()
        : this(zone:  AnyValidZone(),
               grade: AnyValidGrade()) { }

    private static AnyString AnyValidZone() {
        // The constructor's switch admits exactly these four fundamental deviations.
        return Any.String().OneOf("H", "h", "JS", "js");
    }

    private static AnyInt32 AnyValidGrade() {
        return Any.Int32().Between(6, 11);
    }

    private AnyIsoTolerance(IAny<string> zone,
                            IAny<int>    grade) {
        _zone  = zone;
        _grade = grade;
    }

    /// <summary>Pins <c>zone</c> to a fixed value.</summary>
    public AnyIsoTolerance WithZone(string value) {
        return new AnyIsoTolerance(new FixedValue<string>(value), _grade);
    }

    /// <summary>Draws <c>zone</c> from <c>AnyValidZone()</c>, refined by <paramref name="configure" />.</summary>
    public AnyIsoTolerance WithZone(Func<AnyString, AnyString> configure) {
        return new AnyIsoTolerance(configure(AnyValidZone()), _grade);
    }

    /// <summary>Pins <c>grade</c> to a fixed value.</summary>
    public AnyIsoTolerance WithGrade(int value) {
        return new AnyIsoTolerance(_zone, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>grade</c> from <c>AnyValidGrade()</c>, refined by <paramref name="configure" />.</summary>
    public AnyIsoTolerance WithGrade(Func<AnyInt32, AnyInt32> configure) {
        return new AnyIsoTolerance(_zone, configure(AnyValidGrade()));
    }

    /// <summary>Produces one arbitrary <see cref="IsoTolerance" />.</summary>
    public IsoTolerance Generate() {
        return new IsoTolerance(_zone.Generate(),
                                _grade.Generate());
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
