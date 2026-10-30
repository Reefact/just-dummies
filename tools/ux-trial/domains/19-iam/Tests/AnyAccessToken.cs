// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate AccessToken --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

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

    private readonly IAny<Guid>                       _tokenId;
    private readonly IAny<ClientId>                   _clientId;
    private readonly IAny<Guid>                       _subjectId;
    private readonly IAny<IReadOnlyCollection<Scope>> _scopes;
    private readonly IAny<DateTimeOffset>             _issuedAt;
    private readonly IAny<DateTimeOffset>             _expiresAt;

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

    // expiresAt must follow issuedAt by at most 24 hours. The two bases are independent, so they
    // draw from two adjacent 12-hour windows: every pair is ordered and less than a day apart.
    private static readonly DateTimeOffset WindowStart = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

    private static AnyDateTimeOffset AnyValidIssuedAt() {
        return Any.DateTimeOffset().AfterOrEqualTo(WindowStart).Before(WindowStart.AddHours(12));
    }

    private static AnyDateTimeOffset AnyValidExpiresAt() {
        return Any.DateTimeOffset().AfterOrEqualTo(WindowStart.AddHours(12)).Before(WindowStart.AddHours(24));
    }

    private AnyAccessToken(IAny<Guid>                       tokenId,
                           IAny<ClientId>                   clientId,
                           IAny<Guid>                       subjectId,
                           IAny<IReadOnlyCollection<Scope>> scopes,
                           IAny<DateTimeOffset>             issuedAt,
                           IAny<DateTimeOffset>             expiresAt) {
        _tokenId   = tokenId;
        _clientId  = clientId;
        _subjectId = subjectId;
        _scopes    = scopes;
        _issuedAt  = issuedAt;
        _expiresAt = expiresAt;
    }

    /// <summary>Pins <c>tokenId</c> to a fixed value.</summary>
    public AnyAccessToken WithTokenId(Guid value) {
        return new AnyAccessToken(new FixedValue<Guid>(value), _clientId, _subjectId, _scopes, _issuedAt, _expiresAt);
    }

    /// <summary>Draws <c>tokenId</c> from <c>AnyValidTokenId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAccessToken WithTokenId(Func<AnyGuid, AnyGuid> configure) {
        return new AnyAccessToken(configure(AnyValidTokenId()), _clientId, _subjectId, _scopes, _issuedAt, _expiresAt);
    }

    /// <summary>Pins <c>clientId</c> to a fixed value.</summary>
    public AnyAccessToken WithClientId(ClientId value) {
        return new AnyAccessToken(_tokenId, new FixedValue<ClientId>(value), _subjectId, _scopes, _issuedAt, _expiresAt);
    }

    /// <summary>Draws <c>clientId</c> from <c>AnyValidClientId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAccessToken WithClientId(Func<AnyClientId, AnyClientId> configure) {
        return new AnyAccessToken(_tokenId, configure(AnyValidClientId()), _subjectId, _scopes, _issuedAt, _expiresAt);
    }

    /// <summary>Pins <c>subjectId</c> to a fixed value.</summary>
    public AnyAccessToken WithSubjectId(Guid value) {
        return new AnyAccessToken(_tokenId, _clientId, new FixedValue<Guid>(value), _scopes, _issuedAt, _expiresAt);
    }

    /// <summary>Draws <c>subjectId</c> from <c>AnyValidSubjectId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAccessToken WithSubjectId(Func<AnyGuid, AnyGuid> configure) {
        return new AnyAccessToken(_tokenId, _clientId, configure(AnyValidSubjectId()), _scopes, _issuedAt, _expiresAt);
    }

    /// <summary>Pins <c>scopes</c> to a fixed value.</summary>
    public AnyAccessToken WithScopes(IReadOnlyCollection<Scope> value) {
        return new AnyAccessToken(_tokenId, _clientId, _subjectId, new FixedValue<IReadOnlyCollection<Scope>>(value), _issuedAt, _expiresAt);
    }

    /// <summary>Draws <c>scopes</c> from <c>AnyValidScopes()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAccessToken WithScopes(Func<AnyList<Scope>, AnyList<Scope>> configure) {
        return new AnyAccessToken(_tokenId, _clientId, _subjectId, configure(AnyValidScopes()), _issuedAt, _expiresAt);
    }

    /// <summary>Pins <c>issuedAt</c> to a fixed value.</summary>
    public AnyAccessToken WithIssuedAt(DateTimeOffset value) {
        return new AnyAccessToken(_tokenId, _clientId, _subjectId, _scopes, new FixedValue<DateTimeOffset>(value), _expiresAt);
    }

    /// <summary>Draws <c>issuedAt</c> from <c>AnyValidIssuedAt()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAccessToken WithIssuedAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        return new AnyAccessToken(_tokenId, _clientId, _subjectId, _scopes, configure(AnyValidIssuedAt()), _expiresAt);
    }

    /// <summary>Pins <c>expiresAt</c> to a fixed value.</summary>
    public AnyAccessToken WithExpiresAt(DateTimeOffset value) {
        return new AnyAccessToken(_tokenId, _clientId, _subjectId, _scopes, _issuedAt, new FixedValue<DateTimeOffset>(value));
    }

    /// <summary>Draws <c>expiresAt</c> from <c>AnyValidExpiresAt()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAccessToken WithExpiresAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        return new AnyAccessToken(_tokenId, _clientId, _subjectId, _scopes, _issuedAt, configure(AnyValidExpiresAt()));
    }

    /// <summary>Produces one arbitrary <see cref="AccessToken" />.</summary>
    public AccessToken Generate() {
        return new AccessToken(_tokenId.Generate(),
                               _clientId.Generate(),
                               _subjectId.Generate(),
                               _scopes.Generate(),
                               _issuedAt.Generate(),
                               _expiresAt.Generate());
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
