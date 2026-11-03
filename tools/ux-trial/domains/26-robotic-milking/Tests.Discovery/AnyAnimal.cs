// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Animal --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace RoboticMilking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Animal" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAnimal : IAny<Animal> {

    private IAny<AnimalId>                  _id;
    private IAny<int>                       _lactationNumber;
    private IAny<int>                       _daysInMilk;
    private IAny<IEnumerable<UdderQuarter>> _blindQuarters;
    private IAny<DateTimeOffset?>           _lastMilkedAt;

    private AnyAnimalId               _idRecipe;
    private AnyInt32                  _lactationNumberRecipe;
    private AnyInt32                  _daysInMilkRecipe;
    private AnySequence<UdderQuarter> _blindQuartersRecipe;
    private AnyDateTimeOffset         _lastMilkedAtRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAnimal()
        : this(id:              AnyValidId(),
               lactationNumber: AnyValidLactationNumber(),
               daysInMilk:      AnyValidDaysInMilk(),
               blindQuarters:   AnyValidBlindQuarters(),
               lastMilkedAt:    AnyValidLastMilkedAt()) { }

    private static AnyAnimalId AnyValidId() {
        return new AnyAnimalId();
    }

    private static AnyInt32 AnyValidLactationNumber() {
        return Any.Int32().Positive().LessThanOrEqualTo(Animal.MaxLactationNumber);
    }

    private static AnyInt32 AnyValidDaysInMilk() {
        return Any.Int32().Between(0, Animal.MaxDaysInMilk);
    }

    private static AnySequence<UdderQuarter> AnyValidBlindQuarters() {
        // A cow keeps at least one milkable quarter: at most three distinct blind ones.
        return Any.SequenceOf(Any.Enum<UdderQuarter>()).WithMaxCount(3).Distinct();
    }

    private static AnyDateTimeOffset AnyValidLastMilkedAt() {
        return Any.DateTimeOffset();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyAnimal(AnyAnimalId               id,
                      AnyInt32                  lactationNumber,
                      AnyInt32                  daysInMilk,
                      AnySequence<UdderQuarter> blindQuarters,
                      AnyDateTimeOffset         lastMilkedAt) {
        _id                    = id;
        _idRecipe              = id;
        _lactationNumber       = lactationNumber;
        _lactationNumberRecipe = lactationNumber;
        _daysInMilk            = daysInMilk;
        _daysInMilkRecipe      = daysInMilk;
        _blindQuarters         = blindQuarters;
        _blindQuartersRecipe   = blindQuarters;
        _lastMilkedAt          = lastMilkedAt.OrNull();
        _lastMilkedAtRecipe    = lastMilkedAt;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyAnimal(AnyAnimal from) {
        _id                    = from._id;
        _idRecipe              = from._idRecipe;
        _lactationNumber       = from._lactationNumber;
        _lactationNumberRecipe = from._lactationNumberRecipe;
        _daysInMilk            = from._daysInMilk;
        _daysInMilkRecipe      = from._daysInMilkRecipe;
        _blindQuarters         = from._blindQuarters;
        _blindQuartersRecipe   = from._blindQuartersRecipe;
        _lastMilkedAt          = from._lastMilkedAt;
        _lastMilkedAtRecipe    = from._lastMilkedAtRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAnimal WithId(AnimalId value) {
        AnyAnimal copy = new AnyAnimal(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyAnimalId, AnyAnimalId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAnimal WithId(Func<AnyAnimalId, AnyAnimalId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAnimal copy = new AnyAnimal(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>lactationNumber</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAnimal WithLactationNumber(int value) {
        AnyAnimal copy = new AnyAnimal(this);

        copy._lactationNumber = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>lactationNumber</c> is drawn from — <c>AnyValidLactationNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAnimal WithLactationNumber(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAnimal copy = new AnyAnimal(this);

        copy._lactationNumberRecipe = configure(_lactationNumberRecipe);
        copy._lactationNumber       = copy._lactationNumberRecipe;

        return copy;
    }

    /// <summary>Pins <c>daysInMilk</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAnimal WithDaysInMilk(int value) {
        AnyAnimal copy = new AnyAnimal(this);

        copy._daysInMilk = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>daysInMilk</c> is drawn from — <c>AnyValidDaysInMilk()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAnimal WithDaysInMilk(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAnimal copy = new AnyAnimal(this);

        copy._daysInMilkRecipe = configure(_daysInMilkRecipe);
        copy._daysInMilk       = copy._daysInMilkRecipe;

        return copy;
    }

    /// <summary>Pins <c>blindQuarters</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAnimal WithBlindQuarters(IEnumerable<UdderQuarter> value) {
        AnyAnimal copy = new AnyAnimal(this);

        copy._blindQuarters = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>blindQuarters</c> is drawn from — <c>AnyValidBlindQuarters()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySequence&lt;UdderQuarter&gt;, AnySequence&lt;UdderQuarter&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAnimal WithBlindQuarters(Func<AnySequence<UdderQuarter>, AnySequence<UdderQuarter>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAnimal copy = new AnyAnimal(this);

        copy._blindQuartersRecipe = configure(_blindQuartersRecipe);
        copy._blindQuarters       = copy._blindQuartersRecipe;

        return copy;
    }

    /// <summary>Pins <c>lastMilkedAt</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAnimal WithLastMilkedAt(DateTimeOffset? value) {
        AnyAnimal copy = new AnyAnimal(this);

        copy._lastMilkedAt = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>lastMilkedAt</c> is drawn from — <c>AnyValidLastMilkedAt()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTimeOffset, AnyDateTimeOffset&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAnimal WithLastMilkedAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAnimal copy = new AnyAnimal(this);

        copy._lastMilkedAtRecipe = configure(_lastMilkedAtRecipe);
        copy._lastMilkedAt       = copy._lastMilkedAtRecipe.AsNullable();

        return copy;
    }

    /// <summary>Pins <c>lastMilkedAt</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyAnimal WithoutLastMilkedAt() {
        AnyAnimal copy = new AnyAnimal(this);

        copy._lastMilkedAt = Any.Fixed<DateTimeOffset?>(null);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Animal" />.</summary>
    public Animal Generate() {
        return new Animal(_id.Generate(),
                          _lactationNumber.Generate(),
                          _daysInMilk.Generate(),
                          _blindQuarters.Generate(),
                          _lastMilkedAt.Generate());
    }

}
