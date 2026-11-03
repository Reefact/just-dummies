// Scaffolded by `any` (JustDummies), then completed by hand: `sections` is tied to the cast-on (every row must
// consume what the previous one left), so unless a test pins it, Generate() builds an expandable pattern from
// the drawn cast-on.
// `any generate KnittingPattern --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;
using KnittingPatterns.Tests.Expert.Support;
using KnittingPatterns.Domain;

namespace KnittingPatterns.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="KnittingPattern" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyKnittingPattern : IAny<KnittingPattern> {

    private IAny<string>                        _title;
    private IAny<Gauge>                         _gauge;
    private IAny<int>                           _castOnCount;
    private IAny<IReadOnlyList<PatternSection>>? _sections;   // null: built in Generate() from the cast-on

    private AnyString               _titleRecipe;
    private AnyGauge                _gaugeRecipe;
    private AnyInt32                _castOnCountRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyKnittingPattern()
        : this(title:       AnyValidTitle(),
               gauge:       AnyValidGauge(),
               castOnCount: AnyValidCastOnCount()) { }

    private static AnyString AnyValidTitle() {
        return Any.String().NotBlank().WithMaxLength(80);
    }

    private static AnyGauge AnyValidGauge() {
        return new AnyGauge();
    }

    private static AnyInt32 AnyValidCastOnCount() {
        // Stays well under KnittingPattern.MaxCastOn: a row is built of runs of at most 500 stitches, and
        // the rows may add a few stitches each.
        return Any.Int32().Between(1, 100);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyKnittingPattern(AnyString               title,
                               AnyGauge                gauge,
                               AnyInt32                castOnCount) {
        _title             = title;
        _titleRecipe       = title;
        _gauge             = gauge;
        _gaugeRecipe       = gauge;
        _castOnCount       = castOnCount;
        _castOnCountRecipe = castOnCount;
        _sections          = null;
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

    /// <summary>Produces one arbitrary <see cref="KnittingPattern" />.</summary>
    public KnittingPattern Generate() {
        int castOnCount = _castOnCount.Generate();

        return new KnittingPattern(_title.Generate(),
                                   _gauge.Generate(),
                                   castOnCount,
                                   _sections is not null ? _sections.Generate() : AnyExpandableSections(castOnCount));
    }

    /// <summary>Sections that start at row 1, follow one another, carry distinct names, and work the cast-on to the end.</summary>
    private static IReadOnlyList<PatternSection> AnyExpandableSections(int castOnCount) {
        int                  sectionCount = Any.Int32().Between(1, 4).Generate();
        List<string>         names        = Any.ListOf(Any.String().Alpha().WithLengthBetween(1, 20))
                                               .WithCount(sectionCount)
                                               .Distinct(StringComparer.OrdinalIgnoreCase)
                                               .Generate();
        List<PatternSection> sections     = new List<PatternSection>(sectionCount);
        int                  live         = castOnCount;
        int                  rowNumber    = 1;
        foreach (string name in names) {
            int                  rowCount = Any.Int32().Between(1, 5).Generate();
            List<RowInstruction> rows     = new List<RowInstruction>(rowCount);
            for (int i = 0; i < rowCount; i++) {
                RowInstruction row = Rows.ArbitraryRowConsuming(rowNumber, live);

                rows.Add(row);
                live = row.Produces;
                rowNumber++;
            }

            sections.Add(new PatternSection(name, rows));
        }

        return sections;
    }

}
