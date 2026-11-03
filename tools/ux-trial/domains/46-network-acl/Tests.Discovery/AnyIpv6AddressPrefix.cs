using JustDummies;

namespace NetworkAcl.Domain;

/// <summary>A generator of arbitrary IPv6 <see cref="AddressPrefix" /> values (the scaffold of <c>AddressPrefix</c> covers IPv4 only).</summary>
public sealed class AnyIpv6AddressPrefix : IAny<AddressPrefix> {

    private readonly IAny<AddressPrefix> _recipe = Any.Combine(
        Any.UInt128(),
        Any.Int32().Between(0, Ipv6Address.BitLength),
        (raw, length) => {
            UInt128 mask = length == 0 ? UInt128.Zero : UInt128.MaxValue << (Ipv6Address.BitLength - length);

            return new AddressPrefix(new Ipv6Address(raw & mask), length);
        });

    public AddressPrefix Generate() {
        return _recipe.Generate();
    }

}
