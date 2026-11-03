// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CarbonRegistryLedger --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace CarbonLedger.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CarbonRegistryLedger" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCarbonRegistryLedger : IAny<CarbonRegistryLedger> {

    private IAny<string>  _registry;
    private IAny<Account> _issuanceAccount;

    private AnyPattern _registryRecipe;
    private AnyAccount _issuanceAccountRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCarbonRegistryLedger()
        : this(registry:        AnyValidRegistry(),
               issuanceAccount: AnyValidIssuanceAccount()) { }

    private static AnyPattern AnyValidRegistry() {
        return Any.StringMatching("[A-Z]{2,5}");
    }

    private static AnyAccount AnyValidIssuanceAccount() {
        return new AnyAccount().WithKind(AccountKind.Issuance);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCarbonRegistryLedger(AnyPattern registry,
                                    AnyAccount issuanceAccount) {
        _registry              = registry;
        _registryRecipe        = registry;
        _issuanceAccount       = issuanceAccount;
        _issuanceAccountRecipe = issuanceAccount;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCarbonRegistryLedger(AnyCarbonRegistryLedger from) {
        _registry              = from._registry;
        _registryRecipe        = from._registryRecipe;
        _issuanceAccount       = from._issuanceAccount;
        _issuanceAccountRecipe = from._issuanceAccountRecipe;
    }

    /// <summary>Pins <c>registry</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCarbonRegistryLedger WithRegistry(string value) {
        AnyCarbonRegistryLedger copy = new AnyCarbonRegistryLedger(this);

        copy._registry = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>registry</c> is drawn from — <c>AnyValidRegistry()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCarbonRegistryLedger WithRegistry(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCarbonRegistryLedger copy = new AnyCarbonRegistryLedger(this);

        copy._registryRecipe = configure(_registryRecipe);
        copy._registry       = copy._registryRecipe;

        return copy;
    }

    /// <summary>Pins <c>issuanceAccount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCarbonRegistryLedger WithIssuanceAccount(Account value) {
        AnyCarbonRegistryLedger copy = new AnyCarbonRegistryLedger(this);

        copy._issuanceAccount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>issuanceAccount</c> is drawn from — <c>AnyValidIssuanceAccount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyAccount, AnyAccount&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCarbonRegistryLedger WithIssuanceAccount(Func<AnyAccount, AnyAccount> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCarbonRegistryLedger copy = new AnyCarbonRegistryLedger(this);

        copy._issuanceAccountRecipe = configure(_issuanceAccountRecipe);
        copy._issuanceAccount       = copy._issuanceAccountRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CarbonRegistryLedger" />.</summary>
    public CarbonRegistryLedger Generate() {
        return new CarbonRegistryLedger(_registry.Generate(),
                                        _issuanceAccount.Generate());
    }

}
