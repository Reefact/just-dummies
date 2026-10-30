namespace Shop.Domain;

/// <summary>A marketing code granting a percentage off, within a validity window and above a minimum basket.</summary>
public sealed class PromoCode {

    public PromoCode(string code, decimal percentOff, DateOnly validFrom, DateOnly validUntil, decimal minimumBasket, int maxUses) {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (code.Length is < 4 or > 12) {
            throw new ArgumentException("A promo code has 4 to 12 characters.", nameof(code));
        }
        if (percentOff is <= 0m or > 50m) {
            throw new ArgumentOutOfRangeException(nameof(percentOff), percentOff, "A promo takes more than 0 % and at most 50 % off.");
        }
        if (validUntil < validFrom) {
            throw new ArgumentException("A promo code cannot expire before it starts.", nameof(validUntil));
        }
        if (0m > minimumBasket) {
            throw new ArgumentOutOfRangeException(nameof(minimumBasket), minimumBasket, "A minimum basket cannot be negative.");
        }

        Code          = code.ToUpperInvariant();
        PercentOff    = percentOff;
        ValidFrom     = validFrom;
        ValidUntil    = validUntil;
        MinimumBasket = minimumBasket;
        MaxUses       = maxUses > 0 ? maxUses : throw new ArgumentOutOfRangeException(nameof(maxUses), maxUses, "A promo code is usable at least once.");
    }

    public string Code { get; }

    public decimal PercentOff { get; }

    public DateOnly ValidFrom { get; }

    public DateOnly ValidUntil { get; }

    public decimal MinimumBasket { get; }

    public int MaxUses { get; }

    public bool AppliesTo(Money basket, DateOnly on) => on >= ValidFrom && on <= ValidUntil && basket.Amount >= MinimumBasket;

}
