// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Plan --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Telecom.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Plan" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPlan : IAny<Plan> {

    private readonly IAny<string>   _name;
    private readonly IAny<PlanTier> _tier;
    private readonly IAny<decimal>  _monthlyFee;
    private readonly IAny<int>      _dataAllowanceGb;
    private readonly IAny<decimal>  _overagePerGb;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPlan()
        : this(name:            AnyValidName(),
               tier:            AnyValidTier(),
               monthlyFee:      AnyValidMonthlyFee(),
               dataAllowanceGb: AnyValidDataAllowanceGb(),
               overagePerGb:    AnyValidOveragePerGb()) { }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank().WithMaxLength(40);
    }

    private static AnyEnum<PlanTier> AnyValidTier() {
        return Any.Enum<PlanTier>();
    }

    private static AnyDecimal AnyValidMonthlyFee() {
        return Any.Decimal().GreaterThanOrEqualTo(0m).WithScale(2);
    }

    private static AnyInt32 AnyValidDataAllowanceGb() {
        return Any.Int32().Positive();
    }

    private static AnyDecimal AnyValidOveragePerGb() {
        return Any.Decimal().GreaterThanOrEqualTo(0m);
    }

    private AnyPlan(IAny<string>   name,
                    IAny<PlanTier> tier,
                    IAny<decimal>  monthlyFee,
                    IAny<int>      dataAllowanceGb,
                    IAny<decimal>  overagePerGb) {
        _name            = name;
        _tier            = tier;
        _monthlyFee      = monthlyFee;
        _dataAllowanceGb = dataAllowanceGb;
        _overagePerGb    = overagePerGb;
    }

    /// <summary>Pins <c>name</c> to a fixed value.</summary>
    public AnyPlan WithName(string value) {
        return new AnyPlan(new FixedValue<string>(value), _tier, _monthlyFee, _dataAllowanceGb, _overagePerGb);
    }

    /// <summary>Draws <c>name</c> from <c>AnyValidName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPlan WithName(Func<AnyString, AnyString> configure) {
        return new AnyPlan(configure(AnyValidName()), _tier, _monthlyFee, _dataAllowanceGb, _overagePerGb);
    }

    /// <summary>Pins <c>tier</c> to a fixed value.</summary>
    public AnyPlan WithTier(PlanTier value) {
        return new AnyPlan(_name, new FixedValue<PlanTier>(value), _monthlyFee, _dataAllowanceGb, _overagePerGb);
    }

    /// <summary>Draws <c>tier</c> from <c>AnyValidTier()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPlan WithTier(Func<AnyEnum<PlanTier>, AnyEnum<PlanTier>> configure) {
        return new AnyPlan(_name, configure(AnyValidTier()), _monthlyFee, _dataAllowanceGb, _overagePerGb);
    }

    /// <summary>Pins <c>monthlyFee</c> to a fixed value.</summary>
    public AnyPlan WithMonthlyFee(decimal value) {
        return new AnyPlan(_name, _tier, new FixedValue<decimal>(value), _dataAllowanceGb, _overagePerGb);
    }

    /// <summary>Draws <c>monthlyFee</c> from <c>AnyValidMonthlyFee()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPlan WithMonthlyFee(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyPlan(_name, _tier, configure(AnyValidMonthlyFee()), _dataAllowanceGb, _overagePerGb);
    }

    /// <summary>Pins <c>dataAllowanceGb</c> to a fixed value.</summary>
    public AnyPlan WithDataAllowanceGb(int value) {
        return new AnyPlan(_name, _tier, _monthlyFee, new FixedValue<int>(value), _overagePerGb);
    }

    /// <summary>Draws <c>dataAllowanceGb</c> from <c>AnyValidDataAllowanceGb()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPlan WithDataAllowanceGb(Func<AnyInt32, AnyInt32> configure) {
        return new AnyPlan(_name, _tier, _monthlyFee, configure(AnyValidDataAllowanceGb()), _overagePerGb);
    }

    /// <summary>Pins <c>overagePerGb</c> to a fixed value.</summary>
    public AnyPlan WithOveragePerGb(decimal value) {
        return new AnyPlan(_name, _tier, _monthlyFee, _dataAllowanceGb, new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>overagePerGb</c> from <c>AnyValidOveragePerGb()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPlan WithOveragePerGb(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyPlan(_name, _tier, _monthlyFee, _dataAllowanceGb, configure(AnyValidOveragePerGb()));
    }

    /// <summary>Produces one arbitrary <see cref="Plan" />.</summary>
    public Plan Generate() {
        return new Plan(_name.Generate(),
                        _tier.Generate(),
                        _monthlyFee.Generate(),
                        _dataAllowanceGb.Generate(),
                        _overagePerGb.Generate());
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
