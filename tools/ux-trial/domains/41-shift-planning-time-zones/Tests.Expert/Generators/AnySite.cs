// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Site --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using ShiftPlanning.Domain;

namespace ShiftPlanning.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Site" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySite : IAny<Site> {

    private IAny<SiteId>       _id;
    private IAny<string>       _name;
    private IAny<SiteZone>     _zone;
    private IAny<WorkingHours> _workingHours;

    private AnySiteId       _idRecipe;
    private AnyString       _nameRecipe;
    private AnySiteZone     _zoneRecipe;
    private AnyWorkingHours _workingHoursRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySite()
        : this(id:           AnyValidId(),
               name:         AnyValidName(),
               zone:         AnyValidZone(),
               workingHours: AnyValidWorkingHours()) { }

    private static AnySiteId AnyValidId() {
        return new AnySiteId();
    }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank().WithMaxLength(Site.MaxNameLength);
    }

    private static AnySiteZone AnyValidZone() {
        return new AnySiteZone();
    }

    private static AnyWorkingHours AnyValidWorkingHours() {
        return new AnyWorkingHours();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySite(AnySiteId       id,
                    AnyString       name,
                    AnySiteZone     zone,
                    AnyWorkingHours workingHours) {
        _id                 = id;
        _idRecipe           = id;
        _name               = name;
        _nameRecipe         = name;
        _zone               = zone;
        _zoneRecipe         = zone;
        _workingHours       = workingHours;
        _workingHoursRecipe = workingHours;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySite(AnySite from) {
        _id                 = from._id;
        _idRecipe           = from._idRecipe;
        _name               = from._name;
        _nameRecipe         = from._nameRecipe;
        _zone               = from._zone;
        _zoneRecipe         = from._zoneRecipe;
        _workingHours       = from._workingHours;
        _workingHoursRecipe = from._workingHoursRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySite WithId(SiteId value) {
        AnySite copy = new AnySite(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySiteId, AnySiteId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySite WithId(Func<AnySiteId, AnySiteId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySite copy = new AnySite(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySite WithName(string value) {
        AnySite copy = new AnySite(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>name</c> is drawn from — <c>AnyValidName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySite WithName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySite copy = new AnySite(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Pins <c>zone</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySite WithZone(SiteZone value) {
        AnySite copy = new AnySite(this);

        copy._zone = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>zone</c> is drawn from — <c>AnyValidZone()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySiteZone, AnySiteZone&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySite WithZone(Func<AnySiteZone, AnySiteZone> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySite copy = new AnySite(this);

        copy._zoneRecipe = configure(_zoneRecipe);
        copy._zone       = copy._zoneRecipe;

        return copy;
    }

    /// <summary>Pins <c>workingHours</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySite WithWorkingHours(WorkingHours value) {
        AnySite copy = new AnySite(this);

        copy._workingHours = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>workingHours</c> is drawn from — <c>AnyValidWorkingHours()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyWorkingHours, AnyWorkingHours&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySite WithWorkingHours(Func<AnyWorkingHours, AnyWorkingHours> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySite copy = new AnySite(this);

        copy._workingHoursRecipe = configure(_workingHoursRecipe);
        copy._workingHours       = copy._workingHoursRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Site" />.</summary>
    public Site Generate() {
        return new Site(_id.Generate(),
                        _name.Generate(),
                        _zone.Generate(),
                        _workingHours.Generate());
    }

}
