namespace Payments.Domain;

/// <summary>The bank's tariff: standard transfers are free up to a threshold, instant ones cost a flat fee plus a rate.</summary>
public sealed class FeePolicy {

    public FeePolicy(decimal instantFlatFee, decimal instantRate, decimal freeStandardThreshold, decimal standardRate) {
        ArgumentOutOfRangeException.ThrowIfNegative(instantFlatFee);
        ValidateRate(instantRate, nameof(instantRate));
        ValidateRate(standardRate, nameof(standardRate));
        ArgumentOutOfRangeException.ThrowIfNegative(freeStandardThreshold);
        InstantFlatFee        = instantFlatFee;
        InstantRate           = instantRate;
        FreeStandardThreshold = freeStandardThreshold;
        StandardRate          = standardRate;
    }

    public decimal InstantFlatFee { get; }
    public decimal InstantRate { get; }
    public decimal FreeStandardThreshold { get; }
    public decimal StandardRate { get; }

    public decimal FeeFor(CreditTransfer transfer) {
        decimal amount = transfer.Amount.Amount;
        decimal fee = transfer.Priority switch {
            TransferPriority.Instant => InstantFlatFee + amount * InstantRate,
            _ => amount <= FreeStandardThreshold ? 0m : (amount - FreeStandardThreshold) * StandardRate
        };

        return decimal.Round(fee, 2, MidpointRounding.AwayFromZero);
    }

    private static void ValidateRate(decimal rate, string parameterName) {
        if (rate < 0m || rate > 0.05m) {
            throw new ArgumentOutOfRangeException(parameterName, rate, "A rate lies between 0 and 5 %.");
        }
    }

}
