// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ContractualFlow --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace LiquidityGap.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ContractualFlow" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyContractualFlow : IAny<ContractualFlow> {

    private IAny<string>        _flowId;
    private IAny<DateOnly>      _valueDate;
    private IAny<decimal>       _amount;
    private IAny<Currency>      _currency;
    private IAny<FlowDirection> _direction;

    private AnyString              _flowIdRecipe;
    private AnyDateOnly            _valueDateRecipe;
    private AnyDecimal             _amountRecipe;
    private AnyCurrency            _currencyRecipe;
    private AnyEnum<FlowDirection> _directionRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyContractualFlow()
        : this(flowId:    AnyValidFlowId(),
               valueDate: AnyValidValueDate(),
               amount:    AnyValidAmount(),
               currency:  AnyValidCurrency(),
               direction: AnyValidDirection()) { }

    private static AnyString AnyValidFlowId() {
        return Any.String().NotBlank();
    }

    private static AnyDateOnly AnyValidValueDate() {
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31));
    }

    private static AnyDecimal AnyValidAmount() {
        return Any.Decimal().Between(0.01m, 1_000_000m).WithScale(2);
    }

    private static AnyCurrency AnyValidCurrency() {
        return new AnyCurrency();
    }

    private static AnyEnum<FlowDirection> AnyValidDirection() {
        return Any.Enum<FlowDirection>();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyContractualFlow(AnyString              flowId,
                               AnyDateOnly            valueDate,
                               AnyDecimal             amount,
                               AnyCurrency            currency,
                               AnyEnum<FlowDirection> direction) {
        _flowId          = flowId;
        _flowIdRecipe    = flowId;
        _valueDate       = valueDate;
        _valueDateRecipe = valueDate;
        _amount          = amount;
        _amountRecipe    = amount;
        _currency        = currency;
        _currencyRecipe  = currency;
        _direction       = direction;
        _directionRecipe = direction;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyContractualFlow(AnyContractualFlow from) {
        _flowId          = from._flowId;
        _flowIdRecipe    = from._flowIdRecipe;
        _valueDate       = from._valueDate;
        _valueDateRecipe = from._valueDateRecipe;
        _amount          = from._amount;
        _amountRecipe    = from._amountRecipe;
        _currency        = from._currency;
        _currencyRecipe  = from._currencyRecipe;
        _direction       = from._direction;
        _directionRecipe = from._directionRecipe;
    }

    /// <summary>Pins <c>flowId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyContractualFlow WithFlowId(string value) {
        AnyContractualFlow copy = new AnyContractualFlow(this);

        copy._flowId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>flowId</c> is drawn from — <c>AnyValidFlowId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyContractualFlow WithFlowId(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyContractualFlow copy = new AnyContractualFlow(this);

        copy._flowIdRecipe = configure(_flowIdRecipe);
        copy._flowId       = copy._flowIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>valueDate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyContractualFlow WithValueDate(DateOnly value) {
        AnyContractualFlow copy = new AnyContractualFlow(this);

        copy._valueDate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>valueDate</c> is drawn from — <c>AnyValidValueDate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyContractualFlow WithValueDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyContractualFlow copy = new AnyContractualFlow(this);

        copy._valueDateRecipe = configure(_valueDateRecipe);
        copy._valueDate       = copy._valueDateRecipe;

        return copy;
    }

    /// <summary>Pins <c>amount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyContractualFlow WithAmount(decimal value) {
        AnyContractualFlow copy = new AnyContractualFlow(this);

        copy._amount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>amount</c> is drawn from — <c>AnyValidAmount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyContractualFlow WithAmount(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyContractualFlow copy = new AnyContractualFlow(this);

        copy._amountRecipe = configure(_amountRecipe);
        copy._amount       = copy._amountRecipe;

        return copy;
    }

    /// <summary>Pins <c>currency</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyContractualFlow WithCurrency(Currency value) {
        AnyContractualFlow copy = new AnyContractualFlow(this);

        copy._currency = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>currency</c> is drawn from — <c>AnyValidCurrency()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCurrency, AnyCurrency&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyContractualFlow WithCurrency(Func<AnyCurrency, AnyCurrency> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyContractualFlow copy = new AnyContractualFlow(this);

        copy._currencyRecipe = configure(_currencyRecipe);
        copy._currency       = copy._currencyRecipe;

        return copy;
    }

    /// <summary>Pins <c>direction</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyContractualFlow WithDirection(FlowDirection value) {
        AnyContractualFlow copy = new AnyContractualFlow(this);

        copy._direction = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>direction</c> is drawn from — <c>AnyValidDirection()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;FlowDirection&gt;, AnyEnum&lt;FlowDirection&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyContractualFlow WithDirection(Func<AnyEnum<FlowDirection>, AnyEnum<FlowDirection>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyContractualFlow copy = new AnyContractualFlow(this);

        copy._directionRecipe = configure(_directionRecipe);
        copy._direction       = copy._directionRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ContractualFlow" />.</summary>
    public ContractualFlow Generate() {
        return new ContractualFlow(_flowId.Generate(),
                                   _valueDate.Generate(),
                                   _amount.Generate(),
                                   _currency.Generate(),
                                   _direction.Generate());
    }

}
