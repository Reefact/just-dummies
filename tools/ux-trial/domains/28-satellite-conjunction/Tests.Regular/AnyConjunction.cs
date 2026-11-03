// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Conjunction --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace SatelliteConjunction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Conjunction" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyConjunction : IAny<Conjunction> {

    private IAny<Guid>       _id;
    private IAny<Trajectory> _primary;
    private IAny<Trajectory> _secondary;
    private IAny<Epoch>      _timeOfClosestApproach;

    private AnyGuid       _idRecipe;
    private AnyTrajectory _primaryRecipe;
    private AnyTrajectory _secondaryRecipe;
    private AnyEpoch      _timeOfClosestApproachRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyConjunction()
        : this(id:                    AnyValidId(),
               primary:               AnyValidPrimary(),
               secondary:             AnyValidSecondary(),
               timeOfClosestApproach: AnyValidTimeOfClosestApproach()) { }

    private static AnyGuid AnyValidId() {
        return Any.Guid().NonEmpty();
    }

    private static AnyTrajectory AnyValidPrimary() {
        // The primary must be an active satellite; Generate() makes it cover the time of closest approach.
        return new AnyTrajectory().WithSpaceObject(o => o.Active());
    }

    private static AnyTrajectory AnyValidSecondary() {
        // Generate() gives it a catalogue number other than the primary's, and makes it cover the time of closest approach.
        return new AnyTrajectory();
    }

    private static AnyEpoch AnyValidTimeOfClosestApproach() {
        return new AnyEpoch();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyConjunction(AnyGuid       id,
                           AnyTrajectory primary,
                           AnyTrajectory secondary,
                           AnyEpoch      timeOfClosestApproach) {
        _id                          = id;
        _idRecipe                    = id;
        _primary                     = primary;
        _primaryRecipe               = primary;
        _secondary                   = secondary;
        _secondaryRecipe             = secondary;
        _timeOfClosestApproach       = timeOfClosestApproach;
        _timeOfClosestApproachRecipe = timeOfClosestApproach;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyConjunction(AnyConjunction from) {
        _id                          = from._id;
        _idRecipe                    = from._idRecipe;
        _primary                     = from._primary;
        _primaryRecipe               = from._primaryRecipe;
        _secondary                   = from._secondary;
        _secondaryRecipe             = from._secondaryRecipe;
        _timeOfClosestApproach       = from._timeOfClosestApproach;
        _timeOfClosestApproachRecipe = from._timeOfClosestApproachRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyConjunction WithId(Guid value) {
        AnyConjunction copy = new AnyConjunction(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGuid, AnyGuid&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyConjunction WithId(Func<AnyGuid, AnyGuid> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyConjunction copy = new AnyConjunction(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>primary</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyConjunction WithPrimary(Trajectory value) {
        AnyConjunction copy = new AnyConjunction(this);

        copy._primary = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>primary</c> is drawn from — <c>AnyValidPrimary()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTrajectory, AnyTrajectory&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyConjunction WithPrimary(Func<AnyTrajectory, AnyTrajectory> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyConjunction copy = new AnyConjunction(this);

        copy._primaryRecipe = configure(_primaryRecipe);
        copy._primary       = copy._primaryRecipe;

        return copy;
    }

    /// <summary>Pins <c>secondary</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyConjunction WithSecondary(Trajectory value) {
        AnyConjunction copy = new AnyConjunction(this);

        copy._secondary = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>secondary</c> is drawn from — <c>AnyValidSecondary()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTrajectory, AnyTrajectory&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyConjunction WithSecondary(Func<AnyTrajectory, AnyTrajectory> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyConjunction copy = new AnyConjunction(this);

        copy._secondaryRecipe = configure(_secondaryRecipe);
        copy._secondary       = copy._secondaryRecipe;

        return copy;
    }

    /// <summary>Pins <c>timeOfClosestApproach</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyConjunction WithTimeOfClosestApproach(Epoch value) {
        AnyConjunction copy = new AnyConjunction(this);

        copy._timeOfClosestApproach = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>timeOfClosestApproach</c> is drawn from — <c>AnyValidTimeOfClosestApproach()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEpoch, AnyEpoch&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyConjunction WithTimeOfClosestApproach(Func<AnyEpoch, AnyEpoch> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyConjunction copy = new AnyConjunction(this);

        copy._timeOfClosestApproachRecipe = configure(_timeOfClosestApproachRecipe);
        copy._timeOfClosestApproach       = copy._timeOfClosestApproachRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Conjunction" />.</summary>
    public Conjunction Generate() {
        // The three ties of the constructor are written here: the time of closest approach first, then the two
        // trajectories around it, the secondary on another catalogue number than the primary. A pinned trajectory
        // is used as it is, so a test that pins one pins the time too.
        Epoch tca = _timeOfClosestApproach.Generate();

        Trajectory primary = ReferenceEquals(_primary, _primaryRecipe)
            ? _primaryRecipe.Covering(tca).Generate()
            : _primary.Generate();

        Trajectory secondary = ReferenceEquals(_secondary, _secondaryRecipe)
            ? _secondaryRecipe.WithSpaceObject(o => o.WithCatalogNumber(n => n.DifferentFrom(primary.SpaceObject.CatalogNumber)))
                              .Covering(tca)
                              .Generate()
            : _secondary.Generate();

        return new Conjunction(_id.Generate(), primary, secondary, tca);
    }

}
