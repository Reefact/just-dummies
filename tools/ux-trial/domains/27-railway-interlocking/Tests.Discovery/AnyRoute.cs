// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Route --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace RailwayInterlocking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Route" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRoute : IAny<Route> {

    private IAny<string>                      _name;
    private IAny<string>                      _entrySignal;
    private IAny<string>                      _exitSignal;
    private IAny<IReadOnlyList<SectionId>>    _sections;
    private IAny<IReadOnlyList<PointSetting>> _pointSettings;

    private AnyString             _nameRecipe;
    private AnyString             _entrySignalRecipe;
    private AnyString             _exitSignalRecipe;
    private AnyList<SectionId>    _sectionsRecipe;
    private AnyList<PointSetting> _pointSettingsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRoute()
        : this(name:          AnyValidName(),
               entrySignal:   AnyValidEntrySignal(),
               exitSignal:    AnyValidExitSignal(),
               sections:      AnyValidSections(),
               pointSettings: AnyValidPointSettings()) { }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank();
    }

    private static AnyString AnyValidEntrySignal() {
        return Any.String().NotBlank();
    }

    private static AnyString AnyValidExitSignal() {
        return Any.String().NotBlank();
    }

    private static AnyList<SectionId> AnyValidSections() {
        return Any.ListOf(new AnySectionId()).NonEmpty().Distinct();
    }

    private static AnyList<PointSetting> AnyValidPointSettings() {
        return Any.ListOf(new AnyPointSetting()).DistinctBy(setting => setting.PointName);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRoute(AnyString             name,
                     AnyString             entrySignal,
                     AnyString             exitSignal,
                     AnyList<SectionId>    sections,
                     AnyList<PointSetting> pointSettings) {
        _name                = name;
        _nameRecipe          = name;
        _entrySignal         = entrySignal;
        _entrySignalRecipe   = entrySignal;
        _exitSignal          = exitSignal;
        _exitSignalRecipe    = exitSignal;
        _sections            = sections;
        _sectionsRecipe      = sections;
        _pointSettings       = pointSettings;
        _pointSettingsRecipe = pointSettings;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRoute(AnyRoute from) {
        _name                = from._name;
        _nameRecipe          = from._nameRecipe;
        _entrySignal         = from._entrySignal;
        _entrySignalRecipe   = from._entrySignalRecipe;
        _exitSignal          = from._exitSignal;
        _exitSignalRecipe    = from._exitSignalRecipe;
        _sections            = from._sections;
        _sectionsRecipe      = from._sectionsRecipe;
        _pointSettings       = from._pointSettings;
        _pointSettingsRecipe = from._pointSettingsRecipe;
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRoute WithName(string value) {
        AnyRoute copy = new AnyRoute(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>name</c> is drawn from — <c>AnyValidName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRoute WithName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRoute copy = new AnyRoute(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Pins <c>entrySignal</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRoute WithEntrySignal(string value) {
        AnyRoute copy = new AnyRoute(this);

        copy._entrySignal = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>entrySignal</c> is drawn from — <c>AnyValidEntrySignal()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRoute WithEntrySignal(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRoute copy = new AnyRoute(this);

        copy._entrySignalRecipe = configure(_entrySignalRecipe);
        copy._entrySignal       = copy._entrySignalRecipe;

        return copy;
    }

    /// <summary>Pins <c>exitSignal</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRoute WithExitSignal(string value) {
        AnyRoute copy = new AnyRoute(this);

        copy._exitSignal = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>exitSignal</c> is drawn from — <c>AnyValidExitSignal()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRoute WithExitSignal(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRoute copy = new AnyRoute(this);

        copy._exitSignalRecipe = configure(_exitSignalRecipe);
        copy._exitSignal       = copy._exitSignalRecipe;

        return copy;
    }

    /// <summary>Pins <c>sections</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRoute WithSections(IReadOnlyList<SectionId> value) {
        AnyRoute copy = new AnyRoute(this);

        copy._sections = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>sections</c> is drawn from — <c>AnyValidSections()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;SectionId&gt;, AnyList&lt;SectionId&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRoute WithSections(Func<AnyList<SectionId>, AnyList<SectionId>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRoute copy = new AnyRoute(this);

        copy._sectionsRecipe = configure(_sectionsRecipe);
        copy._sections       = copy._sectionsRecipe;

        return copy;
    }

    /// <summary>Pins <c>pointSettings</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRoute WithPointSettings(IReadOnlyList<PointSetting> value) {
        AnyRoute copy = new AnyRoute(this);

        copy._pointSettings = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>pointSettings</c> is drawn from — <c>AnyValidPointSettings()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;PointSetting&gt;, AnyList&lt;PointSetting&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRoute WithPointSettings(Func<AnyList<PointSetting>, AnyList<PointSetting>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRoute copy = new AnyRoute(this);

        copy._pointSettingsRecipe = configure(_pointSettingsRecipe);
        copy._pointSettings       = copy._pointSettingsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Route" />.</summary>
    public Route Generate() {
        // Entry and exit are tied (they must differ): draw the exit again, bounded, while it equals the entry.
        string entrySignal = _entrySignal.Generate();

        return new Route(_name.Generate(),
                         entrySignal,
                         _exitSignal.DifferentFrom(entrySignal).Generate(),
                         _sections.Generate(),
                         _pointSettings.Generate());
    }

}
