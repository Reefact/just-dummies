// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MilkingSession --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace RoboticMilking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="MilkingSession" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMilkingSession : IAny<MilkingSession> {

    private IAny<Animal>          _animal;
    private IAny<int>             _boxNumber;
    private IAny<DateTimeOffset>  _startedAt;
    private IAny<MilkingSettings> _settings;

    private AnyAnimal          _animalRecipe;
    private AnyInt32           _boxNumberRecipe;
    private AnyDateTimeOffset  _startedAtRecipe;
    private AnyMilkingSettings _settingsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMilkingSession()
        : this(animal:    AnyValidAnimal(),
               boxNumber: AnyValidBoxNumber(),
               startedAt: AnyValidStartedAt(),
               settings:  AnyValidSettings()) { }

    private static AnyAnimal AnyValidAnimal() {
        // Realistic last-milked dates, so that "last milked + minimum interval" never overflows the type.
        return new AnyAnimal().WithLastMilkedAt(d => d.Between(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                                               new DateTimeOffset(2035, 1, 1, 0, 0, 0, TimeSpan.Zero)));
    }

    private static AnyInt32 AnyValidBoxNumber() {
        return Any.Int32().Positive().LessThanOrEqualTo(MilkingSession.BoxCount);
    }

    private static AnyDateTimeOffset AnyValidStartedAt() {
        return Any.DateTimeOffset().Between(new DateTimeOffset(2036, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                            new DateTimeOffset(2040, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    private static AnyMilkingSettings AnyValidSettings() {
        return new AnyMilkingSettings();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMilkingSession(AnyAnimal          animal,
                              AnyInt32           boxNumber,
                              AnyDateTimeOffset  startedAt,
                              AnyMilkingSettings settings) {
        _animal          = animal;
        _animalRecipe    = animal;
        _boxNumber       = boxNumber;
        _boxNumberRecipe = boxNumber;
        _startedAt       = startedAt;
        _startedAtRecipe = startedAt;
        _settings        = settings;
        _settingsRecipe  = settings;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyMilkingSession(AnyMilkingSession from) {
        _animal          = from._animal;
        _animalRecipe    = from._animalRecipe;
        _boxNumber       = from._boxNumber;
        _boxNumberRecipe = from._boxNumberRecipe;
        _startedAt       = from._startedAt;
        _startedAtRecipe = from._startedAtRecipe;
        _settings        = from._settings;
        _settingsRecipe  = from._settingsRecipe;
    }

    /// <summary>Pins <c>animal</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMilkingSession WithAnimal(Animal value) {
        AnyMilkingSession copy = new AnyMilkingSession(this);

        copy._animal = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>animal</c> is drawn from — <c>AnyValidAnimal()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyAnimal, AnyAnimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMilkingSession WithAnimal(Func<AnyAnimal, AnyAnimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMilkingSession copy = new AnyMilkingSession(this);

        copy._animalRecipe = configure(_animalRecipe);
        copy._animal       = copy._animalRecipe;

        return copy;
    }

    /// <summary>Pins <c>boxNumber</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMilkingSession WithBoxNumber(int value) {
        AnyMilkingSession copy = new AnyMilkingSession(this);

        copy._boxNumber = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>boxNumber</c> is drawn from — <c>AnyValidBoxNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMilkingSession WithBoxNumber(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMilkingSession copy = new AnyMilkingSession(this);

        copy._boxNumberRecipe = configure(_boxNumberRecipe);
        copy._boxNumber       = copy._boxNumberRecipe;

        return copy;
    }

    /// <summary>Pins <c>startedAt</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMilkingSession WithStartedAt(DateTimeOffset value) {
        AnyMilkingSession copy = new AnyMilkingSession(this);

        copy._startedAt = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>startedAt</c> is drawn from — <c>AnyValidStartedAt()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTimeOffset, AnyDateTimeOffset&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMilkingSession WithStartedAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMilkingSession copy = new AnyMilkingSession(this);

        copy._startedAtRecipe = configure(_startedAtRecipe);
        copy._startedAt       = copy._startedAtRecipe;

        return copy;
    }

    /// <summary>Pins <c>settings</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMilkingSession WithSettings(MilkingSettings value) {
        AnyMilkingSession copy = new AnyMilkingSession(this);

        copy._settings = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>settings</c> is drawn from — <c>AnyValidSettings()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMilkingSettings, AnyMilkingSettings&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMilkingSession WithSettings(Func<AnyMilkingSettings, AnyMilkingSettings> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMilkingSession copy = new AnyMilkingSession(this);

        copy._settingsRecipe = configure(_settingsRecipe);
        copy._settings       = copy._settingsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="MilkingSession" />.</summary>
    public MilkingSession Generate() {
        // The tie: a cow last milked at L may only start a session at L + MinimumInterval or later (not before L).
        // The start is derived from the cow and the settings; the drawn startedAt serves a cow never milked.
        Animal           animal   = _animal.Generate();
        MilkingSettings  settings = _settings.Generate();
        DateTimeOffset   startedAt = animal.LastMilkedAt is { } last
            ? last + settings.MinimumInterval + Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromDays(2)).Generate()
            : _startedAt.Generate();

        return new MilkingSession(animal,
                                  _boxNumber.Generate(),
                                  startedAt,
                                  settings);
    }

}
