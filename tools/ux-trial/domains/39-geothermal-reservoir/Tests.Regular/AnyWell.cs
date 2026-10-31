// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Well --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Well" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyWell : IAny<Well> {

    private readonly IAny<string>         _id;
    private readonly IAny<WellKind>       _kind;
    private readonly IAny<CellCoordinate> _completion;
    private readonly IAny<double>         _rateCubicMetresPerSecond;
    private readonly IAny<Temperature?>   _injectionTemperature;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyWell()
        : this(id:                       AnyValidId(),
               kind:                     AnyValidKind(),
               completion:               AnyValidCompletion(),
               rateCubicMetresPerSecond: AnyValidRateCubicMetresPerSecond(),
               injectionTemperature:     AnyValidInjectionTemperature()) { }

    private static AnyPattern AnyValidId() {
        // The two digits of the id; Generate() prefixes them with INJ- or PRD- according to the kind,
        // because the constructor ties the id's prefix to the kind.
        return Any.StringMatching("[0-9]{2}");
    }

    private static AnyEnum<WellKind> AnyValidKind() {
        return Any.Enum<WellKind>();
    }

    private static AnyCellCoordinate AnyValidCompletion() {
        return new AnyCellCoordinate();
    }

    private static AnyDouble AnyValidRateCubicMetresPerSecond() {
        return Any.Double().GreaterThan(0.0).LessThanOrEqualTo(Well.MaximumRateCubicMetresPerSecond);
    }

    private static AnyTemperature AnyValidInjectionTemperature() {
        // Only an injector receives it: Generate() hands a producer null.
        return new AnyTemperature();
    }

    private AnyWell(IAny<string>         id,
                    IAny<WellKind>       kind,
                    IAny<CellCoordinate> completion,
                    IAny<double>         rateCubicMetresPerSecond,
                    IAny<Temperature?>   injectionTemperature) {
        _id                       = id;
        _kind                     = kind;
        _completion               = completion;
        _rateCubicMetresPerSecond = rateCubicMetresPerSecond;
        _injectionTemperature     = injectionTemperature;
    }

    /// <summary>Pins the two digits of the id; the prefix still follows the kind.</summary>
    public AnyWell WithId(string value) {
        return new AnyWell(new FixedValue<string>(value), _kind, _completion, _rateCubicMetresPerSecond, _injectionTemperature);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWell WithId(Func<AnyPattern, AnyPattern> configure) {
        return new AnyWell(configure(AnyValidId()), _kind, _completion, _rateCubicMetresPerSecond, _injectionTemperature);
    }

    /// <summary>Pins <c>kind</c> to a fixed value.</summary>
    public AnyWell WithKind(WellKind value) {
        return new AnyWell(_id, new FixedValue<WellKind>(value), _completion, _rateCubicMetresPerSecond, _injectionTemperature);
    }

    /// <summary>Draws <c>kind</c> from <c>AnyValidKind()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWell WithKind(Func<AnyEnum<WellKind>, AnyEnum<WellKind>> configure) {
        return new AnyWell(_id, configure(AnyValidKind()), _completion, _rateCubicMetresPerSecond, _injectionTemperature);
    }

    /// <summary>Pins <c>completion</c> to a fixed value.</summary>
    public AnyWell WithCompletion(CellCoordinate value) {
        return new AnyWell(_id, _kind, new FixedValue<CellCoordinate>(value), _rateCubicMetresPerSecond, _injectionTemperature);
    }

    /// <summary>Draws <c>completion</c> from <c>AnyValidCompletion()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWell WithCompletion(Func<AnyCellCoordinate, AnyCellCoordinate> configure) {
        return new AnyWell(_id, _kind, configure(AnyValidCompletion()), _rateCubicMetresPerSecond, _injectionTemperature);
    }

    /// <summary>Pins <c>rateCubicMetresPerSecond</c> to a fixed value.</summary>
    public AnyWell WithRateCubicMetresPerSecond(double value) {
        return new AnyWell(_id, _kind, _completion, new FixedValue<double>(value), _injectionTemperature);
    }

    /// <summary>Draws <c>rateCubicMetresPerSecond</c> from <c>AnyValidRateCubicMetresPerSecond()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWell WithRateCubicMetresPerSecond(Func<AnyDouble, AnyDouble> configure) {
        return new AnyWell(_id, _kind, _completion, configure(AnyValidRateCubicMetresPerSecond()), _injectionTemperature);
    }

    /// <summary>Pins <c>injectionTemperature</c> to a fixed value.</summary>
    public AnyWell WithInjectionTemperature(Temperature? value) {
        return new AnyWell(_id, _kind, _completion, _rateCubicMetresPerSecond, new FixedValue<Temperature?>(value));
    }

    /// <summary>Draws <c>injectionTemperature</c> from <c>AnyValidInjectionTemperature()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWell WithInjectionTemperature(Func<AnyTemperature, AnyTemperature> configure) {
        return new AnyWell(_id, _kind, _completion, _rateCubicMetresPerSecond, configure(AnyValidInjectionTemperature()));
    }

    /// <summary>Produces one arbitrary <see cref="Well" />.</summary>
    public Well Generate() {
        WellKind kind   = _kind.Generate();
        string   prefix = kind == WellKind.Injector ? "INJ" : "PRD";

        return new Well(prefix + "-" + _id.Generate(),
                        kind,
                        _completion.Generate(),
                        _rateCubicMetresPerSecond.Generate(),
                        kind == WellKind.Injector ? _injectionTemperature.Generate() : null);
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
