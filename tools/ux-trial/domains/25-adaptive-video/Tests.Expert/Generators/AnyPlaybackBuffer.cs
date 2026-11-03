// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PlaybackBuffer --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PlaybackBuffer" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPlaybackBuffer : IAny<PlaybackBuffer> {

    private AnyOrderedPair<double> _levelSecondsAndCapacitySeconds;

    private AnyDouble _levelSecondsRecipe;
    private AnyDouble _capacitySecondsRecipe;

    private double? _levelSecondsPin;
    private double? _capacitySecondsPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPlaybackBuffer()
        : this(levelSeconds:    AnyValidLevelSeconds(),
               capacitySeconds: AnyValidCapacitySeconds()) { }

    private static AnyDouble AnyValidLevelSeconds() {
        return Any.Double().GreaterThanOrEqualTo(0d);
    }

    private static AnyDouble AnyValidCapacitySeconds() {
        return Any.Double().Between(4d, 300d);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPlaybackBuffer(AnyDouble levelSeconds,
                              AnyDouble capacitySeconds) {
        _levelSecondsRecipe             = levelSeconds;
        _capacitySecondsRecipe          = capacitySeconds;
        _levelSecondsAndCapacitySeconds = LevelSecondsAndCapacitySeconds();
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPlaybackBuffer(AnyPlaybackBuffer from) {
        _levelSecondsAndCapacitySeconds = from._levelSecondsAndCapacitySeconds;
        _levelSecondsRecipe             = from._levelSecondsRecipe;
        _levelSecondsPin                = from._levelSecondsPin;
        _capacitySecondsRecipe          = from._capacitySecondsRecipe;
        _capacitySecondsPin             = from._capacitySecondsPin;
    }

    /// <summary>Draws <c>levelSeconds</c> and <c>capacitySeconds</c> together, <c>levelSeconds</c> never after <c>capacitySeconds</c> as the constructor requires, each from its current recipe and pin.</summary>
    private AnyOrderedPair<double> LevelSecondsAndCapacitySeconds() {
        AnyOrderedPair<double> pair = Any.OrderedPair(_levelSecondsRecipe, _capacitySecondsRecipe);

        if (_levelSecondsPin is { } levelSeconds) { pair = pair.WithFirst(levelSeconds); }
        if (_capacitySecondsPin is { } capacitySeconds) { pair = pair.WithSecond(capacitySeconds); }

        return pair;
    }

    /// <summary>Pins <c>levelSeconds</c> to a fixed value, which bounds <c>capacitySeconds</c>; its recipe is kept for a later refinement.</summary>
    public AnyPlaybackBuffer WithLevelSeconds(double value) {
        AnyPlaybackBuffer copy = new AnyPlaybackBuffer(this);

        copy._levelSecondsPin                = value;
        copy._levelSecondsAndCapacitySeconds = copy.LevelSecondsAndCapacitySeconds();

        return copy;
    }

    /// <summary>Refines the recipe <c>levelSeconds</c> is drawn from — <c>AnyValidLevelSeconds()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>capacitySeconds</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPlaybackBuffer WithLevelSeconds(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPlaybackBuffer copy = new AnyPlaybackBuffer(this);

        copy._levelSecondsRecipe             = configure(_levelSecondsRecipe);
        copy._levelSecondsPin                = null;
        copy._levelSecondsAndCapacitySeconds = copy.LevelSecondsAndCapacitySeconds();

        return copy;
    }

    /// <summary>Pins <c>capacitySeconds</c> to a fixed value, which bounds <c>levelSeconds</c>; its recipe is kept for a later refinement.</summary>
    public AnyPlaybackBuffer WithCapacitySeconds(double value) {
        AnyPlaybackBuffer copy = new AnyPlaybackBuffer(this);

        copy._capacitySecondsPin             = value;
        copy._levelSecondsAndCapacitySeconds = copy.LevelSecondsAndCapacitySeconds();

        return copy;
    }

    /// <summary>Refines the recipe <c>capacitySeconds</c> is drawn from — <c>AnyValidCapacitySeconds()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>levelSeconds</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPlaybackBuffer WithCapacitySeconds(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPlaybackBuffer copy = new AnyPlaybackBuffer(this);

        copy._capacitySecondsRecipe          = configure(_capacitySecondsRecipe);
        copy._capacitySecondsPin             = null;
        copy._levelSecondsAndCapacitySeconds = copy.LevelSecondsAndCapacitySeconds();

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PlaybackBuffer" />.</summary>
    public PlaybackBuffer Generate() {
        (double levelSeconds, double capacitySeconds) = _levelSecondsAndCapacitySeconds.Generate();

        return new PlaybackBuffer(levelSeconds,
                                  capacitySeconds);
    }

}
