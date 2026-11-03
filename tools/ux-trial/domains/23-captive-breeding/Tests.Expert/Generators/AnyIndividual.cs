// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Individual --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using CaptiveBreeding.Domain;
using JustDummies;

namespace CaptiveBreeding.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Individual" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyIndividual : IAny<Individual> {

    private IAny<StudbookId>  _id;
    private IAny<string>      _name;
    private IAny<Sex>         _sex;
    private IAny<DateOnly>    _birthDate;
    private IAny<StudbookId?> _sireId;
    private IAny<StudbookId?> _damId;

    private AnyStudbookId _idRecipe;
    private AnyString     _nameRecipe;
    private AnyEnum<Sex>  _sexRecipe;
    private AnyDateOnly   _birthDateRecipe;
    private AnyStudbookId _sireIdRecipe;
    private AnyStudbookId _damIdRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyIndividual()
        : this(id:        AnyValidId(),
               name:      AnyValidName(),
               sex:       AnyValidSex(),
               birthDate: AnyValidBirthDate(),
               sireId:    AnyValidSireId(),
               damId:     AnyValidDamId()) { }

    private static AnyStudbookId AnyValidId() {
        return new AnyStudbookId();
    }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank().WithMaxLength(40);
    }

    private static AnyEnum<Sex> AnyValidSex() {
        return Any.Enum<Sex>();
    }

    private static AnyDateOnly AnyValidBirthDate() {
        return Any.DateOnly();
    }

    private static AnyStudbookId AnyValidSireId() {
        return new AnyStudbookId();
    }

    private static AnyStudbookId AnyValidDamId() {
        return new AnyStudbookId();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyIndividual(AnyStudbookId id,
                          AnyString     name,
                          AnyEnum<Sex>  sex,
                          AnyDateOnly   birthDate,
                          AnyStudbookId sireId,
                          AnyStudbookId damId) {
        _id              = id;
        _idRecipe        = id;
        _name            = name;
        _nameRecipe      = name;
        _sex             = sex;
        _sexRecipe       = sex;
        _birthDate       = birthDate;
        _birthDateRecipe = birthDate;
        _sireId          = Any.Fixed<StudbookId?>(null);   // a founder by default: parentage is pinned as a pair
        _sireIdRecipe    = sireId;
        _damId           = Any.Fixed<StudbookId?>(null);
        _damIdRecipe     = damId;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyIndividual(AnyIndividual from) {
        _id              = from._id;
        _idRecipe        = from._idRecipe;
        _name            = from._name;
        _nameRecipe      = from._nameRecipe;
        _sex             = from._sex;
        _sexRecipe       = from._sexRecipe;
        _birthDate       = from._birthDate;
        _birthDateRecipe = from._birthDateRecipe;
        _sireId          = from._sireId;
        _sireIdRecipe    = from._sireIdRecipe;
        _damId           = from._damId;
        _damIdRecipe     = from._damIdRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyIndividual WithId(StudbookId value) {
        AnyIndividual copy = new AnyIndividual(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyStudbookId, AnyStudbookId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyIndividual WithId(Func<AnyStudbookId, AnyStudbookId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyIndividual copy = new AnyIndividual(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyIndividual WithName(string value) {
        AnyIndividual copy = new AnyIndividual(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>name</c> is drawn from — <c>AnyValidName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyIndividual WithName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyIndividual copy = new AnyIndividual(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Pins <c>sex</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyIndividual WithSex(Sex value) {
        AnyIndividual copy = new AnyIndividual(this);

        copy._sex = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>sex</c> is drawn from — <c>AnyValidSex()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;Sex&gt;, AnyEnum&lt;Sex&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyIndividual WithSex(Func<AnyEnum<Sex>, AnyEnum<Sex>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyIndividual copy = new AnyIndividual(this);

        copy._sexRecipe = configure(_sexRecipe);
        copy._sex       = copy._sexRecipe;

        return copy;
    }

    /// <summary>Pins <c>birthDate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyIndividual WithBirthDate(DateOnly value) {
        AnyIndividual copy = new AnyIndividual(this);

        copy._birthDate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>birthDate</c> is drawn from — <c>AnyValidBirthDate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyIndividual WithBirthDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyIndividual copy = new AnyIndividual(this);

        copy._birthDateRecipe = configure(_birthDateRecipe);
        copy._birthDate       = copy._birthDateRecipe;

        return copy;
    }

    /// <summary>Pins <c>sireId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyIndividual WithSireId(StudbookId? value) {
        AnyIndividual copy = new AnyIndividual(this);

        copy._sireId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>sireId</c> is drawn from — <c>AnyValidSireId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyStudbookId, AnyStudbookId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyIndividual WithSireId(Func<AnyStudbookId, AnyStudbookId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyIndividual copy = new AnyIndividual(this);

        copy._sireIdRecipe = configure(_sireIdRecipe);
        copy._sireId       = copy._sireIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>sireId</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyIndividual WithoutSireId() {
        AnyIndividual copy = new AnyIndividual(this);

        copy._sireId = Any.Fixed<StudbookId?>(null);

        return copy;
    }

    /// <summary>Pins <c>damId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyIndividual WithDamId(StudbookId? value) {
        AnyIndividual copy = new AnyIndividual(this);

        copy._damId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>damId</c> is drawn from — <c>AnyValidDamId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyStudbookId, AnyStudbookId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyIndividual WithDamId(Func<AnyStudbookId, AnyStudbookId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyIndividual copy = new AnyIndividual(this);

        copy._damIdRecipe = configure(_damIdRecipe);
        copy._damId       = copy._damIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>damId</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyIndividual WithoutDamId() {
        AnyIndividual copy = new AnyIndividual(this);

        copy._damId = Any.Fixed<StudbookId?>(null);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Individual" />.</summary>
    public Individual Generate() {
        return new Individual(_id.Generate(),
                              _name.Generate(),
                              _sex.Generate(),
                              _birthDate.Generate(),
                              _sireId.Generate(),
                              _damId.Generate());
    }

}
