// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TokenPolicy --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Identity.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TokenPolicy" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTokenPolicy : IAny<TokenPolicy> {

    private readonly IAny<TimeSpan> _maximumLifetime;
    private readonly IAny<int>      _maximumScopes;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTokenPolicy()
        : this(maximumLifetime: AnyValidMaximumLifetime(),
               maximumScopes:   AnyValidMaximumScopes()) { }

    private static AnyTimeSpan AnyValidMaximumLifetime() {
        return Any.TimeSpan().Positive().LessThanOrEqualTo(TokenPolicy.HardMaximumLifetime);
    }

    private static AnyInt32 AnyValidMaximumScopes() {
        return Any.Int32().Positive().LessThanOrEqualTo(20);
    }

    private AnyTokenPolicy(IAny<TimeSpan> maximumLifetime,
                           IAny<int>      maximumScopes) {
        _maximumLifetime = maximumLifetime;
        _maximumScopes   = maximumScopes;
    }

    /// <summary>Pins <c>maximumLifetime</c> to a fixed value.</summary>
    public AnyTokenPolicy WithMaximumLifetime(TimeSpan value) {
        return new AnyTokenPolicy(new FixedValue<TimeSpan>(value), _maximumScopes);
    }

    /// <summary>Draws <c>maximumLifetime</c> from <c>AnyValidMaximumLifetime()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTokenPolicy WithMaximumLifetime(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        return new AnyTokenPolicy(configure(AnyValidMaximumLifetime()), _maximumScopes);
    }

    /// <summary>Pins <c>maximumScopes</c> to a fixed value.</summary>
    public AnyTokenPolicy WithMaximumScopes(int value) {
        return new AnyTokenPolicy(_maximumLifetime, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>maximumScopes</c> from <c>AnyValidMaximumScopes()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTokenPolicy WithMaximumScopes(Func<AnyInt32, AnyInt32> configure) {
        return new AnyTokenPolicy(_maximumLifetime, configure(AnyValidMaximumScopes()));
    }

    /// <summary>Produces one arbitrary <see cref="TokenPolicy" />.</summary>
    public TokenPolicy Generate() {
        return new TokenPolicy(_maximumLifetime.Generate(),
                               _maximumScopes.Generate());
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
