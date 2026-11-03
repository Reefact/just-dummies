// Scaffolded by `any` (JustDummies), then completed by hand: `upper` ties to `lower` (upper > lower), so it is drawn
// as "lower plus a positive width" inside Generate(), rather than from a recipe of its own.

using System;

using JustDummies;

namespace TaxRules.Domain;

public sealed partial class AnyTaxBracket : IAny<TaxBracket> {

    private IAny<Money>                _lower;
    private Func<Money, IAny<Money?>>  _upperGiven;
    private IAny<Rate>                 _rate;

    private AnyMoney _lowerRecipe;
    private AnyRate  _rateRecipe;

    public AnyTaxBracket()
        : this(lower: AnyValidLower(),
               rate:  AnyValidRate()) { }

    private static AnyMoney AnyValidLower() {
        return new AnyMoney().WithAmount(a => a.Between(0m, 1_000_000_000m));
    }

    private static IAny<Money> AnyUpperAbove(Money lower) {
        // strictly above lower by construction
        return Any.Decimal().Between(0.01m, 1_000_000_000m).WithScale(2)
                  .As(width => new Money(lower.Amount + width));
    }

    private static IAny<Money?> AnyValidUpperAbove(Money lower) {
        return AnyUpperAbove(lower).OrNull();
    }

    private static AnyRate AnyValidRate() {
        return new AnyRate();
    }

    private AnyTaxBracket(AnyMoney lower, AnyRate rate) {
        _lower       = lower;
        _lowerRecipe = lower;
        _upperGiven  = AnyValidUpperAbove;
        _rate        = rate;
        _rateRecipe  = rate;
    }

    private AnyTaxBracket(AnyTaxBracket from) {
        _lower       = from._lower;
        _lowerRecipe = from._lowerRecipe;
        _upperGiven  = from._upperGiven;
        _rate        = from._rate;
        _rateRecipe  = from._rateRecipe;
    }

    public AnyTaxBracket WithLower(Money value) {
        AnyTaxBracket copy = new AnyTaxBracket(this);

        copy._lower = Any.Fixed(value);

        return copy;
    }

    public AnyTaxBracket WithLower(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTaxBracket copy = new AnyTaxBracket(this);

        copy._lowerRecipe = configure(_lowerRecipe);
        copy._lower       = copy._lowerRecipe;

        return copy;
    }

    public AnyTaxBracket WithUpper(Money? value) {
        AnyTaxBracket copy = new AnyTaxBracket(this);

        copy._upperGiven = _ => Any.Fixed(value);

        return copy;
    }

    public AnyTaxBracket WithoutUpper() {
        return WithUpper(null);
    }

    /// <summary>The bracket is bounded: an upper bound is always drawn, still strictly above the lower one.</summary>
    public AnyTaxBracket Bounded() {
        AnyTaxBracket copy = new AnyTaxBracket(this);

        copy._upperGiven = lower => AnyUpperAbove(lower).As(u => (Money?)u);

        return copy;
    }

    public AnyTaxBracket WithRate(Rate value) {
        AnyTaxBracket copy = new AnyTaxBracket(this);

        copy._rate = Any.Fixed(value);

        return copy;
    }

    public AnyTaxBracket WithRate(Func<AnyRate, AnyRate> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTaxBracket copy = new AnyTaxBracket(this);

        copy._rateRecipe = configure(_rateRecipe);
        copy._rate       = copy._rateRecipe;

        return copy;
    }

    public TaxBracket Generate() {
        Money  lower = _lower.Generate();
        Money? upper = _upperGiven(lower).Generate();

        return new TaxBracket(lower, upper, _rate.Generate());
    }

}
