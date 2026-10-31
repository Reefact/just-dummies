// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Animal --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace RoboticMilking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Animal" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAnimal : IAny<Animal> {

    private readonly IAny<AnimalId>                  _id;
    private readonly IAny<int>                       _lactationNumber;
    private readonly IAny<int>                       _daysInMilk;
    private readonly IAny<IEnumerable<UdderQuarter>> _blindQuarters;
    private readonly IAny<DateTimeOffset?>           _lastMilkedAt;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAnimal()
        : this(id:              AnyValidId(),
               lactationNumber: AnyValidLactationNumber(),
               daysInMilk:      AnyValidDaysInMilk(),
               blindQuarters:   AnyValidBlindQuarters(),
               lastMilkedAt:    AnyValidLastMilkedAt().AsNullable()) { }

    private static AnyAnimalId AnyValidId() {
        return new AnyAnimalId();
    }

    private static AnyInt32 AnyValidLactationNumber() {
        return Any.Int32().Positive().LessThanOrEqualTo(20);
    }

    private static AnyInt32 AnyValidDaysInMilk() {
        return Any.Int32().Between(0, 999);
    }

    private static AnySequence<UdderQuarter> AnyValidBlindQuarters() {
        // Duplicates collapse in the constructor; four distinct blind quarters are refused.
        return Any.SequenceOf(Any.Enum<UdderQuarter>()).Distinct().WithMaxCount(3);
    }

    private static AnyDateTimeOffset AnyValidLastMilkedAt() {
        return Any.DateTimeOffset();
    }

    private AnyAnimal(IAny<AnimalId>                  id,
                      IAny<int>                       lactationNumber,
                      IAny<int>                       daysInMilk,
                      IAny<IEnumerable<UdderQuarter>> blindQuarters,
                      IAny<DateTimeOffset?>           lastMilkedAt) {
        _id              = id;
        _lactationNumber = lactationNumber;
        _daysInMilk      = daysInMilk;
        _blindQuarters   = blindQuarters;
        _lastMilkedAt    = lastMilkedAt;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnyAnimal WithId(AnimalId value) {
        return new AnyAnimal(new FixedValue<AnimalId>(value), _lactationNumber, _daysInMilk, _blindQuarters, _lastMilkedAt);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAnimal WithId(Func<AnyAnimalId, AnyAnimalId> configure) {
        return new AnyAnimal(configure(AnyValidId()), _lactationNumber, _daysInMilk, _blindQuarters, _lastMilkedAt);
    }

    /// <summary>Pins <c>lactationNumber</c> to a fixed value.</summary>
    public AnyAnimal WithLactationNumber(int value) {
        return new AnyAnimal(_id, new FixedValue<int>(value), _daysInMilk, _blindQuarters, _lastMilkedAt);
    }

    /// <summary>Draws <c>lactationNumber</c> from <c>AnyValidLactationNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAnimal WithLactationNumber(Func<AnyInt32, AnyInt32> configure) {
        return new AnyAnimal(_id, configure(AnyValidLactationNumber()), _daysInMilk, _blindQuarters, _lastMilkedAt);
    }

    /// <summary>Pins <c>daysInMilk</c> to a fixed value.</summary>
    public AnyAnimal WithDaysInMilk(int value) {
        return new AnyAnimal(_id, _lactationNumber, new FixedValue<int>(value), _blindQuarters, _lastMilkedAt);
    }

    /// <summary>Draws <c>daysInMilk</c> from <c>AnyValidDaysInMilk()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAnimal WithDaysInMilk(Func<AnyInt32, AnyInt32> configure) {
        return new AnyAnimal(_id, _lactationNumber, configure(AnyValidDaysInMilk()), _blindQuarters, _lastMilkedAt);
    }

    /// <summary>Pins <c>blindQuarters</c> to a fixed value.</summary>
    public AnyAnimal WithBlindQuarters(IEnumerable<UdderQuarter> value) {
        return new AnyAnimal(_id, _lactationNumber, _daysInMilk, new FixedValue<IEnumerable<UdderQuarter>>(value), _lastMilkedAt);
    }

    /// <summary>Draws <c>blindQuarters</c> from <c>AnyValidBlindQuarters()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAnimal WithBlindQuarters(Func<AnySequence<UdderQuarter>, AnySequence<UdderQuarter>> configure) {
        return new AnyAnimal(_id, _lactationNumber, _daysInMilk, configure(AnyValidBlindQuarters()), _lastMilkedAt);
    }

    /// <summary>Pins <c>lastMilkedAt</c> to a fixed value.</summary>
    public AnyAnimal WithLastMilkedAt(DateTimeOffset? value) {
        return new AnyAnimal(_id, _lactationNumber, _daysInMilk, _blindQuarters, new FixedValue<DateTimeOffset?>(value));
    }

    /// <summary>Draws <c>lastMilkedAt</c> from <c>AnyValidLastMilkedAt()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAnimal WithLastMilkedAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        return new AnyAnimal(_id, _lactationNumber, _daysInMilk, _blindQuarters, configure(AnyValidLastMilkedAt()).AsNullable());
    }

    /// <summary>Produces one arbitrary <see cref="Animal" />.</summary>
    public Animal Generate() {
        return new Animal(_id.Generate(),
                          _lactationNumber.Generate(),
                          _daysInMilk.Generate(),
                          _blindQuarters.Generate(),
                          _lastMilkedAt.Generate());
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
