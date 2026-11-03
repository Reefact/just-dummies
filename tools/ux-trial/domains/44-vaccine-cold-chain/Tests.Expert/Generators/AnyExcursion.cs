// Started from `any generate Excursion`, then rewritten by hand: whether the extreme lies below or above the
// labelled range depends on the kind, a rule across three parameters that the scaffolder can only flag. The extreme is
// therefore drawn last, from the kind and the range already drawn.

using System;

using ColdChain.Domain;
using JustDummies;
using Warehouse.Domain;

namespace ColdChain.Tests.Expert.Generators;

public sealed partial class AnyExcursion : IAny<Excursion> {

    private IAny<ExcursionKind>    _kind          = Any.Enum<ExcursionKind>();
    private IAny<TemperatureRange> _labelledRange = new AnyTemperatureRange();
    private IAny<DateTimeOffset>   _startedAt     = Any.DateTimeOffset().Between(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2027, 12, 31, 0, 0, 0, TimeSpan.Zero));
    private IAny<TimeSpan>         _duration      = Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromDays(3));
    private IAny<bool>             _isOpen        = Any.Boolean();
    private decimal?               _extremeCelsius;

    public AnyExcursion() { }

    private AnyExcursion(AnyExcursion from) {
        _kind           = from._kind;
        _labelledRange  = from._labelledRange;
        _startedAt      = from._startedAt;
        _duration       = from._duration;
        _isOpen         = from._isOpen;
        _extremeCelsius = from._extremeCelsius;
    }

    public AnyExcursion WithKind(ExcursionKind value) {
        AnyExcursion copy = new AnyExcursion(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    public AnyExcursion WithLabelledRange(TemperatureRange value) {
        AnyExcursion copy = new AnyExcursion(this);

        copy._labelledRange = Any.Fixed(value);

        return copy;
    }

    public AnyExcursion WithStartedAt(DateTimeOffset value) {
        AnyExcursion copy = new AnyExcursion(this);

        copy._startedAt = Any.Fixed(value);

        return copy;
    }

    public AnyExcursion WithDuration(TimeSpan value) {
        AnyExcursion copy = new AnyExcursion(this);

        copy._duration = Any.Fixed(value);

        return copy;
    }

    public AnyExcursion WithExtremeCelsius(decimal value) {
        AnyExcursion copy = new AnyExcursion(this);

        copy._extremeCelsius = value;

        return copy;
    }

    public AnyExcursion WithIsOpen(bool value) {
        AnyExcursion copy = new AnyExcursion(this);

        copy._isOpen = Any.Fixed(value);

        return copy;
    }

    public Excursion Generate() {
        ExcursionKind    kind      = _kind.Generate();
        TemperatureRange range     = _labelledRange.Generate();
        DateTimeOffset   startedAt = _startedAt.Generate();
        decimal          extreme   = _extremeCelsius ?? (kind == ExcursionKind.Cold
                                                              ? Any.Decimal().Between(range.MinCelsius - 50m, range.MinCelsius - 0.1m).Generate()
                                                              : Any.Decimal().Between(range.MaxCelsius + 0.1m, range.MaxCelsius + 50m).Generate());

        return new Excursion(kind, range, startedAt, startedAt + _duration.Generate(), extreme, _isOpen.Generate());
    }

}
