// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RedirectUri --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Identity.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RedirectUri" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRedirectUri : IAny<RedirectUri> {

    private readonly IAny<Uri> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRedirectUri()
        : this(value: AnyValidValue()) { }

    private static AnyWebUri AnyValidValue() {
        // RedirectUri.Validate: absolute, https (http only on loopback), no fragment.
        return Any.Uri().Web().UsingHttps();
    }

    private AnyRedirectUri(IAny<Uri> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyRedirectUri WithValue(Uri value) {
        return new AnyRedirectUri(new FixedValue<Uri>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRedirectUri WithValue(Func<AnyWebUri, AnyWebUri> configure) {
        return new AnyRedirectUri(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="RedirectUri" />.</summary>
    public RedirectUri Generate() {
        return new RedirectUri(_value.Generate());
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
