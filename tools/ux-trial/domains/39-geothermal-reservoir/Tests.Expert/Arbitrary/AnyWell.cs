// Scaffolded by `any` (JustDummies), then reshaped by hand: Well's constructor ties three parameters
// together (the id's prefix names the kind, and only an injector carries an injection temperature), a
// rule no per-parameter recipe can honour. The kind is drawn first and the other two follow from it, so
// every draw is valid by construction. Do NOT re-run `any generate Well --force` over this file.

using System;

using GeothermalReservoir.Domain;
using JustDummies;

namespace GeothermalReservoir.Tests.Expert.Arbitrary;

/// <summary>A generator of arbitrary, valid <see cref="Well" /> values.</summary>
public sealed partial class AnyWell : IAny<Well> {

    private readonly IAny<WellKind>       _kind;
    private readonly IAny<int>            _serial;
    private readonly IAny<CellCoordinate> _completion;
    private readonly IAny<double>         _rateCubicMetresPerSecond;
    private readonly IAny<Temperature>    _injectionTemperature;

    public AnyWell()
        : this(kind:                     AnyValidKind(),
               serial:                   AnyValidSerial(),
               completion:               AnyValidCompletion(),
               rateCubicMetresPerSecond: AnyValidRateCubicMetresPerSecond(),
               injectionTemperature:     AnyValidInjectionTemperature()) { }

    private static AnyEnum<WellKind> AnyValidKind() {
        return Any.Enum<WellKind>();
    }

    /// <summary>The two digits after "INJ-" or "PRD-".</summary>
    private static AnyInt32 AnyValidSerial() {
        return Any.Int32().Between(0, 99);
    }

    private static AnyCellCoordinate AnyValidCompletion() {
        return new AnyCellCoordinate();
    }

    private static AnyDouble AnyValidRateCubicMetresPerSecond() {
        return Any.Double().GreaterThan(0.0).LessThanOrEqualTo(Well.MaximumRateCubicMetresPerSecond);
    }

    /// <summary>Only drawn for an injector.</summary>
    private static AnyTemperature AnyValidInjectionTemperature() {
        return new AnyTemperature();
    }

    private AnyWell(IAny<WellKind>       kind,
                    IAny<int>            serial,
                    IAny<CellCoordinate> completion,
                    IAny<double>         rateCubicMetresPerSecond,
                    IAny<Temperature>    injectionTemperature) {
        _kind                     = kind;
        _serial                   = serial;
        _completion               = completion;
        _rateCubicMetresPerSecond = rateCubicMetresPerSecond;
        _injectionTemperature     = injectionTemperature;
    }

    public AnyWell WithKind(WellKind value) {
        return new AnyWell(new FixedValue<WellKind>(value), _serial, _completion, _rateCubicMetresPerSecond, _injectionTemperature);
    }

    public AnyWell WithSerial(int value) {
        return new AnyWell(_kind, new FixedValue<int>(value), _completion, _rateCubicMetresPerSecond, _injectionTemperature);
    }

    public AnyWell WithSerial(Func<AnyInt32, AnyInt32> configure) {
        return new AnyWell(_kind, configure(AnyValidSerial()), _completion, _rateCubicMetresPerSecond, _injectionTemperature);
    }

    public AnyWell WithCompletion(CellCoordinate value) {
        return new AnyWell(_kind, _serial, new FixedValue<CellCoordinate>(value), _rateCubicMetresPerSecond, _injectionTemperature);
    }

    public AnyWell WithCompletion(Func<AnyCellCoordinate, AnyCellCoordinate> configure) {
        return new AnyWell(_kind, _serial, configure(AnyValidCompletion()), _rateCubicMetresPerSecond, _injectionTemperature);
    }

    public AnyWell WithRateCubicMetresPerSecond(double value) {
        return new AnyWell(_kind, _serial, _completion, new FixedValue<double>(value), _injectionTemperature);
    }

    public AnyWell WithRateCubicMetresPerSecond(Func<AnyDouble, AnyDouble> configure) {
        return new AnyWell(_kind, _serial, _completion, configure(AnyValidRateCubicMetresPerSecond()), _injectionTemperature);
    }

    /// <summary>Pins the injected fluid's temperature; it only matters for an injector.</summary>
    public AnyWell WithInjectionTemperature(Temperature value) {
        return new AnyWell(_kind, _serial, _completion, _rateCubicMetresPerSecond, new FixedValue<Temperature>(value));
    }

    public AnyWell WithInjectionTemperature(Func<AnyTemperature, AnyTemperature> configure) {
        return new AnyWell(_kind, _serial, _completion, _rateCubicMetresPerSecond, configure(AnyValidInjectionTemperature()));
    }

    public Well Generate() {
        WellKind kind   = _kind.Generate();
        string   prefix = kind == WellKind.Injector ? "INJ" : "PRD";
        string   id     = $"{prefix}-{_serial.Generate():00}";

        return new Well(id,
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
