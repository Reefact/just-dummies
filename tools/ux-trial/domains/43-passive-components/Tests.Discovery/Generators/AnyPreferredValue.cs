// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PreferredValue --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace PassiveComponents.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PreferredValue" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPreferredValue : IAny<PreferredValue> {

    private IAny<ESeries> _series;
    private IAny<int>     _decade;
    private IAny<int>     _index;

    private AnyEnum<ESeries> _seriesRecipe;
    private AnyInt32         _decadeRecipe;
    private AnyInt32         _indexRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPreferredValue()
        : this(series: AnyValidSeries(),
               decade: AnyValidDecade(),
               index:  AnyValidIndex()) { }

    private static AnyEnum<ESeries> AnyValidSeries() {
        return Any.Enum<ESeries>();
    }

    private static AnyInt32 AnyValidDecade() {
        return Any.Int32().Between(PreferredValue.MinDecade, PreferredValue.MaxDecade);
    }

    private static AnyInt32 AnyValidIndex() {
        // E6 has the fewest values per decade (6): 0..5 is an index every series accepts.
        return Any.Int32().Between(0, 5);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPreferredValue(AnyEnum<ESeries> series,
                              AnyInt32         decade,
                              AnyInt32         index) {
        _series       = series;
        _seriesRecipe = series;
        _decade       = decade;
        _decadeRecipe = decade;
        _index        = index;
        _indexRecipe  = index;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPreferredValue(AnyPreferredValue from) {
        _series       = from._series;
        _seriesRecipe = from._seriesRecipe;
        _decade       = from._decade;
        _decadeRecipe = from._decadeRecipe;
        _index        = from._index;
        _indexRecipe  = from._indexRecipe;
    }

    /// <summary>Pins <c>series</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPreferredValue WithSeries(ESeries value) {
        AnyPreferredValue copy = new AnyPreferredValue(this);

        copy._series = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>series</c> is drawn from — <c>AnyValidSeries()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;ESeries&gt;, AnyEnum&lt;ESeries&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPreferredValue WithSeries(Func<AnyEnum<ESeries>, AnyEnum<ESeries>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPreferredValue copy = new AnyPreferredValue(this);

        copy._seriesRecipe = configure(_seriesRecipe);
        copy._series       = copy._seriesRecipe;

        return copy;
    }

    /// <summary>Pins <c>decade</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPreferredValue WithDecade(int value) {
        AnyPreferredValue copy = new AnyPreferredValue(this);

        copy._decade = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>decade</c> is drawn from — <c>AnyValidDecade()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPreferredValue WithDecade(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPreferredValue copy = new AnyPreferredValue(this);

        copy._decadeRecipe = configure(_decadeRecipe);
        copy._decade       = copy._decadeRecipe;

        return copy;
    }

    /// <summary>Pins <c>index</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPreferredValue WithIndex(int value) {
        AnyPreferredValue copy = new AnyPreferredValue(this);

        copy._index = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>index</c> is drawn from — <c>AnyValidIndex()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPreferredValue WithIndex(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPreferredValue copy = new AnyPreferredValue(this);

        copy._indexRecipe = configure(_indexRecipe);
        copy._index       = copy._indexRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PreferredValue" />.</summary>
    public PreferredValue Generate() {
        return new PreferredValue(_series.Generate(),
                                  _decade.Generate(),
                                  _index.Generate());
    }

}
