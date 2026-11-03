// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TrackSection --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RailwayInterlocking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TrackSection" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTrackSection : IAny<TrackSection> {

    private IAny<SectionId> _id;
    private IAny<double>    _lengthMetres;
    private IAny<double>    _lineSpeedKmh;

    private AnySectionId _idRecipe;
    private AnyDouble    _lengthMetresRecipe;
    private AnyDouble    _lineSpeedKmhRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTrackSection()
        : this(id:           AnyValidId(),
               lengthMetres: AnyValidLengthMetres(),
               lineSpeedKmh: AnyValidLineSpeedKmh()) { }

    private static AnySectionId AnyValidId() {
        return new AnySectionId();
    }

    private static AnyDouble AnyValidLengthMetres() {
        return Any.Double().Between(TrackSection.MinimumLengthMetres, TrackSection.MaximumLengthMetres);
    }

    private static AnyDouble AnyValidLineSpeedKmh() {
        return Any.Double().Positive().LessThanOrEqualTo(300d);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTrackSection(AnySectionId id,
                            AnyDouble    lengthMetres,
                            AnyDouble    lineSpeedKmh) {
        _id                 = id;
        _idRecipe           = id;
        _lengthMetres       = lengthMetres;
        _lengthMetresRecipe = lengthMetres;
        _lineSpeedKmh       = lineSpeedKmh;
        _lineSpeedKmhRecipe = lineSpeedKmh;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTrackSection(AnyTrackSection from) {
        _id                 = from._id;
        _idRecipe           = from._idRecipe;
        _lengthMetres       = from._lengthMetres;
        _lengthMetresRecipe = from._lengthMetresRecipe;
        _lineSpeedKmh       = from._lineSpeedKmh;
        _lineSpeedKmhRecipe = from._lineSpeedKmhRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTrackSection WithId(SectionId value) {
        AnyTrackSection copy = new AnyTrackSection(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySectionId, AnySectionId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTrackSection WithId(Func<AnySectionId, AnySectionId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTrackSection copy = new AnyTrackSection(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>lengthMetres</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTrackSection WithLengthMetres(double value) {
        AnyTrackSection copy = new AnyTrackSection(this);

        copy._lengthMetres = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>lengthMetres</c> is drawn from — <c>AnyValidLengthMetres()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTrackSection WithLengthMetres(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTrackSection copy = new AnyTrackSection(this);

        copy._lengthMetresRecipe = configure(_lengthMetresRecipe);
        copy._lengthMetres       = copy._lengthMetresRecipe;

        return copy;
    }

    /// <summary>Pins <c>lineSpeedKmh</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTrackSection WithLineSpeedKmh(double value) {
        AnyTrackSection copy = new AnyTrackSection(this);

        copy._lineSpeedKmh = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>lineSpeedKmh</c> is drawn from — <c>AnyValidLineSpeedKmh()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTrackSection WithLineSpeedKmh(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTrackSection copy = new AnyTrackSection(this);

        copy._lineSpeedKmhRecipe = configure(_lineSpeedKmhRecipe);
        copy._lineSpeedKmh       = copy._lineSpeedKmhRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TrackSection" />.</summary>
    public TrackSection Generate() {
        return new TrackSection(_id.Generate(),
                                _lengthMetres.Generate(),
                                _lineSpeedKmh.Generate());
    }

}
