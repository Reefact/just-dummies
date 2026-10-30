// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate EmployeeId --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Payroll.Domain;

/// <summary>
///     A generator of arbitrary <see cref="EmployeeId" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyEmployeeId : IAny<EmployeeId> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyEmployeeId()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        // EmployeeId checks ^EMP-\d{6}$: the pattern is the guard, verbatim.
        return Any.StringMatching(@"^EMP-\d{6}$");
    }

    private AnyEmployeeId(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyEmployeeId WithValue(string value) {
        return new AnyEmployeeId(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEmployeeId WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnyEmployeeId(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="EmployeeId" />.</summary>
    public EmployeeId Generate() {
        return new EmployeeId(_value.Generate());
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
