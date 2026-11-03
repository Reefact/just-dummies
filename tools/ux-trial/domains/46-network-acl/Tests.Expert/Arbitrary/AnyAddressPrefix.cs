using JustDummies;

using NetworkAcl.Domain;

namespace NetworkAcl.Tests.Expert.Arbitrary;

/// <summary>
///     Arbitrary <see cref="AddressPrefix" /> values of either family. Written by hand: the scaffold only saw the IPv4
///     constructor and could not express "host bits are zero", which ties the address to the length.
///     The network is drawn as a whole address and its host bits are cleared, so the prefix is valid by construction.
/// </summary>
public sealed class AnyAddressPrefix : IAny<AddressPrefix> {

    private IpVersion? _version;
    private int?       _length;

    public AnyAddressPrefix OfFamily(IpVersion version) {
        AnyAddressPrefix copy = (AnyAddressPrefix)MemberwiseClone();
        copy._version = version;

        return copy;
    }

    public AnyAddressPrefix WithLength(int length) {
        AnyAddressPrefix copy = (AnyAddressPrefix)MemberwiseClone();
        copy._length = length;

        return copy;
    }

    public AddressPrefix Generate() {
        IpVersion version = _version ?? Any.Enum<IpVersion>().Generate();

        return version == IpVersion.V4 ? DrawIpv4() : DrawIpv6();
    }

    private AddressPrefix DrawIpv4() {
        int  length = _length ?? Any.Int32().Between(0, 32).Generate();
        uint bits   = Any.UInt32().Generate();
        uint host   = length == 0 ? uint.MaxValue : uint.MaxValue >> length;   // the host bits of a /length

        return new AddressPrefix(new Ipv4Address(bits & ~host), length);
    }

    private AddressPrefix DrawIpv6() {
        int     length = _length ?? Any.Int32().Between(0, 128).Generate();
        UInt128 bits   = Any.UInt128().Generate();
        UInt128 host   = length == 0 ? UInt128.MaxValue : UInt128.MaxValue >> length;

        return new AddressPrefix(new Ipv6Address(bits & ~host), length);
    }

}
