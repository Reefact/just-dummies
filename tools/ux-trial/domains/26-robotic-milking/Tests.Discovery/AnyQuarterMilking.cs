// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate QuarterMilking --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RoboticMilking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="QuarterMilking" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyQuarterMilking : IAny<QuarterMilking> {

    private IAny<UdderQuarter> _quarter;
    private IAny<int>          _maxAttachAttempts;

    private AnyEnum<UdderQuarter> _quarterRecipe;
    private AnyInt32              _maxAttachAttemptsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyQuarterMilking()
        : this(quarter:           AnyValidQuarter(),
               maxAttachAttempts: AnyValidMaxAttachAttempts()) { }

    private static AnyEnum<UdderQuarter> AnyValidQuarter() {
        return Any.Enum<UdderQuarter>();
    }

    private static AnyInt32 AnyValidMaxAttachAttempts() {
        return Any.Int32().Positive().LessThanOrEqualTo(5);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyQuarterMilking(AnyEnum<UdderQuarter> quarter,
                              AnyInt32              maxAttachAttempts) {
        _quarter                 = quarter;
        _quarterRecipe           = quarter;
        _maxAttachAttempts       = maxAttachAttempts;
        _maxAttachAttemptsRecipe = maxAttachAttempts;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyQuarterMilking(AnyQuarterMilking from) {
        _quarter                 = from._quarter;
        _quarterRecipe           = from._quarterRecipe;
        _maxAttachAttempts       = from._maxAttachAttempts;
        _maxAttachAttemptsRecipe = from._maxAttachAttemptsRecipe;
    }

    /// <summary>Pins <c>quarter</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyQuarterMilking WithQuarter(UdderQuarter value) {
        AnyQuarterMilking copy = new AnyQuarterMilking(this);

        copy._quarter = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>quarter</c> is drawn from — <c>AnyValidQuarter()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;UdderQuarter&gt;, AnyEnum&lt;UdderQuarter&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyQuarterMilking WithQuarter(Func<AnyEnum<UdderQuarter>, AnyEnum<UdderQuarter>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyQuarterMilking copy = new AnyQuarterMilking(this);

        copy._quarterRecipe = configure(_quarterRecipe);
        copy._quarter       = copy._quarterRecipe;

        return copy;
    }

    /// <summary>Pins <c>maxAttachAttempts</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyQuarterMilking WithMaxAttachAttempts(int value) {
        AnyQuarterMilking copy = new AnyQuarterMilking(this);

        copy._maxAttachAttempts = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>maxAttachAttempts</c> is drawn from — <c>AnyValidMaxAttachAttempts()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyQuarterMilking WithMaxAttachAttempts(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyQuarterMilking copy = new AnyQuarterMilking(this);

        copy._maxAttachAttemptsRecipe = configure(_maxAttachAttemptsRecipe);
        copy._maxAttachAttempts       = copy._maxAttachAttemptsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="QuarterMilking" />.</summary>
    public QuarterMilking Generate() {
        return new QuarterMilking(_quarter.Generate(),
                                  _maxAttachAttempts.Generate());
    }

}
