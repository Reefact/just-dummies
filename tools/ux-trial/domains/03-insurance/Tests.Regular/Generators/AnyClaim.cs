// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Claim --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Insurance.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Claim" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyClaim : IAny<Claim> {

    private IAny<Guid>           _id;
    private IAny<DateOnly>       _occurredOn;
    private IAny<decimal>        _damage;
    private IAny<Responsibility> _responsibility;

    private AnyGuid                 _idRecipe;
    private AnyDateOnly             _occurredOnRecipe;
    private AnyDecimal              _damageRecipe;
    private AnyEnum<Responsibility> _responsibilityRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyClaim()
        : this(id:             AnyValidId(),
               occurredOn:     AnyValidOccurredOn(),
               damage:         AnyValidDamage(),
               responsibility: AnyValidResponsibility()) { }

    private static AnyGuid AnyValidId() {
        return Any.Guid().NonEmpty();
    }

    private static AnyDateOnly AnyValidOccurredOn() {
        return Any.DateOnly();
    }

    private static AnyDecimal AnyValidDamage() {
        return Any.Decimal().Positive();
    }

    private static AnyEnum<Responsibility> AnyValidResponsibility() {
        return Any.Enum<Responsibility>();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyClaim(AnyGuid                 id,
                     AnyDateOnly             occurredOn,
                     AnyDecimal              damage,
                     AnyEnum<Responsibility> responsibility) {
        _id                   = id;
        _idRecipe             = id;
        _occurredOn           = occurredOn;
        _occurredOnRecipe     = occurredOn;
        _damage               = damage;
        _damageRecipe         = damage;
        _responsibility       = responsibility;
        _responsibilityRecipe = responsibility;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyClaim(AnyClaim from) {
        _id                   = from._id;
        _idRecipe             = from._idRecipe;
        _occurredOn           = from._occurredOn;
        _occurredOnRecipe     = from._occurredOnRecipe;
        _damage               = from._damage;
        _damageRecipe         = from._damageRecipe;
        _responsibility       = from._responsibility;
        _responsibilityRecipe = from._responsibilityRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyClaim WithId(Guid value) {
        AnyClaim copy = new AnyClaim(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGuid, AnyGuid&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyClaim WithId(Func<AnyGuid, AnyGuid> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyClaim copy = new AnyClaim(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>occurredOn</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyClaim WithOccurredOn(DateOnly value) {
        AnyClaim copy = new AnyClaim(this);

        copy._occurredOn = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>occurredOn</c> is drawn from — <c>AnyValidOccurredOn()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyClaim WithOccurredOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyClaim copy = new AnyClaim(this);

        copy._occurredOnRecipe = configure(_occurredOnRecipe);
        copy._occurredOn       = copy._occurredOnRecipe;

        return copy;
    }

    /// <summary>Pins <c>damage</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyClaim WithDamage(decimal value) {
        AnyClaim copy = new AnyClaim(this);

        copy._damage = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>damage</c> is drawn from — <c>AnyValidDamage()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyClaim WithDamage(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyClaim copy = new AnyClaim(this);

        copy._damageRecipe = configure(_damageRecipe);
        copy._damage       = copy._damageRecipe;

        return copy;
    }

    /// <summary>Pins <c>responsibility</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyClaim WithResponsibility(Responsibility value) {
        AnyClaim copy = new AnyClaim(this);

        copy._responsibility = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>responsibility</c> is drawn from — <c>AnyValidResponsibility()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;Responsibility&gt;, AnyEnum&lt;Responsibility&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyClaim WithResponsibility(Func<AnyEnum<Responsibility>, AnyEnum<Responsibility>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyClaim copy = new AnyClaim(this);

        copy._responsibilityRecipe = configure(_responsibilityRecipe);
        copy._responsibility       = copy._responsibilityRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Claim" />.</summary>
    public Claim Generate() {
        return new Claim(_id.Generate(),
                         _occurredOn.Generate(),
                         _damage.Generate(),
                         _responsibility.Generate());
    }

}
