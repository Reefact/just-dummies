// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TrackedNodule --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using PulmonaryNodules.Domain;

namespace PulmonaryNodules.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="TrackedNodule" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTrackedNodule : IAny<TrackedNodule> {

    private IAny<Guid>              _id;
    private IAny<string>            _patientId;
    private IAny<NoduleObservation> _baseline;
    private IAny<double>            _matchToleranceMm;

    private AnyGuid              _idRecipe;
    private AnyString            _patientIdRecipe;
    private AnyNoduleObservation _baselineRecipe;
    private AnyDouble            _matchToleranceMmRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTrackedNodule()
        : this(id:               AnyValidId(),
               patientId:        AnyValidPatientId(),
               baseline:         AnyValidBaseline(),
               matchToleranceMm: AnyValidMatchToleranceMm()) { }

    private static AnyGuid AnyValidId() {
        return Any.Guid().NonEmpty();
    }

    private static AnyString AnyValidPatientId() {
        return Any.String().NotBlank().WithMaxLength(64);
    }

    private static AnyNoduleObservation AnyValidBaseline() {
        return new AnyNoduleObservation();
    }

    private static AnyDouble AnyValidMatchToleranceMm() {
        return Any.Double().Between(TrackedNodule.MinMatchToleranceMm, TrackedNodule.MaxMatchToleranceMm);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTrackedNodule(AnyGuid              id,
                             AnyString            patientId,
                             AnyNoduleObservation baseline,
                             AnyDouble            matchToleranceMm) {
        _id                     = id;
        _idRecipe               = id;
        _patientId              = patientId;
        _patientIdRecipe        = patientId;
        _baseline               = baseline;
        _baselineRecipe         = baseline;
        _matchToleranceMm       = matchToleranceMm;
        _matchToleranceMmRecipe = matchToleranceMm;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTrackedNodule(AnyTrackedNodule from) {
        _id                     = from._id;
        _idRecipe               = from._idRecipe;
        _patientId              = from._patientId;
        _patientIdRecipe        = from._patientIdRecipe;
        _baseline               = from._baseline;
        _baselineRecipe         = from._baselineRecipe;
        _matchToleranceMm       = from._matchToleranceMm;
        _matchToleranceMmRecipe = from._matchToleranceMmRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTrackedNodule WithId(Guid value) {
        AnyTrackedNodule copy = new AnyTrackedNodule(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGuid, AnyGuid&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTrackedNodule WithId(Func<AnyGuid, AnyGuid> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTrackedNodule copy = new AnyTrackedNodule(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>patientId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTrackedNodule WithPatientId(string value) {
        AnyTrackedNodule copy = new AnyTrackedNodule(this);

        copy._patientId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>patientId</c> is drawn from — <c>AnyValidPatientId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTrackedNodule WithPatientId(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTrackedNodule copy = new AnyTrackedNodule(this);

        copy._patientIdRecipe = configure(_patientIdRecipe);
        copy._patientId       = copy._patientIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>baseline</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTrackedNodule WithBaseline(NoduleObservation value) {
        AnyTrackedNodule copy = new AnyTrackedNodule(this);

        copy._baseline = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>baseline</c> is drawn from — <c>AnyValidBaseline()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyNoduleObservation, AnyNoduleObservation&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTrackedNodule WithBaseline(Func<AnyNoduleObservation, AnyNoduleObservation> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTrackedNodule copy = new AnyTrackedNodule(this);

        copy._baselineRecipe = configure(_baselineRecipe);
        copy._baseline       = copy._baselineRecipe;

        return copy;
    }

    /// <summary>Pins <c>matchToleranceMm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTrackedNodule WithMatchToleranceMm(double value) {
        AnyTrackedNodule copy = new AnyTrackedNodule(this);

        copy._matchToleranceMm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>matchToleranceMm</c> is drawn from — <c>AnyValidMatchToleranceMm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTrackedNodule WithMatchToleranceMm(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTrackedNodule copy = new AnyTrackedNodule(this);

        copy._matchToleranceMmRecipe = configure(_matchToleranceMmRecipe);
        copy._matchToleranceMm       = copy._matchToleranceMmRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TrackedNodule" />.</summary>
    public TrackedNodule Generate() {
        return new TrackedNodule(_id.Generate(),
                                 _patientId.Generate(),
                                 _baseline.Generate(),
                                 _matchToleranceMm.Generate());
    }

}
