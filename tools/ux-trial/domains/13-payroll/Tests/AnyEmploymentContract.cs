// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate EmploymentContract --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Payroll.Domain;

/// <summary>
///     A generator of arbitrary <see cref="EmploymentContract" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyEmploymentContract : IAny<EmploymentContract> {

    private readonly IAny<EmployeeId>   _employee;
    private readonly IAny<ContractType> _type;
    private readonly IAny<DateOnly>     _hiredOn;
    private readonly IAny<DateOnly?>    _endsOn;
    private readonly IAny<HourlyRate>   _rate;
    private readonly IAny<int>          _weeklyHours;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyEmploymentContract()
        : this(employee:    AnyValidEmployee(),
               type:        AnyValidType(),
               hiredOn:     AnyValidHiredOn(),
               endsOn:      AnyValidEndsOn().AsNullable(),
               rate:        AnyValidRate(),
               weeklyHours: AnyValidWeeklyHours()) { }

    private static AnyEmployeeId AnyValidEmployee() {
        return new AnyEmployeeId();
    }

    private static AnyEnum<ContractType> AnyValidType() {
        // Enum.IsDefined: Any.Enum already stays inside the declared members.
        return Any.Enum<ContractType>();
    }

    private static AnyDateOnly AnyValidHiredOn() {
        // Not before 1970. Capped at 2100 so a fixed-term end date (see Generate) always fits.
        return Any.DateOnly().Between(EmploymentContract.EarliestHiringDate, new DateOnly(2100, 12, 31));
    }

    private static AnyDateOnly AnyValidEndsOn() {
        // Depends on the type and on hiredOn, which a per-parameter base cannot see: Generate() reconciles it.
        return Any.DateOnly();
    }

    private static AnyHourlyRate AnyValidRate() {
        return new AnyHourlyRate();
    }

    private static AnyInt32 AnyValidWeeklyHours() {
        // 1 to 48, but only up to 35 for apprentices and interns: the base keeps to what every type accepts.
        return Any.Int32().Between(1, 35);
    }

    private AnyEmploymentContract(IAny<EmployeeId>   employee,
                                  IAny<ContractType> type,
                                  IAny<DateOnly>     hiredOn,
                                  IAny<DateOnly?>    endsOn,
                                  IAny<HourlyRate>   rate,
                                  IAny<int>          weeklyHours) {
        _employee    = employee;
        _type        = type;
        _hiredOn     = hiredOn;
        _endsOn      = endsOn;
        _rate        = rate;
        _weeklyHours = weeklyHours;
    }

    /// <summary>Pins <c>employee</c> to a fixed value.</summary>
    public AnyEmploymentContract WithEmployee(EmployeeId value) {
        return new AnyEmploymentContract(new FixedValue<EmployeeId>(value), _type, _hiredOn, _endsOn, _rate, _weeklyHours);
    }

    /// <summary>Draws <c>employee</c> from <c>AnyValidEmployee()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEmploymentContract WithEmployee(Func<AnyEmployeeId, AnyEmployeeId> configure) {
        return new AnyEmploymentContract(configure(AnyValidEmployee()), _type, _hiredOn, _endsOn, _rate, _weeklyHours);
    }

    /// <summary>Pins <c>type</c> to a fixed value.</summary>
    public AnyEmploymentContract WithType(ContractType value) {
        return new AnyEmploymentContract(_employee, new FixedValue<ContractType>(value), _hiredOn, _endsOn, _rate, _weeklyHours);
    }

    /// <summary>Draws <c>type</c> from <c>AnyValidType()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEmploymentContract WithType(Func<AnyEnum<ContractType>, AnyEnum<ContractType>> configure) {
        return new AnyEmploymentContract(_employee, configure(AnyValidType()), _hiredOn, _endsOn, _rate, _weeklyHours);
    }

    /// <summary>Pins <c>hiredOn</c> to a fixed value.</summary>
    public AnyEmploymentContract WithHiredOn(DateOnly value) {
        return new AnyEmploymentContract(_employee, _type, new FixedValue<DateOnly>(value), _endsOn, _rate, _weeklyHours);
    }

    /// <summary>Draws <c>hiredOn</c> from <c>AnyValidHiredOn()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEmploymentContract WithHiredOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyEmploymentContract(_employee, _type, configure(AnyValidHiredOn()), _endsOn, _rate, _weeklyHours);
    }

    /// <summary>Pins <c>endsOn</c> to a fixed value.</summary>
    public AnyEmploymentContract WithEndsOn(DateOnly? value) {
        return new AnyEmploymentContract(_employee, _type, _hiredOn, new FixedValue<DateOnly?>(value), _rate, _weeklyHours);
    }

    /// <summary>Draws <c>endsOn</c> from <c>AnyValidEndsOn()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEmploymentContract WithEndsOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyEmploymentContract(_employee, _type, _hiredOn, configure(AnyValidEndsOn()).AsNullable(), _rate, _weeklyHours);
    }

    /// <summary>Pins <c>rate</c> to a fixed value.</summary>
    public AnyEmploymentContract WithRate(HourlyRate value) {
        return new AnyEmploymentContract(_employee, _type, _hiredOn, _endsOn, new FixedValue<HourlyRate>(value), _weeklyHours);
    }

    /// <summary>Draws <c>rate</c> from <c>AnyValidRate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEmploymentContract WithRate(Func<AnyHourlyRate, AnyHourlyRate> configure) {
        return new AnyEmploymentContract(_employee, _type, _hiredOn, _endsOn, configure(AnyValidRate()), _weeklyHours);
    }

    /// <summary>Pins <c>weeklyHours</c> to a fixed value.</summary>
    public AnyEmploymentContract WithWeeklyHours(int value) {
        return new AnyEmploymentContract(_employee, _type, _hiredOn, _endsOn, _rate, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>weeklyHours</c> from <c>AnyValidWeeklyHours()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEmploymentContract WithWeeklyHours(Func<AnyInt32, AnyInt32> configure) {
        return new AnyEmploymentContract(_employee, _type, _hiredOn, _endsOn, _rate, configure(AnyValidWeeklyHours()));
    }

    /// <summary>Produces one arbitrary <see cref="EmploymentContract" />.</summary>
    public EmploymentContract Generate() {
        EmployeeId   employee = _employee.Generate();
        ContractType type     = _type.Generate();
        DateOnly     hiredOn  = _hiredOn.Generate();
        DateOnly?    endsOn   = _endsOn.Generate();

        // Hand-written: a permanent contract has no end, any other ends after it starts.
        if (type == ContractType.Permanent) {
            endsOn = null;
        } else if (endsOn is null || endsOn <= hiredOn) {
            endsOn = hiredOn.AddDays(Any.Int32().Between(1, 3 * 365).Generate());
        }

        return new EmploymentContract(employee,
                                      type,
                                      hiredOn,
                                      endsOn,
                                      _rate.Generate(),
                                      _weeklyHours.Generate());
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
