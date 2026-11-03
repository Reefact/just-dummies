// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Account --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace TokenLedger.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Account" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAccount : IAny<Account> {

    private IAny<Address> _address;

    private AnyAddress _addressRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAccount()
        : this(address: AnyValidAddress()) { }

    private static AnyAddress AnyValidAddress() {
        return new AnyAddress();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyAccount(AnyAddress address) {
        _address       = address;
        _addressRecipe = address;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyAccount(AnyAccount from) {
        _address       = from._address;
        _addressRecipe = from._addressRecipe;
    }

    /// <summary>Pins <c>address</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAccount WithAddress(Address value) {
        AnyAccount copy = new AnyAccount(this);

        copy._address = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>address</c> is drawn from — <c>AnyValidAddress()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyAddress, AnyAddress&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAccount WithAddress(Func<AnyAddress, AnyAddress> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAccount copy = new AnyAccount(this);

        copy._addressRecipe = configure(_addressRecipe);
        copy._address       = copy._addressRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Account" />.</summary>
    public Account Generate() {
        return new Account(_address.Generate());
    }

}
