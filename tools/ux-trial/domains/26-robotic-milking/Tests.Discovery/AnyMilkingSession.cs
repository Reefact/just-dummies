// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MilkingSession --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RoboticMilking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="MilkingSession" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMilkingSession : IAny<MilkingSession> {

    private readonly IAny<Animal>          _animal;
    private readonly IAny<int>             _boxNumber;
    private readonly IAny<DateTimeOffset>  _startedAt;
    private readonly IAny<MilkingSettings> _settings;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMilkingSession()
        : this(animal:    AnyValidAnimal(),
               boxNumber: AnyValidBoxNumber(),
               startedAt: AnyValidStartedAt(),
               settings:  AnyValidSettings()) { }

    private static AnyAnimal AnyValidAnimal() {
        // The session refuses a cow milked after it starts or less than MinimumInterval before it, a rule
        // between animal, startedAt and settings. The base draws a cow the robot never milked; a test about
        // the interval pins LastMilkedAt itself.
        return new AnyAnimal().WithLastMilkedAt((DateTimeOffset?)null);
    }

    private static AnyInt32 AnyValidBoxNumber() {
        return Any.Int32().Positive().LessThanOrEqualTo(8);
    }

    private static AnyDateTimeOffset AnyValidStartedAt() {
        // Not a constructor guard: a session is a moment in a robot's working life, and tests step from it.
        return Any.DateTimeOffset().Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                            new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    private static AnyMilkingSettings AnyValidSettings() {
        return new AnyMilkingSettings();
    }

    private AnyMilkingSession(IAny<Animal>          animal,
                              IAny<int>             boxNumber,
                              IAny<DateTimeOffset>  startedAt,
                              IAny<MilkingSettings> settings) {
        _animal    = animal;
        _boxNumber = boxNumber;
        _startedAt = startedAt;
        _settings  = settings;
    }

    /// <summary>Pins <c>animal</c> to a fixed value.</summary>
    public AnyMilkingSession WithAnimal(Animal value) {
        return new AnyMilkingSession(new FixedValue<Animal>(value), _boxNumber, _startedAt, _settings);
    }

    /// <summary>Draws <c>animal</c> from <c>AnyValidAnimal()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMilkingSession WithAnimal(Func<AnyAnimal, AnyAnimal> configure) {
        return new AnyMilkingSession(configure(AnyValidAnimal()), _boxNumber, _startedAt, _settings);
    }

    /// <summary>Pins <c>boxNumber</c> to a fixed value.</summary>
    public AnyMilkingSession WithBoxNumber(int value) {
        return new AnyMilkingSession(_animal, new FixedValue<int>(value), _startedAt, _settings);
    }

    /// <summary>Draws <c>boxNumber</c> from <c>AnyValidBoxNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMilkingSession WithBoxNumber(Func<AnyInt32, AnyInt32> configure) {
        return new AnyMilkingSession(_animal, configure(AnyValidBoxNumber()), _startedAt, _settings);
    }

    /// <summary>Pins <c>startedAt</c> to a fixed value.</summary>
    public AnyMilkingSession WithStartedAt(DateTimeOffset value) {
        return new AnyMilkingSession(_animal, _boxNumber, new FixedValue<DateTimeOffset>(value), _settings);
    }

    /// <summary>Draws <c>startedAt</c> from <c>AnyValidStartedAt()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMilkingSession WithStartedAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        return new AnyMilkingSession(_animal, _boxNumber, configure(AnyValidStartedAt()), _settings);
    }

    /// <summary>Pins <c>settings</c> to a fixed value.</summary>
    public AnyMilkingSession WithSettings(MilkingSettings value) {
        return new AnyMilkingSession(_animal, _boxNumber, _startedAt, new FixedValue<MilkingSettings>(value));
    }

    /// <summary>Draws <c>settings</c> from <c>AnyValidSettings()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMilkingSession WithSettings(Func<AnyMilkingSettings, AnyMilkingSettings> configure) {
        return new AnyMilkingSession(_animal, _boxNumber, _startedAt, configure(AnyValidSettings()));
    }

    /// <summary>Produces one arbitrary <see cref="MilkingSession" />.</summary>
    public MilkingSession Generate() {
        return new MilkingSession(_animal.Generate(),
                                  _boxNumber.Generate(),
                                  _startedAt.Generate(),
                                  _settings.Generate());
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
