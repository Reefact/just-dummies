// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PayslipLine --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Payroll.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PayslipLine" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPayslipLine : IAny<PayslipLine> {

    private readonly IAny<string>   _label;
    private readonly IAny<LineKind> _kind;
    private readonly IAny<decimal>  _amount;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPayslipLine()
        : this(label:  AnyValidLabel(),
               kind:   AnyValidKind(),
               amount: AnyValidAmount()) { }

    private static AnyString AnyValidLabel() {
        return Any.String().NotBlank().WithMaxLength(40);
    }

    private static AnyEnum<LineKind> AnyValidKind() {
        return Any.Enum<LineKind>();
    }

    private static AnyDecimal AnyValidAmount() {
        // PayslipLine also requires an amount to the cent.
        return Any.Decimal().Positive().WithScale(2);
    }

    private AnyPayslipLine(IAny<string>   label,
                           IAny<LineKind> kind,
                           IAny<decimal>  amount) {
        _label  = label;
        _kind   = kind;
        _amount = amount;
    }

    /// <summary>Pins <c>label</c> to a fixed value.</summary>
    public AnyPayslipLine WithLabel(string value) {
        return new AnyPayslipLine(new FixedValue<string>(value), _kind, _amount);
    }

    /// <summary>Draws <c>label</c> from <c>AnyValidLabel()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPayslipLine WithLabel(Func<AnyString, AnyString> configure) {
        return new AnyPayslipLine(configure(AnyValidLabel()), _kind, _amount);
    }

    /// <summary>Pins <c>kind</c> to a fixed value.</summary>
    public AnyPayslipLine WithKind(LineKind value) {
        return new AnyPayslipLine(_label, new FixedValue<LineKind>(value), _amount);
    }

    /// <summary>Draws <c>kind</c> from <c>AnyValidKind()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPayslipLine WithKind(Func<AnyEnum<LineKind>, AnyEnum<LineKind>> configure) {
        return new AnyPayslipLine(_label, configure(AnyValidKind()), _amount);
    }

    /// <summary>Pins <c>amount</c> to a fixed value.</summary>
    public AnyPayslipLine WithAmount(decimal value) {
        return new AnyPayslipLine(_label, _kind, new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>amount</c> from <c>AnyValidAmount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPayslipLine WithAmount(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyPayslipLine(_label, _kind, configure(AnyValidAmount()));
    }

    /// <summary>Produces one arbitrary <see cref="PayslipLine" />.</summary>
    public PayslipLine Generate() {
        return new PayslipLine(_label.Generate(),
                               _kind.Generate(),
                               _amount.Generate());
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
