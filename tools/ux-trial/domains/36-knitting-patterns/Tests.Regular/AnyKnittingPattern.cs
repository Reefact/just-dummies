// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate KnittingPattern --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>
///     A generator of arbitrary <see cref="KnittingPattern" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyKnittingPattern : IAny<KnittingPattern> {

    private IAny<string>                        _title;
    private IAny<Gauge>                         _gauge;
    private IAny<int>                           _castOnCount;
    private IAny<IReadOnlyList<PatternSection>> _sections;

    private AnyString               _titleRecipe;
    private AnyGauge                _gaugeRecipe;
    private AnyInt32                _castOnCountRecipe;
    private AnyContiguousSections _sectionsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyKnittingPattern()
        : this(title:       AnyValidTitle(),
               gauge:       AnyValidGauge(),
               castOnCount: AnyValidCastOnCount(),
               sections:    AnyValidSections()) { }

    private static AnyString AnyValidTitle() {
        return Any.String().NotBlank();
    }

    private static AnyGauge AnyValidGauge() {
        return new AnyGauge();
    }

    private static AnyInt32 AnyValidCastOnCount() {
        return Any.Int32().Positive().LessThanOrEqualTo(KnittingPattern.MaxCastOn);
    }

    private static AnyContiguousSections AnyValidSections() {
        // First section at row 1, contiguous sections, distinct names: see AnyContiguousSections.
        return new AnyContiguousSections();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyKnittingPattern(AnyString               title,
                               AnyGauge                gauge,
                               AnyInt32                castOnCount,
                               AnyContiguousSections sections) {
        _title             = title;
        _titleRecipe       = title;
        _gauge             = gauge;
        _gaugeRecipe       = gauge;
        _castOnCount       = castOnCount;
        _castOnCountRecipe = castOnCount;
        _sections          = sections;
        _sectionsRecipe    = sections;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyKnittingPattern(AnyKnittingPattern from) {
        _title             = from._title;
        _titleRecipe       = from._titleRecipe;
        _gauge             = from._gauge;
        _gaugeRecipe       = from._gaugeRecipe;
        _castOnCount       = from._castOnCount;
        _castOnCountRecipe = from._castOnCountRecipe;
        _sections          = from._sections;
        _sectionsRecipe    = from._sectionsRecipe;
    }

    /// <summary>Pins <c>title</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyKnittingPattern WithTitle(string value) {
        AnyKnittingPattern copy = new AnyKnittingPattern(this);

        copy._title = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>title</c> is drawn from — <c>AnyValidTitle()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyKnittingPattern WithTitle(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyKnittingPattern copy = new AnyKnittingPattern(this);

        copy._titleRecipe = configure(_titleRecipe);
        copy._title       = copy._titleRecipe;

        return copy;
    }

    /// <summary>Pins <c>gauge</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyKnittingPattern WithGauge(Gauge value) {
        AnyKnittingPattern copy = new AnyKnittingPattern(this);

        copy._gauge = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>gauge</c> is drawn from — <c>AnyValidGauge()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGauge, AnyGauge&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyKnittingPattern WithGauge(Func<AnyGauge, AnyGauge> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyKnittingPattern copy = new AnyKnittingPattern(this);

        copy._gaugeRecipe = configure(_gaugeRecipe);
        copy._gauge       = copy._gaugeRecipe;

        return copy;
    }

    /// <summary>Pins <c>castOnCount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyKnittingPattern WithCastOnCount(int value) {
        AnyKnittingPattern copy = new AnyKnittingPattern(this);

        copy._castOnCount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>castOnCount</c> is drawn from — <c>AnyValidCastOnCount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyKnittingPattern WithCastOnCount(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyKnittingPattern copy = new AnyKnittingPattern(this);

        copy._castOnCountRecipe = configure(_castOnCountRecipe);
        copy._castOnCount       = copy._castOnCountRecipe;

        return copy;
    }

    /// <summary>Pins <c>sections</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyKnittingPattern WithSections(IReadOnlyList<PatternSection> value) {
        AnyKnittingPattern copy = new AnyKnittingPattern(this);

        copy._sections = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>sections</c> is drawn from — <c>AnyValidSections()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;PatternSection&gt;, AnyList&lt;PatternSection&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyKnittingPattern WithSections(Func<AnyContiguousSections, AnyContiguousSections> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyKnittingPattern copy = new AnyKnittingPattern(this);

        copy._sectionsRecipe = configure(_sectionsRecipe);
        copy._sections       = copy._sectionsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="KnittingPattern" />.</summary>
    public KnittingPattern Generate() {
        return new KnittingPattern(_title.Generate(),
                                   _gauge.Generate(),
                                   _castOnCount.Generate(),
                                   _sections.Generate());
    }

}
