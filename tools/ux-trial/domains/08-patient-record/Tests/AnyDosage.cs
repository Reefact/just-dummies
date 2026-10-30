// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Dosage --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Clinical.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Dosage" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDosage : IAny<Dosage> {

    private readonly IAny<decimal>  _amount;
    private readonly IAny<DoseUnit> _unit;
    private readonly IAny<int>      _timesPerDay;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDosage()
        : this(amount:      AnyValidAmount(),
               unit:        AnyValidUnit(),
               timesPerDay: AnyValidTimesPerDay()) { }

    private static AnyDecimal AnyValidAmount() {
        // At most three decimals: the constructor refuses a finer dose.
        return Any.Decimal().Positive().LessThanOrEqualTo(10000m).WithScale(3);
    }

    private static AnyEnum<DoseUnit> AnyValidUnit() {
        return Any.Enum<DoseUnit>();
    }

    private static AnyInt32 AnyValidTimesPerDay() {
        return Any.Int32().Between(1, 6);
    }

    private AnyDosage(IAny<decimal>  amount,
                      IAny<DoseUnit> unit,
                      IAny<int>      timesPerDay) {
        _amount      = amount;
        _unit        = unit;
        _timesPerDay = timesPerDay;
    }

    /// <summary>Pins <c>amount</c> to a fixed value.</summary>
    public AnyDosage WithAmount(decimal value) {
        return new AnyDosage(new FixedValue<decimal>(value), _unit, _timesPerDay);
    }

    /// <summary>Draws <c>amount</c> from <c>AnyValidAmount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDosage WithAmount(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyDosage(configure(AnyValidAmount()), _unit, _timesPerDay);
    }

    /// <summary>Pins <c>unit</c> to a fixed value.</summary>
    public AnyDosage WithUnit(DoseUnit value) {
        return new AnyDosage(_amount, new FixedValue<DoseUnit>(value), _timesPerDay);
    }

    /// <summary>Draws <c>unit</c> from <c>AnyValidUnit()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDosage WithUnit(Func<AnyEnum<DoseUnit>, AnyEnum<DoseUnit>> configure) {
        return new AnyDosage(_amount, configure(AnyValidUnit()), _timesPerDay);
    }

    /// <summary>Pins <c>timesPerDay</c> to a fixed value.</summary>
    public AnyDosage WithTimesPerDay(int value) {
        return new AnyDosage(_amount, _unit, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>timesPerDay</c> from <c>AnyValidTimesPerDay()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDosage WithTimesPerDay(Func<AnyInt32, AnyInt32> configure) {
        return new AnyDosage(_amount, _unit, configure(AnyValidTimesPerDay()));
    }

    /// <summary>Produces one arbitrary <see cref="Dosage" />.</summary>
    public Dosage Generate() {
        return new Dosage(_amount.Generate(),
                          _unit.Generate(),
                          _timesPerDay.Generate());
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
