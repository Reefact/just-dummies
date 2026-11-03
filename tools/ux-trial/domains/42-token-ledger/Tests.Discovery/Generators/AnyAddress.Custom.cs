using JustDummies;

namespace TokenLedger.Domain;

public sealed partial class AnyAddress {

    private const string ZeroText = "0x0000000000000000000000000000000000000000";

    /// <summary>An address other than the zero address, which owns no account.</summary>
    public AnyAddress NonZero() {
        return WithValue(value => value.DifferentFrom(ZeroText));
    }

    /// <summary>An address different from <paramref name="other" />.</summary>
    public AnyAddress DifferentFromAddress(Address other) {
        return WithValue(value => value.DifferentFrom(other.Value));
    }

}
