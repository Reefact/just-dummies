// Scaffolded by `any` (JustDummies), then rewritten by hand: the constructor ties `upper` to `lower`
// (`upper.CompareTo(lower) <= 0` throws), a rule no per-parameter recipe can honour, so the upper bound
// is drawn as a positive width above the lower one. `any generate TaxBracket --force` would lose this.

using System;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>A generator of arbitrary <see cref="TaxBracket" /> values: sometimes open-ended, otherwise closed above its lower bound.</summary>
public sealed partial class AnyTaxBracket : IAny<TaxBracket> {

    private readonly IAny<Money> _lower;
    private readonly IAny<Rate>  _rate;
    private readonly bool        _upperPinned;
    private readonly Money?      _upper;

    public AnyTaxBracket()
        : this(lower: Plausible.Amount(), rate: new AnyRate(), upperPinned: false, upper: null) { }

    private AnyTaxBracket(IAny<Money> lower, IAny<Rate> rate, bool upperPinned, Money? upper) {
        _lower       = lower;
        _rate        = rate;
        _upperPinned = upperPinned;
        _upper       = upper;
    }

    public AnyTaxBracket WithLower(Money value) {
        return new AnyTaxBracket(Any.Fixed(value), _rate, _upperPinned, _upper);
    }

    public AnyTaxBracket WithRate(Rate value) {
        return new AnyTaxBracket(_lower, Any.Fixed(value), _upperPinned, _upper);
    }

    /// <summary>Pins the upper bound, <c>null</c> for an open-ended bracket. A pin is a literal: it must lie above the lower bound.</summary>
    public AnyTaxBracket WithUpper(Money? value) {
        return new AnyTaxBracket(_lower, _rate, true, value);
    }

    public AnyTaxBracket OpenEnded() {
        return WithUpper(null);
    }

    public TaxBracket Generate() {
        Money lower = _lower.Generate();
        Money? upper = _upperPinned ? _upper : DrawUpperAbove(lower);

        return new TaxBracket(lower, upper, _rate.Generate());
    }

    private static Money? DrawUpperAbove(Money lower) {
        if (Any.Int32().Between(1, 4).Generate() == 1) { return null; }

        Money width = new Money(Any.Decimal().Between(0.01m, 10_000_000m).WithScale(2).Generate());

        return lower.Plus(width);
    }

}
