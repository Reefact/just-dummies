// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ClientApplication --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using Identity.Domain;
using JustDummies;

namespace Identity.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="ClientApplication" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyClientApplication : IAny<ClientApplication> {

    private IAny<ClientId>                         _id;
    private IAny<string>                           _displayName;
    private IAny<IReadOnlyCollection<RedirectUri>> _redirectUris;
    private IAny<IReadOnlyCollection<Scope>>       _allowedScopes;

    private AnyClientId          _idRecipe;
    private AnyString            _displayNameRecipe;
    private AnyList<RedirectUri> _redirectUrisRecipe;
    private AnyList<Scope>       _allowedScopesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyClientApplication()
        : this(id:            AnyValidId(),
               displayName:   AnyValidDisplayName(),
               redirectUris:  AnyValidRedirectUris(),
               allowedScopes: AnyValidAllowedScopes()) { }

    private static AnyClientId AnyValidId() {
        return new AnyClientId();
    }

    private static AnyString AnyValidDisplayName() {
        return Any.String().NotBlank().WithMaxLength(80);
    }

    private static AnyList<RedirectUri> AnyValidRedirectUris() {
        return Any.ListOf(new AnyRedirectUri()).NonEmpty().Distinct();
    }

    private static AnyList<Scope> AnyValidAllowedScopes() {
        return Any.ListOf(new AnyScope()).NonEmpty();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyClientApplication(AnyClientId          id,
                                 AnyString            displayName,
                                 AnyList<RedirectUri> redirectUris,
                                 AnyList<Scope>       allowedScopes) {
        _id                  = id;
        _idRecipe            = id;
        _displayName         = displayName;
        _displayNameRecipe   = displayName;
        _redirectUris        = redirectUris;
        _redirectUrisRecipe  = redirectUris;
        _allowedScopes       = allowedScopes;
        _allowedScopesRecipe = allowedScopes;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyClientApplication(AnyClientApplication from) {
        _id                  = from._id;
        _idRecipe            = from._idRecipe;
        _displayName         = from._displayName;
        _displayNameRecipe   = from._displayNameRecipe;
        _redirectUris        = from._redirectUris;
        _redirectUrisRecipe  = from._redirectUrisRecipe;
        _allowedScopes       = from._allowedScopes;
        _allowedScopesRecipe = from._allowedScopesRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyClientApplication WithId(ClientId value) {
        AnyClientApplication copy = new AnyClientApplication(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyClientId, AnyClientId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyClientApplication WithId(Func<AnyClientId, AnyClientId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyClientApplication copy = new AnyClientApplication(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>displayName</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyClientApplication WithDisplayName(string value) {
        AnyClientApplication copy = new AnyClientApplication(this);

        copy._displayName = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>displayName</c> is drawn from — <c>AnyValidDisplayName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyClientApplication WithDisplayName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyClientApplication copy = new AnyClientApplication(this);

        copy._displayNameRecipe = configure(_displayNameRecipe);
        copy._displayName       = copy._displayNameRecipe;

        return copy;
    }

    /// <summary>Pins <c>redirectUris</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyClientApplication WithRedirectUris(IReadOnlyCollection<RedirectUri> value) {
        AnyClientApplication copy = new AnyClientApplication(this);

        copy._redirectUris = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>redirectUris</c> is drawn from — <c>AnyValidRedirectUris()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;RedirectUri&gt;, AnyList&lt;RedirectUri&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyClientApplication WithRedirectUris(Func<AnyList<RedirectUri>, AnyList<RedirectUri>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyClientApplication copy = new AnyClientApplication(this);

        copy._redirectUrisRecipe = configure(_redirectUrisRecipe);
        copy._redirectUris       = copy._redirectUrisRecipe;

        return copy;
    }

    /// <summary>Pins <c>allowedScopes</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyClientApplication WithAllowedScopes(IReadOnlyCollection<Scope> value) {
        AnyClientApplication copy = new AnyClientApplication(this);

        copy._allowedScopes = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>allowedScopes</c> is drawn from — <c>AnyValidAllowedScopes()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;Scope&gt;, AnyList&lt;Scope&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyClientApplication WithAllowedScopes(Func<AnyList<Scope>, AnyList<Scope>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyClientApplication copy = new AnyClientApplication(this);

        copy._allowedScopesRecipe = configure(_allowedScopesRecipe);
        copy._allowedScopes       = copy._allowedScopesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ClientApplication" />.</summary>
    public ClientApplication Generate() {
        return new ClientApplication(_id.Generate(),
                                     _displayName.Generate(),
                                     _redirectUris.Generate(),
                                     _allowedScopes.Generate());
    }

}
