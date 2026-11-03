// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Party --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;
using Payments.Domain;

namespace HealthcareClaims.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Party" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyParty : IAny<Party> {

    private IAny<string> _name;
    private IAny<Iban>   _account;
    private IAny<Bic?>   _agent;

    private AnyString _nameRecipe;
    private AnyIban   _accountRecipe;
    private AnyBic    _agentRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyParty()
        : this(name:    AnyValidName(),
               account: AnyValidAccount(),
               agent:   AnyValidAgent()) { }

    private static AnyString AnyValidName() {
        return Any.String().Alpha().WithLengthBetween(1, 70);
    }

    private static AnyIban AnyValidAccount() {
        // A party's agent must be in its account's country: both bases are French, so the rule holds by construction.
        return new AnyIban().InCountry("FR");
    }

    private static AnyBic AnyValidAgent() {
        return new AnyBic().InCountry("FR");
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyParty(AnyString name,
                     AnyIban   account,
                     AnyBic    agent) {
        _name          = name;
        _nameRecipe    = name;
        _account       = account;
        _accountRecipe = account;
        _agent         = agent.OrNull();
        _agentRecipe   = agent;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyParty(AnyParty from) {
        _name          = from._name;
        _nameRecipe    = from._nameRecipe;
        _account       = from._account;
        _accountRecipe = from._accountRecipe;
        _agent         = from._agent;
        _agentRecipe   = from._agentRecipe;
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyParty WithName(string value) {
        AnyParty copy = new AnyParty(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>name</c> is drawn from — <c>AnyValidName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyParty WithName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyParty copy = new AnyParty(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Pins <c>account</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyParty WithAccount(Iban value) {
        AnyParty copy = new AnyParty(this);

        copy._account = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>account</c> is drawn from — <c>AnyValidAccount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyIban, AnyIban&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyParty WithAccount(Func<AnyIban, AnyIban> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyParty copy = new AnyParty(this);

        copy._accountRecipe = configure(_accountRecipe);
        copy._account       = copy._accountRecipe;

        return copy;
    }

    /// <summary>Pins <c>agent</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyParty WithAgent(Bic? value) {
        AnyParty copy = new AnyParty(this);

        copy._agent = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>agent</c> is drawn from — <c>AnyValidAgent()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBic, AnyBic&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyParty WithAgent(Func<AnyBic, AnyBic> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyParty copy = new AnyParty(this);

        copy._agentRecipe = configure(_agentRecipe);
        copy._agent       = copy._agentRecipe;

        return copy;
    }

    /// <summary>Pins <c>agent</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyParty WithoutAgent() {
        AnyParty copy = new AnyParty(this);

        copy._agent = Any.Fixed<Bic?>(null);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Party" />.</summary>
    public Party Generate() {
        return new Party(_name.Generate(),
                         _account.Generate(),
                         _agent.Generate());
    }

}
