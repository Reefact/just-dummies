// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PostLogoutRedirects --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Identity.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PostLogoutRedirects" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPostLogoutRedirects : IAny<PostLogoutRedirects> {

    private readonly IAny<IReadOnlyCollection<RedirectUri>> _uris;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPostLogoutRedirects()
        : this(uris: AnyValidUris()) { }

    private static AnyList<RedirectUri> AnyValidUris() {
        return Any.ListOf(new AnyRedirectUri());
    }

    private AnyPostLogoutRedirects(IAny<IReadOnlyCollection<RedirectUri>> uris) {
        _uris = uris;
    }

    /// <summary>Pins <c>uris</c> to a fixed value.</summary>
    public AnyPostLogoutRedirects WithUris(IReadOnlyCollection<RedirectUri> value) {
        return new AnyPostLogoutRedirects(new FixedValue<IReadOnlyCollection<RedirectUri>>(value));
    }

    /// <summary>Draws <c>uris</c> from <c>AnyValidUris()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPostLogoutRedirects WithUris(Func<AnyList<RedirectUri>, AnyList<RedirectUri>> configure) {
        return new AnyPostLogoutRedirects(configure(AnyValidUris()));
    }

    /// <summary>Produces one arbitrary <see cref="PostLogoutRedirects" />.</summary>
    public PostLogoutRedirects Generate() {
        return new PostLogoutRedirects(_uris.Generate());
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
