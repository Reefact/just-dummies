// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ClientApplication --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Identity.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ClientApplication" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyClientApplication : IAny<ClientApplication> {

    private readonly IAny<ClientId>                         _id;
    private readonly IAny<string>                           _displayName;
    private readonly IAny<IReadOnlyCollection<RedirectUri>> _redirectUris;
    private readonly IAny<IReadOnlyCollection<Scope>>       _allowedScopes;

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
        return Any.ListOf(new AnyRedirectUri()).NonEmpty();
    }

    private static AnyList<Scope> AnyValidAllowedScopes() {
        return Any.ListOf(new AnyScope()).NonEmpty();
    }

    private AnyClientApplication(IAny<ClientId>                         id,
                                 IAny<string>                           displayName,
                                 IAny<IReadOnlyCollection<RedirectUri>> redirectUris,
                                 IAny<IReadOnlyCollection<Scope>>       allowedScopes) {
        _id            = id;
        _displayName   = displayName;
        _redirectUris  = redirectUris;
        _allowedScopes = allowedScopes;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnyClientApplication WithId(ClientId value) {
        return new AnyClientApplication(new FixedValue<ClientId>(value), _displayName, _redirectUris, _allowedScopes);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyClientApplication WithId(Func<AnyClientId, AnyClientId> configure) {
        return new AnyClientApplication(configure(AnyValidId()), _displayName, _redirectUris, _allowedScopes);
    }

    /// <summary>Pins <c>displayName</c> to a fixed value.</summary>
    public AnyClientApplication WithDisplayName(string value) {
        return new AnyClientApplication(_id, new FixedValue<string>(value), _redirectUris, _allowedScopes);
    }

    /// <summary>Draws <c>displayName</c> from <c>AnyValidDisplayName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyClientApplication WithDisplayName(Func<AnyString, AnyString> configure) {
        return new AnyClientApplication(_id, configure(AnyValidDisplayName()), _redirectUris, _allowedScopes);
    }

    /// <summary>Pins <c>redirectUris</c> to a fixed value.</summary>
    public AnyClientApplication WithRedirectUris(IReadOnlyCollection<RedirectUri> value) {
        return new AnyClientApplication(_id, _displayName, new FixedValue<IReadOnlyCollection<RedirectUri>>(value), _allowedScopes);
    }

    /// <summary>Draws <c>redirectUris</c> from <c>AnyValidRedirectUris()</c>, refined by <paramref name="configure" />.</summary>
    public AnyClientApplication WithRedirectUris(Func<AnyList<RedirectUri>, AnyList<RedirectUri>> configure) {
        return new AnyClientApplication(_id, _displayName, configure(AnyValidRedirectUris()), _allowedScopes);
    }

    /// <summary>Pins <c>allowedScopes</c> to a fixed value.</summary>
    public AnyClientApplication WithAllowedScopes(IReadOnlyCollection<Scope> value) {
        return new AnyClientApplication(_id, _displayName, _redirectUris, new FixedValue<IReadOnlyCollection<Scope>>(value));
    }

    /// <summary>Draws <c>allowedScopes</c> from <c>AnyValidAllowedScopes()</c>, refined by <paramref name="configure" />.</summary>
    public AnyClientApplication WithAllowedScopes(Func<AnyList<Scope>, AnyList<Scope>> configure) {
        return new AnyClientApplication(_id, _displayName, _redirectUris, configure(AnyValidAllowedScopes()));
    }

    /// <summary>Produces one arbitrary <see cref="ClientApplication" />.</summary>
    public ClientApplication Generate() {
        return new ClientApplication(_id.Generate(),
                                     _displayName.Generate(),
                                     _redirectUris.Generate(),
                                     _allowedScopes.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
