// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Well --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
//
// Hand-edited after scaffolding: the id prefix, the kind and the injection temperature are tied
// together by the constructor, so Generate() derives the dependent ones from the drawn kind.

using System;

using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Well" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyWell : IAny<Well> {

    private readonly string?              _pinnedId;
    private readonly IAny<string>         _idNumber;
    private readonly IAny<WellKind>       _kind;
    private readonly IAny<CellCoordinate> _completion;
    private readonly IAny<double>         _rateCubicMetresPerSecond;
    private readonly IAny<Temperature>    _injectionTemperature;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyWell()
        : this(pinnedId:                 null,
               idNumber:                 AnyValidIdNumber(),
               kind:                     AnyValidKind(),
               completion:               AnyValidCompletion(),
               rateCubicMetresPerSecond: AnyValidRateCubicMetresPerSecond(),
               injectionTemperature:     AnyValidInjectionTemperature()) { }

    // The id is "INJ-nn" or "PRD-nn"; the prefix comes from the kind, so only the number is drawn.
    private static AnyPattern AnyValidIdNumber() {
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

    // Only an injector carries an injection temperature: Generate() leaves it out for a producer.
    private static AnyTemperature AnyValidInjectionTemperature() {
        return new AnyTemperature();
    }

    private AnyWell(string?              pinnedId,
                    IAny<string>         idNumber,
                    IAny<WellKind>       kind,
                    IAny<CellCoordinate> completion,
                    IAny<double>         rateCubicMetresPerSecond,
                    IAny<Temperature>    injectionTemperature) {
        _pinnedId                 = pinnedId;
        _idNumber                 = idNumber;
        _kind                     = kind;
        _completion               = completion;
        _rateCubicMetresPerSecond = rateCubicMetresPerSecond;
        _injectionTemperature     = injectionTemperature;
    }

    /// <summary>Pins <c>id</c> to a fixed value; it must still agree with the kind.</summary>
    public AnyWell WithId(string value) {
        return new AnyWell(value, _idNumber, _kind, _completion, _rateCubicMetresPerSecond, _injectionTemperature);
    }

    /// <summary>Pins <c>kind</c> to a fixed value.</summary>
    public AnyWell WithKind(WellKind value) {
        return new AnyWell(_pinnedId, _idNumber, new FixedValue<WellKind>(value), _completion, _rateCubicMetresPerSecond, _injectionTemperature);
    }

    /// <summary>Pins <c>completion</c> to a fixed value.</summary>
    public AnyWell WithCompletion(CellCoordinate value) {
        return new AnyWell(_pinnedId, _idNumber, _kind, new FixedValue<CellCoordinate>(value), _rateCubicMetresPerSecond, _injectionTemperature);
    }

    /// <summary>Draws <c>completion</c> from <c>AnyValidCompletion()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWell WithCompletion(Func<AnyCellCoordinate, AnyCellCoordinate> configure) {
        return new AnyWell(_pinnedId, _idNumber, _kind, configure(AnyValidCompletion()), _rateCubicMetresPerSecond, _injectionTemperature);
    }

    /// <summary>Pins <c>rateCubicMetresPerSecond</c> to a fixed value.</summary>
    public AnyWell WithRateCubicMetresPerSecond(double value) {
        return new AnyWell(_pinnedId, _idNumber, _kind, _completion, new FixedValue<double>(value), _injectionTemperature);
    }

    /// <summary>Draws <c>rateCubicMetresPerSecond</c> from <c>AnyValidRateCubicMetresPerSecond()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWell WithRateCubicMetresPerSecond(Func<AnyDouble, AnyDouble> configure) {
        return new AnyWell(_pinnedId, _idNumber, _kind, _completion, configure(AnyValidRateCubicMetresPerSecond()), _injectionTemperature);
    }

    /// <summary>Pins <c>injectionTemperature</c> (used only when the well is an injector).</summary>
    public AnyWell WithInjectionTemperature(Temperature value) {
        return new AnyWell(_pinnedId, _idNumber, _kind, _completion, _rateCubicMetresPerSecond, new FixedValue<Temperature>(value));
    }

    /// <summary>Produces one arbitrary <see cref="Well" />.</summary>
    public Well Generate() {
        WellKind     kind                 = _kind.Generate();
        string       id                   = _pinnedId ?? (kind == WellKind.Injector ? "INJ-" : "PRD-") + _idNumber.Generate();
        Temperature? injectionTemperature = kind == WellKind.Injector ? _injectionTemperature.Generate() : null;

        return new Well(id,
                        kind,
                        _completion.Generate(),
                        _rateCubicMetresPerSecond.Generate(),
                        injectionTemperature);
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
