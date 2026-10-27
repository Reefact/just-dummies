// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MobileLine --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Telecom.Domain;

/// <summary>
///     A generator of arbitrary <see cref="MobileLine" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMobileLine : IAny<MobileLine> {

    private readonly IAny<Msisdn>   _number;
    private readonly IAny<Imei>     _handset;
    private readonly IAny<Plan>     _plan;
    private readonly IAny<DateOnly> _activatedOn;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMobileLine()
        : this(number:      AnyValidNumber(),
               handset:     AnyValidHandset(),
               plan:        AnyValidPlan(),
               activatedOn: AnyValidActivatedOn()) { }

    private static AnyMsisdn AnyValidNumber() {
        return new AnyMsisdn();
    }

    private static AnyImei AnyValidHandset() {
        return new AnyImei();
    }

    private static AnyPlan AnyValidPlan() {
        return new AnyPlan();
    }

    private static AnyDateOnly AnyValidActivatedOn() {
        return Any.DateOnly().AfterOrEqualTo(new DateOnly(2000, 1, 1));
    }

    private AnyMobileLine(IAny<Msisdn>   number,
                          IAny<Imei>     handset,
                          IAny<Plan>     plan,
                          IAny<DateOnly> activatedOn) {
        _number      = number;
        _handset     = handset;
        _plan        = plan;
        _activatedOn = activatedOn;
    }

    /// <summary>Pins <c>number</c> to a fixed value.</summary>
    public AnyMobileLine WithNumber(Msisdn value) {
        return new AnyMobileLine(new FixedValue<Msisdn>(value), _handset, _plan, _activatedOn);
    }

    /// <summary>Draws <c>number</c> from <c>AnyValidNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMobileLine WithNumber(Func<AnyMsisdn, AnyMsisdn> configure) {
        return new AnyMobileLine(configure(AnyValidNumber()), _handset, _plan, _activatedOn);
    }

    /// <summary>Pins <c>handset</c> to a fixed value.</summary>
    public AnyMobileLine WithHandset(Imei value) {
        return new AnyMobileLine(_number, new FixedValue<Imei>(value), _plan, _activatedOn);
    }

    /// <summary>Draws <c>handset</c> from <c>AnyValidHandset()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMobileLine WithHandset(Func<AnyImei, AnyImei> configure) {
        return new AnyMobileLine(_number, configure(AnyValidHandset()), _plan, _activatedOn);
    }

    /// <summary>Pins <c>plan</c> to a fixed value.</summary>
    public AnyMobileLine WithPlan(Plan value) {
        return new AnyMobileLine(_number, _handset, new FixedValue<Plan>(value), _activatedOn);
    }

    /// <summary>Draws <c>plan</c> from <c>AnyValidPlan()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMobileLine WithPlan(Func<AnyPlan, AnyPlan> configure) {
        return new AnyMobileLine(_number, _handset, configure(AnyValidPlan()), _activatedOn);
    }

    /// <summary>Pins <c>activatedOn</c> to a fixed value.</summary>
    public AnyMobileLine WithActivatedOn(DateOnly value) {
        return new AnyMobileLine(_number, _handset, _plan, new FixedValue<DateOnly>(value));
    }

    /// <summary>Draws <c>activatedOn</c> from <c>AnyValidActivatedOn()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMobileLine WithActivatedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyMobileLine(_number, _handset, _plan, configure(AnyValidActivatedOn()));
    }

    /// <summary>Produces one arbitrary <see cref="MobileLine" />.</summary>
    public MobileLine Generate() {
        return new MobileLine(_number.Generate(),
                              _handset.Generate(),
                              _plan.Generate(),
                              _activatedOn.Generate());
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
