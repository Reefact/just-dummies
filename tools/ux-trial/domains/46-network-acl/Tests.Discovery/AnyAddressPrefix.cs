// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate AddressPrefix --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace NetworkAcl.Domain;

/// <summary>
///     A generator of arbitrary <see cref="AddressPrefix" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAddressPrefix : IAny<AddressPrefix> {

    private IAny<Ipv4Address> _network;
    private IAny<int>         _prefixLength;

    private AnyIpv4Address _networkRecipe;
    private AnyInt32       _prefixLengthRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAddressPrefix()
        : this(network:      AnyValidNetwork(),
               prefixLength: AnyValidPrefixLength()) { }

    private static AnyIpv4Address AnyValidNetwork() {
        return new AnyIpv4Address();
    }

    private static AnyInt32 AnyValidPrefixLength() {
        return Any.Int32().Between(0, Ipv4Address.BitLength);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyAddressPrefix(AnyIpv4Address network,
                             AnyInt32       prefixLength) {
        _network            = network;
        _networkRecipe      = network;
        _prefixLength       = prefixLength;
        _prefixLengthRecipe = prefixLength;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyAddressPrefix(AnyAddressPrefix from) {
        _network            = from._network;
        _networkRecipe      = from._networkRecipe;
        _prefixLength       = from._prefixLength;
        _prefixLengthRecipe = from._prefixLengthRecipe;
    }

    /// <summary>Pins <c>network</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAddressPrefix WithNetwork(Ipv4Address value) {
        AnyAddressPrefix copy = new AnyAddressPrefix(this);

        copy._network = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>network</c> is drawn from — <c>AnyValidNetwork()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyIpv4Address, AnyIpv4Address&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAddressPrefix WithNetwork(Func<AnyIpv4Address, AnyIpv4Address> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAddressPrefix copy = new AnyAddressPrefix(this);

        copy._networkRecipe = configure(_networkRecipe);
        copy._network       = copy._networkRecipe;

        return copy;
    }

    /// <summary>Pins <c>prefixLength</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAddressPrefix WithPrefixLength(int value) {
        AnyAddressPrefix copy = new AnyAddressPrefix(this);

        copy._prefixLength = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>prefixLength</c> is drawn from — <c>AnyValidPrefixLength()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAddressPrefix WithPrefixLength(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAddressPrefix copy = new AnyAddressPrefix(this);

        copy._prefixLengthRecipe = configure(_prefixLengthRecipe);
        copy._prefixLength       = copy._prefixLengthRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="AddressPrefix" />.</summary>
    public AddressPrefix Generate() {
        int  length = _prefixLength.Generate();
        uint mask   = length == 0 ? 0u : uint.MaxValue << (Ipv4Address.BitLength - length);

        // Host bits past the length must be zero: keep only the network part of the drawn address.
        return new AddressPrefix(new Ipv4Address(_network.Generate().Value & mask),
                                 length);
    }

}
