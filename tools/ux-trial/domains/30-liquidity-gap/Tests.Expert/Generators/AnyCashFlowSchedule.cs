// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CashFlowSchedule --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;
using LiquidityGap.Domain;

namespace LiquidityGap.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="CashFlowSchedule" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCashFlowSchedule : IAny<CashFlowSchedule> {

    private IAny<string>                _scheduleId;
    private IAny<DateOnly>              _firstPaymentDate;
    private IAny<int>                   _frequencyMonths;
    private IAny<int>                   _numberOfPayments;
    private IAny<decimal>               _amountPerPayment;
    private IAny<Currency>              _currency;
    private IAny<FlowDirection>         _direction;
    private IAny<BusinessDayConvention> _convention;

    private AnyString                      _scheduleIdRecipe;
    private AnyDateOnly                    _firstPaymentDateRecipe;
    private AnyInt32                       _frequencyMonthsRecipe;
    private AnyInt32                       _numberOfPaymentsRecipe;
    private AnyDecimal                     _amountPerPaymentRecipe;
    private AnyCurrency                    _currencyRecipe;
    private AnyEnum<FlowDirection>         _directionRecipe;
    private AnyEnum<BusinessDayConvention> _conventionRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCashFlowSchedule()
        : this(scheduleId:       AnyValidScheduleId(),
               firstPaymentDate: AnyValidFirstPaymentDate(),
               frequencyMonths:  AnyValidFrequencyMonths(),
               numberOfPayments: AnyValidNumberOfPayments(),
               amountPerPayment: AnyValidAmountPerPayment(),
               currency:         AnyValidCurrency(),
               direction:        AnyValidDirection(),
               convention:       AnyValidConvention()) { }

    private static AnyString AnyValidScheduleId() {
        return Any.String().NotBlank();
    }

