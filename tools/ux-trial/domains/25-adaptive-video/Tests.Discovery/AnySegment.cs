// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Segment --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Segment" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySegment : IAny<Segment> {

    private IAny<int>    _index;
    private IAny<double> _durationSeconds;

    private AnyInt32  _indexRecipe;
    private AnyDouble _durationSecondsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySegment()
        : this(index:           AnyValidIndex(),
               durationSeconds: AnyValidDurationSeconds()) { }

    private static AnyInt32 AnyValidIndex() {
        return Any.Int32().GreaterThanOrEqualTo(0);
    }

    private static AnyDouble AnyValidDurationSeconds() {
        return Any.Double().Between(Segment.MinDurationSeconds, Segment.MaxDurationSeconds);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySegment(AnyInt32  index,
                       AnyDouble durationSeconds) {
        _index                 = index;
        _indexRecipe           = index;
        _durationSeconds       = durationSeconds;
        _durationSecondsRecipe = durationSeconds;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySegment(AnySegment from) {
        _index                 = from._index;
        _indexRecipe           = from._indexRecipe;
        _durationSeconds       = from._durationSeconds;
        _durationSecondsRecipe = from._durationSecondsRecipe;
    }

    /// <summary>Pins <c>index</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySegment WithIndex(int value) {
        AnySegment copy = new AnySegment(this);

        copy._index = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>index</c> is drawn from — <c>AnyValidIndex()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySegment WithIndex(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySegment copy = new AnySegment(this);

        copy._indexRecipe = configure(_indexRecipe);
        copy._index       = copy._indexRecipe;

        return copy;
    }

    /// <summary>Pins <c>durationSeconds</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySegment WithDurationSeconds(double value) {
        AnySegment copy = new AnySegment(this);

        copy._durationSeconds = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>durationSeconds</c> is drawn from — <c>AnyValidDurationSeconds()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySegment WithDurationSeconds(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySegment copy = new AnySegment(this);

        copy._durationSecondsRecipe = configure(_durationSecondsRecipe);
        copy._durationSeconds       = copy._durationSecondsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Segment" />.</summary>
    public Segment Generate() {
        return new Segment(_index.Generate(),
                           _durationSeconds.Generate());
    }

}
