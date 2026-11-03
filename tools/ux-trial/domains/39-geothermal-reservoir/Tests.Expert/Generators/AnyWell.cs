// Scaffolded by `any`, then rewritten by hand: the id, the kind and the injection temperature are tied
// by the constructor (prefix agrees with kind; an injector has a temperature, a producer has none), which
// no per-parameter recipe can express. The kind is drawn first and the rest follows from it.
// It is ours; do not re-scaffold it with --force.

using System;

using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>A generator of arbitrary <see cref="Well" /> values: injector or producer, id and temperature consistent with it.</summary>
public sealed partial class AnyWell : IAny<Well> {

    private IAny<WellKind>       _kind;
    private IAny<int>            _number;
    private string?              _pinnedId;
    private IAny<CellCoordinate> _completion;
    private IAny<double>         _rate;
    private IAny<Temperature>    _injectionTemperature;

    private AnyEnum<WellKind>  _kindRecipe;
    private AnyInt32           _numberRecipe;
    private AnyCellCoordinate  _completionRecipe;
    private AnyDouble          _rateRecipe;
    private AnyTemperature     _injectionTemperatureRecipe;

    public AnyWell()
        : this(Any.Enum<WellKind>(),
               Any.Int32().Between(0, 99),
               new AnyCellCoordinate(),
               Any.Double().Positive().LessThanOrEqualTo(Well.MaximumRateCubicMetresPerSecond),
               new AnyTemperature()) { }

    private AnyWell(AnyEnum<WellKind> kind, AnyInt32 number, AnyCellCoordinate completion, AnyDouble rate, AnyTemperature injectionTemperature) {
        _kind                       = kind;
        _kindRecipe                 = kind;
        _number                     = number;
        _numberRecipe               = number;
        _completion                 = completion;
        _completionRecipe           = completion;
        _rate                       = rate;
        _rateRecipe                 = rate;
        _injectionTemperature       = injectionTemperature;
        _injectionTemperatureRecipe = injectionTemperature;
    }

    private AnyWell(AnyWell from) {
        _kind                       = from._kind;
        _kindRecipe                 = from._kindRecipe;
        _number                     = from._number;
        _numberRecipe               = from._numberRecipe;
        _pinnedId                   = from._pinnedId;
        _completion                 = from._completion;
        _completionRecipe           = from._completionRecipe;
        _rate                       = from._rate;
        _rateRecipe                 = from._rateRecipe;
        _injectionTemperature       = from._injectionTemperature;
        _injectionTemperatureRecipe = from._injectionTemperatureRecipe;
    }

    public AnyWell Injector() { return WithKind(WellKind.Injector); }

    public AnyWell Producer() { return WithKind(WellKind.Producer); }

    public AnyWell WithKind(WellKind value) {
        AnyWell copy = new AnyWell(this);
        copy._kind = Any.Fixed(value);
        return copy;
    }

    /// <summary>Pins the two digits of the id; the prefix follows the kind.</summary>
    public AnyWell WithNumber(int value) {
        AnyWell copy = new AnyWell(this);
        copy._number = Any.Fixed(value);
        return copy;
    }

    /// <summary>Pins the whole id as written, whatever the kind: a mismatch is the constructor's to refuse.</summary>
    public AnyWell WithId(string value) {
        AnyWell copy = new AnyWell(this);
        copy._pinnedId = value;
        return copy;
    }

    public AnyWell WithCompletion(CellCoordinate value) {
        AnyWell copy = new AnyWell(this);
        copy._completion = Any.Fixed(value);
        return copy;
    }

    public AnyWell WithRate(double value) {
        AnyWell copy = new AnyWell(this);
        copy._rate = Any.Fixed(value);
        return copy;
    }

    public AnyWell WithRate(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWell copy = new AnyWell(this);
        copy._rateRecipe = configure(_rateRecipe);
        copy._rate       = copy._rateRecipe;
        return copy;
    }

    /// <summary>Pins the injection temperature; it only reaches an injector, a producer never has one.</summary>
    public AnyWell WithInjectionTemperature(Temperature value) {
        AnyWell copy = new AnyWell(this);
        copy._injectionTemperature = Any.Fixed(value);
        return copy;
    }

    public Well Generate() {
        WellKind     kind        = _kind.Generate();
        string       prefix      = kind == WellKind.Injector ? "INJ" : "PRD";
        string       id          = _pinnedId ?? $"{prefix}-{_number.Generate():00}";
        Temperature? temperature = kind == WellKind.Injector ? _injectionTemperature.Generate() : null;

        return new Well(id, kind, _completion.Generate(), _rate.Generate(), temperature);
    }

}
