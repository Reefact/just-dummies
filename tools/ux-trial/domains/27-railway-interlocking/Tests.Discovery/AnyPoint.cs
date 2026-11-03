// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Point --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RailwayInterlocking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Point" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPoint : IAny<Point> {

    private IAny<string>        _name;
    private IAny<SectionId>     _section;
    private IAny<PointPosition> _initialPosition;

    private AnyString              _nameRecipe;
    private AnySectionId           _sectionRecipe;
    private AnyEnum<PointPosition> _initialPositionRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPoint()
        : this(name:            AnyValidName(),
               section:         AnyValidSection(),
               initialPosition: AnyValidInitialPosition()) { }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank();
    }

    private static AnySectionId AnyValidSection() {
        return new AnySectionId();
    }

    private static AnyEnum<PointPosition> AnyValidInitialPosition() {
        return Any.Enum<PointPosition>();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPoint(AnyString              name,
                     AnySectionId           section,
                     AnyEnum<PointPosition> initialPosition) {
        _name                  = name;
        _nameRecipe            = name;
        _section               = section;
        _sectionRecipe         = section;
        _initialPosition       = initialPosition;
        _initialPositionRecipe = initialPosition;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPoint(AnyPoint from) {
        _name                  = from._name;
        _nameRecipe            = from._nameRecipe;
        _section               = from._section;
        _sectionRecipe         = from._sectionRecipe;
        _initialPosition       = from._initialPosition;
        _initialPositionRecipe = from._initialPositionRecipe;
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPoint WithName(string value) {
        AnyPoint copy = new AnyPoint(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>name</c> is drawn from — <c>AnyValidName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPoint WithName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPoint copy = new AnyPoint(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Pins <c>section</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPoint WithSection(SectionId value) {
        AnyPoint copy = new AnyPoint(this);

        copy._section = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>section</c> is drawn from — <c>AnyValidSection()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySectionId, AnySectionId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPoint WithSection(Func<AnySectionId, AnySectionId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPoint copy = new AnyPoint(this);

        copy._sectionRecipe = configure(_sectionRecipe);
        copy._section       = copy._sectionRecipe;

        return copy;
    }

    /// <summary>Pins <c>initialPosition</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPoint WithInitialPosition(PointPosition value) {
        AnyPoint copy = new AnyPoint(this);

        copy._initialPosition = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>initialPosition</c> is drawn from — <c>AnyValidInitialPosition()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;PointPosition&gt;, AnyEnum&lt;PointPosition&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPoint WithInitialPosition(Func<AnyEnum<PointPosition>, AnyEnum<PointPosition>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPoint copy = new AnyPoint(this);

        copy._initialPositionRecipe = configure(_initialPositionRecipe);
        copy._initialPosition       = copy._initialPositionRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Point" />.</summary>
    public Point Generate() {
        return new Point(_name.Generate(),
                         _section.Generate(),
                         _initialPosition.Generate());
    }

}
