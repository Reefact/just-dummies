// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate AccessToken --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace Identity.Domain;

/// <summary>
///     A generator of arbitrary <see cref="AccessToken" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAccessToken : IAny<AccessToken> {

    private IAny<Guid>                       _tokenId;
    private IAny<ClientId>                   _clientId;
    private IAny<Guid>                       _subjectId;
    private IAny<IReadOnlyCollection<Scope>> _scopes;
    private AnyOrderedPair<DateTimeOffset>   _issuedAtAndExpiresAt;

    private AnyGuid           _tokenIdRecipe;
    private AnyClientId       _clientIdRecipe;
    private AnyGuid           _subjectIdRecipe;
    private AnyList<Scope>    _scopesRecipe;
    private AnyDateTimeOffset _issuedAtRecipe;
    private AnyDateTimeOffset _expiresAtRecipe;

    private DateTimeOffset? _issuedAtPin;
    private DateTimeOffset? _expiresAtPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAccessToken()
        : this(tokenId:   AnyValidTokenId(),
               clientId:  AnyValidClientId(),
               subjectId: AnyValidSubjectId(),
               scopes:    AnyValidScopes(),
               issuedAt:  AnyValidIssuedAt(),
               expiresAt: AnyValidExpiresAt()) { }

    private static AnyGuid AnyValidTokenId() {
        return Any.Guid().NonEmpty();
    }

    private static AnyClientId AnyValidClientId() {
        return new AnyClientId();
    }

    private static AnyGuid AnyValidSubjectId() {
        return Any.Guid().NonEmpty();
    }

    private static AnyList<Scope> AnyValidScopes() {
        return Any.ListOf(new AnyScope()).NonEmpty();
    }

    private static AnyDateTimeOffset AnyValidIssuedAt() {
        return Any.DateTimeOffset().Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    private static AnyDateTimeOffset AnyValidExpiresAt() {
        return Any.DateTimeOffset().Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyAccessToken(AnyGuid           tokenId,
                           AnyClientId       clientId,
                           AnyGuid           subjectId,
                           AnyList<Scope>    scopes,
                           AnyDateTimeOffset issuedAt,
                           AnyDateTimeOffset expiresAt) {
        _tokenId              = tokenId;
        _tokenIdRecipe        = tokenId;
        _clientId             = clientId;
        _clientIdRecipe       = clientId;
        _subjectId            = subjectId;
        _subjectIdRecipe      = subjectId;
        _scopes               = scopes;
        _scopesRecipe         = scopes;
        _issuedAtRecipe       = issuedAt;
        _expiresAtRecipe      = expiresAt;
        _issuedAtAndExpiresAt = IssuedAtAndExpiresAt();
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyAccessToken(AnyAccessToken from) {
        _tokenId              = from._tokenId;
        _tokenIdRecipe        = from._tokenIdRecipe;
        _clientId             = from._clientId;
        _clientIdRecipe       = from._clientIdRecipe;
        _subjectId            = from._subjectId;
        _subjectIdRecipe      = from._subjectIdRecipe;
        _scopes               = from._scopes;
        _scopesRecipe         = from._scopesRecipe;
        _issuedAtAndExpiresAt = from._issuedAtAndExpiresAt;
        _issuedAtRecipe       = from._issuedAtRecipe;
        _issuedAtPin          = from._issuedAtPin;
        _expiresAtRecipe      = from._expiresAtRecipe;
        _expiresAtPin         = from._expiresAtPin;
    }

    /// <summary>Draws <c>issuedAt</c> and <c>expiresAt</c> together, <c>issuedAt</c> always before <c>expiresAt</c> as the constructor requires, each from its current recipe and pin.</summary>
    private AnyOrderedPair<DateTimeOffset> IssuedAtAndExpiresAt() {
        AnyOrderedPair<DateTimeOffset> pair = Any.OrderedPair(_issuedAtRecipe, _expiresAtRecipe, strict: true);

        if (_issuedAtPin is { } issuedAt) { pair = pair.WithFirst(issuedAt); }
        if (_expiresAtPin is { } expiresAt) { pair = pair.WithSecond(expiresAt); }

        return pair;
    }

    /// <summary>Pins <c>tokenId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAccessToken WithTokenId(Guid value) {
        AnyAccessToken copy = new AnyAccessToken(this);

        copy._tokenId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>tokenId</c> is drawn from — <c>AnyValidTokenId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGuid, AnyGuid&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAccessToken WithTokenId(Func<AnyGuid, AnyGuid> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAccessToken copy = new AnyAccessToken(this);

        copy._tokenIdRecipe = configure(_tokenIdRecipe);
        copy._tokenId       = copy._tokenIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>clientId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAccessToken WithClientId(ClientId value) {
        AnyAccessToken copy = new AnyAccessToken(this);

        copy._clientId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>clientId</c> is drawn from — <c>AnyValidClientId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyClientId, AnyClientId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAccessToken WithClientId(Func<AnyClientId, AnyClientId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAccessToken copy = new AnyAccessToken(this);

        copy._clientIdRecipe = configure(_clientIdRecipe);
        copy._clientId       = copy._clientIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>subjectId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAccessToken WithSubjectId(Guid value) {
        AnyAccessToken copy = new AnyAccessToken(this);

        copy._subjectId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>subjectId</c> is drawn from — <c>AnyValidSubjectId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGuid, AnyGuid&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAccessToken WithSubjectId(Func<AnyGuid, AnyGuid> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAccessToken copy = new AnyAccessToken(this);

        copy._subjectIdRecipe = configure(_subjectIdRecipe);
        copy._subjectId       = copy._subjectIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>scopes</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAccessToken WithScopes(IReadOnlyCollection<Scope> value) {
        AnyAccessToken copy = new AnyAccessToken(this);

        copy._scopes = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>scopes</c> is drawn from — <c>AnyValidScopes()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;Scope&gt;, AnyList&lt;Scope&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAccessToken WithScopes(Func<AnyList<Scope>, AnyList<Scope>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAccessToken copy = new AnyAccessToken(this);

        copy._scopesRecipe = configure(_scopesRecipe);
        copy._scopes       = copy._scopesRecipe;

        return copy;
    }

    /// <summary>Pins <c>issuedAt</c> to a fixed value, which bounds <c>expiresAt</c>; its recipe is kept for a later refinement.</summary>
    public AnyAccessToken WithIssuedAt(DateTimeOffset value) {
        AnyAccessToken copy = new AnyAccessToken(this);

        copy._issuedAtPin          = value;
        copy._issuedAtAndExpiresAt = copy.IssuedAtAndExpiresAt();

        return copy;
    }

    /// <summary>Refines the recipe <c>issuedAt</c> is drawn from — <c>AnyValidIssuedAt()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>expiresAt</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTimeOffset, AnyDateTimeOffset&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAccessToken WithIssuedAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAccessToken copy = new AnyAccessToken(this);

        copy._issuedAtRecipe       = configure(_issuedAtRecipe);
        copy._issuedAtPin          = null;
        copy._issuedAtAndExpiresAt = copy.IssuedAtAndExpiresAt();

        return copy;
    }

    /// <summary>Pins <c>expiresAt</c> to a fixed value, which bounds <c>issuedAt</c>; its recipe is kept for a later refinement.</summary>
    public AnyAccessToken WithExpiresAt(DateTimeOffset value) {
        AnyAccessToken copy = new AnyAccessToken(this);

        copy._expiresAtPin         = value;
        copy._issuedAtAndExpiresAt = copy.IssuedAtAndExpiresAt();

        return copy;
    }

    /// <summary>Refines the recipe <c>expiresAt</c> is drawn from — <c>AnyValidExpiresAt()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>issuedAt</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTimeOffset, AnyDateTimeOffset&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAccessToken WithExpiresAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAccessToken copy = new AnyAccessToken(this);

        copy._expiresAtRecipe      = configure(_expiresAtRecipe);
        copy._expiresAtPin         = null;
        copy._issuedAtAndExpiresAt = copy.IssuedAtAndExpiresAt();

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="AccessToken" />.</summary>
    public AccessToken Generate() {
        (DateTimeOffset issuedAt, DateTimeOffset expiresAt) = _issuedAtAndExpiresAt.Generate();

        // The constructor also caps the lifetime at 24 hours, which the pair cannot express: cap it here.
        if (expiresAt - issuedAt > TokenPolicy.HardMaximumLifetime) { expiresAt = issuedAt + TokenPolicy.HardMaximumLifetime; }

        return new AccessToken(_tokenId.Generate(),
                               _clientId.Generate(),
                               _subjectId.Generate(),
                               _scopes.Generate(),
                               issuedAt,
                               expiresAt);
    }

}
