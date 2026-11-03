// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate AnatomicalLandmark --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using PulmonaryNodules.Domain;

namespace PulmonaryNodules.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="AnatomicalLandmark" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAnatomicalLandmark : IAny<AnatomicalLandmark> {

    private IAny<LandmarkKind> _kind;
    private IAny<SpatialPoint> _position;

    private AnyEnum<LandmarkKind> _kindRecipe;
    private AnySpatialPoint       _positionRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAnatomicalLandmark()
        : this(kind:     AnyValidKind(),
               position: AnyValidPosition()) { }

    private static AnyEnum<LandmarkKind> AnyValidKind() {
        return Any.Enum<LandmarkKind>();
    }

    private static AnySpatialPoint AnyValidPosition() {
        return new AnySpatialPoint();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyAnatomicalLandmark(AnyEnum<LandmarkKind> kind,
                                  AnySpatialPoint       position) {
        _kind           = kind;
        _kindRecipe     = kind;
        _position       = position;
        _positionRecipe = position;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyAnatomicalLandmark(AnyAnatomicalLandmark from) {
        _kind           = from._kind;
        _kindRecipe     = from._kindRecipe;
        _position       = from._position;
        _positionRecipe = from._positionRecipe;
    }

    /// <summary>Pins <c>kind</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAnatomicalLandmark WithKind(LandmarkKind value) {
        AnyAnatomicalLandmark copy = new AnyAnatomicalLandmark(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kind</c> is drawn from — <c>AnyValidKind()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;LandmarkKind&gt;, AnyEnum&lt;LandmarkKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAnatomicalLandmark WithKind(Func<AnyEnum<LandmarkKind>, AnyEnum<LandmarkKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAnatomicalLandmark copy = new AnyAnatomicalLandmark(this);

        copy._kindRecipe = configure(_kindRecipe);
        copy._kind       = copy._kindRecipe;

        return copy;
    }

    /// <summary>Pins <c>position</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAnatomicalLandmark WithPosition(SpatialPoint value) {
        AnyAnatomicalLandmark copy = new AnyAnatomicalLandmark(this);

        copy._position = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>position</c> is drawn from — <c>AnyValidPosition()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySpatialPoint, AnySpatialPoint&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAnatomicalLandmark WithPosition(Func<AnySpatialPoint, AnySpatialPoint> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAnatomicalLandmark copy = new AnyAnatomicalLandmark(this);

        copy._positionRecipe = configure(_positionRecipe);
        copy._position       = copy._positionRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="AnatomicalLandmark" />.</summary>
    public AnatomicalLandmark Generate() {
        return new AnatomicalLandmark(_kind.Generate(),
                                      _position.Generate());
    }

}
