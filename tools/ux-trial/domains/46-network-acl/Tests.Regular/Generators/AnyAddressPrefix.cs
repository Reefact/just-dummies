using System;

using JustDummies;

namespace NetworkAcl.Domain;

/// <summary>
///     A generator of arbitrary <see cref="AddressPrefix" /> values of either family. The scaffold only knew the IPv4
///     constructor, so this one is written by hand: the host bits are cleared from a drawn address, which is what
///     makes every prefix valid by construction.
/// </summary>
public sealed class AnyAddressPrefix : IAny<AddressPrefix> {

    private readonly IpVersion? _version;
    private readonly int?       _minLength;
    private readonly int?       _maxLength;

    public AnyAddressPrefix()
        : this(null, null, null) { }

    private AnyAddressPrefix(IpVersion? version, int? minLength, int? maxLength) {
        _version   = version;
        _minLength = minLength;
        _maxLength = maxLength;
    }

    public AnyAddressPrefix OfVersion(IpVersion version) {
        return new AnyAddressPrefix(version, _minLength, _maxLength);
    }

    public AnyAddressPrefix WithLengthBetween(int min, int max) {
        return new AnyAddressPrefix(_version, min, max);
    }

    public AddressPrefix Generate() {
        IAny<IpVersion> versions = _version is { } version ? Any.Fixed(version) : Any.Enum<IpVersion>();

        return versions.Then(Draw).Generate();
    }

    private IAny<AddressPrefix> Draw(IpVersion version) {
        int bits = version == IpVersion.V4 ? 32 : 128;

        IAny<int> lengths = Any.Int32().Between(Math.Min(_minLength ?? 0, bits), Math.Min(_maxLength ?? bits, bits));

        if (version == IpVersion.V4) {
            return Any.Combine(Any.UInt32(), lengths, (address, length) => {
                int hostBits = 32 - length;

                return new AddressPrefix(new Ipv4Address((uint)(((ulong)address >> hostBits) << hostBits)), length);
            });
        }

        return Any.Combine(Any.UInt128(), lengths, (address, length) => {
            int hostBits = 128 - length;

            UInt128 network = hostBits == 128 ? UInt128.Zero : (address >> hostBits) << hostBits;

            return new AddressPrefix(new Ipv6Address(network), length);
        });
    }

}
