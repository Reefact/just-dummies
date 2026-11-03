// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Account --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace CarbonLedger.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Account" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAccount : IAny<Account> {

    private IAny<string>         _accountId;
    private IAny<Holder>         _holder;
    private IAny<AccountKind>    _kind;
    private IAny<DateTimeOffset> _openedOn;

    private AnyPattern           _accountIdRecipe;
    private AnyHolder            _holderRecipe;
    private AnyEnum<AccountKind> _kindRecipe;
    private AnyDateTimeOffset    _openedOnRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAccount()
        : this(accountId: AnyValidAccountId(),
               holder:    AnyValidHolder(),
               kind:      AnyValidKind(),
               openedOn:  AnyValidOpenedOn()) { }

    private static AnyPattern AnyValidAccountId() {
        // The six digits only: Generate() prefixes them with the drawn holder's country code (the tie the constructor enforces).
        return Any.StringMatching("[0-9]{6}");
    }

    private static AnyHolder AnyValidHolder() {
        return new AnyHolder();
    }

    private static AnyEnum<AccountKind> AnyValidKind() {
        return Any.Enum<AccountKind>();
    }

    private static AnyDateTimeOffset AnyValidOpenedOn() {
        return Any.DateTimeOffset();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyAccount(AnyPattern           accountId,
                       AnyHolder            holder,
                       AnyEnum<AccountKind> kind,
                       AnyDateTimeOffset    openedOn) {
        _accountId       = accountId;
        _accountIdRecipe = accountId;
        _holder          = holder;
        _holderRecipe    = holder;
        _kind            = kind;
        _kindRecipe      = kind;
        _openedOn        = openedOn;
        _openedOnRecipe  = openedOn;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyAccount(AnyAccount from) {
        _accountId       = from._accountId;
        _accountIdRecipe = from._accountIdRecipe;
        _holder          = from._holder;
        _holderRecipe    = from._holderRecipe;
        _kind            = from._kind;
        _kindRecipe      = from._kindRecipe;
        _openedOn        = from._openedOn;
        _openedOnRecipe  = from._openedOnRecipe;
    }

    /// <summary>Pins <c>accountId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAccount WithAccountId(string value) {
        AnyAccount copy = new AnyAccount(this);

        copy._accountId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>accountId</c> is drawn from — <c>AnyValidAccountId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAccount WithAccountId(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAccount copy = new AnyAccount(this);

        copy._accountIdRecipe = configure(_accountIdRecipe);
        copy._accountId       = copy._accountIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>holder</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAccount WithHolder(Holder value) {
        AnyAccount copy = new AnyAccount(this);

        copy._holder = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>holder</c> is drawn from — <c>AnyValidHolder()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyHolder, AnyHolder&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAccount WithHolder(Func<AnyHolder, AnyHolder> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAccount copy = new AnyAccount(this);

        copy._holderRecipe = configure(_holderRecipe);
        copy._holder       = copy._holderRecipe;

        return copy;
    }

    /// <summary>Pins <c>kind</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAccount WithKind(AccountKind value) {
        AnyAccount copy = new AnyAccount(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kind</c> is drawn from — <c>AnyValidKind()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;AccountKind&gt;, AnyEnum&lt;AccountKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAccount WithKind(Func<AnyEnum<AccountKind>, AnyEnum<AccountKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAccount copy = new AnyAccount(this);

        copy._kindRecipe = configure(_kindRecipe);
        copy._kind       = copy._kindRecipe;

        return copy;
    }

    /// <summary>Pins <c>openedOn</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAccount WithOpenedOn(DateTimeOffset value) {
        AnyAccount copy = new AnyAccount(this);

        copy._openedOn = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>openedOn</c> is drawn from — <c>AnyValidOpenedOn()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTimeOffset, AnyDateTimeOffset&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAccount WithOpenedOn(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAccount copy = new AnyAccount(this);

        copy._openedOnRecipe = configure(_openedOnRecipe);
        copy._openedOn       = copy._openedOnRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Account" />.</summary>
    public Account Generate() {
        Holder holder = _holder.Generate();

        return new Account(holder.CountryCode + "-" + _accountId.Generate(),
                           holder,
                           _kind.Generate(),
                           _openedOn.Generate());
    }

}
