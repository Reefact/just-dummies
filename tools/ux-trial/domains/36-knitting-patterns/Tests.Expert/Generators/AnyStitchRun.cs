// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate StitchRun --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using KnittingPatterns.Domain;

namespace KnittingPatterns.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="StitchRun" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStitchRun : IAny<StitchRun> {

    private IAny<StitchKind> _kind;
    private IAny<int>        _count;

    private AnyEnum<StitchKind> _kindRecipe;
    private AnyInt32            _countRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyStitchRun()
        : this(kind:  AnyValidKind(),
               count: AnyValidCount()) { }

    private static AnyEnum<StitchKind> AnyValidKind() {
        return Any.Enum<StitchKind>();
    }

    private static AnyInt32 AnyValidCount() {
        return Any.Int32().Positive().LessThanOrEqualTo(StitchRun.MaxCount);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyStitchRun(AnyEnum<StitchKind> kind,
                         AnyInt32            count) {
        _kind        = kind;
        _kindRecipe  = kind;
        _count       = count;
        _countRecipe = count;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyStitchRun(AnyStitchRun from) {
        _kind        = from._kind;
        _kindRecipe  = from._kindRecipe;
        _count       = from._count;
        _countRecipe = from._countRecipe;
    }

    /// <summary>Pins <c>kind</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStitchRun WithKind(StitchKind value) {
        AnyStitchRun copy = new AnyStitchRun(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kind</c> is drawn from — <c>AnyValidKind()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;StitchKind&gt;, AnyEnum&lt;StitchKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStitchRun WithKind(Func<AnyEnum<StitchKind>, AnyEnum<StitchKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStitchRun copy = new AnyStitchRun(this);

        copy._kindRecipe = configure(_kindRecipe);
        copy._kind       = copy._kindRecipe;

        return copy;
    }

    /// <summary>Pins <c>count</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStitchRun WithCount(int value) {
        AnyStitchRun copy = new AnyStitchRun(this);

        copy._count = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>count</c> is drawn from — <c>AnyValidCount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStitchRun WithCount(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStitchRun copy = new AnyStitchRun(this);

        copy._countRecipe = configure(_countRecipe);
        copy._count       = copy._countRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="StitchRun" />.</summary>
    public StitchRun Generate() {
        return new StitchRun(_kind.Generate(),
                             _count.Generate());
    }

}
