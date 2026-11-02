// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TokenPolicy --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using Identity.Domain;
using JustDummies;

namespace Identity.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="TokenPolicy" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTokenPolicy : IAny<TokenPolicy> {

    private IAny<TimeSpan> _maximumLifetime;
    private IAny<int>      _maximumScopes;

    private AnyTimeSpan _maximumLifetimeRecipe;
    private AnyInt32    _maximumScopesRecipe;

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

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTokenPolicy(AnyTimeSpan maximumLifetime,
                           AnyInt32    maximumScopes) {
        _maximumLifetime       = maximumLifetime;
        _maximumLifetimeRecipe = maximumLifetime;
        _maximumScopes         = maximumScopes;
        _maximumScopesRecipe   = maximumScopes;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTokenPolicy(AnyTokenPolicy from) {
        _maximumLifetime       = from._maximumLifetime;
        _maximumLifetimeRecipe = from._maximumLifetimeRecipe;
        _maximumScopes         = from._maximumScopes;
        _maximumScopesRecipe   = from._maximumScopesRecipe;
    }

    /// <summary>Pins <c>maximumLifetime</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTokenPolicy WithMaximumLifetime(TimeSpan value) {
        AnyTokenPolicy copy = new AnyTokenPolicy(this);

        copy._maximumLifetime = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>maximumLifetime</c> is drawn from — <c>AnyValidMaximumLifetime()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeSpan, AnyTimeSpan&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTokenPolicy WithMaximumLifetime(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTokenPolicy copy = new AnyTokenPolicy(this);

        copy._maximumLifetimeRecipe = configure(_maximumLifetimeRecipe);
        copy._maximumLifetime       = copy._maximumLifetimeRecipe;

        return copy;
    }

    /// <summary>Pins <c>maximumScopes</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTokenPolicy WithMaximumScopes(int value) {
        AnyTokenPolicy copy = new AnyTokenPolicy(this);

        copy._maximumScopes = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>maximumScopes</c> is drawn from — <c>AnyValidMaximumScopes()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTokenPolicy WithMaximumScopes(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTokenPolicy copy = new AnyTokenPolicy(this);

        copy._maximumScopesRecipe = configure(_maximumScopesRecipe);
        copy._maximumScopes       = copy._maximumScopesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TokenPolicy" />.</summary>
    public TokenPolicy Generate() {
        return new TokenPolicy(_maximumLifetime.Generate(),
                               _maximumScopes.Generate());
    }

}