    private static AnyDateOnly AnyValidFirstPaymentDate() {
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31));
    }

    private static AnyInt32 AnyValidFrequencyMonths() {
        return Any.Int32().OneOf(1, 3, 6, 12);
    }

    private static AnyInt32 AnyValidNumberOfPayments() {
        return Any.Int32().Positive().LessThanOrEqualTo(600);
    }

    private static AnyDecimal AnyValidAmountPerPayment() {
        return Any.Decimal().Between(0.01m, 1_000_000_000m).WithScale(2);
    }

    private static AnyCurrency AnyValidCurrency() {
        return new AnyCurrency();
    }

    private static AnyEnum<FlowDirection> AnyValidDirection() {
        return Any.Enum<FlowDirection>();
    }

    private static AnyEnum<BusinessDayConvention> AnyValidConvention() {
        return Any.Enum<BusinessDayConvention>();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCashFlowSchedule(AnyString                      scheduleId,
                                AnyDateOnly                    firstPaymentDate,
                                AnyInt32                       frequencyMonths,
                                AnyInt32                       numberOfPayments,
                                AnyDecimal                     amountPerPayment,
                                AnyCurrency                    currency,
                                AnyEnum<FlowDirection>         direction,
                                AnyEnum<BusinessDayConvention> convention) {
        _scheduleId             = scheduleId;
        _scheduleIdRecipe       = scheduleId;
        _firstPaymentDate       = firstPaymentDate;
        _firstPaymentDateRecipe = firstPaymentDate;
        _frequencyMonths        = frequencyMonths;
        _frequencyMonthsRecipe  = frequencyMonths;
        _numberOfPayments       = numberOfPayments;
        _numberOfPaymentsRecipe = numberOfPayments;
        _amountPerPayment       = amountPerPayment;
        _amountPerPaymentRecipe = amountPerPayment;
        _currency               = currency;
        _currencyRecipe         = currency;
        _direction              = direction;
        _directionRecipe        = direction;
        _convention             = convention;
        _conventionRecipe       = convention;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCashFlowSchedule(AnyCashFlowSchedule from) {
        _scheduleId             = from._scheduleId;
        _scheduleIdRecipe       = from._scheduleIdRecipe;
        _firstPaymentDate       = from._firstPaymentDate;
        _firstPaymentDateRecipe = from._firstPaymentDateRecipe;
        _frequencyMonths        = from._frequencyMonths;
        _frequencyMonthsRecipe  = from._frequencyMonthsRecipe;
        _numberOfPayments       = from._numberOfPayments;
        _numberOfPaymentsRecipe = from._numberOfPaymentsRecipe;
        _amountPerPayment       = from._amountPerPayment;
        _amountPerPaymentRecipe = from._amountPerPaymentRecipe;
        _currency               = from._currency;
        _currencyRecipe         = from._currencyRecipe;
        _direction              = from._direction;
        _directionRecipe        = from._directionRecipe;
        _convention             = from._convention;
        _conventionRecipe       = from._conventionRecipe;
    }

    /// <summary>Pins <c>scheduleId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCashFlowSchedule WithScheduleId(string value) {
        AnyCashFlowSchedule copy = new AnyCashFlowSchedule(this);

        copy._scheduleId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>scheduleId</c> is drawn from — <c>AnyValidScheduleId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCashFlowSchedule WithScheduleId(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCashFlowSchedule copy = new AnyCashFlowSchedule(this);

        copy._scheduleIdRecipe = configure(_scheduleIdRecipe);
        copy._scheduleId       = copy._scheduleIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>firstPaymentDate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCashFlowSchedule WithFirstPaymentDate(DateOnly value) {
        AnyCashFlowSchedule copy = new AnyCashFlowSchedule(this);

        copy._firstPaymentDate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>firstPaymentDate</c> is drawn from — <c>AnyValidFirstPaymentDate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCashFlowSchedule WithFirstPaymentDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCashFlowSchedule copy = new AnyCashFlowSchedule(this);

        copy._firstPaymentDateRecipe = configure(_firstPaymentDateRecipe);
        copy._firstPaymentDate       = copy._firstPaymentDateRecipe;

        return copy;
    }

    /// <summary>Pins <c>frequencyMonths</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCashFlowSchedule WithFrequencyMonths(int value) {
        AnyCashFlowSchedule copy = new AnyCashFlowSchedule(this);

        copy._frequencyMonths = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>frequencyMonths</c> is drawn from — <c>AnyValidFrequencyMonths()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCashFlowSchedule WithFrequencyMonths(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCashFlowSchedule copy = new AnyCashFlowSchedule(this);

        copy._frequencyMonthsRecipe = configure(_frequencyMonthsRecipe);
        copy._frequencyMonths       = copy._frequencyMonthsRecipe;

        return copy;
    }

    /// <summary>Pins <c>numberOfPayments</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCashFlowSchedule WithNumberOfPayments(int value) {
        AnyCashFlowSchedule copy = new AnyCashFlowSchedule(this);

        copy._numberOfPayments = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>numberOfPayments</c> is drawn from — <c>AnyValidNumberOfPayments()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCashFlowSchedule WithNumberOfPayments(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCashFlowSchedule copy = new AnyCashFlowSchedule(this);

        copy._numberOfPaymentsRecipe = configure(_numberOfPaymentsRecipe);
        copy._numberOfPayments       = copy._numberOfPaymentsRecipe;

        return copy;
    }

    /// <summary>Pins <c>amountPerPayment</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCashFlowSchedule WithAmountPerPayment(decimal value) {
        AnyCashFlowSchedule copy = new AnyCashFlowSchedule(this);

        copy._amountPerPayment = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>amountPerPayment</c> is drawn from — <c>AnyValidAmountPerPayment()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCashFlowSchedule WithAmountPerPayment(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCashFlowSchedule copy = new AnyCashFlowSchedule(this);

        copy._amountPerPaymentRecipe = configure(_amountPerPaymentRecipe);
        copy._amountPerPayment       = copy._amountPerPaymentRecipe;

        return copy;
    }

    /// <summary>Pins <c>currency</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCashFlowSchedule WithCurrency(Currency value) {
        AnyCashFlowSchedule copy = new AnyCashFlowSchedule(this);

        copy._currency = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>currency</c> is drawn from — <c>AnyValidCurrency()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCurrency, AnyCurrency&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCashFlowSchedule WithCurrency(Func<AnyCurrency, AnyCurrency> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCashFlowSchedule copy = new AnyCashFlowSchedule(this);

        copy._currencyRecipe = configure(_currencyRecipe);
        copy._currency       = copy._currencyRecipe;

        return copy;
    }

    /// <summary>Pins <c>direction</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCashFlowSchedule WithDirection(FlowDirection value) {
        AnyCashFlowSchedule copy = new AnyCashFlowSchedule(this);

        copy._direction = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>direction</c> is drawn from — <c>AnyValidDirection()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;FlowDirection&gt;, AnyEnum&lt;FlowDirection&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCashFlowSchedule WithDirection(Func<AnyEnum<FlowDirection>, AnyEnum<FlowDirection>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCashFlowSchedule copy = new AnyCashFlowSchedule(this);

        copy._directionRecipe = configure(_directionRecipe);
        copy._direction       = copy._directionRecipe;

        return copy;
    }

    /// <summary>Pins <c>convention</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCashFlowSchedule WithConvention(BusinessDayConvention value) {
        AnyCashFlowSchedule copy = new AnyCashFlowSchedule(this);

        copy._convention = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>convention</c> is drawn from — <c>AnyValidConvention()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;BusinessDayConvention&gt;, AnyEnum&lt;BusinessDayConvention&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCashFlowSchedule WithConvention(Func<AnyEnum<BusinessDayConvention>, AnyEnum<BusinessDayConvention>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCashFlowSchedule copy = new AnyCashFlowSchedule(this);

        copy._conventionRecipe = configure(_conventionRecipe);
        copy._convention       = copy._conventionRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CashFlowSchedule" />.</summary>
    public CashFlowSchedule Generate() {
        return new CashFlowSchedule(_scheduleId.Generate(),
                                    _firstPaymentDate.Generate(),
                                    _frequencyMonths.Generate(),
                                    _numberOfPayments.Generate(),
                                    _amountPerPayment.Generate(),
                                    _currency.Generate(),
                                    _direction.Generate(),
                                    _convention.Generate());
    }

}
