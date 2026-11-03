namespace Trading.Domain;

/// <summary>Shorthands for the values the order tests pin: written once, beside the generator, so a test says what it chose and nothing more.</summary>
public sealed partial class AnyOrder {

    public AnyOrder WithShares(int shares) {
        return WithQuantity(new Quantity(shares));
    }

    public AnyOrder WithLimitAt(decimal amount) {
        return WithLimit(new Price(amount));
    }

    public AnyOrder WithStopAt(decimal amount) {
        return WithStop(new Price(amount));
    }

    public AnyOrder WithTickOf(decimal increment) {
        return WithTick(new TickSize(increment));
    }

    /// <summary>Any time in force but fill-or-kill, the one that refuses a partial fill.</summary>
    public AnyOrder AllowingPartialFills() {
        return WithTimeInForce(timeInForce => timeInForce.Except(TimeInForce.FillOrKill));
    }

}
